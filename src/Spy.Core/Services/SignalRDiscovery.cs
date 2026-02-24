using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spy.Core.Contracts;

namespace Spy.Core.Services
{
    /// <summary>
    /// Discovers SignalR hub methods by scanning for types that inherit from Hub or Hub&lt;T&gt;.
    /// </summary>
    public class SignalRDiscovery : IDiscovery
    {
        private readonly AttributeAnalyzer _attributeAnalyzer;

        private static readonly HashSet<string> LifecycleMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "OnConnectedAsync",
            "OnDisconnectedAsync",
            "Dispose"
        };

        /// <summary>
        /// Initializes a new instance of <see cref="SignalRDiscovery"/>.
        /// </summary>
        public SignalRDiscovery(AttributeAnalyzer attributeAnalyzer)
        {
            _attributeAnalyzer = attributeAnalyzer ?? throw new ArgumentNullException(nameof(attributeAnalyzer));
        }

        /// <inheritdoc />
        public SurfaceType SurfaceType => SurfaceType.SignalRMethod;

        /// <inheritdoc />
        public List<InputSurface> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            foreach (var type in types)
            {
                if (IsHub(type))
                {
                    surfaces.AddRange(DiscoverHubMethods(type));
                }
            }

            return surfaces;
        }

        private List<SignalRMethod> DiscoverHubMethods(Type hubType)
        {
            var methods = new List<SignalRMethod>();
            var hubName = hubType.Name;
            var hubRoute = GetConventionalRoute(hubName);

            var hubHasAuth = _attributeAnalyzer.HasAuthorizeAttribute(hubType);
            var hubAllowAnon = _attributeAnalyzer.HasAllowAnonymousAttribute(hubType);
            var hubRoles = _attributeAnalyzer.GetRoles(hubType);
            var hubPolicies = _attributeAnalyzer.GetPolicies(hubType);
            var hubSecurityAttrs = _attributeAnalyzer.GetSecurityAttributes(hubType);

            var declaredMethods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in declaredMethods)
            {
                if (method.IsSpecialName) continue;
                if (LifecycleMethods.Contains(method.Name)) continue;

                var actionHasAuth = _attributeAnalyzer.HasAuthorizeAttribute(method);
                var actionAllowAnon = _attributeAnalyzer.HasAllowAnonymousAttribute(method);
                var actionRoles = _attributeAnalyzer.GetRoles(method);
                var actionPolicies = _attributeAnalyzer.GetPolicies(method);
                var actionSecurityAttrs = _attributeAnalyzer.GetSecurityAttributes(method);

                var requiresAuth = actionHasAuth || (hubHasAuth && !actionAllowAnon);
                var allowAnon = actionAllowAnon || (hubAllowAnon && !actionHasAuth);

                var allRoles = new List<string>(hubRoles);
                allRoles.AddRange(actionRoles);

                var allPolicies = new List<string>(hubPolicies);
                allPolicies.AddRange(actionPolicies);

                var allSecurityAttrs = new List<SecurityAttribute>(hubSecurityAttrs);
                allSecurityAttrs.AddRange(actionSecurityAttrs);

                var parameters = GetParameters(method);
                var returnType = HttpEndpointDiscovery.GetFriendlyTypeName(method.ReturnType);
                var isAsync = IsAsyncMethod(method);
                var isStreamingResult = IsStreamingReturnType(method.ReturnType);
                var acceptsStreaming = method.GetParameters().Any(p => IsStreamingType(p.ParameterType));

                methods.Add(new SignalRMethod
                {
                    HubName = hubName,
                    HubRoute = hubRoute,
                    ClassName = hubName,
                    MethodName = method.Name,
                    RequiresAuthorization = requiresAuth,
                    AllowAnonymous = allowAnon,
                    Roles = allRoles.Distinct().ToList(),
                    Policies = allPolicies.Distinct().ToList(),
                    Parameters = parameters,
                    ReturnType = returnType,
                    IsAsync = isAsync,
                    SecurityAttributes = allSecurityAttrs,
                    IsStreamingResult = isStreamingResult,
                    AcceptsStreaming = acceptsStreaming
                });
            }

            return methods;
        }

        private List<EndpointParameterInfo> GetParameters(MethodInfo method)
        {
            var parameters = new List<EndpointParameterInfo>();

            foreach (var param in method.GetParameters())
            {
                var isRequired = !param.HasDefaultValue && !IsNullableType(param.ParameterType);

                parameters.Add(new EndpointParameterInfo
                {
                    Name = param.Name,
                    Type = HttpEndpointDiscovery.GetFriendlyTypeName(param.ParameterType),
                    IsRequired = isRequired,
                    Source = ParameterSource.Unknown
                });
            }

            return parameters;
        }

        private static bool IsHub(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract || !type.IsPublic)
                return false;

            var current = type.BaseType;
            while (current != null)
            {
                var name = current.Name;
                if (name == "Hub" || name.StartsWith("Hub`", StringComparison.Ordinal))
                {
                    return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        private static string GetConventionalRoute(string hubName)
        {
            // Strip "Hub" suffix for conventional route name
            if (hubName.EndsWith("Hub", StringComparison.Ordinal) && hubName.Length > 3)
            {
                var name = hubName.Substring(0, hubName.Length - 3);
                // Lowercase first character
                return char.ToLowerInvariant(name[0]) + name.Substring(1);
            }
            return char.ToLowerInvariant(hubName[0]) + hubName.Substring(1);
        }

        private static bool IsAsyncMethod(MethodInfo method)
        {
            var typeName = method.ReturnType.Name;
            if (typeName == "Task" || typeName.StartsWith("Task`", StringComparison.Ordinal))
                return true;
            if (typeName == "ValueTask" || typeName.StartsWith("ValueTask`", StringComparison.Ordinal))
                return true;
            return false;
        }

        private static bool IsStreamingReturnType(Type returnType)
        {
            return IsStreamingType(returnType) || IsWrappedStreamingType(returnType);
        }

        private static bool IsStreamingType(Type type)
        {
            var name = type.Name;
            if (name.StartsWith("IAsyncEnumerable`", StringComparison.Ordinal)) return true;
            if (name.StartsWith("ChannelReader`", StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool IsWrappedStreamingType(Type type)
        {
            // Check Task<IAsyncEnumerable<T>> or Task<ChannelReader<T>>
            if (type.IsGenericType)
            {
                var name = type.Name;
                if (name.StartsWith("Task`", StringComparison.Ordinal) || name.StartsWith("ValueTask`", StringComparison.Ordinal))
                {
                    var inner = type.GetGenericArguments()[0];
                    return IsStreamingType(inner);
                }
            }
            return false;
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                || !type.IsValueType;
        }
    }
}
