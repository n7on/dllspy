using System;

namespace Spy.Core.Services
{
    /// <summary>
    /// Static factory for creating Spy.Core service instances without a DI container.
    /// </summary>
    public static class ServiceFactory
    {
        /// <summary>
        /// Creates a new <see cref="IAttributeAnalyzer"/> instance.
        /// </summary>
        public static IAttributeAnalyzer CreateAttributeAnalyzer()
        {
            return new AttributeAnalyzer();
        }

        /// <summary>
        /// Creates a new <see cref="IEndpointDiscovery"/> instance.
        /// </summary>
        public static IEndpointDiscovery CreateEndpointDiscovery()
        {
            return new EndpointDiscovery(CreateAttributeAnalyzer());
        }

        /// <summary>
        /// Creates a new <see cref="IEndpointDiscovery"/> instance with a custom attribute analyzer.
        /// </summary>
        public static IEndpointDiscovery CreateEndpointDiscovery(IAttributeAnalyzer attributeAnalyzer)
        {
            return new EndpointDiscovery(attributeAnalyzer);
        }

        /// <summary>
        /// Creates a new <see cref="IAssemblyScanner"/> instance.
        /// </summary>
        public static IAssemblyScanner CreateAssemblyScanner()
        {
            return new AssemblyScanner(CreateEndpointDiscovery());
        }

        /// <summary>
        /// Creates a new <see cref="IAssemblyScanner"/> instance with a custom endpoint discovery.
        /// </summary>
        public static IAssemblyScanner CreateAssemblyScanner(IEndpointDiscovery endpointDiscovery)
        {
            return new AssemblyScanner(endpointDiscovery);
        }
    }
}
