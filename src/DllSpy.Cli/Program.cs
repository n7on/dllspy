using System;
using System.CommandLine;
using System.Linq;
using System.Text.RegularExpressions;
using DllSpy.Core.Contracts;
using DllSpy.Core.Services;

namespace DllSpy.Cli
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var path = new Argument<string>("path") { Description = "Path to the .NET assembly to scan" };
            var scan = new Option<bool>("--scan", "-s") { Description = "Scan for security vulnerabilities" };
            var type = new Option<SurfaceType?>("--type", "-t") { Description = "Filter by surface type" };
            var method = new Option<string>("--method", "-m") { Description = "Filter HTTP endpoints by verb (GET, POST, PUT, DELETE, etc.)" };
            var cls = new Option<string>("--class", "-c") { Description = "Filter by class name (supports * wildcards)" };
            var auth = new Option<bool>("--auth") { Description = "Only show surfaces requiring authorization" };
            var anon = new Option<bool>("--anon") { Description = "Only show surfaces allowing anonymous access" };
            var minSev = new Option<SecuritySeverity>("--min-severity") { Description = "Minimum severity for scan mode", DefaultValueFactory = _ => SecuritySeverity.Info };
            var json = new Option<bool>("--json") { Description = "Output as JSON" };

            var root = new RootCommand("Discover input surfaces and security issues in .NET assemblies")
            {
                path, scan, type, method, cls, auth, anon, minSev, json
            };

            root.SetAction(r =>
            {
                try
                {
                    var report = ScannerFactory.Create().ScanAssembly(r.GetValue(path));

                    return r.GetValue(scan)
                        ? RunScan(report, r.GetValue(type), r.GetValue(minSev), r.GetValue(json))
                        : RunList(report, r.GetValue(type), r.GetValue(method), r.GetValue(cls),
                            r.GetValue(auth), r.GetValue(anon), r.GetValue(json));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    return 1;
                }
            });

            return root.Parse(args).Invoke();
        }

        private static int RunList(AssemblyReport report, SurfaceType? typeFilter,
            string methodFilter, string classFilter, bool authOnly, bool anonOnly, bool json)
        {
            var surfaces = report.Surfaces.AsEnumerable();

            if (typeFilter.HasValue)
                surfaces = surfaces.Where(s => s.SurfaceType == typeFilter.Value);

            if (methodFilter != null)
                surfaces = surfaces.Where(s =>
                    s is HttpEndpoint http &&
                    string.Equals(http.HttpMethod, methodFilter, StringComparison.OrdinalIgnoreCase));

            if (classFilter != null)
            {
                var regex = "^" + Regex.Escape(classFilter).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                surfaces = surfaces.Where(s => Regex.IsMatch(s.ClassName, regex, RegexOptions.IgnoreCase));
            }

            if (authOnly)
                surfaces = surfaces.Where(s => s.RequiresAuthorization);

            if (anonOnly)
                surfaces = surfaces.Where(s => s.AllowAnonymous);

            OutputWriter.PrintSurfaces(surfaces.ToList(), json);
            return 0;
        }

        private static int RunScan(AssemblyReport report, SurfaceType? typeFilter,
            SecuritySeverity minSeverity, bool json)
        {
            var issues = report.SecurityIssues.AsEnumerable();

            if (typeFilter.HasValue)
                issues = issues.Where(i => i.SurfaceType == typeFilter.Value);

            issues = issues.Where(i => i.Severity >= minSeverity);
            var list = issues.OrderByDescending(i => i.Severity).ToList();

            OutputWriter.PrintIssues(list, json);

            var highCount = list.Count(i => i.Severity >= SecuritySeverity.High);
            if (highCount > 0)
            {
                Console.Error.WriteLine($"\nFound {highCount} high-severity issue(s).");
                return 2;
            }

            return 0;
        }
    }
}
