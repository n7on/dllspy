using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DllSpy.Core.Contracts;
using DllSpy.Core.Helpers;

namespace DllSpy.Core.Services
{
    /// <summary>
    /// Discovers gRPC service methods by scanning for types that inherit from a generated gRPC base class.
    /// </summary>
    internal class GrpcDiscovery : IDiscovery
    {
        private readonly SecurityResolver _security;

        /// <summary>
        /// Initializes a new instance of <see cref="GrpcDiscovery"/>.
        /// </summary>
        public GrpcDiscovery(AttributeAnalyzer attributeAnalyzer)
        {
            if (attributeAnalyzer == null) throw new ArgumentNullException(nameof(attributeAnalyzer));
            _security = new SecurityResolver(attributeAnalyzer);
        }

        /// <inheritdoc />
        public SurfaceType SurfaceType => SurfaceType.GrpcOperation;

        /// <inheritdoc />
        public List<InputSurface> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();

            foreach (var type in ReflectionHelper.GetTypesSafe(assembly))
            {
                if (IsGrpcService(type))
                {
                    surfaces.AddRange(DiscoverServiceMethods(type));
                }
            }

            return surfaces;
        }

        private List<GrpcOperation> DiscoverServiceMethods(Type serviceType)
        {
            var methods = new List<GrpcOperation>();
            var serviceName = GetServiceName(serviceType);
            var classSec = _security.ReadClass(serviceType);

            var declaredMethods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in declaredMethods)
            {
                if (method.IsSpecialName) continue;
                if (!HasServerCallContextParameter(method)) continue;

                var methodType = DetermineMethodType(method);
                var merged = _security.Merge(classSec, method);
                var parameters = GetUserParameters(method);
                var returnType = ReflectionHelper.GetFriendlyTypeName(method.ReturnType);
                var isAsync = ReflectionHelper.IsAsyncMethod(method);

                methods.Add(new GrpcOperation
                {
                    ServiceName = serviceName,
                    MethodType = methodType,
                    ClassName = serviceType.Name,
                    MethodName = method.Name,
                    RequiresAuthorization = merged.RequiresAuthorization,
                    AllowAnonymous = merged.AllowAnonymous,
                    Roles = merged.Roles,
                    Policies = merged.Policies,
                    Parameters = parameters,
                    ReturnType = returnType,
                    IsAsync = isAsync,
                    SecurityAttributes = merged.SecurityAttributes
                });
            }

            return methods;
        }

        /// <summary>
        /// Detects whether a type is a gRPC service implementation.
        /// A gRPC service inherits from a generated base class whose parent declares a static BindService method.
        /// </summary>
        private static bool IsGrpcService(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract || !type.IsPublic)
                return false;

            var baseType = type.BaseType;
            if (baseType == null || baseType == typeof(object))
                return false;

            // The generated base class (e.g. Greeter.GreeterBase) is abstract
            // and its declaring/parent type has a static BindService method.
            // We detect this by checking if the base is abstract and has a
            // parent type with BindService, OR simply if the base class name
            // ends with "Base" and is nested inside a type with BindService.
            return IsGeneratedGrpcBase(baseType);
        }

        private static bool IsGeneratedGrpcBase(Type baseType)
        {
            if (!baseType.IsAbstract) return false;

            // Pattern 1: Nested type — base is e.g. Greeter.GreeterBase,
            // and Greeter (declaring type) has static BindService method.
            var declaringType = baseType.DeclaringType;
            if (declaringType != null)
            {
                if (HasBindServiceMethod(declaringType))
                    return true;
            }

            // Pattern 2: The base type itself has BindService (some generators).
            if (HasBindServiceMethod(baseType))
                return true;

            // Pattern 3: Non-nested base with "Base" suffix whose name matches
            // the gRPC convention (e.g. GreeterServiceBase).
            if (baseType.Name.EndsWith("Base", StringComparison.Ordinal))
            {
                // Check if the base type has virtual methods with ServerCallContext parameters
                var methods = baseType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                return methods.Any(HasServerCallContextParameter);
            }

            return false;
        }

        private static bool HasBindServiceMethod(Type type)
        {
            try
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                return methods.Any(m => m.Name == "BindService");
            }
            catch
            {
                return false;
            }
        }

        private static bool HasServerCallContextParameter(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return parameters.Any(p => p.ParameterType.Name == "ServerCallContext");
        }

        private static string GetServiceName(Type serviceType)
        {
            var baseType = serviceType.BaseType;
            if (baseType != null)
            {
                // If nested (Greeter.GreeterBase), use the declaring type name
                if (baseType.DeclaringType != null)
                    return baseType.DeclaringType.Name;

                // If base ends with "Base", strip it
                if (baseType.Name.EndsWith("Base", StringComparison.Ordinal) && baseType.Name.Length > 4)
                    return baseType.Name.Substring(0, baseType.Name.Length - 4);
            }

            return serviceType.Name;
        }

        /// <summary>
        /// Determines the gRPC method type from its parameter signature.
        /// </summary>
        private static GrpcMethodType DetermineMethodType(MethodInfo method)
        {
            var parameters = method.GetParameters();
            bool hasStreamReader = parameters.Any(p => IsAsyncStreamReader(p.ParameterType));
            bool hasStreamWriter = parameters.Any(p => IsServerStreamWriter(p.ParameterType));

            if (hasStreamReader && hasStreamWriter) return GrpcMethodType.BidiStreaming;
            if (hasStreamWriter) return GrpcMethodType.ServerStreaming;
            if (hasStreamReader) return GrpcMethodType.ClientStreaming;
            return GrpcMethodType.Unary;
        }

        private static bool IsAsyncStreamReader(Type type)
        {
            return type.Name.StartsWith("IAsyncStreamReader`", StringComparison.Ordinal);
        }

        private static bool IsServerStreamWriter(Type type)
        {
            return type.Name.StartsWith("IServerStreamWriter`", StringComparison.Ordinal);
        }

        /// <summary>
        /// Extracts user-meaningful parameters, excluding ServerCallContext and stream reader/writer params.
        /// </summary>
        private static List<EndpointParameterInfo> GetUserParameters(MethodInfo method)
        {
            var parameters = new List<EndpointParameterInfo>();

            foreach (var param in method.GetParameters())
            {
                var typeName = param.ParameterType.Name;
                if (typeName == "ServerCallContext") continue;
                if (typeName.StartsWith("IServerStreamWriter`", StringComparison.Ordinal)) continue;
                if (typeName.StartsWith("IAsyncStreamReader`", StringComparison.Ordinal)) continue;

                parameters.Add(new EndpointParameterInfo
                {
                    Name = param.Name,
                    Type = ReflectionHelper.GetFriendlyTypeName(param.ParameterType),
                    IsRequired = !param.HasDefaultValue && !ReflectionHelper.IsNullableType(param.ParameterType),
                    Source = ParameterSource.Body
                });
            }

            return parameters;
        }
    }
}
