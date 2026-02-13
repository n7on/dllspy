using System;
using System.Collections.Generic;
using System.Linq;

namespace Spy.Core.Contracts
{
    /// <summary>
    /// Complete scan report for a .NET assembly.
    /// </summary>
    public class AssemblyReport
    {
        /// <summary>Gets or sets the path to the scanned assembly.</summary>
        public string AssemblyPath { get; set; }

        /// <summary>Gets or sets the assembly name.</summary>
        public string AssemblyName { get; set; }

        /// <summary>Gets or sets the timestamp of the scan.</summary>
        public DateTime ScanTimestamp { get; set; }

        /// <summary>Gets or sets the discovered endpoints.</summary>
        public List<EndpointInfo> Endpoints { get; set; } = new List<EndpointInfo>();

        /// <summary>Gets or sets the security issues found.</summary>
        public List<SecurityIssue> SecurityIssues { get; set; } = new List<SecurityIssue>();

        /// <summary>Gets the total number of endpoints discovered.</summary>
        public int TotalEndpoints => Endpoints.Count;

        /// <summary>Gets the number of distinct controllers found.</summary>
        public int TotalControllers => Endpoints.Select(e => e.ControllerName).Distinct().Count();

        /// <summary>Gets the number of endpoints requiring authorization.</summary>
        public int AuthenticatedEndpoints => Endpoints.Count(e => e.RequiresAuthorization);

        /// <summary>Gets the number of endpoints allowing anonymous access.</summary>
        public int AnonymousEndpoints => Endpoints.Count(e => e.AllowAnonymous || !e.RequiresAuthorization);

        /// <summary>Gets the total number of security issues found.</summary>
        public int TotalSecurityIssues => SecurityIssues.Count;

        /// <summary>Gets the number of high-severity security issues.</summary>
        public int HighSeverityIssues => SecurityIssues.Count(e => e.Severity == SecuritySeverity.High);

        /// <summary>Gets the number of medium-severity security issues.</summary>
        public int MediumSeverityIssues => SecurityIssues.Count(e => e.Severity == SecuritySeverity.Medium);

        /// <summary>Gets the number of low-severity security issues.</summary>
        public int LowSeverityIssues => SecurityIssues.Count(e => e.Severity == SecuritySeverity.Low);

        /// <inheritdoc />
        public override string ToString() =>
            $"Assembly: {AssemblyName} | Endpoints: {TotalEndpoints} | Issues: {TotalSecurityIssues}";
    }
}
