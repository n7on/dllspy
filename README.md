# Spy

A PowerShell module that uses .NET reflection to discover and analyze input surfaces in compiled .NET assemblies.

Spy scans your ASP.NET Core assemblies to map every HTTP endpoint and SignalR hub method, check authorization configuration, and flag security issues — all without running the application.

## Features

- **Multi-Surface Discovery** — Finds HTTP endpoints (controllers, `[HttpGet]`/`[HttpPost]`/etc.) and SignalR hub methods in a single scan
- **Security Analysis** — Identifies missing `[Authorize]` attributes, unprotected state-changing endpoints, unauthenticated hub methods, and overly broad authorization
- **PowerShell Filtering** — Filter by surface type, HTTP method, class name (with wildcards), auth requirements
- **Custom Formatting** — Table and list views designed for quick triage in the terminal
- **netstandard2.0** — Works with PowerShell 5.1+ and PowerShell 7+

## Quick Start

```powershell
# Build
dotnet build

# Import the module
Import-Module ./out/Spy

# Discover all input surfaces (HTTP endpoints + SignalR methods)
Get-SpySurface -Path ./MyApi.dll

# Find security issues
Find-SpyVulnerability -Path ./MyApi.dll
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

### Get-SpySurface

Discovers input surfaces in a .NET assembly.

```powershell
# All surfaces (HTTP endpoints + SignalR methods)
Get-SpySurface -Path .\MyApi.dll

# Only HTTP endpoints
Get-SpySurface -Path .\MyApi.dll -Type HttpEndpoint

# Only SignalR hub methods
Get-SpySurface -Path .\MyApi.dll -Type SignalRMethod

# Filter by HTTP method
Get-SpySurface -Path .\MyApi.dll -HttpMethod DELETE

# Filter by class name (supports wildcards)
Get-SpySurface -Path .\MyApi.dll -Class User*

# Only authenticated surfaces
Get-SpySurface -Path .\MyApi.dll -RequiresAuth

# Only anonymous surfaces
Get-SpySurface -Path .\MyApi.dll -AllowAnonymous

# Detailed view
Get-SpySurface -Path .\MyApi.dll | Format-List
```

### Find-SpyVulnerability

Analyzes surfaces for security issues.

```powershell
# All issues
Find-SpyVulnerability -Path .\MyApi.dll

# Only high-severity issues
Find-SpyVulnerability -Path .\MyApi.dll -MinimumSeverity High

# Only SignalR issues
Find-SpyVulnerability -Path .\MyApi.dll -Type SignalRMethod

# Detailed view
Find-SpyVulnerability -Path .\MyApi.dll | Format-List
```

## Security Rules

### HTTP Endpoints

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated state-changing endpoint | `DELETE`, `POST`, `PUT`, or `PATCH` without `[Authorize]` |
| **Medium** | Missing authorization declaration | Endpoint has neither `[Authorize]` nor `[AllowAnonymous]` |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

### SignalR Hub Methods

| Severity | Rule | Description |
|----------|------|-------------|
| **High** | Unauthenticated hub method | Hub method without `[Authorize]` (directly invocable by clients) |
| **Low** | Authorize without role/policy | `[Authorize]` present but no `Roles` or `Policy` specified |

## Project Structure

```
spy/
├── src/
│   ├── Spy.Core/              # Core library (netstandard2.0)
│   │   ├── Contracts/         # Models: InputSurface, HttpEndpoint, SignalRMethod, SecurityIssue, etc.
│   │   └── Services/          # Logic: IDiscovery, HttpEndpointDiscovery, SignalRDiscovery, AssemblyScanner
│   └── Spy.PowerShell/        # PowerShell module (netstandard2.0)
│       ├── Commands/          # Cmdlets: Get-SpySurface, Find-SpyVulnerability
│       └── Formatters/        # ps1xml formatting definitions
├── samples/                   # Sample API controllers and hubs for testing
├── docs/                      # Documentation
└── Spy.sln                    # Solution file
```

## How It Works

Spy loads .NET assemblies via `System.Reflection` and scans for types that represent input surfaces:

### HTTP Endpoints
1. Classes inheriting from `ControllerBase`, `Controller`, or `ApiController`
2. Classes with `[ApiController]` attribute
3. Classes with names ending in `Controller` that have public action methods

### SignalR Hub Methods
1. Classes inheriting from `Hub` or `Hub<T>`
2. Public instance methods (excluding lifecycle methods like `OnConnectedAsync`)

For HTTP endpoints, routes are resolved by combining controller-level and action-level templates, with support for `[controller]` and `[action]` tokens.

For SignalR hubs, routes use conventional naming (strip "Hub" suffix) since actual `MapHub<T>("/route")` calls are registered at startup and aren't discoverable via reflection.

## License

See [LICENSE](LICENSE).
