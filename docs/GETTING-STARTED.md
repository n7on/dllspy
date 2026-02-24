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
Cmdlet          Find-SpyVulnerability   1.0.0      Spy
Cmdlet          Get-SpySurface          1.0.0      Spy
```

## Your First Scan

Point Spy at any compiled ASP.NET Core assembly:

```powershell
Get-SpySurface -Path C:\Projects\MyApi\bin\Debug\net8.0\MyApi.dll
```

HTTP endpoints output:

```
Method   Route                    Class         Action           Auth
------   -----                    -----         ------           ----
GET      api/Users                Users         GetAll           No
GET      api/Users/{id}           Users         GetById          Anon
POST     api/Users                Users         Create           Yes
PUT      api/Users/{id}           Users         Update           Yes
DELETE   api/Users/{id}           Users         Delete           No
```

SignalR hub methods output:

```
Hub              Method         HubRoute       Streaming  Auth
---              ------         --------       ---------  ----
ChatHub          SendMessage    chat           No         No
ChatHub          JoinGroup      chat           No         No
NotificationHub  Subscribe      notification   No         Yes
```

## Checking for Vulnerabilities

```powershell
Find-SpyVulnerability -Path .\MyApi.dll
```

Output:

```
Severity   Type           Title                                  Surface                    Description
--------   ----           -----                                  -------                    -----------
High       HttpEndpoint   Unauthenticated DELETE endpoint        DELETE api/Users/{id}      The DELETE endpoint...
High       SignalRMethod  Unauthenticated SignalR hub method     WS chat/SendMessage        The hub method...
Medium     HttpEndpoint   Missing authorization declaration      GET api/Users              The endpoint...
```

## Filtering Results

### By Surface Type

```powershell
# Only HTTP endpoints
Get-SpySurface -Path .\MyApi.dll -Type HttpEndpoint

# Only SignalR hub methods
Get-SpySurface -Path .\MyApi.dll -Type SignalRMethod
```

### By HTTP Method

```powershell
Get-SpySurface -Path .\MyApi.dll -HttpMethod DELETE
```

### By Class Name

```powershell
# Exact match
Get-SpySurface -Path .\MyApi.dll -Class Users

# Wildcard
Get-SpySurface -Path .\MyApi.dll -Class *Hub
```

### By Authorization Status

```powershell
# Surfaces that require auth
Get-SpySurface -Path .\MyApi.dll -RequiresAuth

# Surfaces that allow anonymous
Get-SpySurface -Path .\MyApi.dll -AllowAnonymous
```

### By Severity

```powershell
# Only high-severity issues
Find-SpyVulnerability -Path .\MyApi.dll -MinimumSeverity High

# Only SignalR issues
Find-SpyVulnerability -Path .\MyApi.dll -Type SignalRMethod
```

## Scanning Multiple Assemblies

Use wildcards or pipe paths:

```powershell
# Wildcard
Get-SpySurface -Path C:\Projects\MyApi\bin\Release\net8.0\*.dll

# Pipeline
Get-ChildItem .\*.dll | Get-SpySurface
```

## Next Steps

- See [EXAMPLES.md](EXAMPLES.md) for real-world scenarios
- Run `Get-Help Get-SpySurface -Full` for complete parameter documentation
