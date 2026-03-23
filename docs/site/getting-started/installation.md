---
title: Installation
layout: default
parent: Getting Started
nav_order: 1
---

# Installation
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Packages

DotOcpi is distributed as four NuGet packages. Install only what you need:

| Package | When to use |
|:--------|:------------|
| `DotOcpi` | Always required. Core models, interfaces, token management, version negotiation. |
| `DotOcpi.AspNetCore` | Your eMSP receives pushes from CPOs (locations, sessions, CDRs, tariffs). |
| `DotOcpi.Client` | Your eMSP pulls data from CPOs or sends commands. |
| `DotOcpi.Testing` | Integration tests against a fake OCPI-compliant CPO server. |

## Install via CLI

```bash
# Core (always required)
dotnet add package DotOcpi

# Server — receive pushes from CPOs
dotnet add package DotOcpi.AspNetCore

# Client — pull data and send commands to CPOs
dotnet add package DotOcpi.Client

# Testing — integration test helpers (add to test project only)
dotnet add package DotOcpi.Testing
```

## Install via PackageReference

Add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="DotOcpi" Version="0.1.*" />
  <PackageReference Include="DotOcpi.AspNetCore" Version="0.1.*" />
  <PackageReference Include="DotOcpi.Client" Version="0.1.*" />
</ItemGroup>
```

For test projects:

```xml
<ItemGroup>
  <PackageReference Include="DotOcpi.Testing" Version="0.1.*" />
</ItemGroup>
```

## Common Configurations

### Server + Client (most common)

Your eMSP both receives pushes from CPOs and pulls data / sends commands:

```bash
dotnet add package DotOcpi
dotnet add package DotOcpi.AspNetCore
dotnet add package DotOcpi.Client
```

### Server Only

Your eMSP only receives pushes from CPOs:

```bash
dotnet add package DotOcpi
dotnet add package DotOcpi.AspNetCore
```

### Client Only

No ASP.NET Core — you only pull data from CPOs (e.g., a background worker):

```bash
dotnet add package DotOcpi
dotnet add package DotOcpi.Client
```

## Supported Frameworks

| Framework | Status |
|:----------|:-------|
| .NET 10.0 (LTS) | Recommended |
| .NET 8.0 (LTS) | Supported |

## Next Steps

[Quick Start](/DotOcpi/getting-started/quick-start/){: .btn .btn-primary }
