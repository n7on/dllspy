using System;

namespace Spy.Core.Contracts
{
    /// <summary>
    /// Severity level of a security issue.
    /// </summary>
    public enum SecuritySeverity
    {
        /// <summary>Informational finding.</summary>
        Info,
        /// <summary>Low severity issue.</summary>
        Low,
        /// <summary>Medium severity issue.</summary>
        Medium,
        /// <summary>High severity issue.</summary>
        High,
        /// <summary>Critical severity issue.</summary>
        Critical
    }

    /// <summary>
    /// Represents a security vulnerability or misconfiguration found in an input surface.
    /// </summary>
    public class SecurityIssue
    {
        /// <summary>Gets or sets the short title of the security issue.</summary>
        public string Title { get; set; }

        /// <summary>Gets or sets the detailed description of the issue.</summary>
        public string Description { get; set; }

        /// <summary>Gets or sets the severity level.</summary>
        public SecuritySeverity Severity { get; set; }

        /// <summary>Gets or sets the display route of the affected surface (e.g. "GET api/users" or "WS chat/Send").</summary>
        public string SurfaceRoute { get; set; }

        /// <summary>Gets or sets the type of input surface where the issue was found.</summary>
        public SurfaceType SurfaceType { get; set; }

        /// <summary>Gets or sets the recommended remediation.</summary>
        public string Recommendation { get; set; }

        /// <summary>Gets or sets the class name of the affected surface.</summary>
        public string ClassName { get; set; }

        /// <summary>Gets or sets the method name of the affected surface.</summary>
        public string MethodName { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"[{Severity}] {Title}: {SurfaceRoute}";
    }
}
