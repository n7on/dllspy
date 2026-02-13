using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Spy.Core.Contracts;
using Spy.Core.Services;

namespace Spy.PowerShell.Commands
{
    /// <summary>
    /// Exports a scan report in JSON, CSV, or Markdown format.
    /// </summary>
    /// <example>
    /// <code>Export-SpyReport -Path .\MyApi.dll -OutputPath .\report.md -Format Markdown</code>
    /// </example>
    /// <example>
    /// <code>Export-SpyReport -Path .\MyApi.dll -Format JSON</code>
    /// </example>
    [Cmdlet(VerbsData.Export, "SpyReport")]
    [OutputType(typeof(AssemblyReport), typeof(string))]
    public class ExportSpyReportCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the path to the .NET assembly.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Alias("AssemblyPath", "FullName")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the output file path. If not specified, output goes to the pipeline.
        /// </summary>
        [Parameter(Position = 1)]
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the report format.
        /// </summary>
        [Parameter]
        [ValidateSet("JSON", "CSV", "Markdown")]
        public string Format { get; set; } = "JSON";

        private IAssemblyScanner _scanner;

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            _scanner = ServiceFactory.CreateAssemblyScanner();
        }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            string resolvedPath;
            try
            {
                var providerPaths = GetResolvedProviderPathFromPSPath(Path, out var provider);
                resolvedPath = providerPaths.First();
            }
            catch
            {
                resolvedPath = GetUnresolvedProviderPathFromPSPath(Path);
            }

            AssemblyReport report;
            try
            {
                report = _scanner.ScanAssembly(resolvedPath);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "ReportGenerationError",
                    ErrorCategory.ReadError,
                    resolvedPath));
                return;
            }

            string content;
            switch (Format.ToUpperInvariant())
            {
                case "CSV":
                    content = GenerateCsv(report);
                    break;
                case "MARKDOWN":
                    content = GenerateMarkdown(report);
                    break;
                case "JSON":
                default:
                    content = GenerateJson(report);
                    break;
            }

            if (!string.IsNullOrEmpty(OutputPath))
            {
                var outputResolved = GetUnresolvedProviderPathFromPSPath(OutputPath);
                File.WriteAllText(outputResolved, content, Encoding.UTF8);
                WriteVerbose($"Report written to {outputResolved}");
                WriteObject(report);
            }
            else
            {
                WriteObject(content);
            }
        }

        private static string GenerateJson(AssemblyReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"assemblyName\": \"{EscapeJson(report.AssemblyName)}\",");
            sb.AppendLine($"  \"assemblyPath\": \"{EscapeJson(report.AssemblyPath)}\",");
            sb.AppendLine($"  \"scanTimestamp\": \"{report.ScanTimestamp:O}\",");
            sb.AppendLine($"  \"summary\": {{");
            sb.AppendLine($"    \"totalEndpoints\": {report.TotalEndpoints},");
            sb.AppendLine($"    \"totalControllers\": {report.TotalControllers},");
            sb.AppendLine($"    \"authenticatedEndpoints\": {report.AuthenticatedEndpoints},");
            sb.AppendLine($"    \"anonymousEndpoints\": {report.AnonymousEndpoints},");
            sb.AppendLine($"    \"totalSecurityIssues\": {report.TotalSecurityIssues},");
            sb.AppendLine($"    \"highSeverityIssues\": {report.HighSeverityIssues},");
            sb.AppendLine($"    \"mediumSeverityIssues\": {report.MediumSeverityIssues},");
            sb.AppendLine($"    \"lowSeverityIssues\": {report.LowSeverityIssues}");
            sb.AppendLine("  },");

            sb.AppendLine("  \"endpoints\": [");
            for (int i = 0; i < report.Endpoints.Count; i++)
            {
                var ep = report.Endpoints[i];
                var comma = i < report.Endpoints.Count - 1 ? "," : "";
                sb.AppendLine("    {");
                sb.AppendLine($"      \"httpMethod\": \"{EscapeJson(ep.HttpMethod)}\",");
                sb.AppendLine($"      \"route\": \"{EscapeJson(ep.Route)}\",");
                sb.AppendLine($"      \"controller\": \"{EscapeJson(ep.ControllerName)}\",");
                sb.AppendLine($"      \"action\": \"{EscapeJson(ep.ActionName)}\",");
                sb.AppendLine($"      \"requiresAuthorization\": {ep.RequiresAuthorization.ToString().ToLower()},");
                sb.AppendLine($"      \"allowAnonymous\": {ep.AllowAnonymous.ToString().ToLower()},");
                sb.AppendLine($"      \"roles\": [{string.Join(", ", ep.Roles.Select(r => $"\"{EscapeJson(r)}\""))}],");
                sb.AppendLine($"      \"policies\": [{string.Join(", ", ep.Policies.Select(p => $"\"{EscapeJson(p)}\""))}],");
                sb.AppendLine($"      \"returnType\": \"{EscapeJson(ep.ReturnType)}\",");
                sb.AppendLine($"      \"isAsync\": {ep.IsAsync.ToString().ToLower()}");
                sb.AppendLine($"    }}{comma}");
            }
            sb.AppendLine("  ],");

            sb.AppendLine("  \"securityIssues\": [");
            for (int i = 0; i < report.SecurityIssues.Count; i++)
            {
                var issue = report.SecurityIssues[i];
                var comma = i < report.SecurityIssues.Count - 1 ? "," : "";
                sb.AppendLine("    {");
                sb.AppendLine($"      \"severity\": \"{issue.Severity}\",");
                sb.AppendLine($"      \"title\": \"{EscapeJson(issue.Title)}\",");
                sb.AppendLine($"      \"endpoint\": \"{EscapeJson(issue.EndpointRoute)}\",");
                sb.AppendLine($"      \"httpMethod\": \"{EscapeJson(issue.HttpMethod)}\",");
                sb.AppendLine($"      \"description\": \"{EscapeJson(issue.Description)}\",");
                sb.AppendLine($"      \"recommendation\": \"{EscapeJson(issue.Recommendation)}\"");
                sb.AppendLine($"    }}{comma}");
            }
            sb.AppendLine("  ]");

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string GenerateCsv(AssemblyReport report)
        {
            var sb = new StringBuilder();

            // Endpoints section
            sb.AppendLine("# Endpoints");
            sb.AppendLine("HttpMethod,Route,Controller,Action,RequiresAuth,AllowAnonymous,Roles,Policies,ReturnType,IsAsync");
            foreach (var ep in report.Endpoints)
            {
                sb.AppendLine(string.Join(",",
                    CsvEscape(ep.HttpMethod),
                    CsvEscape(ep.Route),
                    CsvEscape(ep.ControllerName),
                    CsvEscape(ep.ActionName),
                    ep.RequiresAuthorization,
                    ep.AllowAnonymous,
                    CsvEscape(string.Join(";", ep.Roles)),
                    CsvEscape(string.Join(";", ep.Policies)),
                    CsvEscape(ep.ReturnType),
                    ep.IsAsync));
            }

            sb.AppendLine();

            // Security issues section
            sb.AppendLine("# Security Issues");
            sb.AppendLine("Severity,Title,Endpoint,HttpMethod,Description,Recommendation");
            foreach (var issue in report.SecurityIssues)
            {
                sb.AppendLine(string.Join(",",
                    issue.Severity,
                    CsvEscape(issue.Title),
                    CsvEscape(issue.EndpointRoute),
                    CsvEscape(issue.HttpMethod),
                    CsvEscape(issue.Description),
                    CsvEscape(issue.Recommendation)));
            }

            return sb.ToString();
        }

        private static string GenerateMarkdown(AssemblyReport report)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# Spy Report: {report.AssemblyName}");
            sb.AppendLine();
            sb.AppendLine($"**Scanned:** {report.ScanTimestamp:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"**Assembly:** `{report.AssemblyPath}`");
            sb.AppendLine();

            // Summary
            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine($"| Metric | Count |");
            sb.AppendLine($"|--------|-------|");
            sb.AppendLine($"| Total Endpoints | {report.TotalEndpoints} |");
            sb.AppendLine($"| Total Controllers | {report.TotalControllers} |");
            sb.AppendLine($"| Authenticated Endpoints | {report.AuthenticatedEndpoints} |");
            sb.AppendLine($"| Anonymous Endpoints | {report.AnonymousEndpoints} |");
            sb.AppendLine($"| Security Issues | {report.TotalSecurityIssues} |");
            sb.AppendLine($"| High Severity | {report.HighSeverityIssues} |");
            sb.AppendLine($"| Medium Severity | {report.MediumSeverityIssues} |");
            sb.AppendLine($"| Low Severity | {report.LowSeverityIssues} |");
            sb.AppendLine();

            // Endpoints
            sb.AppendLine("## Endpoints");
            sb.AppendLine();
            sb.AppendLine("| Method | Route | Controller | Action | Auth |");
            sb.AppendLine("|--------|-------|------------|--------|------|");
            foreach (var ep in report.Endpoints)
            {
                var auth = ep.RequiresAuthorization ? "Yes" : (ep.AllowAnonymous ? "Anon" : "No");
                sb.AppendLine($"| {ep.HttpMethod} | `{ep.Route}` | {ep.ControllerName} | {ep.ActionName} | {auth} |");
            }
            sb.AppendLine();

            // Security Issues
            if (report.SecurityIssues.Count > 0)
            {
                sb.AppendLine("## Security Issues");
                sb.AppendLine();
                foreach (var issue in report.SecurityIssues.OrderByDescending(i => i.Severity))
                {
                    var severityBadge = issue.Severity >= SecuritySeverity.High ? "**" : "";
                    sb.AppendLine($"### {severityBadge}[{issue.Severity}]{severityBadge} {issue.Title}");
                    sb.AppendLine();
                    sb.AppendLine($"- **Endpoint:** `{issue.HttpMethod} {issue.EndpointRoute}`");
                    sb.AppendLine($"- **Description:** {issue.Description}");
                    sb.AppendLine($"- **Recommendation:** {issue.Recommendation}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (value == null) return "";
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private static string CsvEscape(string value)
        {
            if (value == null) return "\"\"";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
