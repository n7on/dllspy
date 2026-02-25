using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DllSpy.Core.Contracts;
using DllSpy.Core.Helpers;

namespace DllSpy.Core.Services
{
    /// <summary>
    /// Discovers WCF service operations by scanning for interfaces with [ServiceContract].
    /// </summary>
    internal class WcfDiscovery : IDiscovery
    {
        private readonly SecurityResolver _security;

        /// <summary>
        /// Initializes a new instance of <see cref="WcfDiscovery"/>.
        /// </summary>
        public WcfDiscovery(AttributeAnalyzer attributeAnalyzer)
        {
            if (attributeAnalyzer == null) throw new ArgumentNullException(nameof(attributeAnalyzer));
            _security = new SecurityResolver(attributeAnalyzer);
        }

        /// <inheritdoc />
        public SurfaceType SurfaceType => SurfaceType.WcfOperation;

        /// <inheritdoc />
        public List<InputSurface> Discover(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var surfaces = new List<InputSurface>();
            var types = ReflectionHelper.GetTypesSafe(assembly);

            var contracts = types.Where(IsServiceContract).ToList();

            foreach (var contract in contracts)
            {
                var implementation = FindImplementation(types, contract);
                surfaces.AddRange(DiscoverOperations(contract, implementation));
            }

            return surfaces;
        }

        private List<WcfOperation> DiscoverOperations(Type contractInterface, Type implementation)
        {
            var operations = new List<WcfOperation>();
            var contractName = GetContractName(contractInterface);
            var serviceNamespace = GetServiceNamespace(contractInterface);
            var className = implementation != null ? implementation.Name : contractInterface.Name;

            SecurityResolver.ClassSecurity classSec = null;
            if (implementation != null)
            {
                classSec = _security.ReadClass(implementation);
            }

            var methods = contractInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                if (!HasOperationContract(method)) continue;

                var operationName = GetOperationName(method);
                var isOneWay = GetIsOneWay(method);
                var parameters = ReflectionHelper.GetParameters(method);
                var returnType = ReflectionHelper.GetFriendlyTypeName(method.ReturnType);
                var isAsync = ReflectionHelper.IsAsyncMethod(method);

                bool requiresAuth = false;
                bool allowAnon = false;
                var roles = new List<string>();
                var policies = new List<string>();
                var securityAttributes = new List<SecurityAttribute>();

                if (implementation != null && classSec != null)
                {
                    // Try to find the implementing method via interface map
                    var implMethod = GetImplementingMethod(implementation, contractInterface, method);

                    if (implMethod != null)
                    {
                        var merged = _security.Merge(classSec, implMethod);
                        requiresAuth = merged.RequiresAuthorization;
                        allowAnon = merged.AllowAnonymous;
                        roles = merged.Roles;
                        policies = merged.Policies;
                        securityAttributes = merged.SecurityAttributes;
                    }
                    else
                    {
                        // Fallback to class-level security only
                        requiresAuth = classSec.HasAuth;
                        allowAnon = classSec.AllowAnon;
                        roles = new List<string>(classSec.Roles);
                        policies = new List<string>(classSec.Policies);
                        securityAttributes = new List<SecurityAttribute>(classSec.SecurityAttributes);
                    }
                }

                operations.Add(new WcfOperation
                {
                    ContractName = contractName,
                    ServiceNamespace = serviceNamespace,
                    IsOneWay = isOneWay,
                    ClassName = className,
                    MethodName = operationName,
                    RequiresAuthorization = requiresAuth,
                    AllowAnonymous = allowAnon,
                    Roles = roles,
                    Policies = policies,
                    Parameters = parameters,
                    ReturnType = returnType,
                    IsAsync = isAsync,
                    SecurityAttributes = securityAttributes
                });
            }

            return operations;
        }

        private static MethodInfo GetImplementingMethod(Type implementation, Type contractInterface, MethodInfo interfaceMethod)
        {
            try
            {
                var map = implementation.GetInterfaceMap(contractInterface);
                for (int i = 0; i < map.InterfaceMethods.Length; i++)
                {
                    if (map.InterfaceMethods[i] == interfaceMethod)
                    {
                        return map.TargetMethods[i];
                    }
                }
            }
            catch
            {
                // Fallback if GetInterfaceMap fails
            }

            return null;
        }

        private static Type FindImplementation(Type[] types, Type contractInterface)
        {
            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract || !type.IsPublic) continue;
                if (contractInterface.IsAssignableFrom(type))
                {
                    return type;
                }
            }

            return null;
        }

        private static bool IsServiceContract(Type type)
        {
            if (!type.IsInterface || !type.IsPublic) return false;

            return HasAttributeByName(type, "ServiceContractAttribute");
        }

        private static bool HasOperationContract(MethodInfo method)
        {
            return HasAttributeByName(method, "OperationContractAttribute");
        }

        private static bool HasAttributeByName(MemberInfo member, string attributeName)
        {
            try
            {
                foreach (var attr in Attribute.GetCustomAttributes(member, true))
                {
                    if (attr.GetType().Name == attributeName)
                        return true;
                }
            }
            catch
            {
                // Ignore reflection errors
            }

            return false;
        }

        private static string GetContractName(Type contractInterface)
        {
            try
            {
                foreach (var attr in Attribute.GetCustomAttributes(contractInterface, true))
                {
                    if (attr.GetType().Name == "ServiceContractAttribute")
                    {
                        var nameProp = attr.GetType().GetProperty("Name");
                        if (nameProp != null)
                        {
                            var value = nameProp.GetValue(attr) as string;
                            if (!string.IsNullOrEmpty(value))
                                return value;
                        }
                    }
                }
            }
            catch { }

            return contractInterface.Name;
        }

        private static string GetServiceNamespace(Type contractInterface)
        {
            try
            {
                foreach (var attr in Attribute.GetCustomAttributes(contractInterface, true))
                {
                    if (attr.GetType().Name == "ServiceContractAttribute")
                    {
                        var nsProp = attr.GetType().GetProperty("Namespace");
                        if (nsProp != null)
                        {
                            return nsProp.GetValue(attr) as string;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private static string GetOperationName(MethodInfo method)
        {
            try
            {
                foreach (var attr in Attribute.GetCustomAttributes(method, true))
                {
                    if (attr.GetType().Name == "OperationContractAttribute")
                    {
                        var nameProp = attr.GetType().GetProperty("Name");
                        if (nameProp != null)
                        {
                            var value = nameProp.GetValue(attr) as string;
                            if (!string.IsNullOrEmpty(value))
                                return value;
                        }
                    }
                }
            }
            catch { }

            return method.Name;
        }

        private static bool GetIsOneWay(MethodInfo method)
        {
            try
            {
                foreach (var attr in Attribute.GetCustomAttributes(method, true))
                {
                    if (attr.GetType().Name == "OperationContractAttribute")
                    {
                        var prop = attr.GetType().GetProperty("IsOneWay");
                        if (prop != null)
                        {
                            var value = prop.GetValue(attr);
                            if (value is bool b)
                                return b;
                        }
                    }
                }
            }
            catch { }

            return false;
        }
    }
}
