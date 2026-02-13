using System;

namespace Spy.Core.Contracts
{
    /// <summary>
    /// Represents route information for an endpoint.
    /// </summary>
    public class RouteInfo
    {
        /// <summary>Gets or sets the route template from the controller-level [Route] attribute.</summary>
        public string ControllerRoute { get; set; }

        /// <summary>Gets or sets the route template from the action-level attribute.</summary>
        public string ActionRoute { get; set; }

        /// <summary>Gets or sets the fully resolved route combining controller and action routes.</summary>
        public string CombinedRoute { get; set; }

        /// <summary>Gets or sets the area name, if the controller belongs to an area.</summary>
        public string Area { get; set; }

        /// <inheritdoc />
        public override string ToString() => CombinedRoute ?? $"{ControllerRoute}/{ActionRoute}";
    }
}
