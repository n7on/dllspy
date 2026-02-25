using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;

namespace DllSpy.Core.Helpers
{
    /// <summary>
    /// Shared reflection utilities used by discovery implementations.
    /// </summary>
    internal static class ReflectionHelper
    {
        /// <summary>
        /// Safely loads all types from an assembly, handling partial load failures.
        /// </summary>
        public static Type[] GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).ToArray();
            }
        }

        /// <summary>
        /// Determines whether a method is async (returns Task, Task&lt;T&gt;, ValueTask, or ValueTask&lt;T&gt;).
        /// </summary>
        public static bool IsAsyncMethod(MethodInfo method)
        {
            var returnType = method.ReturnType;
            if (returnType == typeof(Task)) return true;
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)) return true;
            if (returnType.Name == "ValueTask" || (returnType.IsGenericType && returnType.Name.StartsWith("ValueTask")))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether a type is nullable (Nullable&lt;T&gt; or reference type).
        /// </summary>
        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                || !type.IsValueType;
        }

        /// <summary>
        /// Returns a human-friendly name for a CLR type.
        /// </summary>
        public static string GetFriendlyTypeName(Type type)
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

        /// <summary>
        /// Builds parameter info for a method's parameters.
        /// </summary>
        public static List<EndpointParameterInfo> GetParameters(MethodInfo method, AttributeAnalyzer analyzer = null)
        {
            var parameters = new List<EndpointParameterInfo>();

            foreach (var param in method.GetParameters())
            {
                var source = analyzer != null
                    ? analyzer.GetParameterSource(param)
                    : ParameterSource.Unknown;
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
    }
}
