using Mods.Prometheus.Scripts;
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
            state.RequestForcedIgnition(10);

            TestSupport.True(state.ConsumeForcedIgnitionRequest(10));
            TestSupport.False(state.ConsumeForcedIgnitionRequest(10));

            state.RequestForcedIgnition(10);
            state.ClearSnapshotsAndIgnitionRequests();
            TestSupport.False(state.ConsumeForcedIgnitionRequest(10));
            TestSupport.Equal(0, state.PendingForcedIgnitionCount);
        }

        [Fact]
        public void ExposureRuntimeState_DebugIgnitionBlockClearsQueuedRequests_Test()
        {
            var state = new FireExposureRuntimeState();
            state.RequestForcedIgnition(10);
            state.BlockDebugIgnitionsForSeconds(30f);
            state.RequestForcedIgnition(11);

            TestSupport.Equal(0, state.PendingForcedIgnitionCount);
            TestSupport.False(state.ConsumeForcedIgnitionRequest(10));
            TestSupport.False(state.ConsumeForcedIgnitionRequest(11));

            state.TickIgnitionBlock(31f);
            state.RequestForcedIgnition(11);
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
            var recovery = new FireRecoveryRuntimeState();
            var previews = new FireVisualEffectPreviewRuntimeState();
            var registry = new FireResetRegistry(grid, exposure, impact, damage, recovery, previews);
            var entityHookCount = 0;

            registry.RegisterGlobal(FireResetHookKind.BeaverEffect, "test-beaver", () => { });
            registry.RegisterEntity(42, FireResetHookKind.WorkplaceEffect, "test-workplace", () => entityHookCount++);
            exposure.RequestForcedIgnition(42);
            impact.SetSnapshot(42, new FireImpactSnapshot(0.1f, 0.2f, 0.3f, 0.4f, 0.5f));
            damage.SetSnapshot(42, new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Burning, 0.7f, 0.2f, 3));
            recovery.SetSnapshot(42, new FireRecoverySnapshot(true, 0.12f, 0.1f, 0.05f, 4f));

            var result = registry.ResetAll("test");

            TestSupport.True(result.GlobalHookCount >= 7);
            TestSupport.Equal(1, result.EntityHookCount);
            TestSupport.Equal(1, result.EntityCount);
            TestSupport.Equal(0, result.FailureCount);
            TestSupport.Equal(1, entityHookCount);
            TestSupport.Equal(0, exposure.PendingForcedIgnitionCount);
            TestSupport.Equal(0, impact.SnapshotCount);
            TestSupport.Equal(0, damage.SnapshotCount);
            TestSupport.Equal(0, recovery.SnapshotCount);
        }

        [Fact]
        public void FireResetRegistry_UnregistersDisposedEntityHooks_Test()
        {
            var registry = new FireResetRegistry(
                new FireGridRuntimeState(),
                new FireExposureRuntimeState(),
                new FireImpactRuntimeState(),
                new FireDamageStateRuntimeState(),
                new FireRecoveryRuntimeState(),
                new FireVisualEffectPreviewRuntimeState());
            var entityHookCount = 0;
            var registration = registry.RegisterEntity(8, FireResetHookKind.VisualEffect, "test-visual", () => entityHookCount++);

            registration.Dispose();
            var result = registry.ResetAll("test");

            TestSupport.Equal(0, result.EntityHookCount);
            TestSupport.Equal(0, result.EntityCount);
            TestSupport.Equal(0, entityHookCount);
        }

    }
}
