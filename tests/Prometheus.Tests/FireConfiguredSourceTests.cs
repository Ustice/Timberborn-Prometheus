using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireConfiguredSourceTests
    {

        [Fact]
        public void ConfiguredSourceInjector_UsesSourceFieldsRadiusAndAttribution_Test()
        {
            var spec = new FireConfiguredSourceSpec(0.8f, 0.4f, 0.2f, 1f, false);
            var origin = new FireGridCoordinate(0, 0, 0);
            var footprint = new FireGridFootprint(new[] { origin }, origin);

            var injections = FireConfiguredSourceInjector.CreateInjections(footprint, spec, "kiln-42");

            TestSupport.Equal(7, injections.Count);
            FireGridSourceInjection? primaryInjection = null;
            foreach (var injection in injections)
            {
                if (injection.Coordinate.Equals(origin))
                {
                    primaryInjection = injection;
                }
            }

            TestSupport.True(primaryInjection.HasValue);
            TestSupport.Equal(FireSourceKind.ConfiguredSource, primaryInjection.Value.SourceAttribution.Kind);
            TestSupport.Equal("kiln-42", primaryInjection.Value.SourceAttribution.Identity);
            TestSupport.NearlyEqual(0.8f, primaryInjection.Value.State.Heat);
            TestSupport.NearlyEqual(0.4f, primaryInjection.Value.State.EmberPressure);
            TestSupport.NearlyEqual(0.2f, primaryInjection.Value.State.Smoke);
        }

        [Fact]
        public void ConfiguredSourceInjector_RequiresActiveOperationWhenRequested_Test()
        {
            var freeSource = new FireConfiguredSourceSpec(0.5f, 0f, 0f, 1f, false);
            var gatedSource = new FireConfiguredSourceSpec(0.5f, 0f, 0f, 1f, true);

            TestSupport.True(FireConfiguredSourceInjector.ShouldInject(freeSource, TimberbornOperationState.Unknown));
            TestSupport.True(FireConfiguredSourceInjector.ShouldInject(gatedSource, TimberbornOperationState.Active));
            TestSupport.False(FireConfiguredSourceInjector.ShouldInject(gatedSource, TimberbornOperationState.Inactive));
            TestSupport.False(FireConfiguredSourceInjector.ShouldInject(gatedSource, TimberbornOperationState.Unknown));
        }

        [Fact]
        public void ConfiguredSourceGridPressure_CanIgniteThroughGridPropagation_Test()
        {
            var grid = TestSupport.CreateGridWithFuelAroundOrigin();
            var source = new FireGridCoordinate(0, 0, 0);
            var target = new FireGridCoordinate(1, 0, 0);
            var footprint = new FireGridFootprint(new[] { source }, source);
            var spec = new FireConfiguredSourceSpec(1f, 1f, 0.25f, 0f, false);

            foreach (var injection in FireConfiguredSourceInjector.CreateInjections(footprint, spec, "smelter-7"))
            {
                grid.Inject(injection);
            }

            grid.Step(FireGridKernel.Full27);
            var sample = grid.Sample(new[] { target });
            var probability = FireIgnitionRules.ComputeIgnitionProbability(
              sample.Heat,
              sample.EmberPressure,
              sample.OxygenAvailability,
              1f,
              0f,
              0.05f,
              0.5f);

            TestSupport.True(sample.HasActivity);
            TestSupport.Equal(FireSourceKind.ConfiguredSource, sample.SourceAttribution.Kind);
            TestSupport.Equal("smelter-7", sample.SourceAttribution.Identity);
            TestSupport.True(probability > 0f);
        }

        [Fact]
        public void OperationStateAdapter_ReportsActiveInactiveAndUnknownConservatively_Test()
        {
            var active = TimberbornOperationStateAdapter.Evaluate(new[]
            {
                new TimberbornOperationalComponentState("RecipeSelector", true),
                new TimberbornOperationalComponentState("FireExposureController", true),
            });
            var inactive = TimberbornOperationStateAdapter.Evaluate(new[]
            {
                new TimberbornOperationalComponentState("SimpleManufactoryBehaviors", false),
            });
            var unknown = TimberbornOperationStateAdapter.Evaluate(new[]
            {
                new TimberbornOperationalComponentState("RecipeSelector", false, false),
            });
            var missing = TimberbornOperationStateAdapter.Evaluate(new TimberbornOperationalComponentState[0]);

            TestSupport.Equal(TimberbornOperationState.Active, active.State);
            TestSupport.Equal(TimberbornOperationState.Inactive, inactive.State);
            TestSupport.Equal(TimberbornOperationState.Unknown, unknown.State);
            TestSupport.Equal(TimberbornOperationState.Unknown, missing.State);
        }

    }
}
