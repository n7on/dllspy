using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Spy.Core.Contracts;

namespace Spy.Core.Services
{
    /// <summary>
    /// Orchestrates assembly scanning, surface discovery, and security analysis.
    /// </summary>
    public class AssemblyScanner
    {
        private readonly IDiscovery[] _discoveries;

        /// <summary>
        /// Initializes a new instance of <see cref="AssemblyScanner"/>.
        /// </summary>
        public AssemblyScanner(params IDiscovery[] discoveries)
        {
            if (discoveries == null || discoveries.Length == 0)
                throw new ArgumentException("At least one discovery implementation is required.", nameof(discoveries));
            _discoveries = discoveries;
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

            var surfaces = new List<InputSurface>();
            foreach (var discovery in _discoveries)
            {
                surfaces.AddRange(discovery.Discover(assembly));
            }

            var securityIssues = AnalyzeSecurityIssues(surfaces);

            return new AssemblyReport
            {
                AssemblyPath = assembly.Location,
                AssemblyName = assembly.GetName().Name,
                ScanTimestamp = DateTime.UtcNow,
                Surfaces = surfaces,
                SecurityIssues = securityIssues
            };
        }

        /// <summary>
        /// Analyzes all input surfaces for security issues.
        /// </summary>
        public List<SecurityIssue> AnalyzeSecurityIssues(List<InputSurface> surfaces)
        {
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));

            var issues = new List<SecurityIssue>();

            foreach (var surface in surfaces)
            {
                switch (surface)
                {
                    case HttpEndpoint http:
                        issues.AddRange(AnalyzeHttpEndpoint(http));
                        break;
                    case SignalRMethod signalr:
                        issues.AddRange(AnalyzeSignalRMethod(signalr));
                        break;
                }
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeHttpEndpoint(HttpEndpoint endpoint)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: State-changing endpoints (DELETE, POST, PUT, PATCH) without [Authorize]
            if (IsStateChangingMethod(endpoint.HttpMethod) && !endpoint.RequiresAuthorization && !endpoint.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = $"Unauthenticated {endpoint.HttpMethod} endpoint",
                    Description = $"The {endpoint.HttpMethod} endpoint '{endpoint.Route}' on {endpoint.ClassName}.{endpoint.MethodName} " +
                                  $"does not require authentication. State-changing operations should be protected.",
                    Severity = SecuritySeverity.High,
                    SurfaceRoute = endpoint.DisplayRoute,
                    SurfaceType = SurfaceType.HttpEndpoint,
                    ClassName = endpoint.ClassName,
                    MethodName = endpoint.MethodName,
                    Recommendation = $"Add [Authorize] attribute to the {endpoint.MethodName} action or the {endpoint.ClassName}Controller class."
                });
            }

            // MEDIUM: Endpoints without [Authorize] or [AllowAnonymous] (unclear intent)
            if (!endpoint.RequiresAuthorization && !endpoint.AllowAnonymous && !IsStateChangingMethod(endpoint.HttpMethod))
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Missing authorization declaration",
                    Description = $"The endpoint '{endpoint.Route}' on {endpoint.ClassName}.{endpoint.MethodName} " +
                                  $"has neither [Authorize] nor [AllowAnonymous]. Security intent is unclear.",
                    Severity = SecuritySeverity.Medium,
                    SurfaceRoute = endpoint.DisplayRoute,
                    SurfaceType = SurfaceType.HttpEndpoint,
                    ClassName = endpoint.ClassName,
                    MethodName = endpoint.MethodName,
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
                    Description = $"The endpoint '{endpoint.Route}' on {endpoint.ClassName}.{endpoint.MethodName} " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated user can access it.",
                    Severity = SecuritySeverity.Low,
                    SurfaceRoute = endpoint.DisplayRoute,
                    SurfaceType = SurfaceType.HttpEndpoint,
                    ClassName = endpoint.ClassName,
                    MethodName = endpoint.MethodName,
                    Recommendation = "Consider adding Roles or Policy to the [Authorize] attribute to restrict access."
                });
            }

            return issues;
        }

        private static List<SecurityIssue> AnalyzeSignalRMethod(SignalRMethod method)
        {
            var issues = new List<SecurityIssue>();

            // HIGH: Unauthenticated hub method (direct invocation surface)
            if (!method.RequiresAuthorization && !method.AllowAnonymous)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Unauthenticated SignalR hub method",
                    Description = $"The hub method '{method.HubRoute}/{method.MethodName}' on {method.HubName} " +
                                  $"does not require authentication. Hub methods are directly invocable by clients.",
                    Severity = SecuritySeverity.High,
                    SurfaceRoute = method.DisplayRoute,
                    SurfaceType = SurfaceType.SignalRMethod,
                    ClassName = method.ClassName,
                    MethodName = method.MethodName,
                    Recommendation = $"Add [Authorize] attribute to the {method.MethodName} method or the {method.HubName} class."
                });
            }

            // LOW: [Authorize] without roles or policies
            if (method.RequiresAuthorization && !method.AllowAnonymous &&
                method.Roles.Count == 0 && method.Policies.Count == 0)
            {
                issues.Add(new SecurityIssue
                {
                    Title = "Authorize without role or policy restriction",
                    Description = $"The hub method '{method.HubRoute}/{method.MethodName}' on {method.HubName} " +
                                  $"requires authentication but does not specify roles or policies. Any authenticated user can invoke it.",
                    Severity = SecuritySeverity.Low,
                    SurfaceRoute = method.DisplayRoute,
                    SurfaceType = SurfaceType.SignalRMethod,
                    ClassName = method.ClassName,
                    MethodName = method.MethodName,
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
