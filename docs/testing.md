
# Settlement Trade Overview testing policy

This document defines automated testing and runtime acceptance boundaries for Settlement Trade Overview.

The project contains the NUnit-based `SettlementTradeOverview.Tests` project. The current full automated suite passes for the implemented baseline after the composition, optional Planner integration, Details, runtime-target, distance, restock, tint-aware icon, localization and normalized asset changes. The suite covers the project smoke pipeline, the shared-source consumer assembly boundary, domain snapshots, query semantics, search-sensitive category availability, settlement eligibility, settings defaults and policy, command-state policy, live-to-snapshot transformations, canonical genepack compositions, pawn trade details, optional Planner binding and transient projection, full Details sorting, runtime target registry behavior, presentation policies, full cache-key semantics, lightweight reuse-key compatibility and cache lifecycle contracts. Generic UI helper contracts belong to the canonical `Escarval.RimWorld.UI.Tests` project. The final SVG sources and regenerated packaged PNG textures are part of the current baseline. The agreed in-game compatibility, optional-integration, asset, localization and regression scenarios have also been completed successfully.

This is a testing policy, not a product architecture specification, RoadMap or vanilla implementation reference.

## Testing principle

Settlement Trade Overview uses a risk-based, contract-oriented testing approach.

The primary question before adding or retaining an automated test is:

> Which meaningful Settlement Trade Overview-owned contract does this test protect, and which realistic regression can it detect?

Automated tests should protect behavior where a regression can silently change which settlements or goods are shown, produce incorrect ordering or pricing presentation, break cache or settings lifecycle rules, or expose live runtime state across project-owned boundaries.

Test count and broad method-level coverage are not goals by themselves.

A test should not be retained only because the corresponding class or method exists.

## Test project boundary

The automated test boundary is implemented through the dedicated `SettlementTradeOverview.Tests` project under `tests/SettlementTradeOverview.Tests`, included in `src/SettlementTradeOverview.sln`. Its assembly name, namespace and behavioral ownership are unchanged.

The current foundation provides:

* NUnit-based execution through `Microsoft.NET.Test.Sdk` and `NUnit3TestAdapter`;
* Debug and Release solution integration;
* a reference to the production project;
* a minimal smoke test proving that the test pipeline runs;
* contract-oriented fixtures added together with project-owned implementation stages.

The smoke test is infrastructure validation and is not presented as product behavior coverage.

The full current suite is green on the current build. Runtime acceptance remains separate because the deterministic host does not reproduce the complete RimWorld, Verse or Unity runtime.

The normalized test-project location and solution membership preserve all existing fixtures and discovery behavior. Generic shared tests remain outside STO and the runtime acceptance boundary is unchanged.

Tests that require reflection over production types must remain within the assemblies available to the deterministic test host. A test must not force Unity or the full game runtime to load merely to inspect a UI-only implementation detail that belongs to runtime acceptance.

## What automated tests protect

### Core query semantics

Project-owned deterministic query behavior should be tested thoroughly.

This includes:

* category assignment;
* item, pawn and settlement search semantics;
* filter composition;
* all supported sorting modes and directions, including the implemented Details ordering across Planner relevance, colonist and slave outcomes, rideable factors and neutral rows;
* deterministic tie-breaking;
* stable behavior for empty and unavailable data;
* distinction between negotiator-specific price estimates and market-value fallbacks;
* search-sensitive category availability before active-category filtering.

A regression that hides a valid trade entry, includes an ineligible settlement, changes a deterministic order or misrepresents a price state requires automated protection.

Category-availability tests protect the direct unsorted scan contract. They do not benchmark IMGUI performance, but they should detect a semantic regression if category metadata stops matching the search projection.

### Settlement eligibility policy

When eligibility rules are project-owned, tests should protect combinations such as:

* trade availability;
* faction hostility;
* technology requirements;
* communication-console requirements;
* maximum distance;
* world reachability;
* Royalty-aware title and permit eligibility when Royalty is active, with safe core behavior when it is absent.

The tests should exercise project-owned policy from supplied boundary data. They must not claim to prove how RimWorld discovers settlements, identifies player-home maps or calculates paths internally.

The powered-console map scope and live `Building_CommsConsole` power state require runtime validation. Automated tests protect the criteria and settings policy that decide whether the requirement is enabled.

### Snapshot and aggregation contracts

Tests should cover non-trivial transformation from runtime boundary data into project-owned snapshots.

This includes:

* grouping equivalent tradeable objects under the selected project rule;
* runtime-free genepack composition projection according to the verified vanilla trade-row grouping policy;
* currency separation;
* pawn handling, including runtime-free purchase-outcome and rideable metadata;
* per-trader and global aggregation;
* preservation of trader identity where required;
* count and restock metadata projection, including runtime-free expected-restock moment data when available;
* independent distance and ground-route state, including numeric distance preservation for settlements without a traversable ground route;
* isolation of malformed or unavailable trader data;
* deterministic snapshot output for equivalent input;
* immediate conversion from supplied runtime values into project-owned values;
* absence of live `Thing`, `Pawn`, `Genepack`, `GeneDef`, `GeneSet`, `Tradeable` or mutable vanilla collections from long-lived snapshot state;
* safe absence of genetics composition metadata for non-genepack entries;
* one canonical composition per supported genepack row, using distinct non-empty gene definition names in deterministic ordinal order;
* equivalent canonical output for reordered genes or duplicate multiplicity;
* preservation of ordinary trade-row data with absent composition metadata when the representative, `GeneSet` or resulting canonical composition is malformed, unavailable or empty;
* independent support for several trade rows that expose the same canonical composition.

The immutable snapshot boundary remains a meaningful automated contract. UI-owned row presentation is a runtime-facing presentation implementation and must not be tested through reflection in a way that requires loading Unity modules in the deterministic test host.

### Trader adapter contracts

Automated tests may protect the project-owned behavior surrounding a supplied trader boundary without claiming to simulate vanilla stock generation.

Meaningful contracts include:

* one stock-capture operation per trader snapshot build;
* immediate conversion of each supplied live entry into project-owned values;
* extraction of genepack gene def names only during that conversion boundary;
* no later `ITrader.Goods` access from query or UI layers;
* correct treatment of empty, malformed and failed reads, including preserving ordinary row data when only genepack composition extraction fails;
* preservation of public restock metadata and trade-origin expected-time inputs in the resulting snapshot state;
* registration of transient representative item targets without placing live `Thing` values in snapshots;
* safe invalidation and fallback when a representative target is missing or destroyed;
* safe rejection of a trader that becomes unavailable before conversion completes.

Tests should use the smallest practical seam. They must not reproduce `Settlement_TraderTracker` internally or claim to prove when vanilla generates or destroys stock.

### Cache and lifecycle rules

Project-owned cache transitions and compatibility rules are covered by the current automated suite.

The implemented cache model has two related contracts:

* the full cache key protects complete build reuse and includes the base runtime context, ordered trader identities and discovery failure state;
* the lightweight reuse key protects immediate window reopen and intentionally excludes trader identities and discovery failures.

Automated coverage should include:

* first complete build;
* reuse of a valid snapshot through an equivalent full key;
* full-key changes for map, origin, negotiator, eligibility criteria, trader identity order or discovery failure state;
* equivalent lightweight reuse keys producing equal values and hash codes;
* reuse-key changes for map, origin, negotiator or relevant eligibility context;
* powered-console state affecting reuse only when that requirement is enabled;
* Royalty-active state affecting reuse only when the Royalty requirement is enabled;
* trader identities and discovery failures not being exposed by the reuse key;
* read-only reusable-snapshot lookup returning the same snapshot without invoking a factory or changing cache state;
* incompatible reuse keys returning no reusable snapshot;
* explicit manual full refresh replacing the current snapshot;
* complete invalidation removing both full-build and reopen reuse;
* reset after new-game or load boundaries;
* failure isolation between traders;
* Planner relevance not participating in full-key or lightweight-reuse-key equality;
* reusable stock snapshots remaining reusable when only Planner state changes.

Manual refresh tests should protect the project contract:

```text
discard project-owned snapshot
→ request a new full adapter capture
→ replace the snapshot with newly converted values
```

They must not assert that the underlying vanilla `Goods` getter is side-effect free. Vanilla lazy generation belongs to runtime acceptance.

Trader-specific partial refresh is not part of the first-release lifecycle. Tests for partial invalidation and fallback to complete refresh are added only if a new measured post-release requirement leads to that feature being approved and implemented.

Tests should target transition and compatibility contracts rather than the existence of individual methods.

### Settings contracts

The current automated suite protects deterministic mod-owned settings behavior.

This includes:

* accepted default values;
* conversion to `SettlementEligibilityCriteria`;
* disabling technology and distance filters through nullable criteria values;
* equivalence comparison for eligibility-relevant settings;
* normalization of negative maximum distance to the default value;
* clamping maximum distance to the supported upper bound of `3000`;
* independence of the global-tab visibility setting from settlement eligibility;
* safe defaults for newly introduced persisted values.

The settings tests use the runtime-free values and policy boundary. They do not attempt to reproduce the complete RimWorld Scribe or mod-settings UI runtime.

Actual persistence, settings-window interaction, main-button visibility and cache invalidation after player changes require runtime acceptance.

Settings for deferred prototype features are not test targets because those features are not implemented.

### Presentation and projection metadata contracts

Presentation tests are appropriate when they protect non-trivial user-facing policy.

Examples include:

* which status is shown when several fallback states apply;
* user-facing ordering when ordering changes interpretation;
* formatting that combines price, distance, restock or negotiator state;
* loading, empty, unavailable and partial-error state selection;
* search-sensitive category availability;
* Details-column visibility for pawn metadata and optional relevance presentation;
* deterministic Details sorting across every value rendered in the column;
* bounded Planner relevance tooltip presentation;
* distance presentation that keeps numeric tiles separate from ground-route warnings;
* static restock presentation that combines remaining time with an expected timestamp relative to the active trade origin;
* disabled reasons that combine multiple settings or runtime conditions.

Projection metadata and row presentation are cached as part of the UI lifecycle. Automated tests should protect deterministic policies feeding that cache, not frame-level allocations or direct IMGUI calls.

Automated tests are generally not required for:

* literal labels;
* direct translation-key forwarding;
* individual colors;
* spacing or fixed layout constants;
* the presence of a simple glyph;
* direct `Widgets` calls;
* whether a tooltip region is registered on a specific IMGUI event;
* private UI model shape when inspecting it requires Unity runtime assemblies.

A simple presentation helper should not be extracted only to create a trivial mapping test.

### Shared UI API contracts

The compile-time/shared-source UI project tests deterministic helpers with meaningful behavior, such as:

* sortable-header state transitions;
* fixed-height and variable-height visible-range calculations;
* list geometry and clipping projections;
* restoration helpers for global IMGUI state;
* disabled-state or action-policy decisions that combine several values.

The project is connected to the external sibling source. Canonical generic tests belong to `Escarval.RimWorld.UI.Tests` and run through `../../Escarval.RimWorld.UI/src/Escarval.RimWorld.UI.sln`, while STO retains consumer-specific smoke, presentation and integration tests. The existing consumer integration test and current build validation confirm that shared types are compiled into `SettlementTradeOverview.dll`, the source is included once and no runtime UI assembly is required. The STO solution retains the shared production project for navigation and build ordering but does not include the canonical shared test project.

Shared UI tests must protect generic contracts rather than Settlement Trade Overview or Xenogerm Planner business behavior. The implemented tint-aware helpers preserve the generic normal/disabled rendering boundary; consumer integration and the current automated builds confirm source ownership and assembly inclusion. Literal per-icon colors, texture appearance and final tint rendering remain runtime acceptance rather than unit-test targets.

Direct `Widgets` calls, literal colors, spacing constants and single localization-key mappings generally do not require unit tests.

### Optional Xenogerm Planner integration contracts

The implemented optional integration boundary is protected by deterministic tests that do not depend on the real Planner assembly where a supplied adapter result is sufficient.

Current automated coverage includes:

* absence of Planner producing a neutral no-integration result;
* incompatible API version producing safe degradation;
* conversion of runtime-free gene compositions into documented API requests;
* preservation of returned stable `PlanId` and `DisplayName`;
* deterministic row-level matching projection;
* isolation of a failed composition or API call from unrelated trade rows;
* indicator visibility only for genepack rows with valid composition metadata and one or more matches;
* neutral ordinary presentation for malformed, empty or unavailable compositions;
* deterministic Details-column sorting without moving relevance into stock snapshots or cache keys;
* identical projection behavior for the common `TradeListView` used by global and settlement-specific windows;
* relevance remaining outside `TradeInventorySnapshot`, full cache keys, lightweight reuse keys and persisted settings;
* rebuilding relevance on window reopen over a reusable stock snapshot without invoking the stock factory;
* an already open projection remaining unchanged until the normal window lifecycle rebuilds it.

STO tests must not reproduce Coverage, Exact payload, degraded-plan or prerequisite semantics. Those rules belong to the Planner API contract. Consumer tests supply representative API results and verify that STO handles them correctly. End-to-end semantic compatibility with actual Planner versions belongs to runtime acceptance.

## Vanilla-facing boundaries

RimWorld-facing adapters may be tested through minimal injected seams when Settlement Trade Overview applies its own classification, transformation or fallback rules to game data.

Examples include:

* conversion of trader goods to project-owned snapshot entries;
* conversion of a supplied live genepack representative to one canonical runtime-free def-name composition under the verified grouping rule;
* negotiator selection from supplied pawn data;
* transformation of approximate distance and ground-route state into independent project-owned values;
* transformation of scheduled restock data into game-tick and optional runtime-free expected-moment values;
* safe handling of missing optional DLC state;
* isolation of a failed or unavailable integration.

These tests answer:

> Given boundary data with state X, does Settlement Trade Overview apply project rule Y?

They do not prove that RimWorld itself produces state X or implements the corresponding vanilla behavior in a particular way.

Verified vanilla behavior remains grounded in the installed RimWorld implementation, source analysis and runtime validation.

Production architecture should not be expanded with a parallel abstraction solely to make a trivial vanilla API call unit-testable.

## Native settlement integration testing

The implemented settlement-specific entry point uses `WorldObjectCompProperties_SettlementTradeOverview` and a stateless `WorldObjectComp` added to the vanilla `Settlement` Def through XML patching. It does not use a Harmony patch for `Settlement.GetGizmos()`.

Automated tests should protect only deterministic project-owned contracts used by the component, such as:

* settlement eligibility and command visibility policy;
* enabled, disabled and hidden command-state selection;
* disabled-reason selection for structurally supported potential traders;
* safe behavior for an unavailable or no-longer-resolvable settlement;
* delegation from command policy to the application window-opening boundary.

The XML patch, Def inheritance, component initialization, actual gizmo enumeration and save/load behavior require the real RimWorld runtime and belong to runtime acceptance.

## Vanilla settlement stock lifecycle testing

Static analysis confirms that settlement `ITrader.Goods` access can lazily generate stock when the internal stock container is absent or empty. Expired stock is destroyed by the vanilla tracker tick and generated again by a later public `Goods` access.

Automated project tests must not replace this behavior with a simulated private tracker implementation.

The following belong to runtime acceptance:

* the first public `Goods` read before stock has been generated;
* the resulting change to public restock metadata;
* active stock surviving save/load;
* expiry followed by vanilla stock destruction;
* the next public read causing vanilla lazy generation;
* behavior when a generated or traded stock becomes completely empty;
* destruction of live stock objects when a settlement is removed;
* a compatible loaded project snapshot reopening without another stock read;
* an incompatible context or manual refresh returning to the full discovery and stock-capture path.

The project suite instead protects the adapter, full cache-key, reuse-key and invalidation contracts around the public boundary.

## Runtime acceptance

Scenarios requiring the actual RimWorld, Verse or Unity runtime should be checked in game rather than simulated in the test host.

This includes:

* mod loading and metadata;
* Def loading;
* actual settlement and trader discovery;
* vanilla `Tradeable` grouping of physical genepacks and confirmation of the source-derived single-composition `AnyThing` representative contract;
* real trade stock generation and expiration;
* first-access and post-expiry lazy generation through public `ITrader.Goods`;
* public `EverVisited`, `RestockedSinceLastVisit` and `NextRestockTick` transitions;
* save/load of active and expired settlement stock;
* actual trade-price calculations;
* world path and distance behavior;
* Royalty title and permit requirements with Royalty active, and safe core behavior without Royalty;
* communication-console scope across the current and other player-home maps;
* strict powered-console behavior for `Building_CommsConsole` and `CompPowerTrader.PowerOn`;
* settings persistence and settings UI interaction;
* global MainButton visibility changes without disabling settlement-specific commands;
* cache invalidation after eligibility-setting changes;
* application of the settlement `WorldObjectDef` XML patch;
* `WorldObjectComp` initialization for new games and loaded saves;
* settlement-command appearance without duplication or loss of vanilla gizmos;
* inherited Archonexus settlement Def behavior and command suppression for unsupported or player-owned settlements;
* settlement-specific entry-point behavior;
* compatible global and settlement-specific reopen without a mandatory loading frame or full rediscovery;
* optional Planner absence, compatible API discovery and incompatible API-version handling;
* Coverage, Exact payload and exact-conflict-only relevance returned by the real Planner API;
* exclusion of Ready and Degraded plans by the real Planner API;
* relevance refresh on window reopen after plan changes without another `ITrader.Goods` read;
* no automatic update of an already open STO window after Planner changes;
* loading and rendering of the STO-owned relevance PNG in the Details column, bounded tooltip behavior and full Details interaction in both global and settlement-specific lists;
* instance-aware item icons, native item tooltips and info cards with safe `ThingDef` fallback;
* numeric distance plus the ground-route warning icon, including route-unavailable and reachable states;
* expected restock timestamps relative to the active trade origin and static snapshot-time remaining values;
* runtime tinting of final code-rendered monochrome icons and preservation of source colors for multicolor assets after the accepted icon stages;
* final STO and Planner ModIcon, MainButton and `PlannerRelevance` packaged resources;
* fallback to full discovery when the requested settlement is absent from the loaded snapshot;
* IMGUI layout, clipping, scrolling, tooltip behavior and interaction;
* final user-friendly English layouts for both mods;
* Russian and Ukrainian STO layouts after translation, together with synchronized Planner layouts;
* window lifecycle across new games and save loading;
* actual Scribe behavior;
* clean-install packaging and development deployment.

Runtime acceptance should verify visible and integration behavior that cannot be established reliably through deterministic project-owned logic alone.

The following have been validated in game:

* the global overview, settlement-specific stock window and native settlement command;
* first access, active stock, expiry, vanilla destruction, post-expiry lazy generation and manual refresh through public `ITrader.Goods`;
* save/load with active and expired stock, settlement removal and completely empty generated stock handling;
* all supported eligibility and disabled-reason paths, with Royalty active and inactive;
* `WorldObjectComp` initialization, XML inheritance and absence of duplicate commands after save/load;
* runtime confirmation of the accepted single-composition genepack grouping contract, including labels, ordering and malformed or empty composition degradation;
* compatible snapshot reopen without full settlement rediscovery or another stock read;
* Planner absence, API version `1`, unsupported-version degradation and representative Coverage, Exact payload and exact-conflict relevance in both STO surfaces;
* relevance refresh on reopen without moving Planner state into stock snapshots or cache keys;
* final icon, tint and packaged asset paths;
* English, Russian and Ukrainian layouts and fallback behavior;
* representative extreme-world UI performance after projection, runtime-target and row-presentation caching.

The agreed compatibility and regression matrix is complete for the current pre-release baseline. The known performance risk is a temporary hitch during synchronous first build or manual refresh when many eligible settlements require vanilla stock generation. This is a release performance consideration, not an automated pass/fail threshold and not evidence of constant open-window overhead.

## Regression tests

A bug does not automatically require a new automated test.

Add a regression test when:

* the defect belongs to deterministic project-owned logic;
* the original failure can be reproduced through the normal contract of the affected layer;
* the test protects meaningful behavior from realistic recurrence.

Good targets include:

* an eligible settlement being excluded by an incorrect rule combination;
* descending sorting applying ascending tie-breakers inconsistently;
* a market-value fallback being reported as a negotiated price;
* complete invalidation leaving stale trader data in the active snapshot;
* a reusable snapshot being returned for an incompatible map, origin, negotiator or eligibility context;
* trader identities accidentally becoming part of lightweight reuse compatibility;
* a manual refresh reusing an old project snapshot instead of replacing it;
* settings normalization allowing unsupported distance values;
* a trader adapter exposing a live runtime stock object beyond the conversion boundary;
* a genepack row producing a non-canonical composition or exposing live genetics state contrary to the verified grouping policy;
* malformed genepack composition data removing an otherwise valid ordinary trade row;
* Details sorting becoming non-deterministic or ignoring one of the displayed pawn or Planner detail kinds;
* approximate distance being discarded when no ground route is available;
* expected restock moment data using trader-local coordinates instead of the active trade origin;
* a transient runtime target entering immutable snapshot or row-presentation contracts;
* Planner relevance accidentally entering stock-cache compatibility;
* an unavailable optional integration preventing ordinary stock presentation.

A test is usually not justified only to protect the test host from artificial fixture behavior, absent RimWorld initialization or a visual layout detail.

## Guard clauses and invalid input

Null and invalid-input tests should be selective.

They are valuable when:

* the tested method is a public or important internal boundary;
* invalid data can realistically arrive from runtime integration or persistence;
* safe degradation or the type of failure is part of the contract.

Tests should not be added mechanically for every guard clause.

## Stress and performance testing

Fast deterministic stress tests may run in the regular suite when they protect:

* large trade-entry sets;
* repeat determinism;
* stable sorting under many equal values;
* fixed-height and variable-height visible-range calculations;
* large runtime-free genepack composition sets and relevance projections;
* bounded cache or projection behavior.

Performance benchmarks and runtime profiling are diagnostic tools.

They should remain separate from the regular suite, run locally in Release configuration and report useful measurements without workstation-specific pass/fail thresholds unless a requirement is separately justified.

Representative profiling has already validated the current constant UI path and compatible reopen behavior. On the tested system, both the rebuilt implementation and the prototype reached the same configured TPS and FPS limits after the agreed optimizations. These values must not be presented as universal guarantees.

Future performance work requires a new measured problem. Trader-specific partial refresh is not part of the first-release plan because current profiling did not demonstrate a need for it. Temporary synchronous stock-generation hitches should be documented for release and evaluated separately from constant UI rendering performance.

## Test decision checklist

Before adding or retaining an automated test, use the following sequence:

```text
Can the regression change which settlements or goods are shown?
→ Yes: test it.

Can it change project-owned pricing, sorting, settings or cache semantics?
→ Yes: test it.

Can it return a cached snapshot for an incompatible base context?
→ Yes: test the full or reuse key contract.

Can it expose live vanilla stock or genetics objects beyond the adapter boundary?
→ Yes: test it.

Can it lose or corrupt mod-owned persisted settings?
→ Test the runtime-free settings policy where practical, then validate actual persistence in game.

Is this a non-trivial project-owned lifecycle or state transition?
→ Yes: test it.

Is this deterministic filtering, aggregation or transformation?
→ Yes: test it.

Is this a RimWorld-facing or optional-integration adapter where the project applies its own rule?
→ Test the project-owned rule through the smallest practical seam without duplicating Planner semantics.

Is this only a literal label, color, spacing value or direct Widgets call?
→ Usually do not unit-test it.

Does reliable verification require the actual RimWorld or Unity runtime?
→ Use runtime acceptance.

Would production architecture or test deployment need to change only to inspect a trivial UI detail?
→ The test is probably not justified.
```

The automated suite should remain focused on meaningful project contracts.

Runtime acceptance complements the suite; it is not a substitute for deterministic project-owned tests, and the test suite is not a substitute for the game runtime.