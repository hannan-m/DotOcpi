# DotOcpi

An open-source OCPI (Open Charge Point Interface) .NET library for the eMSP side.

## Packages

| Package | Description |
|---|---|
| `DotOcpi` | Core: version-specific models, interfaces, result types, token management, version negotiation |
| `DotOcpi.AspNetCore` | ASP.NET Core server integration: middleware, endpoint routing, OCPI auth pipeline |
| `DotOcpi.Client` | HttpClient-based OCPI client for calling CPO endpoints |
| `DotOcpi.Testing` | In-memory OCPI-compliant test CPO server for consumer integration tests |

## Supported OCPI Versions

- 2.0
- 2.1.1
- 2.2
- 2.2.1

## Requirements

- .NET 8.0 or .NET 10.0

## License

[MIT](LICENSE)
