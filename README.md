# Spy

A PowerShell module that uses .NET reflection to discover and analyze HTTP endpoints in compiled .NET assemblies.

Spy scans your ASP.NET Core and Web API assemblies to map every HTTP endpoint, check authorization configuration, and flag security issues — all without running the application.

## Features

- **Endpoint Discovery** — Finds all HTTP endpoints by scanning for controllers, `[HttpGet]`/`[HttpPost]`/etc. attributes, and route templates
- **Security Analysis** — Identifies missing `[Authorize]` attributes, unprotected state-changing endpoints, and overly broad authorization
- **Report Generation** — Exports results as JSON, CSV, or Markdown
- **PowerShell Filtering** — Filter by HTTP method, controller name (with wildcards), auth requirements
- **Custom Formatting** — Table and list views designed for quick triage in the terminal
- **netstandard2.0** — Works with PowerShell 5.1+ and PowerShell 7+

## Quick Start

```powershell
# Build
dotnet build

# Import the module
Import-Module ./out/Spy

# Discover all endpoints
Get-SpyEndpoint -Path ./MyApi.dll

# Find security issues
Find-SpyVulnerability -Path ./MyApi.dll

# Export a Markdown report
Export-SpyReport -Path ./MyApi.dll -OutputPath report.md -Format Markdown
```

## Installation

### From Source

```powershell
git clone <repo-url> && cd spy
dotnet build
Import-Module ./out/Spy
```

### Prerequisites

- [.NET SDK 6.0+](https://dotnet.microsoft.com/download) (for building)
- PowerShell 5.1+ or PowerShell 7+

## Cmdlets

### Get-SpyEndpoint

Discovers HTTP endpoints in a .NET assembly.

```powershell
# All endpoints
Get-SpyEndpoint -Path .\MyApi.dll

# Filter by HTTP method
Get-SpyEndpoint -Path .\MyApi.dll -HttpMethod DELETE

# Filter by controller (supports wildcards)
Get-SpyEndpoint -Path .\MyApi.dll -Controller User*

# Only authenticated endpoints
Get-SpyEndpoint -Path .\MyApi.dll -RequiresAuth

# Only anonymous endpoints
Get-SpyEndpoint -Path .\MyApi.dll -AllowAnonymous

# Detailed view
Get-SpyEndpoint -Path .\MyApi.dll | Format-List
```

### Find-SpyVulnerability

Analyzes endpoints for security issues.

```powershell
# All issues
Find-SpyVulnerability -Path .\MyApi.dll

# Only high-severity issues
Find-SpyVulnerability -Path .\MyApi.dll -MinimumSeverity High

# Detailed view
Find-SpyVulnerability -Path .\MyApi.dll | Format-List
```

### Export-SpyReport

Generates a full report combining endpoints and security findings.

```powershell
# JSON to stdout
Export-SpyReport -Path .\MyApi.dll -Format JSON

# Markdown to file
Export-SpyReport -Path .\MyApi.dll -OutputPath report.md -Format Markdown

# CSV to file
Export-SpyReport -Path .\MyApi.dll -OutputPath report.csv -Format CSV
```

## Security Rules

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated state-changing endpoint | `DELETE`, `POST`, `PUT`, or `PATCH` without `[Authorize]` |
| **Medium** | Missing authorization declaration | Endpoint has neither `[Authorize]` nor `[AllowAnonymous]` |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

## Project Structure

```
spy/
├── src/
│   ├── Spy.Core/              # Core library (netstandard2.0)
│   │   ├── Contracts/         # Models: EndpointInfo, SecurityIssue, etc.
│   │   └── Services/          # Logic: AttributeAnalyzer, EndpointDiscovery, AssemblyScanner
│   └── Spy.PowerShell/        # PowerShell module (netstandard2.0)
│       ├── Commands/          # Cmdlets: Get-SpyEndpoint, Find-SpyVulnerability, Export-SpyReport
│       └── Formatters/        # ps1xml formatting definitions
├── samples/                   # Sample API controller for testing
├── docs/                      # Documentation
└── Spy.sln                    # Solution file
```

## How It Works

Spy loads .NET assemblies via `System.Reflection` and scans for types that look like ASP.NET Core or Web API controllers:

1. Classes inheriting from `ControllerBase`, `Controller`, or `ApiController`
2. Classes with `[ApiController]` attribute
3. Classes with names ending in `Controller` that have public action methods

For each controller, Spy inspects public methods for HTTP method attributes (`[HttpGet]`, `[HttpPost]`, etc.), route templates (`[Route]`), and security attributes (`[Authorize]`, `[AllowAnonymous]`).

Routes are resolved by combining controller-level and action-level templates, with support for `[controller]` and `[action]` tokens.

## License

See [LICENSE](LICENSE).
