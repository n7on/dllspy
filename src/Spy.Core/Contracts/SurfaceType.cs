namespace Spy.Core.Contracts
{
    /// <summary>
    /// Identifies the kind of input surface discovered in an assembly.
    /// </summary>
    public enum SurfaceType
    {
        /// <summary>An HTTP endpoint on an ASP.NET Core / Web API controller.</summary>
        HttpEndpoint,
        /// <summary>A callable method on a SignalR hub.</summary>
        SignalRMethod
    }
}
