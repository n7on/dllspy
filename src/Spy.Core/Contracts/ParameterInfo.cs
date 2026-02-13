using System;

namespace Spy.Core.Contracts
{
    /// <summary>
    /// Specifies the binding source of a parameter.
    /// </summary>
    public enum ParameterSource
    {
        /// <summary>Parameter source is unknown or not explicitly specified.</summary>
        Unknown,
        /// <summary>Parameter is bound from the request body.</summary>
        Body,
        /// <summary>Parameter is bound from the query string.</summary>
        Query,
        /// <summary>Parameter is bound from the route template.</summary>
        Route,
        /// <summary>Parameter is bound from a request header.</summary>
        Header,
        /// <summary>Parameter is bound from form data.</summary>
        Form,
        /// <summary>Parameter is bound from registered services.</summary>
        Services
    }

    /// <summary>
    /// Represents a parameter on an HTTP endpoint action method.
    /// </summary>
    public class EndpointParameterInfo
    {
        /// <summary>Gets or sets the parameter name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the CLR type name of the parameter.</summary>
        public string Type { get; set; }

        /// <summary>Gets or sets whether the parameter is required.</summary>
        public bool IsRequired { get; set; }

        /// <summary>Gets or sets the binding source of the parameter.</summary>
        public ParameterSource Source { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{Name} ({Type}, {Source})";
    }
}
