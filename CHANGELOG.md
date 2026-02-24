## [Unreleased]

## [0.1.0] - 2026-02-24

### Added
- **Multi-surface discovery architecture** — pluggable `IDiscovery` interface for discovering different input surface types
- **SignalR hub method discovery** — detects hubs inheriting from `Hub` or `Hub<T>`, extracts callable methods, streaming detection
- `SurfaceType` enum (`HttpEndpoint`, `SignalRMethod`) for categorizing discovered surfaces
- `InputSurface` abstract base class with shared properties across all surface types
- `HttpEndpoint` — extends `InputSurface` with route, HTTP method, and route details
- `SignalRMethod` — extends `InputSurface` with hub route, hub name, and streaming flags
- `SignalRDiscovery` service implementing `IDiscovery` for SignalR hubs
- `-Type` parameter on `Get-SpySurface` and `Find-SpyVulnerability` for filtering by surface type
- `-Class` parameter on `Get-SpySurface` (replaces `-Controller`) for filtering by class name with wildcards
- SignalR security rules: unauthenticated hub method (High), authorize without roles (Low)
- Custom formatting views for `SignalRMethod` (table and list)
- Sample SignalR hubs (`ChatHub`, `NotificationHub`) in `samples/SampleApi.cs`

### Changed
- Renamed `Get-SpyEndpoint` to `Get-SpySurface`
- Renamed `EndpointDiscovery` to `HttpEndpointDiscovery`, now implements `IDiscovery`
- `AssemblyScanner` now accepts `params IDiscovery[]` — runs all discoveries and aggregates results
- `AssemblyReport.Endpoints` replaced by `AssemblyReport.Surfaces` with new computed properties (`TotalSurfaces`, `TotalHttpEndpoints`, `TotalSignalRMethods`, `TotalClasses`)
- `SecurityIssue` fields generalized: `EndpointRoute`/`HttpMethod` → `SurfaceRoute`, `ControllerName`/`ActionName` → `ClassName`/`MethodName`, added `SurfaceType`
- Formatting views updated for `HttpEndpoint` (was `EndpointInfo`) and `SecurityIssue`

### Removed
- `EndpointInfo` class (replaced by `HttpEndpoint`)
- `Get-SpyEndpoint` cmdlet (replaced by `Get-SpySurface`)
