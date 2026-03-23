---
title: Modules
layout: default
nav_order: 4
has_children: true
---

# OCPI Modules

Deep dive into each OCPI module supported by DotOcpi.
{: .fs-6 .fw-300 }

DotOcpi implements all eMSP-side OCPI modules:

| Module | eMSP Role | Direction | Description |
|:-------|:----------|:----------|:------------|
| [Credentials](credentials) | Client + Server | Registration handshake | Token A &rarr; B &rarr; C lifecycle |
| [Locations](locations) | Receiver | CPO pushes / eMSP pulls | Charging station data |
| [Sessions](sessions) | Receiver | CPO pushes / eMSP pulls | Active charging sessions |
| [CDRs](cdrs) | Receiver | CPO pushes / eMSP pulls | Charge detail records |
| [Tariffs](tariffs) | Receiver | CPO pushes / eMSP pulls | Pricing information |
| [Tokens](tokens) | Sender + Receiver | eMSP pushes / CPO authorizes | EV driver tokens |
| [Commands](commands) | Sender | eMSP &rarr; CPO | Start/stop, reserve, unlock |
| [Charging Profiles](charging-profiles) | Sender | eMSP &rarr; CPO | Smart charging (2.2+) |
