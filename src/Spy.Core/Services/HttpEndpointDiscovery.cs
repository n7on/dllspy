using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Spy.Core.Contracts;

namespace Spy.Core.Services
{
    /// <summary>
    /// Discovers HTTP endpoints by scanning for ASP.NET Core / Web API controllers.
    /// </summary>
    public class HttpEndpointDiscovery : IDiscovery
    {
        private readonly AttributeAnalyzer _attributeAnalyzer;

        /// <summary>
        /// Initializes a new instance of <see cref="HttpEndpointDiscovery"/>.
        /// </summary>
        public HttpEndpointDiscovery(AttributeAnalyzer attributeAnalyzer)
        {
            _attributeAnalyzer = attributeAnalyzer ?? throw new ArgumentNullException(nameof(attributeAnalyzer));
        }

        /// <inheritdoc />
        public SurfaceType SurfaceType => SurfaceType.HttpEndpoint;

        /// <inheritdoc />
        public List<InputSurface> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();
            var controllerTypes = GetControllerTypes(assembly);

            foreach (var controllerType in controllerTypes)
            {
                surfaces.AddRange(DiscoverControllerEndpoints(controllerType));
            }

            return surfaces;
        }

        private List<Type> GetControllerTypes(Assembly assembly)
        {
            var controllers = new List<Type>();

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
                if (IsController(type))
                {
                    controllers.Add(type);
                }
            }

            return controllers;
        }

        private List<HttpEndpoint> DiscoverControllerEndpoints(Type controllerType)
        {
            var endpoints = new List<HttpEndpoint>();
            var controllerName = GetControllerName(controllerType);
            var controllerRoute = _attributeAnalyzer.GetRouteTemplate(controllerType);
            var controllerHasAuth = _attributeAnalyzer.HasAuthorizeAttribute(controllerType);
            var controllerAllowAnon = _attributeAnalyzer.HasAllowAnonymousAttribute(controllerType);
            var controllerRoles = _attributeAnalyzer.GetRoles(controllerType);
            var controllerPolicies = _attributeAnalyzer.GetPolicies(controllerType);
            var controllerSecurityAttrs = _attributeAnalyzer.GetSecurityAttributes(controllerType);
            var area = _attributeAnalyzer.GetArea(controllerType);

            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                if (method.IsSpecialName) continue;

                var httpMethod = _attributeAnalyzer.GetHttpMethod(method);
                var actionRoute = _attributeAnalyzer.GetRouteTemplate(method);
                var actionHasAuth = _attributeAnalyzer.HasAuthorizeAttribute(method);
                var actionAllowAnon = _attributeAnalyzer.HasAllowAnonymousAttribute(method);
                var actionRoles = _attributeAnalyzer.GetRoles(method);
                var actionPolicies = _attributeAnalyzer.GetPolicies(method);
                var actionSecurityAttrs = _attributeAnalyzer.GetSecurityAttributes(method);

                var httpMethods = new List<string>();
                if (httpMethod != null)
                {
                    httpMethods.Add(httpMethod);
                }
                else if (HasApiControllerAttribute(controllerType) || actionRoute != null || controllerRoute != null)
                {
                    httpMethods.Add(InferHttpMethod(method.Name));
                }
                else
                {
                    httpMethods.Add("GET");
                }

                foreach (var verb in httpMethods)
                {
                    var combinedRoute = BuildRoute(controllerRoute, actionRoute, controllerName, method.Name, area);
                    var requiresAuth = actionHasAuth || (controllerHasAuth && !actionAllowAnon);
                    var allowAnon = actionAllowAnon || (controllerAllowAnon && !actionHasAuth);

                    var allRoles = new List<string>(controllerRoles);
                    allRoles.AddRange(actionRoles);

                    var allPolicies = new List<string>(controllerPolicies);
                    allPolicies.AddRange(actionPolicies);

                    var allSecurityAttrs = new List<SecurityAttribute>(controllerSecurityAttrs);
                    allSecurityAttrs.AddRange(actionSecurityAttrs);

                    var parameters = GetParameters(method);
                    var returnType = GetFriendlyReturnType(method);
                    var isAsync = IsAsyncMethod(method);

                    endpoints.Add(new HttpEndpoint
                    {
                        Route = combinedRoute,
                        HttpMethod = verb,
                        ClassName = controllerName,
                        MethodName = method.Name,
                        RequiresAuthorization = requiresAuth,
                        AllowAnonymous = allowAnon,
                        Roles = allRoles.Distinct().ToList(),
                        Policies = allPolicies.Distinct().ToList(),
                        Parameters = parameters,
                        ReturnType = returnType,
                        IsAsync = isAsync,
                        SecurityAttributes = allSecurityAttrs,
                        RouteDetails = new RouteInfo
                        {
                            ControllerRoute = controllerRoute,
                            ActionRoute = actionRoute,
                            CombinedRoute = combinedRoute,
                            Area = area
                        }
                    });
                }
            }

            return endpoints;
        }

        private List<EndpointParameterInfo> GetParameters(MethodInfo method)
        {
            var parameters = new List<EndpointParameterInfo>();

            foreach (var param in method.GetParameters())
            {
                var source = _attributeAnalyzer.GetParameterSource(param);
                var isRequired = !param.HasDefaultValue && !IsNullableType(param.ParameterType);

                parameters.Add(new EndpointParameterInfo
                {
                    Name = param.Name,
                    Type = GetFriendlyTypeName(param.ParameterType),
                    IsRequired = isRequired,
                    Source = source
                });
            }

            return parameters;
        }

        private static bool IsController(Type type)
        {
            if (type == null || !type.IsClass || type.IsAbstract || !type.IsPublic)
                return false;

            if (InheritsFromController(type))
                return true;

            if (HasApiControllerAttribute(type))
                return true;

            if (type.Name.EndsWith("Controller", StringComparison.Ordinal) && HasPublicActionMethods(type))
                return true;

            return false;
        }

        private static bool InheritsFromController(Type type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                var name = current.Name;
                if (name == "Controller" || name == "ControllerBase" ||
                    name == "ApiController" || name == "ODataController")
                {
                    return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        private static bool HasApiControllerAttribute(Type type)
        {
            try
            {
                return Attribute.GetCustomAttributes(type, true)
                    .Any(a => a.GetType().Name == "ApiControllerAttribute");
            }
            catch
            {
                return false;
            }
        }

        private static bool HasPublicActionMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Any(m => !m.IsSpecialName);
        }

        private static string GetControllerName(Type controllerType)
        {
            var name = controllerType.Name;
            if (name.EndsWith("Controller", StringComparison.Ordinal))
            {
                return name.Substring(0, name.Length - "Controller".Length);
            }
            return name;
        }

        private static string BuildRoute(string controllerRoute, string actionRoute, string controllerName, string actionName, string area)
        {
            if (!string.IsNullOrEmpty(controllerRoute) && !string.IsNullOrEmpty(actionRoute))
            {
                var route = CombinePaths(controllerRoute, actionRoute);
                return ResolveRouteTokens(route, controllerName, actionName, area);
            }

            if (!string.IsNullOrEmpty(controllerRoute))
            {
                return ResolveRouteTokens(controllerRoute, controllerName, actionName, area);
            }

            if (!string.IsNullOrEmpty(actionRoute) && actionRoute.StartsWith("/", StringComparison.Ordinal))
            {
                return ResolveRouteTokens(actionRoute, controllerName, actionName, area);
            }

            if (!string.IsNullOrEmpty(actionRoute))
            {
                var route = CombinePaths($"api/{controllerName}", actionRoute);
                return ResolveRouteTokens(route, controllerName, actionName, area);
            }

            var conventionalRoute = area != null
                ? $"{area}/{controllerName}/{actionName}"
                : $"api/{controllerName}/{actionName}";

            return ResolveRouteTokens(conventionalRoute, controllerName, actionName, area);
        }

        private static string CombinePaths(string basePath, string relativePath)
        {
            basePath = basePath.TrimEnd('/');
            relativePath = relativePath.TrimStart('/');
            return $"{basePath}/{relativePath}";
        }

        private static string ResolveRouteTokens(string route, string controllerName, string actionName, string area)
        {
            route = ReplaceIgnoreCase(route, "[controller]", controllerName);
            route = ReplaceIgnoreCase(route, "[action]", actionName);
            if (area != null)
            {
                route = ReplaceIgnoreCase(route, "[area]", area);
            }
            return route.TrimStart('/');
        }

        private static string ReplaceIgnoreCase(string input, string oldValue, string newValue)
        {
            int index = 0;
            while ((index = input.IndexOf(oldValue, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                input = input.Substring(0, index) + newValue + input.Substring(index + oldValue.Length);
                index += newValue.Length;
            }
            return input;
        }

        private static string InferHttpMethod(string methodName)
        {
            if (methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("List", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Find", StringComparison.OrdinalIgnoreCase))
                return "GET";

            if (methodName.StartsWith("Post", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Add", StringComparison.OrdinalIgnoreCase))
                return "POST";

            if (methodName.StartsWith("Put", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase))
                return "PUT";

            if (methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase) ||
                methodName.StartsWith("Remove", StringComparison.OrdinalIgnoreCase))
                return "DELETE";

            if (methodName.StartsWith("Patch", StringComparison.OrdinalIgnoreCase))
                return "PATCH";

            return "GET";
        }

        private static bool IsAsyncMethod(MethodInfo method)
        {
            var returnType = method.ReturnType;
            if (returnType == typeof(Task)) return true;
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)) return true;
            if (returnType.Name == "ValueTask" || (returnType.IsGenericType && returnType.Name.StartsWith("ValueTask")))
                return true;
            return false;
        }

        private static string GetFriendlyReturnType(MethodInfo method)
        {
            return GetFriendlyTypeName(method.ReturnType);
        }

        internal static string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(void)) return "void";
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(float)) return "float";
            if (type == typeof(DateTime)) return "DateTime";
            if (type == typeof(Guid)) return "Guid";
            if (type == typeof(object)) return "object";

            if (type == typeof(Task)) return "Task";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return $"Task<{GetFriendlyTypeName(type.GetGenericArguments()[0])}>";
            }

            if (type.IsGenericType)
            {
                var baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
                var args = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{baseName}<{args}>";
            }

            if (type.IsArray)
            {
                return $"{GetFriendlyTypeName(type.GetElementType())}[]";
            }

            return type.Name;
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                || !type.IsValueType;
        }
    }
}
