using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Spy.Core.Contracts;
using Spy.Core.Services;

namespace Spy.PowerShell.Commands
{
    /// <summary>
    /// Discovers HTTP endpoints in compiled .NET assemblies.
    /// </summary>
    /// <example>
    /// <code>Get-SpyEndpoint -Path .\MyApi.dll</code>
    /// </example>
    /// <example>
    /// <code>Get-SpyEndpoint -Path .\MyApi.dll -HttpMethod GET -Controller Users</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "SpyEndpoint")]
    [OutputType(typeof(EndpointInfo))]
    public class GetSpyEndpointCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the path to the .NET assembly. Supports wildcards.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("AssemblyPath", "FullName")]
        [ValidateNotNullOrEmpty]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method filter (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        [Parameter]
        [ValidateSet("GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS")]
        public string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets whether to filter by endpoints requiring authorization.
        /// </summary>
        [Parameter]
        public SwitchParameter RequiresAuth { get; set; }

        /// <summary>
        /// Gets or sets whether to filter by endpoints allowing anonymous access.
        /// </summary>
        [Parameter]
        public SwitchParameter AllowAnonymous { get; set; }

        /// <summary>
        /// Gets or sets the controller name filter. Supports wildcards.
        /// </summary>
        [Parameter]
        [SupportsWildcards]
        public string Controller { get; set; }

        private IAssemblyScanner _scanner;

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            _scanner = ServiceFactory.CreateAssemblyScanner();
        }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            foreach (var inputPath in Path)
            {
                var resolvedPaths = ResolvePaths(inputPath);

                foreach (var resolvedPath in resolvedPaths)
                {
                    try
                    {
                        ProcessAssembly(resolvedPath);
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(
                            ex,
                            "AssemblyScanError",
                            ErrorCategory.ReadError,
                            resolvedPath));
                    }
                }
            }
        }

        private void ProcessAssembly(string assemblyPath)
        {
            WriteVerbose($"Scanning assembly: {assemblyPath}");

            var report = _scanner.ScanAssembly(assemblyPath);
            var endpoints = report.Endpoints.AsEnumerable();

            if (!string.IsNullOrEmpty(HttpMethod))
            {
                endpoints = endpoints.Where(e =>
                    string.Equals(e.HttpMethod, HttpMethod, StringComparison.OrdinalIgnoreCase));
            }

            if (RequiresAuth.IsPresent)
            {
                endpoints = endpoints.Where(e => e.RequiresAuthorization);
            }

            if (AllowAnonymous.IsPresent)
            {
                endpoints = endpoints.Where(e => e.AllowAnonymous);
            }

            if (!string.IsNullOrEmpty(Controller))
            {
                var pattern = new WildcardPattern(Controller, WildcardOptions.IgnoreCase);
                endpoints = endpoints.Where(e => pattern.IsMatch(e.ControllerName));
            }

            foreach (var endpoint in endpoints)
            {
                WriteObject(endpoint);
            }

            WriteVerbose($"Found {report.TotalEndpoints} endpoints in {assemblyPath}");
        }

        private List<string> ResolvePaths(string inputPath)
        {
            var resolved = new List<string>();

            try
            {
                var providerPaths = GetResolvedProviderPathFromPSPath(inputPath, out var provider);
                resolved.AddRange(providerPaths);
            }
            catch (ItemNotFoundException)
            {
                // Path doesn't exist — try as literal
                var literalPath = GetUnresolvedProviderPathFromPSPath(inputPath);
                if (File.Exists(literalPath))
                {
                    resolved.Add(literalPath);
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new FileNotFoundException($"Assembly not found: {inputPath}"),
                        "AssemblyNotFound",
                        ErrorCategory.ObjectNotFound,
                        inputPath));
                }
            }

            return resolved;
        }
    }
}
