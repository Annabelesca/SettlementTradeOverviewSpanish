
# Settlement Trade Overview architecture

This document defines the accepted product architecture and implementation boundaries for Settlement Trade Overview.

The project provides a validated pre-release baseline for its Harmony-free read-only trade overview, persisted settings, snapshot/cache lifecycle, source-only shared UI connection, localized presentation, optional Planner integration and normalized visual assets. The implemented baseline includes the global MainTab overview, settlement-specific stock window, native settlement command, canonical runtime-free genepack composition snapshots, optional soft binding to Xenogerm Planner API version `1`, transient relevance projection, shared sortable Details presentation, instance-aware item presentation through a transient runtime target cache, independent distance and ground-route state, and snapshot-time restock timestamps relative to the active trade origin. Compatible loaded snapshots can be reused without full settlement rediscovery, Planner relevance remains outside stock ownership, and the current automated suite protects the deterministic contracts. English, Russian and Ukrainian resources are complete, the final ModIcon, MainButton and `PlannerRelevance` assets are integrated, and the agreed build, runtime compatibility and regression matrix has been validated successfully. There is no active implementation stage in the current pre-release development plan.

The legacy prototype remains the behavioral reference for capabilities explicitly selected for migration, but its internal structure is not an architectural requirement for the rebuilt implementation.

This is an architecture specification, not a RoadMap or implementation guide.

## Sources of truth

The project uses the following source hierarchy:

1. The installed RimWorld 1.6 implementation and verified source analysis define vanilla runtime behavior.
2. The current production implementation defines the behavior that is actually available in the built mod.
3. This document defines accepted product and architecture decisions for Settlement Trade Overview.
4. `docs/testing.md` defines automated testing and runtime acceptance boundaries.
5. The public Xenogerm Planner integration API version `1` contract defines the optional contract consumed by the implemented soft-binding adapter.
6. The legacy prototype source defines only previously implemented behavior selected for migration; it does not define the current architecture.

Vanilla behavior must not be inferred from the legacy implementation when it contradicts the installed game implementation.

Unresolved decisions must remain explicit rather than being encoded indirectly in implementation details.

## Project identity

The accepted project identity is:

* display name: `Settlement Trade Overview`;
* project, assembly and root namespace: `SettlementTradeOverview`;
* package ID: `escarval.settlementtradeoverview`;
* localization prefix: `STO`.

The legacy names `Global Trade Overview`, `GlobalTradeOverview`, `GTO` and `WFS` are not compatibility boundaries because the prototype was not publicly released.

## Product model

Settlement Trade Overview is a read-only planning and information mod for inspecting trade stock exposed by world settlements.

The intended high-level flow is:

```text
world settlements and trader state
        ↓
settlement eligibility and trader discovery
        ↓
public vanilla stock access
        ↓
project-owned inventory snapshots
        ↓
search, category, filter and sorting projection
        ↓
global overview and settlement-specific presentation
```

The project does not own vanilla trader stock and must not treat cached presentation data as authoritative game state.

Remote stock inspection does not imply that the player can complete a trade through this interface.

## First-release product scope

The accepted first-release scope contains the read-only trade overview core:

* a global settlement-stock overview;
* a settlement-specific stock window;
* item categories;
* search across items, pawns and settlements;
* deterministic sorting;
* item count presentation;
* negotiator-aware estimated prices with a market-value fallback;
* settlement distance;
* restock information;
* manual data refresh;
* supported settlement-eligibility filters;
* settings for implemented and supported behavior;
* an optional Xenogerm Planner relevance indicator for settlement genepacks in both the global and settlement-specific trade lists, using an STO-owned custom icon in the sortable Details column and a tooltip listing matching plans when a compatible Planner API is available.

The first release does not include immediate generation of replacement settlement stock or automatic dirty-cache refresh based on that generation. Xenogerm Planner remains optional: the overview must preserve its complete core behavior when that mod or a compatible API is absent.

Trader-specific partial snapshot refresh is not part of the first release. Representative profiling did not demonstrate a need for it to remove constant UI overhead; it may be reconsidered only if a new measured post-release requirement justifies the additional lifecycle complexity.

A continuously updating restock timer is not part of the first-release scope.

## Architectural boundaries

The rebuilt project separates the following concerns:

```text
TRADER DISCOVERY
────────────────────────
world settlements
+ product eligibility rules
→ eligible trader sources


VANILLA STOCK ACCESS
────────────────────────
eligible ITrader source
+ public Goods / restock metadata
→ short-lived live runtime values


INVENTORY SNAPSHOT
────────────────────────
a single public stock read
+ immediate value conversion
→ project-owned immutable snapshot data


QUERY AND PRESENTATION
────────────────────────
snapshots
+ search / category / filters / sort
→ deterministic presentation projection


CACHE AND LIFECYCLE
────────────────────────
runtime context
+ full cache key
+ lightweight reuse key
+ explicit invalidation rules
→ current derived snapshot set


RIMWORLD INTEGRATION
────────────────────────
game entry points
+ settlement access
+ pricing / distance / restock adapters
→ bounded runtime dependencies


OPTIONAL PLANNER INTEGRATION
────────────────────────
runtime-free genepack composition
+ compatible versioned Planner API
→ transient matching-plan projection


UI APPLICATION
────────────────────────
generic shared UI API
+ cached project-owned projection metadata
+ cached row presentation
+ optional matching-plan projection
→ mod windows and dialogs
```

These concerns must remain separate.

In particular:

* UI drawing code must not own trader discovery, filtering semantics or cache invalidation rules;
* project-owned snapshots must not be persisted as an alternative source of truth for vanilla trader stock;
* presentation code consumes project-owned projections and must not reconstruct business rules locally;
* query and UI layers must not call `ITrader.Goods` directly;
* live `Thing`, `Pawn`, `Tradeable` and other runtime stock objects must not become long-lived snapshot or row-presentation state;
* runtime target caches must remain inside RimWorld integration adapters;
* optional DLC behavior must be isolated from the core stock-overview model;
* experimental prototype behavior is not migrated automatically;
* Planner relevance must remain outside authoritative stock cache keys and must not cause a new `ITrader.Goods` read by itself;
* optional integration must consume the documented Planner API and must not reproduce readiness semantics locally;
* the shared UI source must remain generic and must not own trade or genetics business rules.

## Legacy prototype migration boundary

The legacy prototype provides a behavioral reference for:

* the global settlement-stock overview;
* the settlement-specific stock window;
* categories, search and sorting;
* item count, price, settlement, distance and restock information;
* configurable settlement eligibility rules;
* snapshot and cache behavior;
* experimental immediate-restock and dirty-cache handling.

Migration is selective.

The accepted migration classification is:

* the read-only overview capabilities belong to the first-release scope;
* the global and settlement-specific presentations are retained but redesigned;
* settlement access is implemented through the verified native `WorldObjectComp` boundary;
* current settlement stock is read through the public `ITrader.Goods` contract;
* vanilla lazy generation caused by public `Goods` access is accepted as part of that public contract;
* immediate-restock generation and its dirty-cache workflow are deferred;
* trader-specific partial refresh is excluded from the first release after representative profiling did not justify it for constant UI performance;
* the existing class layout, static state, Harmony patches and reflection-based access are not migration targets by themselves.

## Domain and runtime boundary

Project-owned deterministic rules should be represented through types that can be exercised without constructing the complete RimWorld runtime.

This includes, where practical:

* settlement eligibility criteria;
* settings normalization and conversion to eligibility criteria;
* category assignment;
* search and sorting rules;
* presentation values and fallback states;
* runtime-free pawn trade details, including purchase outcome and rideable state;
* snapshot aggregation;
* full cache-key and lightweight reuse-key compatibility decisions;
* cache invalidation decisions.

Direct access to `Find`, `TradeSession`, `ITrader`, `Settlement`, `Pawn`, `Thing` and other live RimWorld types should be concentrated in adapters and composition roots.

A trader snapshot adapter may use live stock objects only during one synchronous conversion operation. It must copy required values immediately and must not expose the live collection or its objects as the project-owned source of truth.

RimWorld `1.6.9676.17735` source analysis and the completed runtime validation confirm that each supported vanilla and current STO genepack trade row has one gene composition relative to `Tradeable.AnyThing`. Every candidate genepack is compared with that representative through `TransferableUtility.TransferAsOne`; the gene collections must be set-equal and their resolved `GeneSet.Label` values must match before the generic stacking conditions are applied. Gene order and duplicate multiplicity are not part of composition identity, although labels and other stacking characteristics may split equivalent compositions into separate rows.

The implemented snapshot boundary copies one canonical runtime-free composition from a genepack row's representative during that same synchronous conversion. The canonical representation contains distinct non-empty `GeneDef.defName` values in deterministic ordinal order. Non-genepack entries contain no genetics state. If the representative is not a valid `Genepack`, its `GeneSet` is unavailable, the composition is empty or no valid definition names remain, the ordinary trade row is preserved without composition metadata and is not eligible for Planner relevance. Malformed composition data is isolated from unrelated rows and traders.

Pawn purchase outcome and caravan rideability are copied into `PawnTradeDetailsSnapshot` during that same conversion boundary. Presentation code consumes the copied enum and numeric values and must not reconstruct a live `Tradeable` or retain a `Pawn` to reproduce vanilla trade-list metadata.

Runtime references used for icons, native tooltips, navigation and info cards may be cached by game- or world-scoped integration adapters. The implemented trade-entry runtime target cache binds an immutable `TradeEntryIdentity` to a representative live `Thing` only for the lifetime of the corresponding stock snapshot. It restores instance-aware item icons, tooltips and info cards while preserving a safe `ThingDef` fallback. These references are cleared with snapshot invalidation or rebuild and are never copied into immutable snapshots or `TradeListRowPresentation`.

Distance snapshots store approximate tile distance independently from the ground-route state. A settlement can therefore retain a numeric distance while reporting that vanilla did not find a traversable ground route. Eligibility may still exclude that settlement when the reachable-route setting is enabled, while presentation uses a warning icon rather than replacing the distance with an absolute unreachable label.

Scheduled restock snapshots preserve the game tick together with a runtime-free absolute tick and world coordinates derived from the active trade origin when those values can be calculated. Presentation formats the expected date and time relative to that origin and remains fixed to the snapshot refresh moment; it does not introduce a continuously updating timer.

The architecture should not introduce abstractions for trivial API forwarding. A boundary is justified when it protects product semantics, lifecycle ownership, performance ownership or testability of non-trivial rules.

## Vanilla settlement stock lifecycle

Vanilla owns settlement stock through `Settlement_TraderTracker`.

The verified lifecycle is:

```text
tracker exists with no stock
        ↓ public ITrader.Goods access
vanilla lazy stock generation
        ↓
active stock with NextRestockTick
        ↓ scheduled tracker tick after expiry
old stock destruction
        ↓
no active stock
        ↓ next public ITrader.Goods access
vanilla lazy stock generation
```

The public `ITrader.Goods` getter is side-effectful for settlements. It generates stock when the internal stock container is absent or empty. This behavior is part of the public vanilla access contract and is accepted by the project.

Settlement Trade Overview must not:

* call `RegenerateStock` or `TryDestroyStock` directly;
* modify the vanilla restock schedule;
* patch settlement stock destruction or regeneration;
* inspect private tracker fields through reflection;
* retain live stock objects after snapshot conversion.

The mod may:

* read public `ITrader` and `ITraderRestockingInfoProvider` values;
* perform one public `Goods` access while building a trader snapshot;
* accept the resulting vanilla lazy generation;
* immediately convert live values into project-owned snapshot data;
* repeat that public read during an explicit project snapshot rebuild.

A compatible loaded project snapshot may be reopened without another `Goods` read. A full public stock capture still occurs for the first build, manual refresh or an incompatible runtime context.

`CanTradeNow` is not evidence that stock already exists. A settlement can report that it can trade while its stock has not yet been generated or has been destroyed after expiry.

The initial full build and manual refresh remain synchronous. When many eligible settlements require vanilla lazy stock generation, that operation can cause a temporary performance hitch. This is an accepted first-release limitation to be documented for users, not a reason to introduce private lifecycle control.

## Project snapshot and cache lifecycle

Trade inventory snapshots are derived project-owned runtime state, separate from the vanilla lifecycle.

The implemented project lifecycle contains:

* complete first snapshot construction;
* reuse of a valid snapshot through the full cache key;
* immediate reopen reuse through a lightweight compatibility key;
* explicit manual full refresh;
* complete invalidation after relevant game lifecycle or eligibility-setting changes;
* failure isolation between traders.

`TradeInventorySnapshotService` is the application boundary for obtaining, reusing, refreshing and invalidating the current snapshot.

The full cache key includes:

* active map and origin;
* selected negotiator snapshot;
* settlement eligibility criteria;
* effective powered-communications state when that filter is enabled;
* effective Royalty-active state when that filter is enabled;
* ordered eligible trader identities;
* discovery failure state.

The lightweight reuse key contains only the base runtime context required to decide whether a loaded snapshot may be shown again without full settlement discovery. It does not contain trader identities or discovery failures. Before reuse, the service also verifies that settlements already represented by the snapshot can still be resolved.

A compatible global reopen can therefore reuse the loaded snapshot without a mandatory loading frame, full eligibility discovery or new stock reads. A settlement-specific window may reuse the same snapshot only when its target trader is already present; otherwise it falls back to the full discovery path.

`GameComponent_SettlementTradeOverview` invalidates derived snapshot state when a new game starts or a save is loaded. Eligibility-setting changes invalidate an incompatible snapshot through the settings service. UI-only global-tab visibility does not alter settlement-specific access.

The cache stores only immutable project-owned snapshot data. It exposes explicit `NotLoaded`, `Loading`, `Available`, `Empty`, `Unavailable`, `Partial` and `Failed` lifecycle states and does not persist its snapshot or keys through Scribe.

Manual refresh means:

```text
discard current project-owned snapshot
        ↓
repeat full context and trader discovery
        ↓
repeat public trader reads
        ↓
accept any vanilla lazy generation caused by Goods
        ↓
rebuild project-owned snapshot values
```

Manual refresh does not request an immediate restock, advance game time, call private tracker methods or alter vanilla schedule fields.

The implemented lifecycle defines project behavior for:

* active map or origin changes;
* selected negotiator changes;
* eligibility-setting changes;
* powered-console state when required;
* Royalty-active state when the Royalty filter is required;
* settlement removal or unavailability;
* new-game and load transitions;
* failed trader reads;
* a public `Goods` read that returns no displayable entries.

Derived snapshots must not be written to save data unless a later product requirement explicitly justifies persistence.

Trader-specific partial refresh is not part of the first-release lifecycle. Representative profiling showed that cached projection metadata, cached row presentation, runtime target caches and lightweight reopen reuse are sufficient to remove the measured constant UI regression. Partial refresh may be reconsidered only for a new measured post-release requirement.

Planner relevance is a separate transient presentation projection. It is not persisted, is not part of `TradeInventorySnapshot`, the full cache key or the lightweight reuse key, and does not invalidate stock data when plans change. An already open window is not refreshed by cross-mod events. When a compatible cached stock snapshot is opened again, relevance is queried again from the current Planner state without rediscovering settlements or reading `ITrader.Goods`.

Failure to refresh one trader should not corrupt unrelated cached data or block access to the rest of the overview.

## Pricing and negotiator boundary

Displayed prices are derived estimates based on current runtime context.

The pricing layer must explicitly distinguish:

* a valid negotiator-specific estimate;
* a market-value fallback when no valid negotiator is available;
* unavailable or structurally invalid price data.

The rule for selecting the negotiator belongs to project-owned policy and must not be duplicated in individual windows. Both full context construction and lightweight reuse validation use the same negotiator-selection logic.

Royalty-aware title and permit requirements belong to the first-release settlement eligibility scope. They remain an optional-DLC RimWorld integration concern rather than part of the generic inventory model, and core behavior must remain available when Royalty is not active.

## Settlement integration and Harmony policy

Static analysis of RimWorld 1.6 confirms the settlement-specific integration boundary:

```text
WorldObjectDef Settlement
        ↓
XML patch adds WorldObjectCompProperties_SettlementTradeOverview to comps
        ↓
stateless WorldObjectComp
        ↓
WorldObject.GetGizmos()
        ↓
Command_Action
```

The settlement command uses the same native `WorldObjectComp.GetGizmos()` lifecycle as vanilla settlement components. A Harmony patch for `Settlement.GetGizmos()` is not required and must not be introduced. The implemented command is enabled for an eligible settlement, disabled with a localized reason for a structurally supported potential trader that currently fails eligibility, and hidden for player-owned or structurally unsupported objects.

The integration component must remain thin:

* it resolves its current `Settlement` parent;
* it delegates eligibility and command policy to project-owned services;
* it returns the resulting native command;
* it does not read trader stock, own cache lifecycle or contain trade business rules;
* it does not persist a separate settlement reference or project state.

Classes derived from RimWorld types follow the game's native naming pattern. The accepted properties-class name is `WorldObjectCompProperties_SettlementTradeOverview`.

The XML patch targets the existing `comps` list of the vanilla `Settlement` `WorldObjectDef`. Vanilla XML descendants of that Def inherit the component, while independent modded settlement Defs are not guaranteed to be covered automatically.

The verified public `ITrader.Goods` boundary is also sufficient for approved first-release stock access. No approved first-release behavior requires Harmony or reflection.

Harmony has been removed from the production build, local configuration and mod metadata. Clean Debug and Release builds, the resulting assembly references and runtime loading without Harmony have been validated.

## Shared UI API boundary

Reusable UI infrastructure is implemented as the compile-time/shared-source sibling project `Escarval.RimWorld.UI`. Settlement Trade Overview already resolves that external project through relative build-time paths and compiles its canonical source directly into `SettlementTradeOverview.dll`.

The accepted pre-release structure is:

```text
RimWorldMods/
├── Escarval.RimWorld.UI/
├── SettlementTradeOverview/
└── XenogermPlanner/
```

Settlement Trade Overview includes the shared source files directly in its production assembly through relative build-time paths. The project reference remains an IDE/build boundary with `ReferenceOutputAssembly="false"`; no separate runtime UI DLL or mandatory user-installed framework mod is required. The sibling-directory arrangement is the current pre-release build contract. A possible Git submodule migration is post-release organizational work and is not part of the accepted first-release implementation scope.

The implemented shared UI project owns generic RimWorld IMGUI building blocks such as:

* style and metric registries;
* project-owned panels and controls;
* search fields and sortable headers;
* scrolling and list-layout helpers;
* visible-range calculations for practical list virtualization;
* contextual icon actions and tooltips;
* safe restoration of global `Text`, `GUI` and related IMGUI state.

It must not own:

* trade-specific models or filtering rules;
* genetics-specific models from Xenogerm Planner;
* persistence for either consuming mod;
* direct lifecycle ownership for a consuming mod's windows;
* localization keys owned by a consuming mod.

The shared UI API remains driven by concrete consuming-project needs rather than a speculative universal framework. Canonical deterministic helpers are covered by `Escarval.RimWorld.UI.Tests` through the standalone `../../Escarval.RimWorld.UI/src/Escarval.RimWorld.UI.sln`, and STO retains an assembly-boundary test proving that shared types are compiled into its production assembly. The STO solution retains the shared production project only for navigation and build ordering, without changing the source-only compilation model.

The implemented shared icon extension provides consumer-neutral tint support for icons rendered directly by project code. Code-rendered monochrome textures use normalized white source pixels and receive an explicit semantic tint from the consumer; disabled actions use the shared disabled-tint contract. Multicolor textures retain source-defined colors and are drawn with a neutral white tint, while RimWorld-owned Def, metadata and gameplay icons remain under their native rendering paths. Trade- and genetics-specific colors and presentation semantics remain in the respective mods.

## Optional Xenogerm Planner integration boundary

The accepted first-release architecture enriches genepack trade rows through a compatible Xenogerm Planner API, while the integration remains optional and read-only. STO must load, browse and refresh settlement stock normally when Planner is absent, inactive, unavailable or exposes an incompatible API version.

The accepted dependency direction is:

```text
Settlement Trade Overview runtime-free genepack composition
        ↓ optional soft binding
versioned public Xenogerm Planner API
        ↓
API status + stable PlanId + DisplayName matches
        ↓
STO transient row presentation
```

STO must not reference Planner implementation types as a mandatory compile-time or runtime dependency and must not inspect private Planner state. The implemented API version `1` is discovered through limited soft binding to `XenogermPlanner.Api.XenogermPlannerApi, XenogermPlanner`. The adapter reads the public static `ApiVersion` property before resolving and invoking `QueryGenepackRelevance`, and it supports only the explicitly known value `1`. A value greater than `1` is not assumed compatible. Expected absence of the optional mod, unavailable Planner context or an unsupported API version is not an error condition. The exact request, response and status contract is defined by the public Xenogerm Planner integration API version `1`.

Planner owns relevance semantics. STO supplies the composition and displays the returned matches. The accepted semantics are:

* Coverage considers an offered pack relevant when it contains at least one Planner-defined missing target gene; additional offered genes are allowed;
* Exact payload requires no offered genes outside the plan target and at least one Planner-defined `Missing` or `ExactPayloadConflict` target gene;
* only `NotReady` plans are returned;
* `Ready`, `EmptyTarget`, `Degraded` and `Unavailable` plans are excluded;
* prerequisite-only genepacks are not returned;
* trade offers do not become Planner product inventory and do not satisfy readiness.

The response keeps stable plan IDs even though the user-facing tooltip displays names. Planner's accepted unique-name policy reduces visual ambiguity but does not replace stable identity.

The integration projection is transient and presentation-owned. Plan changes do not invalidate stock snapshots, do not alter full or lightweight cache keys and do not update an already open window. Reopening a window with a reusable stock snapshot performs a new relevance projection without another public stock read.

The indicator is rendered through the shared `TradeListView`, so the same behavior appears in the global overview and the settlement-specific stock window. It uses an STO-owned custom SVG source converted to a packaged PNG texture and is placed in the existing Details column. The Details header participates in the existing sortable-header and deterministic query boundaries. Sorting covers Planner match count, colonist and slave purchase outcomes, rideable speed factor and neutral rows through one query-owned sort-key contract. Rows without matches, rows without valid composition metadata and all non-genepack rows retain the ordinary presentation. Absence or incompatibility of the optional integration does not require a user-facing warning.

## UI application boundary

Settlement Trade Overview windows own transient interaction state such as:

* active category;
* search text;
* selected sort mode and direction;
* scroll position;
* loading, empty, unavailable and error presentation states.

The implemented global and settlement-specific windows consume the same project-owned query layer, presentation models, shared controls, category tabs, negotiator summary and trade-list boundary. The relevance indicator is implemented once in the existing Details column of this common `TradeListView` boundary so both surfaces remain behaviorally consistent. The Details header participates in the same sortable-state and deterministic query contracts as the other supported columns.

UI state must be separate from vanilla trader stock and from authoritative cache data.

UI windows must not read `ITrader.Goods` directly. Manual refresh and compatible-snapshot reuse are requested through the application/cache boundary.

Large lists use project-owned projection and practical virtualization so drawing work scales with visible content rather than the complete stock set. Category tabs are derived from the current search-matching projection before applying the active category. The shared list exposes one Details column for Planner relevance or pawn purchase-outcome and rideable metadata rather than introducing a separate genetics column. Its visibility and sorting are based on the current filtered projection and the complete set of values rendered in that column.

The UI owns cached projection metadata and immutable row presentation derived from a specific snapshot and query state. Count, price, distance, restock, relevance and static row tooltip strings are prepared when the projection changes rather than during every IMGUI draw event. Runtime `Pawn`, `Settlement`, representative `Thing` and `ThingDef` targets are resolved only through integration caches for visible interactions and are not stored in row presentation.

Contextual row tooltips should be registered only when the corresponding cell or action is hovered where the current shared-control contract permits it. Native item and pawn tooltips remain available on hover.

Localized labels of different lengths remain part of layout design. The implemented English, Russian and Ukrainian resources use the same window, shared-control and presentation boundaries.

## Settings boundary

Mod settings are mod-owned persisted configuration and are implemented only for supported first-release behavior.

The implemented settings are:

* global tab visibility, enabled by default;
* powered communications-console requirement, enabled by default;
* Industrial-or-higher technology requirement, enabled by default;
* optional maximum-distance filtering, enabled by default;
* maximum distance in world tiles, default `40` and normalized to the supported range up to `3000`;
* reachable-settlement requirement, enabled by default;
* Royalty-aware trade-permission requirement, disabled by default and safely ignored when Royalty is inactive.

The powered-console rule is centralized and accepts a powered `Building_CommsConsole` on the current map or another player-home map. Other maps are identified through `Map.IsPlayerHome`; the rule does not use `ParentFaction` as a home-map proxy.

Settings are normalized and converted to the existing `SettlementEligibilityCriteria` by one settings policy. Eligibility changes invalidate incompatible snapshot state and update the cache/reuse compatibility context. Global-tab visibility remains an independent UI setting and does not disable the settlement-specific native command.

Immediate-restock generation, automatic dirty-cache refresh, trader-specific partial refresh and a continuously updating restock timer are not exposed as settings.

Dependencies between settings belong to the settings policy boundary and must not be reimplemented independently in UI and runtime services.

## Localization boundary

The accepted first-release languages are:

* English;
* Russian;
* Ukrainian.

English, Russian and Ukrainian localization is implemented for the overview, settlement entry point, settings, Planner relevance, Details, distance, restock presentation and disabled reasons. The three language sets use matching Keyed and DefInjected contracts, including equivalent placeholders and markup.

Player-facing wording has been audited for clarity and unnecessary implementation terminology. Exact API and architecture terminology remains technical where precision is required.

All STO localization keys use the `STO.*` prefix.

Chinese Simplified localization is not part of the first-release scope. The legacy translation is not migrated because it uses obsolete identifiers and does not match the current string set. Any future Chinese Simplified localization would be separate post-release work and is not currently committed.

## Project and build boundary

The project uses:

* the `src/SettlementTradeOverview.sln` solution;
* an SDK-style C# production project targeting `net472` under `src/SettlementTradeOverview`;
* the `SettlementTradeOverview.Tests` project under `tests/SettlementTradeOverview.Tests`;
* a sibling `Escarval.RimWorld.UI` project resolved through relative build-time paths;
* packaged mod content under `mod/`;
* machine-specific paths in `src/SettlementTradeOverview.Local.props`;
* a tracked template at `docs/SettlementTradeOverview.Local.props.example`;
* post-build development deployment to the configured mod Assemblies directory.

The normalized solution entry resolves the STO test project under `tests/SettlementTradeOverview.Tests`, and that project references production through `../../src/SettlementTradeOverview/SettlementTradeOverview.csproj`. The shared UI test project is owned by its standalone solution rather than the STO solution. Assembly names, namespaces, source-only shared compilation and runtime package contents remain unchanged.

Local RimWorld and deployment paths must not be embedded in tracked project files.

`src/SettlementTradeOverview.Local.props` is intentionally local and ignored by Git.

The production project, local configuration, tracked configuration template and mod metadata are Harmony-free. The external shared-project path, source-only compilation and assembly boundary are implemented. Current Debug and Release build validation confirms the expected assembly references and the absence of a runtime dependency on a standalone `Escarval.RimWorld.UI.dll`. Release packaging remains a separate distribution process and must preserve this boundary.

## Performance boundary

Representative profiling covered the global `All` view under an extreme configuration and compared the rebuilt implementation with the legacy prototype on the same system. After caching projection metadata, runtime targets, row presentation and compatible snapshot reopen state, no measurable TPS or FPS difference remained between the two versions within the tested system limits.

These measurements validate the current architecture and remove trader-specific partial refresh from the first-release plan. They are not universal workstation-independent performance guarantees.

The known performance risk is temporary synchronous work during first snapshot construction or manual refresh when public `ITrader.Goods` causes RimWorld to generate stock for many eligible settlements. The project accepts this public vanilla behavior as a release performance consideration. Eligibility filters can reduce the number of processed settlements, but the mod does not impose a universal settlement-count limit or manipulate private stock generation.

## Resolved scope decisions

The following first-release scope decisions are established:

* Royalty-aware settlement eligibility is included and must degrade safely when Royalty is not active;
* first-release localization is limited to English, Russian and Ukrainian;
* Chinese Simplified localization is outside the first-release scope and is not a committed post-release deliverable;
* persisted settings are limited to implemented UI visibility and settlement-eligibility behavior;
* compatible loaded snapshots may be reused without full settlement rediscovery;
* trader-specific partial refresh is not required for the first release based on current profiling;
* synchronous initial/manual stock generation hitches are documented as a performance consideration rather than addressed through private vanilla lifecycle control;
* Xenogerm Planner integration is optional, read-only and presentation-only;
* Planner relevance is recomputed on window reopen over reusable stock data and never participates in stock cache invalidation;
* the indicator is shown in both global and settlement-specific lists through the common trade-list boundary;
* the relevance indicator uses an STO-owned custom SVG source converted to a packaged PNG and is placed in the sortable Details column;
* each supported genepack row has one canonical composition copied from `Tradeable.AnyThing` as distinct non-empty `GeneDef.defName` values in deterministic ordinal order;
* malformed, empty or unavailable genepack composition data preserves the ordinary trade row but produces no composition metadata or relevance;
* source-derived vanilla grouping behavior and the accepted single-composition snapshot contract have been confirmed through the completed runtime compatibility matrix;
* approximate distance and ground-route reachability are independent snapshot values, with route failure presented as a warning beside the numeric distance;
* expected restock time is a snapshot-time value formatted relative to the active trade origin rather than the trader settlement;
* code-rendered monochrome icons use white source textures and code-defined tint, while multicolor assets retain source-defined colors;
* the final primary icon designs, normalized SVG source library and regenerated packaged textures are part of the implemented baseline;
* the external sibling shared UI source is compiled into the STO assembly and is not a user-installed dependency.