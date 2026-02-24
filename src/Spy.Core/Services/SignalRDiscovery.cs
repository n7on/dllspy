using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Spy.Core.Contracts;
using Spy.Core.Helpers;

namespace Spy.Core.Services
{
    /// <summary>
    /// Discovers SignalR hub methods by scanning for types that inherit from Hub or Hub&lt;T&gt;.
    /// </summary>
    internal class SignalRDiscovery : IDiscovery
    {
        private readonly SecurityResolver _security;

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
            if (attributeAnalyzer == null) throw new ArgumentNullException(nameof(attributeAnalyzer));
            _security = new SecurityResolver(attributeAnalyzer);
        }

        /// <inheritdoc />
        public SurfaceType SurfaceType => SurfaceType.SignalRMethod;

        /// <inheritdoc />
        public List<InputSurface> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();

            foreach (var type in ReflectionHelper.GetTypesSafe(assembly))
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
            var classSec = _security.ReadClass(hubType);

            var declaredMethods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in declaredMethods)
            {
                if (method.IsSpecialName) continue;
                if (LifecycleMethods.Contains(method.Name)) continue;

                var merged = _security.Merge(classSec, method);
                var parameters = ReflectionHelper.GetParameters(method);
                var returnType = ReflectionHelper.GetFriendlyTypeName(method.ReturnType);
                var isAsync = ReflectionHelper.IsAsyncMethod(method);
                var isStreamingResult = IsStreamingReturnType(method.ReturnType);
                var acceptsStreaming = method.GetParameters().Any(p => IsStreamingType(p.ParameterType));

                methods.Add(new SignalRMethod
                {
                    HubName = hubName,
                    HubRoute = hubRoute,
                    ClassName = hubName,
                    MethodName = method.Name,
                    RequiresAuthorization = merged.RequiresAuthorization,
                    AllowAnonymous = merged.AllowAnonymous,
                    Roles = merged.Roles,
                    Policies = merged.Policies,
                    Parameters = parameters,
                    ReturnType = returnType,
                    IsAsync = isAsync,
                    SecurityAttributes = merged.SecurityAttributes,
                    IsStreamingResult = isStreamingResult,
                    AcceptsStreaming = acceptsStreaming
                });
            }

            return methods;
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
            if (hubName.EndsWith("Hub", StringComparison.Ordinal) && hubName.Length > 3)
            {
                var name = hubName.Substring(0, hubName.Length - 3);
                return char.ToLowerInvariant(name[0]) + name.Substring(1);
            }
            return char.ToLowerInvariant(hubName[0]) + hubName.Substring(1);
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
    }
}
