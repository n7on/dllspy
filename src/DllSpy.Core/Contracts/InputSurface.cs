using System.Collections.Generic;

namespace DllSpy.Core.Contracts
{
    /// <summary>
    /// Abstract base class shared by all discoverable input surfaces.
    /// </summary>
    public abstract class InputSurface
    {
        /// <summary>Gets the kind of input surface.</summary>
        public abstract SurfaceType SurfaceType { get; }

        /// <summary>Gets or sets the class name (controller name, hub name, etc.).</summary>
        public string ClassName { get; set; }

        /// <summary>Gets or sets the method name.</summary>
        public string MethodName { get; set; }

        /// <summary>Gets or sets the return type of the method.</summary>
        public string ReturnType { get; set; }

        /// <summary>Gets or sets whether the method is async.</summary>
        public bool IsAsync { get; set; }

        /// <summary>Gets or sets whether the surface requires authorization.</summary>
        public bool RequiresAuthorization { get; set; }

        /// <summary>Gets or sets whether the surface explicitly allows anonymous access.</summary>
        public bool AllowAnonymous { get; set; }

        /// <summary>Gets or sets the roles required to access this surface.</summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>Gets or sets the authorization policies applied to this surface.</summary>
        public List<string> Policies { get; set; } = new List<string>();

        /// <summary>Gets or sets the security attributes applied to this surface.</summary>
        public List<SecurityAttribute> SecurityAttributes { get; set; } = new List<SecurityAttribute>();

        /// <summary>Gets or sets the parameters of the method.</summary>
        public List<EndpointParameterInfo> Parameters { get; set; } = new List<EndpointParameterInfo>();

        /// <summary>Gets the formatted display route for this surface (e.g. "GET api/users" or "WS chat/Send").</summary>
        public abstract string DisplayRoute { get; }

        /// <inheritdoc />
        public override string ToString() => $"{DisplayRoute} -> {ClassName}.{MethodName}";
    }
}
