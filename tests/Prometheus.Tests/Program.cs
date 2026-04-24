using System;
using System.Collections.Generic;
using Mods.Prometheus.Scripts;
using UnityEngine;
using Xunit;

namespace Prometheus.Tests {
  public sealed class Program {

    [Fact]
    public void SnapshotStore_RemovesAndClearsSnapshots_Test() => SnapshotStoreRemovesAndClearsSnapshots();

    [Fact]
    public void SimulationRuntimeState_ForcedIgnitionConsumesAndClears_Test() => SimulationRuntimeStateForcedIgnitionConsumesAndClears();

    [Fact]
    public void SimulationRuntimeState_SpreadIgnitionKeepsStrongestRequest_Test() => SimulationRuntimeStateSpreadIgnitionKeepsStrongestRequest();

    [Fact]
    public void SimulationRuntimeState_SpreadIgnitionIgnoresInvalidIds_Test() => SimulationRuntimeStateSpreadIgnitionIgnoresInvalidIds();

    [Fact]
    public void EntityRegistry_ComputesSpreadPressureOnlyFromBurningNeighbors_Test() => EntityRegistryComputesSpreadPressureOnlyFromBurningNeighbors();

    [Fact]
    public void EntityRegistry_FindsNearestNonBurningTarget_Test() => EntityRegistryFindsNearestNonBurningTarget();

    [Fact]
    public void DamageResetSnapshot_CanPublishHealthy_Test() => DamageResetSnapshotCanPublishHealthy();

    [Fact]
    public void SimulationReset_ClearsActiveFireAndIgnitions_Test() => SimulationResetClearsActiveFireAndIgnitions();

    [Fact]
    public void RecoveryReset_ClearsAshenState_Test() => RecoveryResetClearsAshenState();

    [Fact]
    public void TerminalDeadBuildingSnapshot_CannotBurnOrSpread_Test() => TerminalDeadBuildingSnapshotCannotBurnOrSpread();

    [Fact]
    public void DamageStateThresholds_EncodeLifecycleDecisions_Test() => DamageStateThresholdsEncodeLifecycleDecisions();

    [Fact]
    public void ResponseStateThresholds_EncodeDispatchReadability_Test() => ResponseStateThresholdsEncodeDispatchReadability();

    [Fact]
    public void WorkplaceSupportComponentClassification_PreservesSuppressionBoundary_Test() => WorkplaceSupportComponentClassificationPreservesSuppressionBoundary();

    [Fact]
    public void OperationalComponentClassification_AvoidsFireAndWorkplaceInternals_Test() => OperationalComponentClassificationAvoidsFireAndWorkplaceInternals();

    private static void SnapshotStoreRemovesAndClearsSnapshots() {
      var state = new FireSuppressionRuntimeState();
      var snapshot = new FireSuppressionSnapshot("BucketBrigade", 1f, 0.25f, 0.5f, 6f, 0.08f);

      state.SetSnapshot(42, snapshot);
      Equal(1, state.SnapshotCount);
      True(state.TryGetSnapshot(42, out var storedSnapshot));
      Equal("BucketBrigade", storedSnapshot.FactionApproach);

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

    private static void SimulationRuntimeStateSpreadIgnitionKeepsStrongestRequest() {
      var state = new FireSimulationRuntimeState();

      state.RequestSpreadIgnition(20, 10, 0.4f);
      state.RequestSpreadIgnition(20, 11, 0.2f);
      True(state.ConsumeSpreadIgnitionRequest(20, out var request));
      Equal(10, request.SourceEntityId);
      NearlyEqual(0.4f, request.PropagationChance);

      state.RequestSpreadIgnition(20, 10, 0.2f);
      state.RequestSpreadIgnition(20, 11, 0.7f, PropagationIgnitionSourceKind.Explosion);
      True(state.ConsumeSpreadIgnitionRequest(20, out request));
      Equal(11, request.SourceEntityId);
      NearlyEqual(0.7f, request.PropagationChance);
      Equal(PropagationIgnitionSourceKind.Explosion, request.SourceKind);
    }

    private static void SimulationRuntimeStateSpreadIgnitionIgnoresInvalidIds() {
      var state = new FireSimulationRuntimeState();

      state.RequestSpreadIgnition(0, 10, 0.4f);
      state.RequestSpreadIgnition(20, 0, 0.4f);
      state.RequestSpreadIgnition(20, 20, 0.4f);

      Equal(0, state.PendingSpreadIgnitionCount);
      False(state.ConsumeSpreadIgnitionRequest(20, out _));
    }

    private static void EntityRegistryComputesSpreadPressureOnlyFromBurningNeighbors() {
      var state = new FireEntityRegistryRuntimeState();
      state.SetSnapshot(1, new FireEntityRegistrySnapshot(new Vector3(0f, 0f, 0f), false, 0f, 0.1f));
      state.SetSnapshot(2, new FireEntityRegistrySnapshot(new Vector3(5f, 0f, 0f), true, 0.5f, 0.1f));
      state.SetSnapshot(3, new FireEntityRegistrySnapshot(new Vector3(1f, 0f, 0f), false, 1f, 0.1f));
      state.SetSnapshot(4, new FireEntityRegistrySnapshot(new Vector3(20f, 0f, 0f), true, 1f, 0.1f));

      NearlyEqual(0.025f, state.ComputeNeighborSpreadPressure(1, Vector3.zero, 10f));
    }

    private static void EntityRegistryFindsNearestNonBurningTarget() {
      var state = new FireEntityRegistryRuntimeState();
      state.SetSnapshot(1, new FireEntityRegistrySnapshot(Vector3.zero, true, 1f, 0.1f));
      state.SetSnapshot(2, new FireEntityRegistrySnapshot(new Vector3(7f, 0f, 0f), false, 0f, 0.1f));
      state.SetSnapshot(3, new FireEntityRegistrySnapshot(new Vector3(4f, 0f, 0f), false, 0f, 0.1f));

      True(state.TryGetNearestSpreadTarget(1, Vector3.zero, 10f, out var targetEntityId, out var normalizedDistance));
      Equal(3, targetEntityId);
      NearlyEqual(0.4f, normalizedDistance);
    }

    private static void DamageResetSnapshotCanPublishHealthy() {
      var state = new FireDamageStateRuntimeState();

      state.SetSnapshot(5, new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Dead, 1f, 1f, 12));
      state.SetSnapshot(5, new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Healthy, 0f, 0f, 0));

      True(state.TryGetSnapshot(5, out var snapshot));
      Equal(FireDamageState.Healthy, snapshot.State);
      NearlyEqual(0f, snapshot.Severity);
      NearlyEqual(0f, snapshot.TickProgress);
      Equal(0, snapshot.DamageTicksApplied);
    }

    private static void SimulationResetClearsActiveFireAndIgnitions() {
      var simulation = new FireSimulationRuntimeState();
      var registry = new FireEntityRegistryRuntimeState();
      simulation.SetSnapshot(8, CreateSimulationSnapshot(burning: true, intensity: 0.8f));
      simulation.RequestForcedIgnition(8);
      simulation.RequestSpreadIgnition(9, 8, 0.4f);
      registry.SetSnapshot(8, new FireEntityRegistrySnapshot(Vector3.zero, true, 0.8f, 0.1f));

      simulation.ClearSnapshotsAndIgnitionRequests();
      registry.ClearSnapshots();

      Equal(0, simulation.SnapshotCount);
      Equal(0, simulation.PendingForcedIgnitionCount);
      Equal(0, simulation.PendingSpreadIgnitionCount);
      Equal(0, registry.SnapshotCount);
    }

    private static void RecoveryResetClearsAshenState() {
      var state = new FireRecoveryRuntimeState();
      state.SetSnapshot(2, new FireRecoverySnapshot(true, true, 0.2f, 0.1f, 0.1f, 12f));

      state.ClearSnapshots();

      Equal(0, state.SnapshotCount);
      False(state.TryGetSnapshot(2, out _));
    }

    private static void TerminalDeadBuildingSnapshotCannotBurnOrSpread() {
      var snapshot = FireSimulationRules.CreateTerminalDeadBuildingSnapshot();

      False(snapshot.Burning);
      NearlyEqual(0f, snapshot.Intensity);
      NearlyEqual(0f, snapshot.HeatExposure);
      NearlyEqual(0f, snapshot.SpreadPressure);
      NearlyEqual(0f, snapshot.IgnitionChance);
      Equal("DeadBuilding", snapshot.DominantIgnitionSource);
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

    private static void ResponseStateThresholdsEncodeDispatchReadability() {
      Equal("Stabilized", FireSimulationRules.DetermineResponseState(false, 0f, 0f, 0f));
      Equal("Overwhelmed", FireSimulationRules.DetermineResponseState(true, 0.7f, 0.3f, 0.2f));
      Equal("Contained", FireSimulationRules.DetermineResponseState(true, 0.4f, 0.1f, 0.12f));
      Equal("Stabilized", FireSimulationRules.DetermineResponseState(true, 0.5f, 0.1f, 0.12f));
    }

    private static void WorkplaceSupportComponentClassificationPreservesSuppressionBoundary() {
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

    private static FireSimulationSnapshot CreateSimulationSnapshot(bool burning, float intensity) {
      return new FireSimulationSnapshot(
        burning,
        intensity,
        intensity * 0.7f,
        0.2f,
        intensity * 0.1f,
        intensity * 0.05f,
        intensity * 0.2f,
        burning ? "Weather" : "None",
        0.1f,
        0.1f,
        0f,
        0f,
        0f,
        0.4f,
        0.5f,
        0.2f);
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
