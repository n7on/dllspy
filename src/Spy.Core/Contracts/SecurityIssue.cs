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
    /// Represents a security vulnerability or misconfiguration found in an endpoint.
    /// </summary>
    public class SecurityIssue
    {
        /// <summary>Gets or sets the short title of the security issue.</summary>
        public string Title { get; set; }

        /// <summary>Gets or sets the detailed description of the issue.</summary>
        public string Description { get; set; }

        /// <summary>Gets or sets the severity level.</summary>
        public SecuritySeverity Severity { get; set; }

        /// <summary>Gets or sets the route of the affected endpoint.</summary>
        public string EndpointRoute { get; set; }

        /// <summary>Gets or sets the HTTP method of the affected endpoint.</summary>
        public string HttpMethod { get; set; }

        /// <summary>Gets or sets the recommended remediation.</summary>
        public string Recommendation { get; set; }

        /// <summary>Gets or sets the controller name of the affected endpoint.</summary>
        public string ControllerName { get; set; }

        /// <summary>Gets or sets the action name of the affected endpoint.</summary>
        public string ActionName { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"[{Severity}] {Title}: {EndpointRoute}";
    }
}
