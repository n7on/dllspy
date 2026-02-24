# Contributing to Spy

If you want to contribute to the source you're highly welcome!

## Prerequisites

* .NET SDK 6.0+
* PowerShell 5.1+ or PowerShell 7+

## Build

```powershell
dotnet build
```

For a release build:

```powershell
dotnet build -c Release
```

## Test

Import the module and run it against the sample assembly:

```powershell
Import-Module ./out/Spy

# Discover all surfaces
Get-SpySurface -Path ./SampleApi.dll

# Check for security issues
Find-SpyVulnerability -Path ./SampleApi.dll
```

## Project Structure

```
spy/
├── src/
│   ├── Spy.Core/              # Core library (netstandard2.0)
│   │   ├── Contracts/         # Data models
│   │   └── Services/          # Discovery and analysis logic
│   └── Spy.PowerShell/        # PowerShell module (netstandard2.0)
│       ├── Commands/          # Cmdlets
│       └── Formatters/        # ps1xml formatting
├── samples/                   # Sample controllers and hubs
├── docs/                      # Documentation
└── Spy.sln
```

## Adding a New Discovery Type

To add a new surface type (e.g. gRPC, minimal APIs):

1. Add the type to `SurfaceType` enum in `src/Spy.Core/Contracts/SurfaceType.cs`
2. Create a new class extending `InputSurface` in `src/Spy.Core/Contracts/`
3. Create a new `IDiscovery` implementation in `src/Spy.Core/Services/`
4. Add security analysis rules in `AssemblyScanner.AnalyzeSecurityIssues`
5. Wire it up in the cmdlet constructors (`GetSpySurfaceCommand`, `FindSpyVulnerabilityCommand`)
6. Add formatting views in `Spy.Format.ps1xml`

## Releasing a New Version

1. Update version in `src/Spy.PowerShell/Spy.psd1`:
   ```powershell
   ModuleVersion = '1.1.0'
   ```

2. Update release notes in `src/Spy.PowerShell/Spy.psd1`

3. Update `CHANGELOG.md`

4. Commit and push the changes

5. Create and push a git tag:
   ```bash
   git tag v1.1.0
   git push origin v1.1.0
   ```

GitHub Actions will automatically publish to PowerShell Gallery when a tag is pushed.
