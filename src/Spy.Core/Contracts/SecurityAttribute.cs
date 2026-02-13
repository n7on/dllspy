using System;
using System.Collections.Generic;

namespace Spy.Core.Contracts
{
    /// <summary>
    /// Represents security-related attributes applied to a controller or action.
    /// </summary>
    public class SecurityAttribute
    {
        /// <summary>Gets or sets the attribute type name (e.g., "AuthorizeAttribute").</summary>
        public string AttributeName { get; set; }

        /// <summary>Gets or sets the roles specified in the attribute, if any.</summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>Gets or sets the policy names specified in the attribute, if any.</summary>
        public List<string> Policies { get; set; } = new List<string>();

        /// <summary>Gets or sets the authentication schemes, if any.</summary>
        public List<string> AuthenticationSchemes { get; set; } = new List<string>();

        /// <inheritdoc />
        public override string ToString() => AttributeName;
    }
}
