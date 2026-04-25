using System;
using System.Collections.Generic;
using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests {
  public sealed class Program {

    [Fact]
    public void SnapshotStore_RemovesAndClearsSnapshots_Test() => SnapshotStoreRemovesAndClearsSnapshots();

    [Fact]
    public void SimulationRuntimeState_ForcedIgnitionConsumesAndClears_Test() => SimulationRuntimeStateForcedIgnitionConsumesAndClears();

    [Fact]
    public void SimulationRuntimeState_DebugIgnitionBlockClearsQueuedRequests_Test() => SimulationRuntimeStateDebugIgnitionBlockClearsQueuedRequests();

    [Fact]
    public void SimulationReset_ClearsActiveFireAndIgnitions_Test() => SimulationResetClearsActiveFireAndIgnitions();

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

    private static void SimulationRuntimeStateForcedIgnitionConsumesAndClears() {
      var state = new FireSimulationRuntimeState();
      state.RequestForcedIgnition(10);

      True(state.ConsumeForcedIgnitionRequest(10));
      False(state.ConsumeForcedIgnitionRequest(10));

      state.RequestForcedIgnition(10);
      state.ClearSnapshotsAndIgnitionRequests();
      False(state.ConsumeForcedIgnitionRequest(10));
      Equal(0, state.PendingForcedIgnitionCount);
    }

    private static void SimulationRuntimeStateDebugIgnitionBlockClearsQueuedRequests() {
      var state = new FireSimulationRuntimeState();
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

    private static void SimulationResetClearsActiveFireAndIgnitions() {
      var simulation = new FireSimulationRuntimeState();
      simulation.SetSnapshot(8, CreateSimulationSnapshot(burning: true, intensity: 0.8f));
      simulation.RequestForcedIgnition(8);

      simulation.ClearSnapshotsAndIgnitionRequests();

      Equal(0, simulation.SnapshotCount);
      Equal(0, simulation.PendingForcedIgnitionCount);
    }

    private static void RecoveryResetClearsAshenState() {
      var state = new FireRecoveryRuntimeState();
      state.SetSnapshot(2, new FireRecoverySnapshot(true, 0.2f, 0.1f, 0.1f, 12f));

      state.ClearSnapshots();

      Equal(0, state.SnapshotCount);
      False(state.TryGetSnapshot(2, out _));
    }

    private static void TerminalDeadBuildingSnapshotCannotBurn() {
      var snapshot = FireSimulationRules.CreateTerminalDeadBuildingSnapshot();

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
      False(FireWorkplaceRules.IsOperationalComponentName("FireSimulationController"));
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
      True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.DebugResetFireSimulation) >= 0);
      True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.WorkplaceIndoorExposure) >= 0);
      True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.GridIgnitionSeeded) >= 0);
    }

    private static void FireVisualEffectRulesDryBurningFireProducesReadableEffects() {
      var intensity = FireVisualEffectRules.ComputeIntensity(
        CreateSimulationSnapshot(burning: true, intensity: 0.8f),
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
      var drySimulation = CreateSimulationSnapshot(burning: true, intensity: 0.8f, moistureDampening: 0f);
      var wetSimulation = CreateSimulationSnapshot(burning: true, intensity: 0.8f, moistureDampening: 0.9f);
      var damage = new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Burning, 0.65f, 0.5f, 3);
      var dry = FireVisualEffectRules.ComputeIntensity(drySimulation, damage, FireVisualEffectTuning.Default);
      var wet = FireVisualEffectRules.ComputeIntensity(wetSimulation, damage, FireVisualEffectTuning.Default);

      True(wet.Steam > dry.Steam);
      True(wet.Fire < dry.Fire);
      True(wet.Smoke < dry.Smoke);
    }

    private static void FireVisualEffectRulesDeadDamageStateKeepsCharWithoutFire() {
      var intensity = FireVisualEffectRules.ComputeIntensity(
        CreateSimulationSnapshot(burning: true, intensity: 1f),
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

    private static FireSimulationSnapshot CreateSimulationSnapshot(
      bool burning,
      float intensity,
      float moistureDampening = 0f) {
      return new FireSimulationSnapshot(
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
