# P2S-019 Discover Native Ash Gatherable Path

Status: in-progress

Agent level: High

Dependencies: P2S-008, P2S-018

## Objective

Find and wrap the safest Timberborn-native path for visible collectable Fertile Ash.

## Requirements

- Inspect Timberborn gathering, recoverable good, yielding, inventory, and natural resource APIs.
- Prefer native gatherable or recoverable-good flow.
- Produce a wrapper if safe.
- Stop with evidence if native runtime spawning is unsafe.

## Unknowns

- Runtime-spawned gatherables may require templates or natural-resource factory support.
- Recoverable-good APIs may only support demolishable/building recovery.

## Write Scope

- Integration discovery notes in ticket handoff.
- Native ash gatherable wrapper if safe.
- No broad gameplay behavior unless safe path is confirmed.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- If code is added, run a local build check appropriate to the changed references.

## Integration Notes

High agent recommended. This is an API discovery ticket and must not guess.

## Discovery Notes

2026-04-27 candidate evidence:

- Timberborn gatherable flow is template-owned natural-resource behavior. Current DLL evidence from `Timberborn.Gathering.dll` shows `GatherableSpec` wraps a `YielderSpec`, and imported blueprints such as `BlueberryBush` and `Pine` define `GatherableSpec.Yielder` with `YielderComponentName`, `Yield`, `RemovalTimeInHours`, and `ResourceGroup`.
- Current imported Timberborn resources contain no native ash natural resource or gatherable ash template. `resources.assets` and imported blueprint scans surfaced `RecoveredGoodStack`, `Dirt`, natural resources, and existing goods, but no `Ash` good/template/resource path.
- Timberborn natural-resource runtime spawning exists through `NaturalResourceFactory.SpawnNew(...)`, `PlantNew(...)`, and `SpawnIgnoringConstraints(...)`, but those APIs require a registered resource template id. Because no ash natural-resource template was confirmed, this ticket does not wrap that path.
- Timberborn recovered-good flow is a safer native collection path for visible collectable goods. DLL evidence from `Timberborn.RecoveredGoodSystem.dll` shows `RecoveredGoodStackSpawner.AddAwaitingGoods(Vector3Int, IEnumerable<GoodAmount>)` and `RecoveredGoodStackFactory.Create(Vector3Int, IEnumerable<GoodAmount>)`; imported resources confirm a common `Environment/RecoveredGoodStack/RecoveredGoodStack.blueprint` template and `TemplateCollection.RecoveredGoodStack.Common`.
- Prometheus already registers `Good.FertileAsh.blueprint.json` and appends `FertileAsh` to Folktails and Iron Teeth good collections. The wrapper validates that `IGoodService.HasGood("FertileAsh")` is true before queueing recovered-good stack goods.
- Added `FertileAshRecoveredGoodStackSpawner` as the narrow native wrapper around the recovered-good stack path. It is not called by fire aftermath yet and does not implement ash spawning behavior, field amendment state, farmhouse/farmer integration, or broad economy loops.

Remaining unknown:

- The wrapper still needs a live-game caller/proof pass before Phase 3 depends on it: queue one `FertileAsh` recovered-good stack at valid coordinates, confirm the visible rubble/stack appears, confirm builders can collect it into storage that accepts Fertile Ash, and confirm no Prometheus or recovered-good exceptions appear in `Player.log` / `Fire.log`.
