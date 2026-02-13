using System;
using System.Collections.Generic;

namespace Spy.Core.Contracts
{
    /// <summary>
    /// Represents a discovered HTTP endpoint in a .NET assembly.
    /// </summary>
    public class EndpointInfo
    {
        /// <summary>Gets or sets the full route template for this endpoint.</summary>
        public string Route { get; set; }

        /// <summary>Gets or sets the HTTP method (GET, POST, PUT, DELETE, PATCH, etc.).</summary>
        public string HttpMethod { get; set; }

        /// <summary>Gets or sets the name of the controller containing this endpoint.</summary>
        public string ControllerName { get; set; }

        /// <summary>Gets or sets the action method name.</summary>
        public string ActionName { get; set; }

        /// <summary>Gets or sets whether the endpoint requires authorization.</summary>
        public bool RequiresAuthorization { get; set; }

        /// <summary>Gets or sets whether the endpoint explicitly allows anonymous access.</summary>
        public bool AllowAnonymous { get; set; }

        /// <summary>Gets or sets the roles required to access this endpoint.</summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>Gets or sets the authorization policies applied to this endpoint.</summary>
        public List<string> Policies { get; set; } = new List<string>();

        /// <summary>Gets or sets the parameters of the action method.</summary>
        public List<EndpointParameterInfo> Parameters { get; set; } = new List<EndpointParameterInfo>();

        /// <summary>Gets or sets the return type of the action method.</summary>
        public string ReturnType { get; set; }

        /// <summary>Gets or sets whether the action method is async.</summary>
        public bool IsAsync { get; set; }

        /// <summary>Gets or sets the route information.</summary>
        public RouteInfo RouteDetails { get; set; }

        /// <summary>Gets or sets the security attributes applied to this endpoint.</summary>
        public List<SecurityAttribute> SecurityAttributes { get; set; } = new List<SecurityAttribute>();

        /// <inheritdoc />
        public override string ToString() => $"{HttpMethod} {Route} -> {ControllerName}.{ActionName}";
    }
}
