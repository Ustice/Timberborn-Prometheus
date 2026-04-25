using System;
using System.Collections.Generic;
using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests {
  public sealed class Program {

    [Fact]
    public void SnapshotStore_RemovesAndClearsSnapshots_Test() => SnapshotStoreRemovesAndClearsSnapshots();

    [Fact]
    public void ExposureRuntimeState_ForcedIgnitionConsumesAndClears_Test() => ExposureRuntimeStateForcedIgnitionConsumesAndClears();

    [Fact]
    public void ExposureRuntimeState_DebugIgnitionBlockClearsQueuedRequests_Test() => ExposureRuntimeStateDebugIgnitionBlockClearsQueuedRequests();

    [Fact]
    public void ExposureReset_ClearsActiveFireAndIgnitions_Test() => ExposureResetClearsActiveFireAndIgnitions();

    [Fact]
    public void RecoveryReset_ClearsAshenState_Test() => RecoveryResetClearsAshenState();

    [Fact]
    public void TerminalDeadBuildingSnapshot_CannotBurn_Test() => TerminalDeadBuildingSnapshotCannotBurn();

    [Fact]
    public void DamageStateThresholds_EncodeLifecycleDecisions_Test() => DamageStateThresholdsEncodeLifecycleDecisions();

    [Fact]
    public void WorkplaceSupportComponentClassification_PreservesWorkplaceBoundary_Test() => WorkplaceSupportComponentClassificationPreservesWorkplaceBoundary();

    [Fact]
    public void OperationalComponentClassification_AvoidsFireAndWorkplaceInternals_Test() => OperationalComponentClassificationAvoidsFireAndWorkplaceInternals();

    [Fact]
    public void BeaverExposureRules_ScaleByProximity_Test() => BeaverExposureRulesScaleByProximity();

    [Fact]
    public void BeaverExposureRules_IndoorExposureUsesFullPressure_Test() => BeaverExposureRulesIndoorExposureUsesFullPressure();

    [Fact]
    public void FireTelemetryEvents_AreCentralizedAndUnique_Test() => FireTelemetryEventsAreCentralizedAndUnique();

    [Fact]
    public void FireVisualEffectRules_DryBurningFireProducesReadableEffects_Test() => FireVisualEffectRulesDryBurningFireProducesReadableEffects();

    [Fact]
    public void FireVisualEffectRules_MoistureTradesFireForSteam_Test() => FireVisualEffectRulesMoistureTradesFireForSteam();

    [Fact]
    public void FireVisualEffectRules_DeadDamageStateKeepsCharWithoutFire_Test() => FireVisualEffectRulesDeadDamageStateKeepsCharWithoutFire();

    [Fact]
    public void FireVisualPreset_DefaultsUsePromotedAuthoringValues_Test() => FireVisualPresetDefaultsUsePromotedAuthoringValues();

    [Fact]
    public void FireGridCoordinate_MapsCellsIntoEightByEightByEightChunks_Test() => FireGridCoordinateMapsCellsIntoEightByEightByEightChunks();

    [Fact]
    public void FireGridKernel_Full27IncludesSelfAndAllNeighbors_Test() => FireGridKernelFull27IncludesSelfAndAllNeighbors();

    [Fact]
    public void FireGridRuntimeState_WritesAcrossChunkBoundaries_Test() => FireGridRuntimeStateWritesAcrossChunkBoundaries();

    [Fact]
    public void FireGridRuntimeState_PrunesColdChunks_Test() => FireGridRuntimeStatePrunesColdChunks();

    [Fact]
    public void FireGridRuntimeState_DoubleBufferedStepIsInjectionOrderIndependent_Test() => FireGridRuntimeStateDoubleBufferedStepIsInjectionOrderIndependent();

    [Fact]
    public void FireGridFootprintSampler_ConvertsBoundsToOccupiedCells_Test() => FireGridFootprintSamplerConvertsBoundsToOccupiedCells();

    [Fact]
    public void FireGridRuntimeState_SamplesFootprintAggregate_Test() => FireGridRuntimeStateSamplesFootprintAggregate();

    [Fact]
    public void FireGridRuntimeState_UnderwaterCellsSuppressIgnition_Test() => FireGridRuntimeStateUnderwaterCellsSuppressIgnition();

    [Fact]
    public void FireGridRuntimeState_MoistureAndBarriersReduceTransfer_Test() => FireGridRuntimeStateMoistureAndBarriersReduceTransfer();

    [Fact]
    public void FireGridRuntimeState_OxygenAvailabilityChangesIgnition_Test() => FireGridRuntimeStateOxygenAvailabilityChangesIgnition();

    private static void SnapshotStoreRemovesAndClearsSnapshots() {
      var state = new FireImpactRuntimeState();
      var snapshot = new FireImpactSnapshot(0.1f, 0.2f, 0.3f, 0.4f, 0.5f);

      state.SetSnapshot(42, snapshot);
      Equal(1, state.SnapshotCount);
      True(state.TryGetSnapshot(42, out var storedSnapshot));
      NearlyEqual(0.3f, storedSnapshot.BuildingDamagePressure);

      state.RemoveSnapshot(42);
      False(state.TryGetSnapshot(42, out _));

      state.SetSnapshot(43, snapshot);
      state.SetSnapshot(44, snapshot);
      state.ClearSnapshots();
      Equal(0, state.SnapshotCount);
    }

    private static void ExposureRuntimeStateForcedIgnitionConsumesAndClears() {
      var state = new FireExposureRuntimeState();
      state.RequestForcedIgnition(10);

      True(state.ConsumeForcedIgnitionRequest(10));
      False(state.ConsumeForcedIgnitionRequest(10));

      state.RequestForcedIgnition(10);
      state.ClearSnapshotsAndIgnitionRequests();
      False(state.ConsumeForcedIgnitionRequest(10));
      Equal(0, state.PendingForcedIgnitionCount);
    }

    private static void ExposureRuntimeStateDebugIgnitionBlockClearsQueuedRequests() {
      var state = new FireExposureRuntimeState();
      state.RequestForcedIgnition(10);
      state.BlockDebugIgnitionsForSeconds(30f);
      state.RequestForcedIgnition(11);

      Equal(0, state.PendingForcedIgnitionCount);
      False(state.ConsumeForcedIgnitionRequest(10));
      False(state.ConsumeForcedIgnitionRequest(11));

      state.TickIgnitionBlock(31f);
      state.RequestForcedIgnition(11);
      True(state.ConsumeForcedIgnitionRequest(11));
    }

    private static void ExposureResetClearsActiveFireAndIgnitions() {
      var exposure = new FireExposureRuntimeState();
      exposure.SetSnapshot(8, CreateExposureSnapshot(burning: true, intensity: 0.8f));
      exposure.RequestForcedIgnition(8);

      exposure.ClearSnapshotsAndIgnitionRequests();

      Equal(0, exposure.SnapshotCount);
      Equal(0, exposure.PendingForcedIgnitionCount);
    }

    private static void RecoveryResetClearsAshenState() {
      var state = new FireRecoveryRuntimeState();
      state.SetSnapshot(2, new FireRecoverySnapshot(true, 0.2f, 0.1f, 0.1f, 12f));

      state.ClearSnapshots();

      Equal(0, state.SnapshotCount);
      False(state.TryGetSnapshot(2, out _));
    }

    private static void TerminalDeadBuildingSnapshotCannotBurn() {
      var snapshot = FireExposureRules.CreateTerminalDeadBuildingSnapshot();

      False(snapshot.Burning);
      NearlyEqual(0f, snapshot.Intensity);
      NearlyEqual(0f, snapshot.HeatExposure);
      NearlyEqual(0f, snapshot.EmberPressure);
      Equal("DeadBuilding", snapshot.DominantSource);
    }

    private static void DamageStateThresholdsEncodeLifecycleDecisions() {
      Equal(FireDamageState.Healthy, FireDamageStateRules.DetermineState(0f));
      Equal(FireDamageState.Healthy, FireDamageStateRules.DetermineState(0.199f));
      Equal(FireDamageState.Scorched, FireDamageStateRules.DetermineState(0.2f));
      Equal(FireDamageState.Scorched, FireDamageStateRules.DetermineState(0.599f));
      Equal(FireDamageState.Burning, FireDamageStateRules.DetermineState(0.6f));
      Equal(FireDamageState.Burning, FireDamageStateRules.DetermineState(0.949f));
      Equal(FireDamageState.Dead, FireDamageStateRules.DetermineState(0.95f));
      Equal(FireDamageState.Dead, FireDamageStateRules.DetermineState(1f));
    }

    private static void WorkplaceSupportComponentClassificationPreservesWorkplaceBoundary() {
      True(FireWorkplaceRules.IsWorkplaceSupportComponentName("Workplace"));
      True(FireWorkplaceRules.IsWorkplaceSupportComponentName("BakeryWorkplace"));
      True(FireWorkplaceRules.IsWorkplaceSupportComponentName("WorkplaceWorkerTracker"));
      False(FireWorkplaceRules.IsWorkplaceSupportComponentName("WorkplaceBonuses"));
      False(FireWorkplaceRules.IsWorkplaceSupportComponentName("Manufactory"));
      False(FireWorkplaceRules.IsWorkplaceSupportComponentName(""));
      False(FireWorkplaceRules.IsWorkplaceSupportComponentName(null));
    }

    private static void OperationalComponentClassificationAvoidsFireAndWorkplaceInternals() {
      True(FireWorkplaceRules.IsOperationalComponentName("Manufactory"));
      True(FireWorkplaceRules.IsOperationalComponentName("SimpleManufactoryBehaviors"));
      True(FireWorkplaceRules.IsOperationalComponentName("Workshop"));
      True(FireWorkplaceRules.IsOperationalComponentName("RecipeSelector"));
      False(FireWorkplaceRules.IsOperationalComponentName("FireExposureController"));
      False(FireWorkplaceRules.IsOperationalComponentName("Workplace"));
      False(FireWorkplaceRules.IsOperationalComponentName("WorkplaceBonuses"));
      False(FireWorkplaceRules.IsOperationalComponentName("Deteriorable"));
      False(FireWorkplaceRules.IsOperationalComponentName(""));
      False(FireWorkplaceRules.IsOperationalComponentName(null));
    }

    private static void BeaverExposureRulesScaleByProximity() {
      var impactSnapshot = new FireImpactSnapshot(0f, 0f, 0f, 0.8f, 0.4f);

      var nearDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, 0f);
      var halfDistanceDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, FireBeaverExposureRules.EffectRadius * 0.5f);
      var outsideDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, FireBeaverExposureRules.EffectRadius + 1f);

      NearlyEqual(-0.00008f, nearDeltas.ThirstDelta, 0.000001f);
      NearlyEqual(-0.0003f, nearDeltas.HeatStressDelta, 0.000001f);
      NearlyEqual(nearDeltas.ThirstDelta * 0.5f, halfDistanceDeltas.ThirstDelta, 0.000001f);
      NearlyEqual(nearDeltas.HeatStressDelta * 0.5f, halfDistanceDeltas.HeatStressDelta, 0.000001f);
      False(outsideDeltas.HasEffect);
    }

    private static void BeaverExposureRulesIndoorExposureUsesFullPressure() {
      var impactSnapshot = new FireImpactSnapshot(0f, 0f, 0f, 0.8f, 0.4f);

      var proximityDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, 0f);
      var indoorDeltas = FireBeaverExposureRules.ComputeIndoorNeedDeltas(impactSnapshot);

      NearlyEqual(proximityDeltas.ThirstDelta, indoorDeltas.ThirstDelta, 0.000001f);
      NearlyEqual(proximityDeltas.HeatStressDelta, indoorDeltas.HeatStressDelta, 0.000001f);
      True(indoorDeltas.HasEffect);
    }

    private static void FireTelemetryEventsAreCentralizedAndUnique() {
      True(FireTelemetryEvents.All.Length >= 20);
      Equal(FireTelemetryEvents.All.Length, new HashSet<string>(FireTelemetryEvents.All).Count);
      True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.DebugResetFireExposure) >= 0);
      True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.WorkplaceIndoorExposure) >= 0);
      True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.GridIgnitionSeeded) >= 0);
    }

    private static void FireVisualEffectRulesDryBurningFireProducesReadableEffects() {
      var intensity = FireVisualEffectRules.ComputeIntensity(
        CreateExposureSnapshot(burning: true, intensity: 0.8f),
        new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Burning, 0.7f, 0.5f, 4),
        FireVisualEffectTuning.Default);

      True(intensity.HasAnyVisibleEffect);
      NearlyEqual(0f, intensity.Embers);
      True(intensity.Smoke > 0.4f);
      True(intensity.Fire > 0.7f);
      True(intensity.Char > 0.4f);
      NearlyEqual(0f, intensity.Steam);
    }

    private static void FireVisualEffectRulesMoistureTradesFireForSteam() {
      var dryExposure = CreateExposureSnapshot(burning: true, intensity: 0.8f, moistureDampening: 0f);
      var wetExposure = CreateExposureSnapshot(burning: true, intensity: 0.8f, moistureDampening: 0.9f);
      var damage = new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Burning, 0.65f, 0.5f, 3);
      var dry = FireVisualEffectRules.ComputeIntensity(dryExposure, damage, FireVisualEffectTuning.Default);
      var wet = FireVisualEffectRules.ComputeIntensity(wetExposure, damage, FireVisualEffectTuning.Default);

      True(wet.Steam > dry.Steam);
      True(wet.Fire < dry.Fire);
      True(wet.Smoke < dry.Smoke);
    }

    private static void FireVisualEffectRulesDeadDamageStateKeepsCharWithoutFire() {
      var intensity = FireVisualEffectRules.ComputeIntensity(
        CreateExposureSnapshot(burning: true, intensity: 1f),
        new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Dead, 1f, 1f, 12),
        FireVisualEffectTuning.Default);

      NearlyEqual(0f, intensity.Fire);
      True(intensity.Char > 0.95f);
      True(intensity.Smoke > 0f);
    }

    private static void FireVisualPresetDefaultsUsePromotedAuthoringValues() {
      var preset = new FireVisualPreset();
      var smoke = preset.GetParticle(FireVisualEffectKind.Smoke);
      Equal("FoodFactorySmoke", smoke.SourceName);
      NearlyEqual(2.3f, smoke.Lifetime);
      NearlyEqual(0f, smoke.Spread);

      var ash = preset.GetParticle(FireVisualEffectKind.Ash);
      Equal("BadwaterRigSmoke", ash.SourceName);
      NearlyEqual(0.55f, ash.Intensity);
      NearlyEqual(0.4f, ash.Emission);
      NearlyEqual(0.9f, ash.Position.y);
      NearlyEqual(0.2f, ash.Size);
      NearlyEqual(0.75f, ash.Lifetime);
      NearlyEqual(0.25f, ash.Spread);

      var steam = preset.GetParticle(FireVisualEffectKind.Steam);
      Equal("CoffeeBrewerySmoke", steam.SourceName);
      NearlyEqual(0.35f, steam.Position.y);
      NearlyEqual(0.7f, steam.Velocity.y);
      NearlyEqual(0.8f, steam.Spread);

      var fire = preset.GetParticle(FireVisualEffectKind.Fire);
      Equal("CampfireFire", fire.SourceName);
      NearlyEqual(0.25f, fire.Position.x);
      NearlyEqual(0.15f, fire.Position.z);
      NearlyEqual(1.2f, fire.Lifetime);
      NearlyEqual(0f, fire.Spread);
      NearlyEqual(-0.15f, fire.Gravity);
      Equal(FireVisualSizeOverLifetimePreset.Swell, fire.SizeOverLifetime);

      var sparks = preset.GetParticle(FireVisualEffectKind.Sparks);
      Equal("Sparks_Trail", sparks.SourceName);
      NearlyEqual(0.7f, sparks.Intensity);
      NearlyEqual(0.55f, sparks.Emission);
      NearlyEqual(1.4f, sparks.Spread);
      NearlyEqual(-0.25f, sparks.Gravity);
      NearlyEqual(0.4f, sparks.NoiseStrength);
    }

    private static void FireGridCoordinateMapsCellsIntoEightByEightByEightChunks() {
      Equal(new FireGridChunkCoordinate(0, 0, 0), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(0, 0, 0)));
      Equal(new FireGridChunkCoordinate(0, 0, 0), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(7, 7, 7)));
      Equal(new FireGridChunkCoordinate(1, 0, 0), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(8, 0, 0)));
      Equal(new FireGridChunkCoordinate(-1, -1, -1), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(-1, -1, -1)));
      Equal(new FireGridChunkCoordinate(-1, -1, -1), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(-8, -8, -8)));
      Equal(new FireGridChunkCoordinate(-2, -1, -1), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(-9, -8, -8)));

      Equal(0, FireGridChunkCoordinate.LocalIndex(new FireGridCoordinate(0, 0, 0)));
      Equal(7, FireGridChunkCoordinate.LocalIndex(new FireGridCoordinate(7, 0, 0)));
      Equal(7, FireGridChunkCoordinate.LocalIndex(new FireGridCoordinate(-1, 0, 0)));
    }

    private static void FireGridKernelFull27IncludesSelfAndAllNeighbors() {
      var entries = FireGridKernel.Full27.Entries;
      Equal(27, entries.Count);
      Equal(1, CountKernelEntries(entries, 0, 0, 0));
      Equal(1, CountKernelEntries(entries, -1, -1, -1));
      Equal(1, CountKernelEntries(entries, 1, 1, 1));
      True(entries[0].HeatWeight >= 0f);
      True(FindKernelEntry(entries, 0, 0, 0).IsSelf);
      True(FindKernelEntry(entries, 0, 1, 0).SmokeWeight > FindKernelEntry(entries, 0, -1, 0).SmokeWeight);
      True(FindKernelEntry(entries, 0, 1, 0).HeatWeight > FindKernelEntry(entries, 0, -1, 0).HeatWeight);
    }

    private static void FireGridRuntimeStateWritesAcrossChunkBoundaries() {
      var grid = CreateGridWithFuelAroundOrigin();
      var source = new FireGridCoordinate(7, 0, 0);
      var target = new FireGridCoordinate(8, 0, 0);

      grid.SetEnvironment(source, BurnableEnvironment());
      grid.SetEnvironment(target, BurnableEnvironment());
      grid.Inject(source, HotCell());
      grid.Step(FireGridKernel.Full27);

      True(grid.TryGetState(target, out var targetState));
      True(targetState.Heat > 0f);
      True(grid.ActiveChunkCount >= 2);
    }

    private static void FireGridRuntimeStatePrunesColdChunks() {
      var grid = new FireGridRuntimeState();
      var coordinate = new FireGridCoordinate(0, 0, 0);

      grid.Inject(coordinate, HotCell());
      Equal(1, grid.TotalChunkCount);
      Equal(1, grid.ActiveCellCount);

      grid.ClearCell(coordinate);
      grid.PruneInactiveChunks();

      Equal(0, grid.TotalChunkCount);
      Equal(0, grid.ActiveCellCount);
    }

    private static void FireGridRuntimeStateDoubleBufferedStepIsInjectionOrderIndependent() {
      var first = CreateGridWithFuelAroundOrigin();
      var second = CreateGridWithFuelAroundOrigin();
      var left = new FireGridCoordinate(0, 0, 0);
      var right = new FireGridCoordinate(1, 0, 0);
      var sample = new FireGridCoordinate(0, 1, 0);

      first.Inject(left, new FireCellState(0.8f, 0.6f, 0.4f, 0.2f, 0f, FireGridBurnState.Burning));
      first.Inject(right, new FireCellState(0.3f, 0.9f, 0.2f, 0.1f, 0f, FireGridBurnState.Smoldering));
      second.Inject(right, new FireCellState(0.3f, 0.9f, 0.2f, 0.1f, 0f, FireGridBurnState.Smoldering));
      second.Inject(left, new FireCellState(0.8f, 0.6f, 0.4f, 0.2f, 0f, FireGridBurnState.Burning));

      first.Step(FireGridKernel.Full27);
      second.Step(FireGridKernel.Full27);

      True(first.TryGetState(sample, out var firstSample));
      True(second.TryGetState(sample, out var secondSample));
      NearlyEqual(firstSample.Heat, secondSample.Heat, 0.000001f);
      NearlyEqual(firstSample.EmberPressure, secondSample.EmberPressure, 0.000001f);
      NearlyEqual(firstSample.Smoke, secondSample.Smoke, 0.000001f);
      NearlyEqual(firstSample.IgnitionProgress, secondSample.IgnitionProgress, 0.000001f);
    }

    private static void FireGridFootprintSamplerConvertsBoundsToOccupiedCells() {
      var footprint = FireGridFootprintSampler.FromBounds(new UnityEngine.Bounds(
        new UnityEngine.Vector3(1f, 0.5f, 1f),
        new UnityEngine.Vector3(2f, 1f, 2f)));

      Equal(new FireGridCoordinate(1, 0, 1), footprint.PrimaryCoordinate);
      Equal(4, footprint.Coordinates.Count);
      True(ContainsCoordinate(footprint.Coordinates, new FireGridCoordinate(0, 0, 0)));
      True(ContainsCoordinate(footprint.Coordinates, new FireGridCoordinate(1, 0, 1)));
      False(ContainsCoordinate(footprint.Coordinates, new FireGridCoordinate(2, 0, 1)));
    }

    private static void FireGridRuntimeStateSamplesFootprintAggregate() {
      var grid = new FireGridRuntimeState();
      var first = new FireGridCoordinate(0, 0, 0);
      var second = new FireGridCoordinate(1, 0, 0);
      var footprint = new FireGridFootprint(new[] { first, second }, first);

      grid.SetEnvironment(first, new FireCellEnvironment(FireGridStructureKind.Building, 1f, 0.2f, 0f, 0.8f, 0f, 63));
      grid.SetEnvironment(second, new FireCellEnvironment(FireGridStructureKind.Building, 1f, 0.6f, 0f, 0.4f, 0f, 63));
      grid.Inject(first, new FireCellState(0.2f, 0.1f, 0.3f, 0.2f, 0.1f, FireGridBurnState.Heating));
      grid.Inject(second, new FireCellState(0.7f, 0.4f, 0.2f, 0.8f, 0.3f, FireGridBurnState.Burning));

      var sample = grid.Sample(footprint);

      True(sample.HasActivity);
      True(sample.Burning);
      NearlyEqual(0.7f, sample.Heat);
      NearlyEqual(0.4f, sample.EmberPressure);
      NearlyEqual(0.3f, sample.Smoke);
      NearlyEqual(0.8f, sample.IgnitionProgress);
      NearlyEqual(0.3f, sample.FuelConsumed);
      NearlyEqual(0.4f, sample.MoistureDampening);
      NearlyEqual(0.6f, sample.OxygenAvailability);
    }

    private static void FireGridRuntimeStateUnderwaterCellsSuppressIgnition() {
      var grid = new FireGridRuntimeState();
      var coordinate = new FireGridCoordinate(0, 0, 0);

      grid.SetEnvironment(coordinate, new FireCellEnvironment(FireGridStructureKind.Water, 1f, 0f, 0f, 1f, 1f, 63));
      grid.Inject(coordinate, HotCell());
      grid.Step(FireGridKernel.Full27);

      False(grid.TryGetState(coordinate, out var state) && state.IsActive);
    }

    private static void FireGridRuntimeStateMoistureAndBarriersReduceTransfer() {
      var dryGrid = CreateTwoCellTransferGrid(new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 1f, 0f, 63));
      var dampBarrierGrid = CreateTwoCellTransferGrid(new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0.75f, 0.5f, 1f, 0f, 63));
      var target = new FireGridCoordinate(1, 0, 0);

      dryGrid.Step(FireGridKernel.Full27);
      dampBarrierGrid.Step(FireGridKernel.Full27);

      True(dryGrid.TryGetState(target, out var dryState));
      True(dampBarrierGrid.TryGetState(target, out var dampBarrierState));
      True(dampBarrierState.Heat < dryState.Heat);
      True(dampBarrierState.EmberPressure < dryState.EmberPressure);
    }

    private static void FireGridRuntimeStateOxygenAvailabilityChangesIgnition() {
      var highOxygenGrid = CreateTwoCellTransferGrid(new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 1f, 0f, 63));
      var lowOxygenGrid = CreateTwoCellTransferGrid(new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 0.1f, 0f, 63));
      var target = new FireGridCoordinate(1, 0, 0);

      highOxygenGrid.Step(FireGridKernel.Full27);
      lowOxygenGrid.Step(FireGridKernel.Full27);

      True(highOxygenGrid.TryGetState(target, out var highOxygenState));
      True(lowOxygenGrid.TryGetState(target, out var lowOxygenState));
      True(lowOxygenState.IgnitionProgress < highOxygenState.IgnitionProgress);
    }

    private static FireExposureSnapshot CreateExposureSnapshot(
      bool burning,
      float intensity,
      float moistureDampening = 0f) {
      return new FireExposureSnapshot(
        burning,
        intensity,
        intensity * 0.7f,
        intensity * 0.4f,
        intensity * 0.5f,
        burning ? 1f : 0f,
        burning ? 0.15f : 0f,
        moistureDampening,
        1f,
        burning ? "Grid" : "None");
    }

    private static FireGridRuntimeState CreateGridWithFuelAroundOrigin() {
      var grid = new FireGridRuntimeState();
      for (var x = -1; x <= 2; x++) {
        for (var y = -1; y <= 1; y++) {
          for (var z = -1; z <= 1; z++) {
            grid.SetEnvironment(new FireGridCoordinate(x, y, z), BurnableEnvironment());
          }
        }
      }

      return grid;
    }

    private static FireCellEnvironment BurnableEnvironment() =>
      new(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 1f, 0f, 63);

    private static FireCellState HotCell() =>
      new(1f, 1f, 0.5f, 1f, 0f, FireGridBurnState.Burning);

    private static FireGridRuntimeState CreateTwoCellTransferGrid(FireCellEnvironment targetEnvironment) {
      var grid = new FireGridRuntimeState();
      var source = new FireGridCoordinate(0, 0, 0);
      var target = new FireGridCoordinate(1, 0, 0);

      grid.SetEnvironment(source, BurnableEnvironment());
      grid.SetEnvironment(target, targetEnvironment);
      grid.Inject(source, HotCell());
      return grid;
    }

    private static FireGridKernelEntry FindKernelEntry(IReadOnlyList<FireGridKernelEntry> entries, int dx, int dy, int dz) {
      for (var i = 0; i < entries.Count; i++) {
        var entry = entries[i];
        if (entry.Offset.Dx == dx && entry.Offset.Dy == dy && entry.Offset.Dz == dz) {
          return entry;
        }
      }

      throw new InvalidOperationException($"Missing kernel entry {dx},{dy},{dz}.");
    }

    private static int CountKernelEntries(IReadOnlyList<FireGridKernelEntry> entries, int dx, int dy, int dz) {
      var count = 0;
      for (var i = 0; i < entries.Count; i++) {
        var entry = entries[i];
        if (entry.Offset.Dx == dx && entry.Offset.Dy == dy && entry.Offset.Dz == dz) {
          count++;
        }
      }

      return count;
    }

    private static bool ContainsCoordinate(IReadOnlyList<FireGridCoordinate> coordinates, FireGridCoordinate coordinate) {
      for (var i = 0; i < coordinates.Count; i++) {
        if (coordinates[i].Equals(coordinate)) {
          return true;
        }
      }

      return false;
    }

    private static void True(bool value) {
      if (!value) {
        throw new InvalidOperationException("Expected true.");
      }
    }

    private static void False(bool value) {
      if (value) {
        throw new InvalidOperationException("Expected false.");
      }
    }

    private static void Equal<T>(T expected, T actual) {
      if (!EqualityComparer<T>.Default.Equals(expected, actual)) {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
      }
    }

    private static void NearlyEqual(float expected, float actual, float tolerance = 0.0001f) {
      if (Math.Abs(expected - actual) > tolerance) {
        throw new InvalidOperationException($"Expected {expected:0.####}, got {actual:0.####}.");
      }
    }

  }
}
