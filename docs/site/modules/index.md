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
| [Credentials](/DotOcpi/modules/credentials/) | Client + Server | Registration handshake | Token A &rarr; B &rarr; C lifecycle |
| [Locations](/DotOcpi/modules/locations/) | Receiver | CPO pushes / eMSP pulls | Charging station data |
| [Sessions](/DotOcpi/modules/sessions/) | Receiver | CPO pushes / eMSP pulls | Active charging sessions |
| [CDRs](/DotOcpi/modules/cdrs/) | Receiver | CPO pushes / eMSP pulls | Charge detail records |
| [Tariffs](/DotOcpi/modules/tariffs/) | Receiver | CPO pushes / eMSP pulls | Pricing information |
| [Tokens](/DotOcpi/modules/tokens/) | Sender + Receiver | eMSP pushes / CPO authorizes | EV driver tokens |
| [Commands](/DotOcpi/modules/commands/) | Sender | eMSP &rarr; CPO | Start/stop, reserve, unlock |
| [Charging Profiles](/DotOcpi/modules/charging-profiles/) | Sender | eMSP &rarr; CPO | Smart charging (2.2+) |
