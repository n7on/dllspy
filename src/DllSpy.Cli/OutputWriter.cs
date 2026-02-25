using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DllSpy.Core.Contracts;

namespace DllSpy.Cli
{
    internal static class OutputWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }
        };

        public static void PrintSurfaces(List<InputSurface> surfaces, bool json)
        {
            if (json) { PrintJson(surfaces); return; }

            if (surfaces.Count == 0)
            {
                Console.WriteLine("No surfaces found.");
                return;
            }

            const string typeH = "TYPE", routeH = "ROUTE", classH = "CLASS", methodH = "METHOD", authH = "AUTH";

            int tw = Math.Max(typeH.Length, surfaces.Max(s => GetTypeLabel(s).Length));
            int rw = Math.Max(routeH.Length, surfaces.Max(s => s.DisplayRoute.Length));
            int cw = Math.Max(classH.Length, surfaces.Max(s => s.ClassName.Length));
            int mw = Math.Max(methodH.Length, surfaces.Max(s => s.MethodName.Length));

            var fmt = $"{{0,-{tw}}}  {{1,-{rw}}}  {{2,-{cw}}}  {{3,-{mw}}}  {{4}}";

            Console.WriteLine(fmt, typeH, routeH, classH, methodH, authH);
            Console.WriteLine(new string('-', tw + rw + cw + mw + 12));

            foreach (var s in surfaces)
            {
                var auth = s.RequiresAuthorization ? "Yes" : s.AllowAnonymous ? "Anon" : "No";
                Console.WriteLine(fmt, GetTypeLabel(s), s.DisplayRoute, s.ClassName, s.MethodName, auth);
            }

            Console.WriteLine($"\n{surfaces.Count} surface(s) found.");
        }

        public static void PrintIssues(List<SecurityIssue> issues, bool json)
        {
            if (json) { PrintJson(issues); return; }

            if (issues.Count == 0)
            {
                Console.WriteLine("No issues found.");
                return;
            }

            const string sevH = "SEVERITY", typeH = "TYPE", surfH = "SURFACE", titleH = "TITLE";

            int sw = Math.Max(sevH.Length, issues.Max(i => i.Severity.ToString().Length));
            int tw = Math.Max(typeH.Length, issues.Max(i => i.SurfaceType.ToString().Length));
            int uw = Math.Max(surfH.Length, issues.Max(i => i.SurfaceRoute.Length));

            var fmt = $"{{0,-{sw}}}  {{1,-{tw}}}  {{2,-{uw}}}  {{3}}";

            Console.WriteLine(fmt, sevH, typeH, surfH, titleH);
            Console.WriteLine(new string('-', sw + tw + uw + titleH.Length + 8));

            foreach (var i in issues)
                Console.WriteLine(fmt, i.Severity, i.SurfaceType, i.SurfaceRoute, i.Title);

            Console.WriteLine($"\n{issues.Count} issue(s) found.");
        }

        private static string GetTypeLabel(InputSurface surface) => surface.SurfaceType switch
        {
            SurfaceType.HttpEndpoint => "HTTP",
            SurfaceType.SignalRMethod => "SignalR",
            SurfaceType.WcfOperation => "WCF",
            SurfaceType.GrpcOperation => "gRPC",
            _ => surface.SurfaceType.ToString()
        };

        private static void PrintJson<T>(T data) =>
            Console.WriteLine(JsonSerializer.Serialize(data, JsonOptions));
    }
}
