using System;
using System.Collections.Generic;
using System.Reflection;
using DllSpy.Core.Contracts;

namespace DllSpy.Core.Services
{
    /// <summary>
    /// Analyzes security-related attributes on controllers and actions using reflection.
    /// </summary>
    internal class AttributeAnalyzer
    {
        private static readonly Dictionary<string, string> HttpMethodAttributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "HttpGetAttribute", "GET" },
            { "HttpPostAttribute", "POST" },
            { "HttpPutAttribute", "PUT" },
            { "HttpDeleteAttribute", "DELETE" },
            { "HttpPatchAttribute", "PATCH" },
            { "HttpHeadAttribute", "HEAD" },
            { "HttpOptionsAttribute", "OPTIONS" }
        };

        private static readonly Dictionary<string, ParameterSource> ParameterSourceAttributes = new Dictionary<string, ParameterSource>(StringComparer.OrdinalIgnoreCase)
        {
            { "FromBodyAttribute", ParameterSource.Body },
            { "FromQueryAttribute", ParameterSource.Query },
            { "FromRouteAttribute", ParameterSource.Route },
            { "FromHeaderAttribute", ParameterSource.Header },
            { "FromFormAttribute", ParameterSource.Form },
            { "FromServicesAttribute", ParameterSource.Services }
        };

        /// <inheritdoc />
        public bool HasAuthorizeAttribute(MemberInfo member)
        {
            return HasAttributeByName(member, "AuthorizeAttribute")
                || HasAttributeByName(member, "PrincipalPermissionAttribute");
        }

        /// <inheritdoc />
        public bool HasAllowAnonymousAttribute(MemberInfo member)
        {
            return HasAttributeByName(member, "AllowAnonymousAttribute");
        }

        /// <inheritdoc />
        public List<SecurityAttribute> GetSecurityAttributes(MemberInfo member)
        {
            var result = new List<SecurityAttribute>();

            foreach (var attr in GetCustomAttributesSafe(member))
            {
                var attrType = attr.GetType();
                var attrName = attrType.Name;

                if (attrName == "AuthorizeAttribute")
                {
                    var secAttr = new SecurityAttribute
                    {
                        AttributeName = attrName,
                        Roles = ExtractStringProperty(attr, "Roles"),
                        Policies = ExtractStringProperty(attr, "Policy"),
                        AuthenticationSchemes = ExtractStringProperty(attr, "AuthenticationSchemes")
                    };
                    result.Add(secAttr);
                }
                else if (attrName == "PrincipalPermissionAttribute")
                {
                    var secAttr = new SecurityAttribute
                    {
                        AttributeName = attrName,
                        Roles = ExtractStringProperty(attr, "Role")
                    };
                    result.Add(secAttr);
                }
                else if (attrName == "AllowAnonymousAttribute")
                {
                    result.Add(new SecurityAttribute { AttributeName = attrName });
                }
            }

            return result;
        }

        /// <inheritdoc />
        public List<string> GetRoles(MemberInfo member)
        {
            var roles = new List<string>();

            foreach (var attr in GetCustomAttributesSafe(member))
            {
                var attrName = attr.GetType().Name;
                if (attrName == "AuthorizeAttribute")
                {
                    roles.AddRange(ExtractStringProperty(attr, "Roles"));
                }
                else if (attrName == "PrincipalPermissionAttribute")
                {
                    roles.AddRange(ExtractStringProperty(attr, "Role"));
                }
            }

            return roles;
        }

        /// <inheritdoc />
        public List<string> GetPolicies(MemberInfo member)
        {
            var policies = new List<string>();

            foreach (var attr in GetCustomAttributesSafe(member))
            {
                if (attr.GetType().Name != "AuthorizeAttribute") continue;
                policies.AddRange(ExtractStringProperty(attr, "Policy"));
            }

            return policies;
        }

        /// <inheritdoc />
        public string GetHttpMethod(MethodInfo method)
        {
            foreach (var attr in GetCustomAttributesSafe(method))
            {
                var attrName = attr.GetType().Name;
                if (HttpMethodAttributes.TryGetValue(attrName, out var httpMethod))
                {
                    return httpMethod;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public string GetRouteTemplate(MemberInfo member)
        {
            foreach (var attr in GetCustomAttributesSafe(member))
            {
                var attrType = attr.GetType();
                var attrName = attrType.Name;

                // Check [Route("template")]
                if (attrName == "RouteAttribute")
                {
                    return GetAttributePropertyValue<string>(attr, "Template")
                        ?? GetAttributeConstructorString(attr);
                }

                // Check [HttpGet("template")], [HttpPost("template")], etc.
                if (HttpMethodAttributes.ContainsKey(attrName))
                {
                    var template = GetAttributePropertyValue<string>(attr, "Template")
                        ?? GetAttributeConstructorString(attr);
                    if (!string.IsNullOrEmpty(template))
                    {
                        return template;
                    }
                }
            }

            return null;
        }

        /// <inheritdoc />
        public string GetArea(Type controllerType)
        {
            foreach (var attr in GetCustomAttributesSafe(controllerType))
            {
                if (attr.GetType().Name == "AreaAttribute")
                {
                    return GetAttributePropertyValue<string>(attr, "RouteValue")
                        ?? GetAttributeConstructorString(attr);
                }
            }

            return null;
        }

        /// <inheritdoc />
        public ParameterSource GetParameterSource(System.Reflection.ParameterInfo parameter)
        {
            foreach (var attr in GetParameterAttributesSafe(parameter))
            {
                var attrName = attr.GetType().Name;
                if (ParameterSourceAttributes.TryGetValue(attrName, out var source))
                {
                    return source;
                }
            }

            return ParameterSource.Unknown;
        }

        private static bool HasAttributeByName(MemberInfo member, string attributeName)
        {
            foreach (var attr in GetCustomAttributesSafe(member))
            {
                if (attr.GetType().Name == attributeName)
                    return true;
            }
            return false;
        }

        private static Attribute[] GetCustomAttributesSafe(MemberInfo member)
        {
            try
            {
                return Attribute.GetCustomAttributes(member, true);
            }
            catch
            {
                return Array.Empty<Attribute>();
            }
        }

        private static Attribute[] GetParameterAttributesSafe(System.Reflection.ParameterInfo parameter)
        {
            try
            {
                return Attribute.GetCustomAttributes(parameter, true);
            }
            catch
            {
                return Array.Empty<Attribute>();
            }
        }

        private static List<string> ExtractStringProperty(object attribute, string propertyName)
        {
            var result = new List<string>();
            var value = GetAttributePropertyValue<string>(attribute, propertyName);
            if (string.IsNullOrWhiteSpace(value)) return result;

            foreach (var part in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }

            return result;
        }

        private static T GetAttributePropertyValue<T>(object attribute, string propertyName)
        {
            try
            {
                var prop = attribute.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var value = prop.GetValue(attribute);
                    if (value is T typed)
                        return typed;
                }
            }
            catch
            {
                // Ignore reflection errors
            }

            return default;
        }

        private static string GetAttributeConstructorString(object attribute)
        {
            try
            {
                // Try to get the first string constructor argument via the Template property
                // or by reading the attribute's internal state
                var type = attribute.GetType();

                // Common property names for route templates
                foreach (var propName in new[] { "Template", "Name", "Value" })
                {
                    var prop = type.GetProperty(propName);
                    if (prop != null && prop.PropertyType == typeof(string))
                    {
                        var val = prop.GetValue(attribute) as string;
                        if (!string.IsNullOrEmpty(val))
                            return val;
                    }
                }
            }
            catch
            {
                // Ignore reflection errors
            }

            return null;
        }
    }
}
