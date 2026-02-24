using System.Collections.Generic;
using System.Reflection;
using Spy.Core.Contracts;

namespace Spy.Core.Services
{
    /// <summary>
    /// Discovers input surfaces of a specific type within a .NET assembly.
    /// </summary>
    internal interface IDiscovery
    {
        /// <summary>Gets the surface type this discovery implementation handles.</summary>
        SurfaceType SurfaceType { get; }

        /// <summary>Discovers all input surfaces of this type in the given assembly.</summary>
        List<InputSurface> Discover(Assembly assembly);
    }
}
