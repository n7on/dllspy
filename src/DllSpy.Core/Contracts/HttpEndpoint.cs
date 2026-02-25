namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Represents a discovered HTTP endpoint on an ASP.NET Core / Web API controller.
    /// </summary>
    public class HttpEndpoint : InputSurface
    {
        /// <inheritdoc />
        public override SurfaceType SurfaceType => SurfaceType.HttpEndpoint;

        /// <summary>Gets or sets the full route template for this endpoint.</summary>
        public string Route { get; set; }

        /// <summary>Gets or sets the HTTP method (GET, POST, PUT, DELETE, PATCH, etc.).</summary>
        public string HttpMethod { get; set; }

        /// <summary>Gets or sets the route information.</summary>
        public RouteInfo RouteDetails { get; set; }

        /// <inheritdoc />
        public override string DisplayRoute => $"{HttpMethod} {Route}";
    }
}
