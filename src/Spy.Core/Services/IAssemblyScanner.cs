using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Spy.Core.Contracts;

namespace Spy.Core.Services
{
    /// <summary>
    /// Orchestrates assembly scanning, endpoint discovery, and security analysis.
    /// </summary>
    public interface IAssemblyScanner
    {
        /// <summary>
        /// Scans an assembly at the given path and generates a complete report.
        /// </summary>
        /// <param name="assemblyPath">Path to the .NET assembly.</param>
        /// <returns>A complete assembly report.</returns>
        AssemblyReport ScanAssembly(string assemblyPath);

        /// <summary>
        /// Scans an already-loaded assembly and generates a complete report.
        /// </summary>
        /// <param name="assembly">The loaded assembly.</param>
        /// <returns>A complete assembly report.</returns>
        AssemblyReport ScanAssembly(Assembly assembly);

        /// <summary>
        /// Analyzes endpoints for security issues.
        /// </summary>
        /// <param name="endpoints">The endpoints to analyze.</param>
        /// <returns>A list of security issues found.</returns>
        List<SecurityIssue> AnalyzeSecurityIssues(List<EndpointInfo> endpoints);
    }

    /// <summary>
    /// Default implementation of <see cref="IAssemblyScanner"/>.
    /// </summary>
    public class AssemblyScanner : IAssemblyScanner
    {
        private readonly IEndpointDiscovery _endpointDiscovery;

        /// <summary>
        /// Initializes a new instance of <see cref="AssemblyScanner"/>.
        /// </summary>
        public AssemblyScanner(IEndpointDiscovery endpointDiscovery)
        {
            _endpointDiscovery = endpointDiscovery ?? throw new ArgumentNullException(nameof(endpointDiscovery));
        }

        /// <inheritdoc />
        public AssemblyReport ScanAssembly(string assemblyPath)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
                throw new ArgumentException("Assembly path cannot be null or empty.", nameof(assemblyPath));

            var fullPath = Path.GetFullPath(assemblyPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Assembly not found: {fullPath}", fullPath);

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(fullPath);
            }
            catch (BadImageFormatException ex)
            {
                throw new InvalidOperationException($"The file is not a valid .NET assembly: {fullPath}", ex);
            }
            catch (FileLoadException ex)
            {
                throw new InvalidOperationException($"Failed to load assembly: {fullPath}", ex);
            }

            var report = ScanAssembly(assembly);
            report.AssemblyPath = fullPath;
            return report;
        }

        /// <inheritdoc />
        public AssemblyReport ScanAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var endpoints = _endpointDiscovery.DiscoverEndpoints(assembly);
            var securityIssues = AnalyzeSecurityIssues(endpoints);

            return new AssemblyReport
            {
                AssemblyPath = assembly.Location,
                AssemblyName = assembly.GetName().Name,
                ScanTimestamp = DateTime.UtcNow,
                Endpoints = endpoints,
                SecurityIssues = securityIssues
            };
        }

        /// <inheritdoc />
        public List<SecurityIssue> AnalyzeSecurityIssues(List<EndpointInfo> endpoints)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));

            var issues = new List<SecurityIssue>();

            foreach (var endpoint in endpoints)
            {
                issues.AddRange(AnalyzeEndpoint(endpoint));
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeEndpoint(EndpointInfo endpoint)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: State-changing endpoints (DELETE, POST, PUT) without [Authorize]
            if (IsStateChangingMethod(endpoint.HttpMethod) && !endpoint.RequiresAuthorization && !endpoint.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = $"Unauthenticated {endpoint.HttpMethod} endpoint",
                    Description = $"The {endpoint.HttpMethod} endpoint '{endpoint.Route}' on {endpoint.ControllerName}.{endpoint.ActionName} " +
                                  $"does not require authentication. State-changing operations should be protected.",
                    Severity = SecuritySeverity.High,
                    EndpointRoute = endpoint.Route,
                    HttpMethod = endpoint.HttpMethod,
                    ControllerName = endpoint.ControllerName,
                    ActionName = endpoint.ActionName,
                    Recommendation = $"Add [Authorize] attribute to the {endpoint.ActionName} action or the {endpoint.ControllerName}Controller class."
                });
            }

            // MEDIUM: Endpoints without [Authorize] or [AllowAnonymous] (unclear intent)
            if (!endpoint.RequiresAuthorization && !endpoint.AllowAnonymous && !IsStateChangingMethod(endpoint.HttpMethod))
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Missing authorization declaration",
                    Description = $"The endpoint '{endpoint.Route}' on {endpoint.ControllerName}.{endpoint.ActionName} " +
                                  $"has neither [Authorize] nor [AllowAnonymous]. Security intent is unclear.",
                    Severity = SecuritySeverity.Medium,
                    EndpointRoute = endpoint.Route,
                    HttpMethod = endpoint.HttpMethod,
                    ControllerName = endpoint.ControllerName,
                    ActionName = endpoint.ActionName,
                    Recommendation = "Add [Authorize] or [AllowAnonymous] to explicitly declare the security intent."
                });
            }

            // LOW: [Authorize] without roles or policies (broad access)
            if (endpoint.RequiresAuthorization && !endpoint.AllowAnonymous &&
                endpoint.Roles.Count == 0 && endpoint.Policies.Count == 0)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Authorize without role or policy restriction",
                    Description = $"The endpoint '{endpoint.Route}' on {endpoint.ControllerName}.{endpoint.ActionName} " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated user can access it.",
                    Severity = SecuritySeverity.Low,
                    EndpointRoute = endpoint.Route,
                    HttpMethod = endpoint.HttpMethod,
                    ControllerName = endpoint.ControllerName,
                    ActionName = endpoint.ActionName,
                    Recommendation = "Consider adding Roles or Policy to the [Authorize] attribute to restrict access."
                });
            }

            return issues;
        }

        private static bool IsStateChangingMethod(string httpMethod)
        {
            return httpMethod == "POST" || httpMethod == "PUT" || httpMethod == "DELETE" || httpMethod == "PATCH";
        }
    }
}
