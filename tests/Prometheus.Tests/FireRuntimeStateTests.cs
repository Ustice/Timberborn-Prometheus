using Mods.Prometheus.Scripts;
using UnityEngine;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireRuntimeStateTests
    {

        [Fact]
        public void SnapshotStore_RemovesAndClearsSnapshots_Test()
        {
            var state = new FireImpactRuntimeState();
            var snapshot = new FireImpactSnapshot(0.1f, 0.2f, 0.3f, 0.4f, 0.5f);

            state.SetSnapshot(42, snapshot);
            TestSupport.Equal(1, state.SnapshotCount);
            TestSupport.True(state.TryGetSnapshot(42, out var storedSnapshot));
            TestSupport.NearlyEqual(0.3f, storedSnapshot.BuildingDamagePressure);

            state.RemoveSnapshot(42);
            TestSupport.False(state.TryGetSnapshot(42, out _));

            state.SetSnapshot(43, snapshot);
            state.SetSnapshot(44, snapshot);
            state.ClearSnapshots();
            TestSupport.Equal(0, state.SnapshotCount);
        }

        [Fact]
        public void ExposureRuntimeState_ForcedIgnitionConsumesAndClears_Test()
        {
            var state = new FireExposureRuntimeState();
            TestSupport.True(state.RequestForcedIgnition(10));

            TestSupport.True(state.ConsumeForcedIgnitionRequest(10));
            TestSupport.False(state.ConsumeForcedIgnitionRequest(10));

            TestSupport.True(state.RequestForcedIgnition(10));
            state.ClearSnapshotsAndIgnitionRequests();
            TestSupport.False(state.ConsumeForcedIgnitionRequest(10));
            TestSupport.Equal(0, state.PendingForcedIgnitionCount);
        }

        [Fact]
        public void ExposureRuntimeState_DebugIgnitionBlockClearsQueuedRequests_Test()
        {
            var state = new FireExposureRuntimeState();
            TestSupport.True(state.RequestForcedIgnition(10));
            state.BlockDebugIgnitionsForSeconds(30f);
            TestSupport.False(state.RequestForcedIgnition(11));

            TestSupport.Equal(0, state.PendingForcedIgnitionCount);
            TestSupport.False(state.ConsumeForcedIgnitionRequest(10));
            TestSupport.False(state.ConsumeForcedIgnitionRequest(11));

            state.TickIgnitionBlock(31f);
            TestSupport.True(state.RequestForcedIgnition(11));
            TestSupport.True(state.ConsumeForcedIgnitionRequest(11));
        }

        [Fact]
        public void ExposureReset_ClearsActiveFireAndIgnitions_Test()
        {
            var exposure = new FireExposureRuntimeState();
            exposure.SetSnapshot(8, TestSupport.CreateExposureSnapshot(burning: true, intensity: 0.8f));
            exposure.RequestForcedIgnition(8);

            exposure.ClearSnapshotsAndIgnitionRequests();

            TestSupport.Equal(0, exposure.SnapshotCount);
            TestSupport.Equal(0, exposure.PendingForcedIgnitionCount);
        }

        [Fact]
        public void ExposureRuntimeState_ExtinguishAllBurning_DoesNotModifyDuringEnumeration_Test()
        {
            var exposure = new FireExposureRuntimeState();
            exposure.SetSnapshot(8, TestSupport.CreateExposureSnapshot(burning: true, intensity: 0.8f));
            exposure.SetSnapshot(9, TestSupport.CreateExposureSnapshot(burning: false, intensity: 0.4f));
            exposure.SetSnapshot(10, TestSupport.CreateExposureSnapshot(burning: false, intensity: 0f));

            var extinguished = exposure.ExtinguishAllBurning();

            TestSupport.Equal(2, extinguished);
            TestSupport.True(exposure.TryGetSnapshot(8, out var first));
            TestSupport.True(exposure.TryGetSnapshot(9, out var second));
            TestSupport.True(exposure.TryGetSnapshot(10, out var third));
            TestSupport.False(first.Burning);
            TestSupport.False(second.Burning);
            TestSupport.False(third.Burning);
            TestSupport.NearlyEqual(0f, first.Intensity);
            TestSupport.NearlyEqual(0f, second.Intensity);
            TestSupport.NearlyEqual(0f, third.Intensity);
        }

        [Fact]
        public void ExposureRuntimeState_SuppressionAreasExpireAndClear_Test()
        {
            var exposure = new FireExposureRuntimeState();
            var center = new FireGridCoordinate(4, 2, 6);

            TestSupport.True(exposure.RequestSuppressionArea(center, 3, 0.8f, 10f));
            TestSupport.Equal(1, exposure.ActiveSuppressionZoneCount);
            TestSupport.True(exposure.GetSuppressionStrength(center) > 0.79f);
            TestSupport.True(exposure.GetSuppressionStrength(new FireGridCoordinate(6, 2, 6)) > 0f);
            TestSupport.NearlyEqual(0f, exposure.GetSuppressionStrength(new FireGridCoordinate(9, 2, 6)));

            exposure.TickSuppression(11f);

            TestSupport.Equal(0, exposure.ActiveSuppressionZoneCount);
            TestSupport.NearlyEqual(0f, exposure.GetSuppressionStrength(center));
        }

        [Fact]
        public void FireSuppressionRules_DampenGridSampleAndCell_Test()
        {
            var sample = new FireGridSample(
              true,
              1f,
              0.8f,
              0.6f,
              0.7f,
              0f,
              0f,
              1f,
              FireGridBurnState.Burning,
              FireSourceAttribution.DebugIgnition("test"));
            var suppressedSample = FireSuppressionRules.ApplyToSample(sample, 1f);
            var suppressedCell = FireSuppressionRules.ApplyToCell(TestSupport.HotCell(), 1f);

            TestSupport.True(suppressedSample.Heat < sample.Heat);
            TestSupport.True(suppressedSample.EmberPressure < sample.EmberPressure);
            TestSupport.True(suppressedSample.Smoke < sample.Smoke);
            TestSupport.True(suppressedSample.IgnitionProgress < sample.IgnitionProgress);
            TestSupport.True(suppressedSample.MoistureDampening > sample.MoistureDampening);
            TestSupport.True(suppressedCell.Heat < TestSupport.HotCell().Heat);
            TestSupport.True(FireSuppressionRules.FuelConsumptionMultiplier(1f) < 1f);
        }

        [Fact]
        public void FireGridRuntimeState_ApplySuppressionAreaDampensActiveCells_Test()
        {
            var grid = TestSupport.CreateGridWithFuelAroundOrigin();
            var center = new FireGridCoordinate(0, 0, 0);
            var outside = new FireGridCoordinate(5, 0, 0);
            grid.Inject(center, TestSupport.HotCell());
            grid.SetEnvironment(outside, TestSupport.BurnableEnvironment());
            grid.Inject(outside, TestSupport.HotCell());

            var dampedCells = grid.ApplySuppressionArea(new FireSuppressionZoneSnapshot(center, 2, 1f, 10f));

            TestSupport.Equal(1, dampedCells);
            TestSupport.True(grid.TryGetState(center, out var suppressed));
            TestSupport.True(grid.TryGetState(outside, out var untouched));
            TestSupport.True(suppressed.Heat < untouched.Heat);
            TestSupport.NearlyEqual(1f, untouched.Heat);
        }

        [Fact]
        public void RecoveryReset_ClearsAshenState_Test()
        {
            var state = new FireRecoveryRuntimeState();
            state.SetSnapshot(2, new FireRecoverySnapshot(true, 0.2f, 0.1f, 0.1f, 12f));

            state.ClearSnapshots();

            TestSupport.Equal(0, state.SnapshotCount);
            TestSupport.False(state.TryGetSnapshot(2, out _));
        }

        [Fact]
        public void FireResetRegistry_ResetAllRunsGlobalAndEntityHooks_Test()
        {
            var grid = new FireGridRuntimeState();
            var exposure = new FireExposureRuntimeState();
            var impact = new FireImpactRuntimeState();
            var damage = new FireDamageStateRuntimeState();
            var projection = new FireRuntimeProjectionRuntimeState();
            var recovery = new FireRecoveryRuntimeState();
            var ashTelemetry = new FertileAshRecoveredGoodStackTelemetryState();
            var ashDeposits = new FireBurnedGroundAshDepositRuntimeState();
            var fieldAmendments = new FireFieldAmendmentRuntimeState();
            var previews = new FireVisualEffectPreviewRuntimeState();
            var registry = new FireResetRegistry(grid, exposure, impact, damage, projection, recovery, ashTelemetry, ashDeposits, fieldAmendments, previews);
            var entityHookCount = 0;

            registry.RegisterGlobal(FireResetHookKind.BeaverEffect, "test-beaver", () => { });
            registry.RegisterEntity(42, FireResetHookKind.WorkplaceEffect, "test-workplace", () => entityHookCount++);
            exposure.RequestForcedIgnition(42);
            impact.SetSnapshot(42, new FireImpactSnapshot(0.1f, 0.2f, 0.3f, 0.4f, 0.5f));
            damage.SetSnapshot(42, new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Burning, 0.7f, 0.2f, 3));
            projection.SetImpact(42, new FireImpactSnapshot(0.1f, 0.2f, 0.3f, 0.4f, 0.5f));
            recovery.SetSnapshot(42, new FireRecoverySnapshot(true, 0.12f, 0.1f, 0.05f, 4f));
            ashTelemetry.RecordQueuedStack(
                2,
                new FertileAshSpawnTelemetryContext("BurnedOut", "vegetation", "tree", 42));
            ashDeposits.TryRecordDeposit(
                new Vector3Int(1, 0, 2),
                42,
                2,
                new FertileAshSpawnTelemetryContext("BurnedOut", "vegetation", "tree", 42),
                out _);
            fieldAmendments.SetAmendment(new FireGridCoordinate(1, 0, 2), 12f, 3);

            var result = registry.ResetAll("test");

            TestSupport.True(result.GlobalHookCount >= 7);
            TestSupport.Equal(1, result.EntityHookCount);
            TestSupport.Equal(1, result.EntityCount);
            TestSupport.Equal(0, result.FailureCount);
            TestSupport.Equal(1, entityHookCount);
            TestSupport.Equal(0, exposure.PendingForcedIgnitionCount);
            TestSupport.Equal(0, impact.SnapshotCount);
            TestSupport.Equal(0, damage.SnapshotCount);
            TestSupport.Equal(0, projection.SnapshotCount);
            TestSupport.Equal(0, recovery.SnapshotCount);
            TestSupport.Equal(0, ashTelemetry.QueuedStackCount);
            TestSupport.Equal(0, ashTelemetry.QueuedAshAmount);
            TestSupport.Equal(0, ashDeposits.DepositCount);
            TestSupport.Equal(0, fieldAmendments.ActiveAmendmentCount);
        }

        [Fact]
        public void FertileAshSpawnPolicy_UsesTreeRemnantHarvestForCharredTrees_Test()
        {
            TestSupport.True(FertileAshSpawnPolicy.ShouldUseRemnantHarvest(FireAftermathSourceKind.CharredTree));
            TestSupport.False(FertileAshSpawnPolicy.ShouldUseRemnantHarvest(FireAftermathSourceKind.CharredCrop));
            TestSupport.False(FertileAshSpawnPolicy.ShouldUseRemnantHarvest(FireAftermathSourceKind.CharredBuilding));
            TestSupport.Equal("charred_tree_remnant_harvest", FertileAshSpawnPolicy.CharredTreeRemnantHarvestReason);
        }

        [Fact]
        public void FireResetRegistry_UnregistersDisposedEntityHooks_Test()
        {
            var registry = new FireResetRegistry(
                new FireGridRuntimeState(),
                new FireExposureRuntimeState(),
                new FireImpactRuntimeState(),
                new FireDamageStateRuntimeState(),
                new FireRuntimeProjectionRuntimeState(),
                new FireRecoveryRuntimeState(),
                new FertileAshRecoveredGoodStackTelemetryState(),
                new FireBurnedGroundAshDepositRuntimeState(),
                new FireFieldAmendmentRuntimeState(),
                new FireVisualEffectPreviewRuntimeState());
            var entityHookCount = 0;
            var registration = registry.RegisterEntity(8, FireResetHookKind.VisualEffect, "test-visual", () => entityHookCount++);

            registration.Dispose();
            var result = registry.ResetAll("test");

            TestSupport.Equal(0, result.EntityHookCount);
            TestSupport.Equal(0, result.EntityCount);
            TestSupport.Equal(0, entityHookCount);
        }

        [Fact]
        public void FireResetRegistry_IsolatesFailingHooksAndContinues_Test()
        {
            var registry = new FireResetRegistry(
                new FireGridRuntimeState(),
                new FireExposureRuntimeState(),
                new FireImpactRuntimeState(),
                new FireDamageStateRuntimeState(),
                new FireRuntimeProjectionRuntimeState(),
                new FireRecoveryRuntimeState(),
                new FertileAshRecoveredGoodStackTelemetryState(),
                new FireBurnedGroundAshDepositRuntimeState(),
                new FireFieldAmendmentRuntimeState(),
                new FireVisualEffectPreviewRuntimeState());
            var successfulEntityHookCount = 0;

            registry.RegisterEntity(8, FireResetHookKind.VisualEffect, "failing-visual", () => throw new MissingReferenceException("destroyed visual target"));
            registry.RegisterEntity(9, FireResetHookKind.WorkplaceEffect, "successful-workplace", () => successfulEntityHookCount++);

            var firstResult = registry.ResetAll("test");
            var secondResult = registry.ResetAll("test");

            TestSupport.Equal(1, firstResult.FailureCount);
            TestSupport.Equal(2, firstResult.EntityHookCount);
            TestSupport.Equal(2, firstResult.EntityCount);
            TestSupport.Equal(1, secondResult.EntityHookCount);
            TestSupport.Equal(1, secondResult.EntityCount);
            TestSupport.Equal(0, secondResult.FailureCount);
            TestSupport.Equal(2, successfulEntityHookCount);
        }

    }
}
