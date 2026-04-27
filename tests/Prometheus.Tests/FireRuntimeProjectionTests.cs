using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireRuntimeProjectionTests
    {

        [Fact]
        public void RuntimeProjection_StoresLatestSubsystemSnapshots_Test()
        {
            var state = new FireRuntimeProjectionRuntimeState();
            var exposure = new FireExposureSnapshot(true, 0.8f, 0.7f, 0.4f, 0.3f, 1f, 0.15f, 0f, 1f, "Grid");
            var impact = FireRuntimeProjectionRules.CreateImpact(exposure, 1f);
            var damage = new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Burning, 0.65f, 0.2f, 2);
            var recovery = new FireRecoverySnapshot(true, 0.12f, 0.1f, 0.05f, 4f);

            state.SetExposure(12, exposure);
            state.SetImpact(12, impact);
            state.SetDamageState(12, damage);
            state.SetRecovery(12, recovery);

            TestSupport.True(state.TryGetSnapshot(12, out var projection));
            TestSupport.True(projection.HasExposure);
            TestSupport.True(projection.HasImpact);
            TestSupport.True(projection.HasDamageState);
            TestSupport.True(projection.HasRecovery);
            TestSupport.NearlyEqual(0.8f, projection.Exposure.Intensity);
            TestSupport.NearlyEqual(impact.BuildingDamagePressure, projection.Impact.BuildingDamagePressure);
            TestSupport.Equal(FireDamageState.Burning, projection.DamageState.State);
            TestSupport.True(projection.Recovery.FertileAshAvailable);
        }

        [Fact]
        public void RuntimeProjection_ImpactRulesPreserveExistingPressureContract_Test()
        {
            var exposure = new FireExposureSnapshot(true, 0.6f, 0.8f, 0.5f, 0.4f, 1f, 0.15f, 0f, 1f, "Grid");

            var impact = FireRuntimeProjectionRules.CreateImpact(exposure, 0.5f);

            TestSupport.NearlyEqual(0.32f, impact.CropDamagePressure);
            TestSupport.NearlyEqual(0.26f, impact.TreeDamagePressure);
            TestSupport.NearlyEqual(0.18f, impact.BuildingDamagePressure);
            TestSupport.NearlyEqual(0.39f, impact.DehydrationPressure);
            TestSupport.NearlyEqual(0.314f, impact.InjuryPressure);
        }

        [Fact]
        public void RuntimeProjection_SetExposurePreservesOtherSlices_Test()
        {
            var state = new FireRuntimeProjectionRuntimeState();
            var entityId = 31;
            var impact = new FireImpactSnapshot(0.1f, 0.2f, 0.3f, 0.4f, 0.5f);
            var damage = new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Burning, 0.65f, 0.2f, 2);
            var recovery = new FireRecoverySnapshot(true, 0.12f, 0.1f, 0.05f, 4f);

            state.SetImpact(entityId, impact);
            state.SetDamageState(entityId, damage);
            state.SetRecovery(entityId, recovery);
            state.SetExposure(entityId, FireExposureRules.CreateColdSnapshot("DebugResetFireExposure"));

            TestSupport.True(state.TryGetSnapshot(entityId, out var projection));
            TestSupport.True(projection.HasExposure);
            TestSupport.True(projection.HasImpact);
            TestSupport.True(projection.HasDamageState);
            TestSupport.True(projection.HasRecovery);
            TestSupport.False(projection.Exposure.Burning);
            TestSupport.Equal("DebugResetFireExposure", projection.Exposure.DominantSource);
            TestSupport.NearlyEqual(impact.BuildingDamagePressure, projection.Impact.BuildingDamagePressure);
            TestSupport.Equal(damage.State, projection.DamageState.State);
            TestSupport.True(projection.Recovery.FertileAshAvailable);
        }

        [Fact]
        public void RuntimeProjection_WorkplaceRulesUseDamageStateAndImpactTogether_Test()
        {
            var projection = FireRuntimeProjectionRules.EmptyProjection
              .WithImpact(new FireImpactSnapshot(0f, 0f, 0.4f, 0.2f, 0.1f))
              .WithDamageState(new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Burning, 0.7f, 0.2f, 2));

            var speedMultiplier = FireRuntimeProjectionRules.ComputeWorkplaceSpeedMultiplier(projection, true);

            TestSupport.NearlyEqual(0.37f, speedMultiplier);
            TestSupport.False(FireRuntimeProjectionRules.ShouldDisableWorkplaceOperations(projection, true));

            var deadProjection = projection.WithDamageState(new FireDamageStateSnapshot(FireDamageCategory.Building, FireDamageState.Dead, 1f, 1f, 9));

            TestSupport.NearlyEqual(0f, FireRuntimeProjectionRules.ComputeWorkplaceSpeedMultiplier(deadProjection, true));
            TestSupport.True(FireRuntimeProjectionRules.ShouldDisableWorkplaceOperations(deadProjection, true));
        }

        [Fact]
        public void RuntimeProjection_VisualRulesReadProjectionFallbacks_Test()
        {
            var emptyIntensity = FireVisualEffectRules.ComputeIntensity(
              FireRuntimeProjectionRules.EmptyProjection,
              FireVisualEffectTuning.Default);

            TestSupport.NearlyEqual(0f, emptyIntensity.Fire);
            TestSupport.True(emptyIntensity.Char > 0.7f);

            var burningProjection = FireRuntimeProjectionRules.EmptyProjection
              .WithExposure(TestSupport.CreateExposureSnapshot(burning: true, intensity: 0.85f, moistureDampening: 0f))
              .WithDamageState(new FireDamageStateSnapshot(FireDamageCategory.Tree, FireDamageState.Burning, 0.6f, 0.2f, 2));
            var burningIntensity = FireVisualEffectRules.ComputeIntensity(burningProjection, FireVisualEffectTuning.Default);

            TestSupport.True(burningIntensity.Fire > 0.8f);
            TestSupport.True(burningIntensity.Smoke > 0.4f);
        }

    }
}
