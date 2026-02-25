## [Unreleased]

## [0.2.0] - 2026-02-24

### Added
- **WCF service operation discovery** — detects `[ServiceContract]` interfaces, resolves implementing classes via `IsAssignableFrom`, extracts `[OperationContract]` methods
- **gRPC service operation discovery** — detects services inheriting from generated gRPC base classes (via `BindService` detection), identifies all four streaming modes (Unary, ServerStreaming, ClientStreaming, BidiStreaming)
- `WcfOperation` surface type with `ContractName`, `ServiceNamespace`, `IsOneWay` properties
- `GrpcOperation` surface type with `ServiceName`, `MethodType` properties
- `[PrincipalPermission]` recognized as an authorization attribute (WCF security model)
- WCF security rules: unauthenticated operation (High), authorize without role (Low)
- gRPC security rules: unauthenticated operation (High), authorize without role/policy (Low)
- `TotalWcfOperations` and `TotalGrpcOperations` computed properties on `AssemblyReport`
- Custom formatting views for `WcfOperation` and `GrpcOperation` (table and list)
- Contract-only WCF detection (interface without implementation class)

## [0.1.0] - 2026-02-24

### Added
- **Multi-surface discovery architecture** — pluggable `IDiscovery` interface for discovering different input surface types
- **SignalR hub method discovery** — detects hubs inheriting from `Hub` or `Hub<T>`, extracts callable methods, streaming detection
- `SurfaceType` enum (`HttpEndpoint`, `SignalRMethod`) for categorizing discovered surfaces
- `InputSurface` abstract base class with shared properties across all surface types
- `HttpEndpoint` — extends `InputSurface` with route, HTTP method, and route details
- `SignalRMethod` — extends `InputSurface` with hub route, hub name, and streaming flags
- `SignalRDiscovery` service implementing `IDiscovery` for SignalR hubs
- `-Type` parameter on `Search-DllSpy` and `Test-DllSpy` for filtering by surface type
- `-Class` parameter on `Search-DllSpy` (replaces `-Controller`) for filtering by class name with wildcards
- SignalR security rules: unauthenticated hub method (High), authorize without roles (Low)
- Custom formatting views for `SignalRMethod` (table and list)
- Sample SignalR hubs (`ChatHub`, `NotificationHub`) in `samples/SampleApi.cs`

### Changed
- Renamed `Get-SpyEndpoint` to `Search-DllSpy`
- Renamed `EndpointDiscovery` to `HttpEndpointDiscovery`, now implements `IDiscovery`
- `AssemblyScanner` now accepts `params IDiscovery[]` — runs all discoveries and aggregates results
- `AssemblyReport.Endpoints` replaced by `AssemblyReport.Surfaces` with new computed properties (`TotalSurfaces`, `TotalHttpEndpoints`, `TotalSignalRMethods`, `TotalClasses`)
- `SecurityIssue` fields generalized: `EndpointRoute`/`HttpMethod` → `SurfaceRoute`, `ControllerName`/`ActionName` → `ClassName`/`MethodName`, added `SurfaceType`
- Formatting views updated for `HttpEndpoint` (was `EndpointInfo`) and `SecurityIssue`

### Removed
- `EndpointInfo` class (replaced by `HttpEndpoint`)
- `Get-SpyEndpoint` cmdlet (replaced by `Search-DllSpy`)
