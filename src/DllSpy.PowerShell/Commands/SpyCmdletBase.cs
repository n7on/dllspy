using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

namespace DllSpy.PowerShell.Commands
{
    /// <summary>
    /// Base class for DllSpy cmdlets that scan assemblies.
    /// </summary>
    public abstract class SpyCmdletBase : PSCmdlet
    {
        /// <summary>
        /// Resolves a user-supplied path (which may contain wildcards) to one or more absolute file paths.
        /// </summary>
        protected List<string> ResolvePaths(string inputPath)
        {
            var resolved = new List<string>();

            try
            {
                var providerPaths = GetResolvedProviderPathFromPSPath(inputPath, out var provider);
                resolved.AddRange(providerPaths);
            }
            catch (ItemNotFoundException)
            {
                var literalPath = GetUnresolvedProviderPathFromPSPath(inputPath);
                if (File.Exists(literalPath))
                {
                    resolved.Add(literalPath);
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new FileNotFoundException($"Assembly not found: {inputPath}"),
                        "AssemblyNotFound",
                        ErrorCategory.ObjectNotFound,
                        inputPath));
                }
            }

            return resolved;
        }
    }
}
