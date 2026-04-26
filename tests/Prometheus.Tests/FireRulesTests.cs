using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireRulesTests
    {

        [Fact]
        public void TerminalDeadBuildingSnapshot_CannotBurn_Test()
        {
            var snapshot = FireExposureRules.CreateTerminalDeadBuildingSnapshot();

            TestSupport.False(snapshot.Burning);
            TestSupport.NearlyEqual(0f, snapshot.Intensity);
            TestSupport.NearlyEqual(0f, snapshot.HeatExposure);
            TestSupport.NearlyEqual(0f, snapshot.EmberPressure);
            TestSupport.Equal("DeadBuilding", snapshot.DominantSource);
        }

        [Fact]
        public void DamageStateThresholds_EncodeLifecycleDecisions_Test()
        {
            TestSupport.Equal(FireDamageState.Healthy, FireDamageStateRules.DetermineState(0f));
            TestSupport.Equal(FireDamageState.Healthy, FireDamageStateRules.DetermineState(0.199f));
            TestSupport.Equal(FireDamageState.Scorched, FireDamageStateRules.DetermineState(0.2f));
            TestSupport.Equal(FireDamageState.Scorched, FireDamageStateRules.DetermineState(0.599f));
            TestSupport.Equal(FireDamageState.Burning, FireDamageStateRules.DetermineState(0.6f));
            TestSupport.Equal(FireDamageState.Burning, FireDamageStateRules.DetermineState(0.949f));
            TestSupport.Equal(FireDamageState.Dead, FireDamageStateRules.DetermineState(0.95f));
            TestSupport.Equal(FireDamageState.Dead, FireDamageStateRules.DetermineState(1f));
        }

        [Fact]
        public void WorkplaceSupportComponentClassification_PreservesWorkplaceBoundary_Test()
        {
            TestSupport.True(FireWorkplaceRules.IsWorkplaceSupportComponentName("Workplace"));
            TestSupport.True(FireWorkplaceRules.IsWorkplaceSupportComponentName("BakeryWorkplace"));
            TestSupport.True(FireWorkplaceRules.IsWorkplaceSupportComponentName("WorkplaceWorkerTracker"));
            TestSupport.False(FireWorkplaceRules.IsWorkplaceSupportComponentName("WorkplaceBonuses"));
            TestSupport.False(FireWorkplaceRules.IsWorkplaceSupportComponentName("Manufactory"));
            TestSupport.False(FireWorkplaceRules.IsWorkplaceSupportComponentName(""));
            TestSupport.False(FireWorkplaceRules.IsWorkplaceSupportComponentName(null));
        }

        [Fact]
        public void OperationalComponentClassification_AvoidsFireAndWorkplaceInternals_Test()
        {
            TestSupport.True(FireWorkplaceRules.IsOperationalComponentName("Manufactory"));
            TestSupport.True(FireWorkplaceRules.IsOperationalComponentName("SimpleManufactoryBehaviors"));
            TestSupport.True(FireWorkplaceRules.IsOperationalComponentName("Workshop"));
            TestSupport.True(FireWorkplaceRules.IsOperationalComponentName("RecipeSelector"));
            TestSupport.False(FireWorkplaceRules.IsOperationalComponentName("FireExposureController"));
            TestSupport.False(FireWorkplaceRules.IsOperationalComponentName("Workplace"));
            TestSupport.False(FireWorkplaceRules.IsOperationalComponentName("WorkplaceBonuses"));
            TestSupport.False(FireWorkplaceRules.IsOperationalComponentName("Deteriorable"));
            TestSupport.False(FireWorkplaceRules.IsOperationalComponentName(""));
            TestSupport.False(FireWorkplaceRules.IsOperationalComponentName(null));
        }

        [Fact]
        public void BeaverExposureRules_ScaleByProximity_Test()
        {
            var impactSnapshot = new FireImpactSnapshot(0f, 0f, 0f, 0.8f, 0.4f);

            var nearDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, 0f);
            var halfDistanceDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, FireBeaverExposureRules.EffectRadius * 0.5f);
            var outsideDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, FireBeaverExposureRules.EffectRadius + 1f);

            TestSupport.NearlyEqual(-0.00008f, nearDeltas.ThirstDelta, 0.000001f);
            TestSupport.NearlyEqual(-0.0003f, nearDeltas.HeatStressDelta, 0.000001f);
            TestSupport.NearlyEqual(nearDeltas.ThirstDelta * 0.5f, halfDistanceDeltas.ThirstDelta, 0.000001f);
            TestSupport.NearlyEqual(nearDeltas.HeatStressDelta * 0.5f, halfDistanceDeltas.HeatStressDelta, 0.000001f);
            TestSupport.False(outsideDeltas.HasEffect);
        }

        [Fact]
        public void BeaverExposureRules_IndoorExposureUsesFullPressure_Test()
        {
            var impactSnapshot = new FireImpactSnapshot(0f, 0f, 0f, 0.8f, 0.4f);

            var proximityDeltas = FireBeaverExposureRules.ComputeProximityNeedDeltas(impactSnapshot, 0f);
            var indoorDeltas = FireBeaverExposureRules.ComputeIndoorNeedDeltas(impactSnapshot);

            TestSupport.NearlyEqual(proximityDeltas.ThirstDelta, indoorDeltas.ThirstDelta, 0.000001f);
            TestSupport.NearlyEqual(proximityDeltas.HeatStressDelta, indoorDeltas.HeatStressDelta, 0.000001f);
            TestSupport.True(indoorDeltas.HasEffect);
        }

        [Fact]
        public void FireIgnitionRules_DryFuelIgnitesMoreReadily_Test()
        {
            var wetProbability = FireIgnitionRules.ComputeIgnitionProbability(0.8f, 0.5f, 1f, 1f, 0.9f, 0.45f, 0.5f);
            var dryProbability = FireIgnitionRules.ComputeIgnitionProbability(0.8f, 0.5f, 1f, 1f, 0f, 0.45f, 0.5f);

            TestSupport.NearlyEqual(0f, wetProbability);
            TestSupport.True(dryProbability > wetProbability);
        }

    }
}
