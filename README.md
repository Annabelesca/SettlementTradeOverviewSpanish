# Settlement Trade Overview

*Know what settlements are selling before you make the trip.*

Settlement Trade Overview is a read-only planning and information mod for RimWorld 1.6. It lets you inspect the current trade stock offered by world settlements before deciding where to send your caravan.

Browse eligible settlements together in one global overview, or inspect a specific settlement directly from the world map. Search, filter and sort offers, compare estimated prices, check distance and route availability, review restock information, and inspect relevant pawn trade details.

Settlement Trade Overview does not perform remote trades. Purchases still use RimWorld's normal caravan and trading systems.

## Features

- Browse trade stock from multiple eligible settlements in one global overview.
- Inspect a specific settlement directly from the world map.
- Search across items, pawns and settlements.
- Filter offers by category and sort by the available trade columns.
- See negotiator-aware estimated purchase prices, with market value used as a fallback when needed.
- Check settlement distance separately from ground-route availability.
- Review restock information and expected restock time when available.
- Inspect relevant pawn trade details directly in the list.
- Configure which settlements are included in the overview.
- Refresh trade data manually without forcing settlements to restock.
- Optionally highlight genepacks relevant to unfinished Xenogerm Planner plans.

## Trade data and refresh behavior

Settlement Trade Overview reads settlement trade stock through RimWorld's normal public trading systems and keeps its own read-only snapshot for presentation.

Use **Refresh** when you want the mod to read the current settlement trade data again. Refreshing does not force settlements to restock and does not change RimWorld's normal restock schedule.

On worlds with many eligible settlements, the initial load or a manual refresh may cause a brief pause while RimWorld generates trade stock that has not been generated yet.

## Requirements

- RimWorld 1.6
- No DLC required

Harmony is not required and is not included.

Royalty and Biotech are supported when installed, but neither is required for the core mod.

## Compatibility

Settlement Trade Overview is designed around the standard RimWorld 1.6 settlement and trading systems.

Mods that add ordinary trade goods should generally work normally. Mods that heavily replace settlement definitions, trader stock handling, trade grouping, or other core trading systems may not be fully compatible.

Settlement-specific access is guaranteed for the vanilla `Settlement` definition and compatible XML descendants. Independently defined settlement-like world objects are not guaranteed to receive the settlement trade-stock command automatically.

## Integrations

### [Xenogerm Planner](https://steamcommunity.com/sharedfiles/filedetails/?id=3781523927)

Xenogerm Planner integration is optional.

When both mods are installed and a compatible Planner API is available, Settlement Trade Overview can mark genepacks for sale that contain genes useful for plans that are not ready yet. Hover the relevance indicator to see which plans match the genepack.

Trade offers remain suggestions only and do not count toward plan readiness until the genepack is actually acquired.

Neither mod requires the other for its normal functionality.

## Languages

- English
- Russian
- Ukrainian

## Installation

For normal gameplay, installing Settlement Trade Overview through the Steam Workshop is recommended.

**Steam Workshop:** https://steamcommunity.com/sharedfiles/filedetails/?id=3781528628

This repository contains the source project and is not a ready-to-install mod folder. Development builds require the setup described in the [Development](#development) section below.

## Development

### Prerequisites

- RimWorld 1.6 installed locally.
- A build environment capable of targeting .NET Framework 4.7.2.
- A sibling checkout of `Escarval.RimWorld.UI`.

`Escarval.RimWorld.UI` is compiled into `SettlementTradeOverview.dll` as shared source. It is a development dependency, not a separate runtime mod dependency.

### Workspace layout

The current build expects the repositories to use this layout:

```text
RimWorldMods/
├── Escarval.RimWorld.UI/
└── SettlementTradeOverview/
```

### Local configuration

Copy:

```text
docs/SettlementTradeOverview.Local.props.example
```

to:

```text
src/SettlementTradeOverview.Local.props
```

Then configure:

- `RimWorldManagedDir` — RimWorld's `RimWorldWin64_Data/Managed` directory.
- `RimWorldModAssembliesDir` — the `Assemblies` directory of the local Settlement Trade Overview development installation.

The local props file contains machine-specific paths and is intentionally excluded from Git.

### Build

Build the production solution in Release configuration:

```bash
dotnet build src/SettlementTradeOverview.sln -c Release
```

The production project copies the built `SettlementTradeOverview.dll` to the configured `RimWorldModAssembliesDir` after a successful build.

### Tests

Run the Settlement Trade Overview test project with:

```bash
dotnet test tests/SettlementTradeOverview.Tests/SettlementTradeOverview.Tests.csproj -c Release
```

The deterministic NUnit suite covers project-owned query, eligibility, snapshot, cache, settings, presentation, runtime-boundary, and optional integration contracts. Full RimWorld and Unity behavior is validated separately in game.

## Repository structure

```text
assets/   Source visual assets
docs/     Architecture and testing documentation
mod/      RimWorld package content
src/      Production source and solution
tests/    NUnit test project
```

## Technical documentation

- [Architecture](docs/architecture.md)
- [Testing policy](docs/testing.md)

## Links

- Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3781528628
- GitHub Releases: coming soon