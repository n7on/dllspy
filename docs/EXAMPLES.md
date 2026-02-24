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
            $surfaces = Get-SpySurface -Path $_.FullName -ErrorAction SilentlyContinue
            if ($surfaces) {
                Write-Host "`n--- $($_.Name) ---" -ForegroundColor Cyan
                $surfaces | Format-Table
            }
        } catch {
            # Skip non-API assemblies
        }
    }
```

## Compare Surfaces Between Builds

Save surface snapshots and compare:

```powershell
# Capture baseline
$baseline = Get-SpySurface -Path .\v1\MyApi.dll |
    Select-Object SurfaceType, DisplayRoute, RequiresAuthorization

# Capture current
$current = Get-SpySurface -Path .\v2\MyApi.dll |
    Select-Object SurfaceType, DisplayRoute, RequiresAuthorization

# Find new surfaces
$new = $current | Where-Object {
    $route = $_.DisplayRoute
    -not ($baseline | Where-Object { $_.DisplayRoute -eq $route })
}

if ($new) {
    Write-Host "New surfaces in v2:" -ForegroundColor Yellow
    $new | Format-Table
}

# Find surfaces where auth was removed
$authRemoved = $current | Where-Object {
    $route = $_.DisplayRoute
    -not $_.RequiresAuthorization -and
    ($baseline | Where-Object {
        $_.DisplayRoute -eq $route -and $_.RequiresAuthorization
    })
}

if ($authRemoved) {
    Write-Warning "Surfaces where authorization was REMOVED:"
    $authRemoved | Format-Table
}
```

## Generate API Documentation

Create a quick API reference from the discovered surfaces:

```powershell
Import-Module ./module/Spy

$surfaces = Get-SpySurface -Path .\MyApi.dll

$grouped = $surfaces | Group-Object ClassName

foreach ($group in $grouped) {
    Write-Host "`n## $($group.Name)" -ForegroundColor Cyan

    foreach ($s in $group.Group) {
        $auth = if ($s.RequiresAuthorization) { "[Auth]" }
                elseif ($s.AllowAnonymous) { "[Public]" }
                else { "[?]" }

        $params = ($s.Parameters | ForEach-Object { "$($_.Name): $($_.Type)" }) -join ", "
        Write-Host "  $($s.DisplayRoute.PadRight(40)) $auth  -> $($s.ReturnType)"
        if ($params) { Write-Host "           Params: $params" -ForegroundColor DarkGray }
    }
}
```

## Filter Unauthenticated DELETE Endpoints

Quickly find the most dangerous pattern — delete operations without auth:

```powershell
Get-SpySurface -Path .\MyApi.dll -HttpMethod DELETE |
    Where-Object { -not $_.RequiresAuthorization } |
    Format-List Route, ClassName, MethodName, Roles, Policies
```

## Audit SignalR Hubs

Find all unauthenticated SignalR hub methods:

```powershell
Get-SpySurface -Path .\MyApi.dll -Type SignalRMethod |
    Where-Object { -not $_.RequiresAuthorization } |
    Format-Table HubName, MethodName, HubRoute
```

## Interactive Exploration with Format-List

Get full details on a specific HTTP endpoint:

```powershell
Get-SpySurface -Path .\MyApi.dll -Class Users -HttpMethod POST | Format-List

# Output:
# HTTP Method    : POST
# Route          : api/Users
# Class          : Users
# Action         : Create
# Requires Auth  : True
# Allow Anonymous: False
# Roles          :
# Policies       :
# Return Type    : Task<ActionResult<UserDto>>
# Is Async       : True
# Parameters     : request (CreateUserRequest, Body)
```

Get full details on a SignalR hub method:

```powershell
Get-SpySurface -Path .\MyApi.dll -Type SignalRMethod -Class ChatHub | Format-List

# Output:
# Hub              : ChatHub
# Method           : SendMessage
# Hub Route        : chat
# Streaming Result : False
# Accepts Streaming: False
# Requires Auth    : False
# Allow Anonymous  : False
# Roles            :
# Policies         :
# Return Type      : Task
# Is Async         : True
# Parameters       : user (string, Unknown); message (string, Unknown)
```
