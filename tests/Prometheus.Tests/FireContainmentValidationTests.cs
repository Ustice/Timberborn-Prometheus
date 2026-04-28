using System.Linq;
using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireContainmentValidationTests
    {

        [Fact]
        public void PreparedBurn_MoistureAndBarriersReduceBoundaryPressure_Test()
        {
            var control = CreateLineGrid(
              TestSupport.BurnableEnvironment(),
              TestSupport.BurnableEnvironment(),
              TestSupport.BurnableEnvironment(),
              TestSupport.BurnableEnvironment());
            var prepared = CreateLineGrid(
              TestSupport.BurnableEnvironment(),
              new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0.7f, 0f, 1f, 0f, FireGridExposedFaces.All),
              new FireCellEnvironment(FireGridStructureKind.Barrier, 0.2f, 0f, 0.85f, 1f, 0f, FireGridExposedFaces.All),
              TestSupport.BurnableEnvironment());
            var boundary = new FireGridCoordinate(3, 0, 0);

            Step(control, 4);
            Step(prepared, 4);

            TestSupport.True(control.TryGetState(boundary, out var controlBoundary));
            TestSupport.True(prepared.TryGetState(boundary, out var preparedBoundary));
            TestSupport.True(preparedBoundary.Heat < controlBoundary.Heat);
            TestSupport.True(preparedBoundary.EmberPressure < controlBoundary.EmberPressure);
            TestSupport.True(preparedBoundary.IgnitionProgress < controlBoundary.IgnitionProgress);
        }

        [Fact]
        public void PreparedBurn_WaterFirebreakBlocksSpreadBeyondBreak_Test()
        {
            var control = CreateLineGrid(
              TestSupport.BurnableEnvironment(),
              TestSupport.BurnableEnvironment(),
              TestSupport.BurnableEnvironment(),
              TestSupport.BurnableEnvironment());
            var prepared = CreateLineGrid(
              TestSupport.BurnableEnvironment(),
              WaterEnvironment(),
              TestSupport.BurnableEnvironment(),
              TestSupport.BurnableEnvironment());
            var beyondBreak = new FireGridCoordinate(2, 0, 0);
            SetWaterFirebreakPlane(prepared, 1);

            Step(control, 3);
            Step(prepared, 3);

            TestSupport.True(control.TryGetState(beyondBreak, out var controlBeyondBreak) && controlBeyondBreak.IsActive);
            TestSupport.False(prepared.TryGetState(beyondBreak, out var preparedBeyondBreak) && preparedBeyondBreak.IsActive);
        }

        [Fact]
        public void PreparedBurn_ClosedExposedFacesBlockLateralTransfer_Test()
        {
            var control = CreateLineGrid(
              TestSupport.BurnableEnvironment(),
              TestSupport.BurnableEnvironment(),
              TestSupport.BurnableEnvironment());
            var prepared = CreateLineGrid(
              TestSupport.BurnableEnvironment(),
              new FireCellEnvironment(
                FireGridStructureKind.Vegetation,
                1f,
                0f,
                0f,
                1f,
                0f,
                FireGridExposedFaces.PositiveY),
              TestSupport.BurnableEnvironment());
            var blockedNeighbor = new FireGridCoordinate(1, 0, 0);

            Step(control, 1);
            Step(prepared, 1);

            TestSupport.True(control.TryGetState(blockedNeighbor, out var controlNeighbor) && controlNeighbor.IsActive);
            TestSupport.False(prepared.TryGetState(blockedNeighbor, out var preparedNeighbor) && preparedNeighbor.IsActive);
        }

        [Fact]
        public void PreparedBurn_SpacingReducesComparableTargetPressure_Test()
        {
            var adjacent = new FireGridRuntimeState();
            var spaced = new FireGridRuntimeState();
            var source = new FireGridCoordinate(0, 0, 0);
            var adjacentTarget = new FireGridCoordinate(1, 0, 0);
            var spacedTarget = new FireGridCoordinate(3, 0, 0);

            adjacent.SetEnvironment(source, TestSupport.BurnableEnvironment());
            adjacent.SetEnvironment(adjacentTarget, TestSupport.BurnableEnvironment());
            spaced.SetEnvironment(source, TestSupport.BurnableEnvironment());
            spaced.SetEnvironment(spacedTarget, TestSupport.BurnableEnvironment());
            adjacent.Inject(source, TestSupport.HotCell());
            spaced.Inject(source, TestSupport.HotCell());

            Step(adjacent, 1);
            Step(spaced, 1);

            TestSupport.True(adjacent.TryGetState(adjacentTarget, out var adjacentState) && adjacentState.IsActive);
            TestSupport.False(spaced.TryGetState(spacedTarget, out var spacedState) && spacedState.IsActive);
        }

        [Fact]
        public void PreparedBurn_ProfileThresholdsKeepLowRiskFuelMoreBounded_Test()
        {
            var exposedSample = new FireGridSample(
              true,
              0.55f,
              0.52f,
              0.1f,
              0.32f,
              0f,
              0f,
              1f,
              FireGridBurnState.Heating);

            var lowRiskProbability = FireIgnitionRules.ComputeIgnitionProbability(
              exposedSample.Heat,
              exposedSample.EmberPressure,
              exposedSample.OxygenAvailability,
              1f,
              exposedSample.MoistureDampening,
              0.9f,
              1f);
            var highRiskProbability = FireIgnitionRules.ComputeIgnitionProbability(
              exposedSample.Heat,
              exposedSample.EmberPressure,
              exposedSample.OxygenAvailability,
              1f,
              exposedSample.MoistureDampening,
              0.2f,
              1f);

            TestSupport.NearlyEqual(0f, lowRiskProbability);
            TestSupport.True(highRiskProbability > lowRiskProbability);
        }

        private static FireGridRuntimeState CreateLineGrid(params FireCellEnvironment[] environments)
        {
            var grid = new FireGridRuntimeState();
            environments
              .Select((environment, x) => new { Coordinate = new FireGridCoordinate(x, 0, 0), Environment = environment })
              .ToList()
              .ForEach(cell => grid.SetEnvironment(cell.Coordinate, cell.Environment));
            grid.Inject(new FireGridCoordinate(0, 0, 0), TestSupport.HotCell());
            return grid;
        }

        private static FireCellEnvironment WaterEnvironment() =>
          new(
            FireGridStructureKind.Water,
            0f,
            1f,
            0f,
            1f,
            FireGridPropagationPolicy.WaterSuppressionDepth + 0.01f,
            FireGridExposedFaces.All);

        private static void SetWaterFirebreakPlane(FireGridRuntimeState grid, int x) =>
          Enumerable.Range(-1, 3)
            .SelectMany(y => Enumerable.Range(-1, 3).Select(z => new FireGridCoordinate(x, y, z)))
            .ToList()
            .ForEach(coordinate => grid.SetEnvironment(coordinate, WaterEnvironment()));

        private static void Step(FireGridRuntimeState grid, int count) =>
          Enumerable.Range(0, count).ToList().ForEach(_ => grid.Step(FireGridKernel.Full27));

    }
}
