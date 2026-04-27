# P2S-024 Discover Farmhouse Ash Application Path

Status: in-progress

Agent level: High

Dependencies: P2S-020, P2S-022

## Objective

Find the safest way for farmhouses or farmers to consume Fertile Ash and apply field amendments.

## Requirements

- Inspect farmhouse, planting, inventory, work behavior, and good-consuming APIs.
- Prefer decorating existing farmhouse or farmer behavior over building a parallel system.
- Confirm how stored `FertileAsh` can be consumed.
- Confirm how nearby eligible planting spots can be selected.
- Produce evidence and a chosen implementation path.

## Unknowns

- It is unknown whether existing farmhouse behavior can be decorated safely.
- It is unknown whether a new Prometheus-specific behavior is cleaner than using `GoodConsumingBuilding`.

## Write Scope

- Discovery notes in ticket handoff.
- Integration wrapper or small proof if safe.
- No full implementation unless the safe path is obvious and scoped.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- If code is added, run a build check appropriate to new Timberborn references.

## Integration Notes

High agent recommended because this touches Timberborn work and inventory systems.


## Discovery Handoff

Date: 2026-04-27

P2S-024 inspected current Prometheus recovery code plus Timberborn managed DLLs from:

`/Users/jasonkleinberg/Library/Application Support/Steam/steamapps/common/Timberborn/Timberborn.app/Contents/Resources/Data/Managed`

No runtime implementation was added for this ticket. The safest P2S-025 path is a farmhouse-decorated worker behavior with thin compatibility wrappers around Timberborn inventory and planting APIs.


### Prometheus Evidence

| Source | Exact APIs or types | Evidence |
| --- | --- | --- |
| `FertileAshRecoveredGoodStackSpawner` | `FertileAshRecoveredGoodStackRules.FertileAshGoodId = "FertileAsh"`; `IGoodService.HasGood`; `RecoveredGoodStackSpawner.AddAwaitingGoods` | P2S-020's recovered-good path already emits native recoverable stacks that store `FertileAsh` in district inventory. P2S-025 should reuse this good id instead of adding a second ash resource. |
| `FireFieldAmendmentRuntimeState` | `SetAmendment(FireGridCoordinate, float, int)`; `TryGetAmendment`; `ConsumeCharge`; `ClearAmendments` | P2S-022 already provides the amendment state entry point. P2S-025 only needs to choose a coordinate and consume one stored ash unit before calling `SetAmendment`. |
| `FireRecoveryEffectApplier` | `FireGridFootprintSampler.FromWorldPosition`; primary coordinate and ground coordinate lookup; reflected `Growable.GrowthTimeInDays` | P2S-023 applies the crop buff when an eligible crop's primary or ground coordinate has an active amendment. P2S-025 should write amendments on the same grid coordinate family. |
| `PrometheusConfigurator` | `TemplateModule.Builder.AddDecorator(...)`; `FireFieldAmendmentRuntimeState` singleton binding | Existing Prometheus integration is decorator-based. A farmhouse decorator fits the current module style and keeps fire-grid state separate from Timberborn farming internals. |


### Timberborn APIs Inspected

| DLL | Exact APIs or types | Evidence |
| --- | --- | --- |
| `Timberborn.Fields.dll` | `Timberborn.Fields.FarmHouse`; `FarmHouse.Validate(PlantingSpot)`; `FarmHouseWorkplaceBehavior.Decide(BehaviorAgent)`; `FarmHouseGoodStackRetrieverWorkplaceBehavior.Decide(BehaviorAgent)`; `HarvestStarter.StartHarvesting(Inventory, InRangeYielders, string)` | `FarmHouse.Validate` accepts only planting spots with no `PlantingBlocker`. `FarmHouseWorkplaceBehavior` composes native planting and harvesting decisions, and order changes with `FarmHouse.PlantingPrioritized`. Prometheus should decorate this behavior chain instead of running an unrelated global applier. |
| `Timberborn.Planting.dll` | `PlantBehavior.StartPlanting(BehaviorAgent)`; `PlantingSpotFinder.FindClosest(Vector3)`; `PlantingService.GetSpotAt(Vector3Int)`; `PlantingService.GetResourceAt(Vector3Int)`; `PlantingSpot.Coordinates`; `PlantingSpot.ResourceToPlant`; `PlantingSpot.PlantingBlocker`; `IPlantingSpotValidator.Validate(PlantingSpot)` | `PlantingSpotFinder` is non-public, but its public `FindClosest` method performs the native selection pass: neighboring spots first, then all reachable in-range spots, closest by farmhouse grounded center, with soil, flood, spawn, priority, and farmhouse validation. A reflection-backed adapter can reuse that validation without copying it. |
| `Timberborn.InventorySystem.dll` | `DistrictInventoryRegistry.ActiveInventoriesWithStock(string)`; `DistrictInventoryPicker.ClosestInventoryWithStock(...)`; `Inventory.HasUnreservedStock(GoodAmount)`; `Inventory.HasUnreservedStock(string)`; `Inventory.UnreservedAmountInStock(string)`; `Inventory.Take(GoodAmount)`; `GoodReserver.ReserveExactStockAmount(Inventory, GoodAmount)` | Stored `FertileAsh` is consumable through public inventory APIs. If application is immediate, a selected inventory can be checked and consumed with `HasUnreservedStock` plus `Take(new GoodAmount("FertileAsh", 1))`. If the behavior reserves ash across travel or multiple decisions, reserve first with `GoodReserver`. |
| `Timberborn.GoodConsumingBuildingSystem.dll` | `GoodConsumingBuildingSpec.ConsumedGoods`; `ConsumedGoodSpec.GoodId`; `ConsumedGoodSpec.GoodPerHour`; `GoodConsumingBuilding.Inventory`; `GoodConsumingBuilding.Tick()`; private `ConsumeSupplies`; private `HasSupplies` | This system consumes configured goods from a building-owned inventory over time. It does not choose field coordinates or represent a farmer applying ash. It is a poor primary fit for field amendments. |
| `Timberborn.WorkSystem.dll` | `WorkerRootBehavior.WorkAtWorkplace()`; `Workplace.WorkplaceBehaviors`; `WorkplaceBehavior.Decide(BehaviorAgent)`; `Worker.Workplace` | Worker root behavior iterates workplace behaviors and transfers to the first non-release decision. A new farmhouse-scoped `WorkplaceBehavior` can join native scheduling without replacing farmhouse logic. |
| `Timberborn.GoodStackSystem.dll` | `GoodStackRetrieverBehavior.StartRetrieving(IGoodStackService, Accessible, Inventory)`; `GoodStackService<FarmHouse>` | Farmhouses already retrieve field good stacks into their output inventory. This supports keeping crop pickup native and treating ash application as a separate farmhouse work behavior. |
| `Timberborn.Carrying.dll` | `CarrierInventoryFinder.TryCarryFromAnyInventory(string, Inventory)`; `CarrierInventoryFinder.TryCarryFromAnyInventoryLimited(string, Inventory, int)`; `GoodCarrier.PutGoodsInHands(GoodAmount, bool)` | Native carrying can move goods between inventories, but P2S-025 would need an ash-accepting farmhouse input inventory before this is useful. Direct district-inventory consumption is smaller; a visible hauling path is possible but larger. |
| `Timberborn.SimpleOutputBuildings.dll` | `SimpleOutputInventory.Inventory`; `SimpleOutputInventoryInitializer.AllowGoodsAsTakeable` | Farmhouse output inventory is for harvested crops and stack retrieval. It should not be reused as the ash input path without an explicit new input contract. |


### Confirmed Consumption Path

Stored `FertileAsh` can be consumed without a new good type:

1. Find a district inventory with unreserved `FertileAsh` using `DistrictInventoryRegistry.ActiveInventoriesWithStock("FertileAsh")` or `DistrictInventoryPicker.ClosestInventoryWithStock(...)`.

2. Confirm availability with `Inventory.HasUnreservedStock(new GoodAmount("FertileAsh", 1))` or `Inventory.UnreservedAmountInStock("FertileAsh")`.

3. For immediate application, call `Inventory.Take(new GoodAmount("FertileAsh", 1))` only after a target planting coordinate has been selected.

4. For delayed worker travel or multi-step behavior, reserve first with `GoodReserver.ReserveExactStockAmount(...)` and release the reservation on cancellation.


### Confirmed Planting-Spot Selection Path

Nearby eligible planting spots can be selected through Timberborn's native farmhouse planting path:

1. Resolve the farmhouse's non-public `PlantingSpotFinder` component through a compatibility adapter.

2. Invoke public method `FindClosest(Vector3 agentPosition)` by reflection.

3. Read the returned nullable `PlantingSpot` value and extract `Coordinates` and `ResourceToPlant`.

4. Apply the field amendment at the selected coordinate after ash stock is consumed.

This path intentionally inherits native validation from `PlantingSpotFinder.CanPlantAt(...)`, including prioritized plantable filtering, `PlantingSoilValidator`, `IPlantingSpotValidator`, flood checks, spawn validation, and farmhouse range lookup.


### Chosen P2S-025 Implementation Path

P2S-025 should implement a farmhouse-decorated ash amendment behavior:

1. Add a Prometheus `WorkplaceBehavior` decorator on `Timberborn.Fields.FarmHouse`.

2. Add a small `PlantingSpotFinder` compatibility adapter that hides reflection and returns a stable Prometheus result type with coordinate and resource name.

3. Add a small `FertileAsh` inventory consumer wrapper that locates eligible district stock, checks unreserved availability, and consumes or reserves exactly one unit.

4. In the farmhouse behavior, select a native-validated planting spot, consume one `FertileAsh`, then call `FireFieldAmendmentRuntimeState.SetAmendment(...)`.

5. Emit telemetry for success, no ash, no eligible spot, and inventory-consumption failure.

6. Add tests for wrapper behavior and failure ordering so ash is never consumed when no eligible planting coordinate exists.


### Rejected Alternatives

- `GoodConsumingBuilding` as the primary implementation: rejected because it consumes building supplies continuously and has no field-coordinate selection contract.
- A global Prometheus field scanner: rejected because it bypasses farmhouse range, farmer labor, plant priorities, and native planting validation.
- Reusing `SimpleOutputInventory` as an ash input: rejected because farmhouse output inventory is crop/output oriented and should not silently become an input buffer.
- Full visible hauling in P2S-025's first pass: deferred because it needs an explicit ash input inventory and more UX/test coverage. It remains a possible follow-up if direct district-inventory consumption feels too invisible in live QA.


### Open Unknowns For P2S-025

- Product behavior: native `PlantingSpotFinder` targets empty marked planting spots. If ash must be applicable to already-planted crops, P2S-025 needs an additional crop-in-range selector instead of relying only on `PlantingSpotFinder`.
- Behavior ordering: decide whether the ash amendment behavior should run before native planting only, or opportunistically between harvest and planting when `FarmHouse.PlantingPrioritized` is false.
- UX visibility: direct district-inventory consumption is the lowest-risk implementation, but live QA may need telemetry or UI/status text so the user can see ash is being applied.
- Reservation scope: immediate consumption is safe only when target selection and amendment application are atomic. Any animation/travel path should use `GoodReserver`.


### Verification Results

- `git diff --check`: passed.
- `bash scripts/test.sh`: passed, 85 tests.
- Timberborn reference build check: not run because no code was added.
