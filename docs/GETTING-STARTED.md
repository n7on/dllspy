# Getting Started with Spy

## Prerequisites

- [.NET SDK 6.0+](https://dotnet.microsoft.com/download)
- PowerShell 5.1+ (Windows) or PowerShell 7+ (cross-platform)

## Building

Clone the repository and build with the .NET SDK:

```powershell
git clone <repo-url>
cd spy
dotnet build
```

All module files (manifest, format file, help file) are copied to the build output automatically.

For a release build:

```powershell
dotnet build -c Release
```

To clean before building:

```powershell
dotnet clean && dotnet build
```

## Importing the Module

```powershell
Import-Module ./out/Spy
```

Verify it loaded:

```powershell
Get-Command -Module Spy
```

You should see:

```
CommandType     Name                    Version    Source
-----------     ----                    -------    ------
Cmdlet          Export-SpyReport        1.0.0      Spy
Cmdlet          Find-SpyVulnerability   1.0.0      Spy
Cmdlet          Get-SpyEndpoint         1.0.0      Spy
```

## Your First Scan

Point Spy at any compiled ASP.NET Core or Web API assembly:

```powershell
Get-SpyEndpoint -Path C:\Projects\MyApi\bin\Debug\net8.0\MyApi.dll
```

Output:

```
Method   Route                    Controller    Action           Auth
------   -----                    ----------    ------           ----
GET      api/Users                Users         GetAll           No
GET      api/Users/{id}           Users         GetById          Anon
POST     api/Users                Users         Create           Yes
PUT      api/Users/{id}           Users         Update           Yes
DELETE   api/Users/{id}           Users         Delete           No
```

## Checking for Vulnerabilities

```powershell
Find-SpyVulnerability -Path .\MyApi.dll
```

Output:

```
Severity   Title                                  Endpoint                   Description
--------   -----                                  --------                   -----------
High       Unauthenticated DELETE endpoint         DELETE api/Users/{id}     The DELETE endpoint...
Medium     Missing authorization declaration       GET api/Users             The endpoint...
```

## Generating a Report

```powershell
Export-SpyReport -Path .\MyApi.dll -OutputPath report.md -Format Markdown
```

This creates a Markdown file with a summary table, endpoint listing, and security findings.

## Filtering Results

### By HTTP Method

```powershell
Get-SpyEndpoint -Path .\MyApi.dll -HttpMethod DELETE
```

### By Controller Name

```powershell
# Exact match
Get-SpyEndpoint -Path .\MyApi.dll -Controller Users

# Wildcard
Get-SpyEndpoint -Path .\MyApi.dll -Controller Admin*
```

### By Authorization Status

```powershell
# Endpoints that require auth
Get-SpyEndpoint -Path .\MyApi.dll -RequiresAuth

# Endpoints that allow anonymous
Get-SpyEndpoint -Path .\MyApi.dll -AllowAnonymous
```

### By Severity

```powershell
# Only high-severity issues
Find-SpyVulnerability -Path .\MyApi.dll -MinimumSeverity High
```

## Scanning Multiple Assemblies

Use wildcards or pipe paths:

```powershell
# Wildcard
Get-SpyEndpoint -Path C:\Projects\MyApi\bin\Release\net8.0\*.dll

# Pipeline
Get-ChildItem .\*.dll | Get-SpyEndpoint
```

## Next Steps

- See [EXAMPLES.md](EXAMPLES.md) for real-world scenarios
- Run `Get-Help Get-SpyEndpoint -Full` for complete parameter documentation
