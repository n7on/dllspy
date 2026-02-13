# Spy Examples

Real-world scenarios for using Spy to audit .NET API assemblies.

## CI/CD Security Gate

Fail the build if any high-severity issues are found:

```powershell
Import-Module ./module/Spy

$issues = Find-SpyVulnerability -Path .\MyApi.dll -MinimumSeverity High

if ($issues) {
    Write-Error "Found $($issues.Count) high-severity security issue(s):"
    $issues | Format-Table -AutoSize
    exit 1
}

Write-Host "No high-severity issues found." -ForegroundColor Green
```

## Audit All Assemblies in a Solution

Scan every DLL in a build output folder:

```powershell
Import-Module ./module/Spy

$outputDir = "C:\Projects\MySolution\src\*\bin\Release\net8.0"

Get-ChildItem -Path $outputDir -Filter *.dll -Recurse |
    ForEach-Object {
        try {
            $endpoints = Get-SpyEndpoint -Path $_.FullName -ErrorAction SilentlyContinue
            if ($endpoints) {
                Write-Host "`n--- $($_.Name) ---" -ForegroundColor Cyan
                $endpoints | Format-Table
            }
        } catch {
            # Skip non-API assemblies
        }
    }
```

## Compare Endpoints Between Builds

Save endpoint snapshots and compare:

```powershell
# Capture baseline
$baseline = Get-SpyEndpoint -Path .\v1\MyApi.dll |
    Select-Object HttpMethod, Route, RequiresAuthorization

# Capture current
$current = Get-SpyEndpoint -Path .\v2\MyApi.dll |
    Select-Object HttpMethod, Route, RequiresAuthorization

# Find new endpoints
$new = $current | Where-Object {
    $route = $_.Route
    $method = $_.HttpMethod
    -not ($baseline | Where-Object { $_.Route -eq $route -and $_.HttpMethod -eq $method })
}

if ($new) {
    Write-Host "New endpoints in v2:" -ForegroundColor Yellow
    $new | Format-Table
}

# Find endpoints where auth was removed
$authRemoved = $current | Where-Object {
    $route = $_.Route
    $method = $_.HttpMethod
    -not $_.RequiresAuthorization -and
    ($baseline | Where-Object {
        $_.Route -eq $route -and $_.HttpMethod -eq $method -and $_.RequiresAuthorization
    })
}

if ($authRemoved) {
    Write-Warning "Endpoints where authorization was REMOVED:"
    $authRemoved | Format-Table
}
```

## Generate API Documentation

Create a quick API reference from the discovered endpoints:

```powershell
Import-Module ./module/Spy

$endpoints = Get-SpyEndpoint -Path .\MyApi.dll

$grouped = $endpoints | Group-Object ControllerName

foreach ($group in $grouped) {
    Write-Host "`n## $($group.Name)" -ForegroundColor Cyan

    foreach ($ep in $group.Group) {
        $auth = if ($ep.RequiresAuthorization) { "[Auth]" }
                elseif ($ep.AllowAnonymous) { "[Public]" }
                else { "[?]" }

        $params = ($ep.Parameters | ForEach-Object { "$($_.Name): $($_.Type)" }) -join ", "
        Write-Host "  $($ep.HttpMethod.PadRight(7)) $($ep.Route.PadRight(35)) $auth  -> $($ep.ReturnType)"
        if ($params) { Write-Host "           Params: $params" -ForegroundColor DarkGray }
    }
}
```

## Filter Unauthenticated DELETE Endpoints

Quickly find the most dangerous pattern — delete operations without auth:

```powershell
Get-SpyEndpoint -Path .\MyApi.dll -HttpMethod DELETE |
    Where-Object { -not $_.RequiresAuthorization } |
    Format-List Route, ControllerName, ActionName, Roles, Policies
```

## Export Reports for Multiple Services

Generate a report per microservice:

```powershell
$services = @(
    @{ Name = "UserService";   Path = ".\services\UserService\bin\Release\net8.0\UserService.dll" }
    @{ Name = "OrderService";  Path = ".\services\OrderService\bin\Release\net8.0\OrderService.dll" }
    @{ Name = "PaymentService"; Path = ".\services\PaymentService\bin\Release\net8.0\PaymentService.dll" }
)

foreach ($svc in $services) {
    $outPath = ".\reports\$($svc.Name)-report.md"
    Export-SpyReport -Path $svc.Path -OutputPath $outPath -Format Markdown
    Write-Host "Report generated: $outPath" -ForegroundColor Green
}
```

## Interactive Exploration with Format-List

Get full details on a specific endpoint:

```powershell
Get-SpyEndpoint -Path .\MyApi.dll -Controller Users -HttpMethod POST | Format-List

# Output:
# HTTP Method    : POST
# Route          : api/Users
# Controller     : Users
# Action         : Create
# Requires Auth  : True
# Allow Anonymous: False
# Roles          :
# Policies       :
# Return Type    : Task<ActionResult<UserDto>>
# Is Async       : True
# Parameters     : request (CreateUserRequest, Body)
```

## Security Summary Dashboard

Build a quick summary across all scanned assemblies:

```powershell
$dlls = Get-ChildItem -Path .\bin\Release\net8.0\*.dll

$summary = foreach ($dll in $dlls) {
    try {
        $report = Export-SpyReport -Path $dll.FullName -Format JSON -ErrorAction Stop | ConvertFrom-Json
        if ($report.summary.totalEndpoints -gt 0) {
            [PSCustomObject]@{
                Assembly    = $report.assemblyName
                Endpoints   = $report.summary.totalEndpoints
                Authenticated = $report.summary.authenticatedEndpoints
                Anonymous   = $report.summary.anonymousEndpoints
                Issues      = $report.summary.totalSecurityIssues
                High        = $report.summary.highSeverityIssues
            }
        }
    } catch {
        # Skip non-API assemblies
    }
}

$summary | Format-Table -AutoSize
```
