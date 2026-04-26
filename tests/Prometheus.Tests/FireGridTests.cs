using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireGridTests
    {

        [Fact]
        public void FireGridCoordinate_MapsCellsIntoEightByEightByEightChunks_Test()
        {
            TestSupport.Equal(new FireGridChunkCoordinate(0, 0, 0), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(0, 0, 0)));
            TestSupport.Equal(new FireGridChunkCoordinate(0, 0, 0), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(7, 7, 7)));
            TestSupport.Equal(new FireGridChunkCoordinate(1, 0, 0), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(8, 0, 0)));
            TestSupport.Equal(new FireGridChunkCoordinate(-1, -1, -1), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(-1, -1, -1)));
            TestSupport.Equal(new FireGridChunkCoordinate(-1, -1, -1), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(-8, -8, -8)));
            TestSupport.Equal(new FireGridChunkCoordinate(-2, -1, -1), FireGridChunkCoordinate.FromCell(new FireGridCoordinate(-9, -8, -8)));

            TestSupport.Equal(0, FireGridChunkCoordinate.LocalIndex(new FireGridCoordinate(0, 0, 0)));
            TestSupport.Equal(7, FireGridChunkCoordinate.LocalIndex(new FireGridCoordinate(7, 0, 0)));
            TestSupport.Equal(7, FireGridChunkCoordinate.LocalIndex(new FireGridCoordinate(-1, 0, 0)));
        }

        [Fact]
        public void FireGridKernel_Full27IncludesSelfAndAllNeighbors_Test()
        {
            var entries = FireGridKernel.Full27.Entries;
            TestSupport.Equal(27, entries.Count);
            TestSupport.Equal(1, TestSupport.CountKernelEntries(entries, 0, 0, 0));
            TestSupport.Equal(1, TestSupport.CountKernelEntries(entries, -1, -1, -1));
            TestSupport.Equal(1, TestSupport.CountKernelEntries(entries, 1, 1, 1));
            TestSupport.True(entries[0].HeatWeight >= 0f);
            TestSupport.True(TestSupport.FindKernelEntry(entries, 0, 0, 0).IsSelf);
            TestSupport.True(TestSupport.FindKernelEntry(entries, 0, 1, 0).SmokeWeight > TestSupport.FindKernelEntry(entries, 0, -1, 0).SmokeWeight);
            TestSupport.True(TestSupport.FindKernelEntry(entries, 0, 1, 0).HeatWeight > TestSupport.FindKernelEntry(entries, 0, -1, 0).HeatWeight);
        }

        [Fact]
        public void FireGridPropagationPolicy_UpwardHeatBiasPreservesCurrentWeights_Test()
        {
            var upward = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 0, 1, 0);
            var lateral = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 1, 0, 0);
            var downward = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 0, -1, 0);

            TestSupport.NearlyEqual(
              FireGridPropagationPolicy.NeighborHeatBaseWeight * FireGridPropagationPolicy.UpwardHeatMultiplier,
              upward.HeatWeight);
            TestSupport.NearlyEqual(
              FireGridPropagationPolicy.NeighborHeatBaseWeight * FireGridPropagationPolicy.LateralHeatMultiplier,
              lateral.HeatWeight);
            TestSupport.NearlyEqual(
              FireGridPropagationPolicy.NeighborHeatBaseWeight * FireGridPropagationPolicy.DownwardHeatMultiplier,
              downward.HeatWeight);
            TestSupport.True(upward.HeatWeight > lateral.HeatWeight);
            TestSupport.True(lateral.HeatWeight > downward.HeatWeight);
        }

        [Fact]
        public void FireGridPropagationPolicy_UpwardSmokeBiasPreservesCurrentWeights_Test()
        {
            var upward = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 0, 1, 0);
            var lateral = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 1, 0, 0);
            var downward = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 0, -1, 0);

            TestSupport.NearlyEqual(
              FireGridPropagationPolicy.NeighborSmokeBaseWeight * FireGridPropagationPolicy.UpwardSmokeMultiplier,
              upward.SmokeWeight);
            TestSupport.NearlyEqual(
              FireGridPropagationPolicy.NeighborSmokeBaseWeight * FireGridPropagationPolicy.LateralSmokeMultiplier,
              lateral.SmokeWeight);
            TestSupport.NearlyEqual(
              FireGridPropagationPolicy.NeighborSmokeBaseWeight * FireGridPropagationPolicy.DownwardSmokeMultiplier,
              downward.SmokeWeight);
            TestSupport.True(upward.SmokeWeight > lateral.SmokeWeight);
            TestSupport.True(lateral.SmokeWeight > downward.SmokeWeight);
        }

        [Fact]
        public void FireGridPropagationPolicy_OutwardEmberBiasPreservesCurrentWeights_Test()
        {
            var upward = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 0, 1, 0);
            var lateral = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 1, 0, 0);
            var downward = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 0, -1, 0);

            TestSupport.NearlyEqual(
              FireGridPropagationPolicy.NeighborEmberBaseWeight * FireGridPropagationPolicy.UpwardEmberMultiplier,
              upward.EmberWeight);
            TestSupport.NearlyEqual(
              FireGridPropagationPolicy.NeighborEmberBaseWeight * FireGridPropagationPolicy.OutwardEmberMultiplier,
              lateral.EmberWeight);
            TestSupport.NearlyEqual(
              FireGridPropagationPolicy.NeighborEmberBaseWeight * FireGridPropagationPolicy.DownwardEmberMultiplier,
              downward.EmberWeight);
            TestSupport.True(lateral.EmberWeight > upward.EmberWeight);
            TestSupport.True(upward.EmberWeight > downward.EmberWeight);
        }

        [Fact]
        public void FireGridPropagationPolicy_OxygenPolicyDeterministicallyReducesIgnition_Test()
        {
            var source = TestSupport.HotCell();
            var entry = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 1, 0, 0);
            var highOxygen = FireGridPropagationRules.Transfer(
              source,
              TestSupport.BurnableEnvironment(),
              new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 1f, 0f, 63),
              entry);
            var lowOxygen = FireGridPropagationRules.Transfer(
              source,
              TestSupport.BurnableEnvironment(),
              new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 0.1f, 0f, 63),
              entry);

            TestSupport.True(lowOxygen.IgnitionProgress < highOxygen.IgnitionProgress);
            TestSupport.NearlyEqual(0f, new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 0.1f, 0f, 63).EffectiveOxygen(1f));
        }

        [Fact]
        public void FireGridPropagationPolicy_WaterPolicyDeterministicallyBlocksPropagation_Test()
        {
            var entry = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 1, 0, 0);
            var underwater = new FireCellEnvironment(FireGridStructureKind.Water, 1f, 0f, 0f, 1f, FireGridPropagationPolicy.WaterSuppressionDepth + 0.01f, 63);

            var transfer = FireGridPropagationRules.Transfer(
              TestSupport.HotCell(),
              TestSupport.BurnableEnvironment(),
              underwater,
              entry);
            var finalized = FireGridPropagationRules.FinalizeCell(TestSupport.HotCell(), underwater);

            TestSupport.False(transfer.IsActive);
            TestSupport.False(finalized.IsActive);
        }

        [Fact]
        public void FireGridPropagationPolicy_BarrierPolicyDeterministicallyReducesTransfer_Test()
        {
            var source = TestSupport.HotCell();
            var entry = TestSupport.FindKernelEntry(FireGridKernel.Full27.Entries, 1, 0, 0);
            var open = FireGridPropagationRules.Transfer(
              source,
              TestSupport.BurnableEnvironment(),
              new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 1f, 0f, 63),
              entry);
            var barrier = FireGridPropagationRules.Transfer(
              source,
              TestSupport.BurnableEnvironment(),
              new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0.75f, 1f, 0f, 63),
              entry);

            TestSupport.True(barrier.Heat < open.Heat);
            TestSupport.True(barrier.EmberPressure < open.EmberPressure);
            TestSupport.NearlyEqual(0.25f, new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0.75f, 1f, 0f, 63).TransferMultiplier);
        }

        [Fact]
        public void FireGridPropagationPolicy_BoundsPolicyLimitsOneStepPropagationToKernelNeighbors_Test()
        {
            var grid = TestSupport.CreateGridWithFuelAroundOrigin();
            var source = new FireGridCoordinate(0, 0, 0);
            var neighbor = new FireGridCoordinate(1, 0, 0);
            var beyondKernel = new FireGridCoordinate(2, 0, 0);

            grid.Inject(source, TestSupport.HotCell());
            grid.Step(FireGridKernel.Full27);

            TestSupport.True(grid.TryGetState(neighbor, out var neighborState) && neighborState.IsActive);
            TestSupport.False(grid.TryGetState(beyondKernel, out var beyondState) && beyondState.IsActive);
        }

        [Fact]
        public void FireGridRuntimeState_WritesAcrossChunkBoundaries_Test()
        {
            var grid = TestSupport.CreateGridWithFuelAroundOrigin();
            var source = new FireGridCoordinate(7, 0, 0);
            var target = new FireGridCoordinate(8, 0, 0);

            grid.SetEnvironment(source, TestSupport.BurnableEnvironment());
            grid.SetEnvironment(target, TestSupport.BurnableEnvironment());
            grid.Inject(source, TestSupport.HotCell());
            grid.Step(FireGridKernel.Full27);

            TestSupport.True(grid.TryGetState(target, out var targetState));
            TestSupport.True(targetState.Heat > 0f);
            TestSupport.True(grid.ActiveChunkCount >= 2);
        }

        [Fact]
        public void FireGridRuntimeState_PrunesColdChunks_Test()
        {
            var grid = new FireGridRuntimeState();
            var coordinate = new FireGridCoordinate(0, 0, 0);

            grid.Inject(coordinate, TestSupport.HotCell());
            TestSupport.Equal(1, grid.TotalChunkCount);
            TestSupport.Equal(1, grid.ActiveCellCount);

            grid.ClearCell(coordinate);
            grid.PruneInactiveChunks();

            TestSupport.Equal(0, grid.TotalChunkCount);
            TestSupport.Equal(0, grid.ActiveCellCount);
        }

        [Fact]
        public void FireGridRuntimeState_DoubleBufferedStepIsInjectionOrderIndependent_Test()
        {
            var first = TestSupport.CreateGridWithFuelAroundOrigin();
            var second = TestSupport.CreateGridWithFuelAroundOrigin();
            var left = new FireGridCoordinate(0, 0, 0);
            var right = new FireGridCoordinate(1, 0, 0);
            var sample = new FireGridCoordinate(0, 1, 0);

            first.Inject(left, new FireCellState(0.8f, 0.6f, 0.4f, 0.2f, 0f, FireGridBurnState.Burning));
            first.Inject(right, new FireCellState(0.3f, 0.9f, 0.2f, 0.1f, 0f, FireGridBurnState.Smoldering));
            second.Inject(right, new FireCellState(0.3f, 0.9f, 0.2f, 0.1f, 0f, FireGridBurnState.Smoldering));
            second.Inject(left, new FireCellState(0.8f, 0.6f, 0.4f, 0.2f, 0f, FireGridBurnState.Burning));

            first.Step(FireGridKernel.Full27);
            second.Step(FireGridKernel.Full27);

            TestSupport.True(first.TryGetState(sample, out var firstSample));
            TestSupport.True(second.TryGetState(sample, out var secondSample));
            TestSupport.NearlyEqual(firstSample.Heat, secondSample.Heat, 0.000001f);
            TestSupport.NearlyEqual(firstSample.EmberPressure, secondSample.EmberPressure, 0.000001f);
            TestSupport.NearlyEqual(firstSample.Smoke, secondSample.Smoke, 0.000001f);
            TestSupport.NearlyEqual(firstSample.IgnitionProgress, secondSample.IgnitionProgress, 0.000001f);
        }

        [Fact]
        public void FireGridFootprintSampler_ConvertsBoundsToOccupiedCells_Test()
        {
            var footprint = FireGridFootprintSampler.FromBounds(new UnityEngine.Bounds(
              new UnityEngine.Vector3(1f, 0.5f, 1f),
              new UnityEngine.Vector3(2f, 1f, 2f)));

            TestSupport.Equal(new FireGridCoordinate(1, 0, 1), footprint.PrimaryCoordinate);
            TestSupport.Equal(4, footprint.Coordinates.Count);
            TestSupport.True(TestSupport.ContainsCoordinate(footprint.Coordinates, new FireGridCoordinate(0, 0, 0)));
            TestSupport.True(TestSupport.ContainsCoordinate(footprint.Coordinates, new FireGridCoordinate(1, 0, 1)));
            TestSupport.False(TestSupport.ContainsCoordinate(footprint.Coordinates, new FireGridCoordinate(2, 0, 1)));
        }

        [Fact]
        public void FireGridRuntimeState_SamplesFootprintAggregate_Test()
        {
            var grid = new FireGridRuntimeState();
            var first = new FireGridCoordinate(0, 0, 0);
            var second = new FireGridCoordinate(1, 0, 0);
            var footprint = new FireGridFootprint(new[] { first, second }, first);

            grid.SetEnvironment(first, new FireCellEnvironment(FireGridStructureKind.Building, 1f, 0.2f, 0f, 0.8f, 0f, 63));
            grid.SetEnvironment(second, new FireCellEnvironment(FireGridStructureKind.Building, 1f, 0.6f, 0f, 0.4f, 0f, 63));
            grid.Inject(first, new FireCellState(0.2f, 0.1f, 0.3f, 0.2f, 0.1f, FireGridBurnState.Heating));
            grid.Inject(second, new FireCellState(0.7f, 0.4f, 0.2f, 0.8f, 0.3f, FireGridBurnState.Burning));

            var sample = grid.Sample(footprint);

            TestSupport.True(sample.HasActivity);
            TestSupport.True(sample.Burning);
            TestSupport.NearlyEqual(0.7f, sample.Heat);
            TestSupport.NearlyEqual(0.4f, sample.EmberPressure);
            TestSupport.NearlyEqual(0.3f, sample.Smoke);
            TestSupport.NearlyEqual(0.8f, sample.IgnitionProgress);
            TestSupport.NearlyEqual(0.3f, sample.FuelConsumed);
            TestSupport.NearlyEqual(0.4f, sample.MoistureDampening);
            TestSupport.NearlyEqual(0.6f, sample.OxygenAvailability);
        }

        [Fact]
        public void FireGridRuntimeState_UnderwaterCellsSuppressIgnition_Test()
        {
            var grid = new FireGridRuntimeState();
            var coordinate = new FireGridCoordinate(0, 0, 0);

            grid.SetEnvironment(coordinate, new FireCellEnvironment(FireGridStructureKind.Water, 1f, 0f, 0f, 1f, 1f, 63));
            grid.Inject(coordinate, TestSupport.HotCell());
            grid.Step(FireGridKernel.Full27);

            TestSupport.False(grid.TryGetState(coordinate, out var state) && state.IsActive);
        }

        [Fact]
        public void FireGridRuntimeState_MoistureAndBarriersReduceTransfer_Test()
        {
            var dryGrid = TestSupport.CreateTwoCellTransferGrid(new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 1f, 0f, 63));
            var dampBarrierGrid = TestSupport.CreateTwoCellTransferGrid(new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0.75f, 0.5f, 1f, 0f, 63));
            var target = new FireGridCoordinate(1, 0, 0);

            dryGrid.Step(FireGridKernel.Full27);
            dampBarrierGrid.Step(FireGridKernel.Full27);

            TestSupport.True(dryGrid.TryGetState(target, out var dryState));
            TestSupport.True(dampBarrierGrid.TryGetState(target, out var dampBarrierState));
            TestSupport.True(dampBarrierState.Heat < dryState.Heat);
            TestSupport.True(dampBarrierState.EmberPressure < dryState.EmberPressure);
        }

        [Fact]
        public void FireGridRuntimeState_OxygenAvailabilityChangesIgnition_Test()
        {
            var highOxygenGrid = TestSupport.CreateTwoCellTransferGrid(new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 1f, 0f, 63));
            var lowOxygenGrid = TestSupport.CreateTwoCellTransferGrid(new FireCellEnvironment(FireGridStructureKind.Vegetation, 1f, 0f, 0f, 0.1f, 0f, 63));
            var target = new FireGridCoordinate(1, 0, 0);

            highOxygenGrid.Step(FireGridKernel.Full27);
            lowOxygenGrid.Step(FireGridKernel.Full27);

            TestSupport.True(highOxygenGrid.TryGetState(target, out var highOxygenState));
            TestSupport.True(lowOxygenGrid.TryGetState(target, out var lowOxygenState));
            TestSupport.True(lowOxygenState.IgnitionProgress < highOxygenState.IgnitionProgress);
        }

        [Fact]
        public void FireGridRuntimeState_BurningVegetationEmitsAcrossForestLine_Test()
        {
            var grid = new FireGridRuntimeState();
            for (var x = 0; x < 5; x++)
            {
                grid.SetEnvironment(new FireGridCoordinate(x, 0, 0), TestSupport.BurnableEnvironment());
            }

            grid.Inject(new FireGridCoordinate(0, 0, 0), TestSupport.HotCell());
            for (var step = 0; step < 12; step++)
            {
                grid.Step(FireGridKernel.Full27);
            }

            TestSupport.True(grid.TryGetState(new FireGridCoordinate(3, 0, 0), out var distantTreeState));
            TestSupport.True(distantTreeState.BurnState == FireGridBurnState.Burning || distantTreeState.BurnState == FireGridBurnState.Smoldering);
            TestSupport.True(distantTreeState.IgnitionProgress > 0.35f);
        }

        [Fact]
        public void FireGridRuntimeState_FieldTransferDoesNotConsumeFuel_Test()
        {
            var grid = new FireGridRuntimeState();
            var source = new FireGridCoordinate(0, 0, 0);
            var neighbor = new FireGridCoordinate(1, 0, 0);
            grid.SetEnvironment(source, TestSupport.BurnableEnvironment());
            grid.SetEnvironment(neighbor, TestSupport.BurnableEnvironment());
            grid.Inject(source, new FireCellState(1f, 1f, 0.5f, 1f, 0.25f, FireGridBurnState.Burning));

            grid.Step(FireGridKernel.Full27);

            TestSupport.True(grid.TryGetState(source, out var sourceState));
            TestSupport.NearlyEqual(0.25f, sourceState.FuelConsumed);
        }

        [Fact]
        public void FireGridRuntimeState_ExposedFacesLimitTransfer_Test()
        {
            var openGrid = TestSupport.CreateTwoCellTransferGrid(TestSupport.BurnableEnvironment());
            var blockedGrid = TestSupport.CreateTwoCellTransferGrid(new FireCellEnvironment(
              FireGridStructureKind.Vegetation,
              1f,
              0f,
              0f,
              1f,
              0f,
              FireGridExposedFaces.PositiveY));
            var target = new FireGridCoordinate(1, 0, 0);

            openGrid.Step(FireGridKernel.Full27);
            blockedGrid.Step(FireGridKernel.Full27);

            TestSupport.True(openGrid.TryGetState(target, out var openState));
            TestSupport.False(blockedGrid.TryGetState(target, out var blockedState) && blockedState.IsActive);
        }

        [Fact]
        public void FireGridSimulationCoordinator_StepsGridOnlyOncePerFrame_Test()
        {
            var grid = TestSupport.CreateGridWithFuelAroundOrigin();
            var coordinator = new FireGridSimulationCoordinator(grid);
            var source = new FireGridCoordinate(0, 0, 0);
            var target = new FireGridCoordinate(1, 0, 0);

            grid.Inject(source, TestSupport.HotCell());

            TestSupport.True(coordinator.StepFrame(12));
            TestSupport.True(grid.TryGetState(target, out var firstStepTarget));
            TestSupport.True(firstStepTarget.Heat > 0f);

            TestSupport.False(coordinator.StepFrame(12));
            TestSupport.True(grid.TryGetState(target, out var sameFrameTarget));
            TestSupport.NearlyEqual(firstStepTarget.Heat, sameFrameTarget.Heat);

            TestSupport.True(coordinator.StepFrame(13));
        }

        [Fact]
        public void FireGridEnvironmentSampler_ProfileParsesFuelAndResistances_Test()
        {
            var sample = FireGridEnvironmentSampler.FromProfile("berry-bush", 1.15f, 0.25f, 0.4f);

            TestSupport.Equal(FireGridStructureKind.Vegetation, sample.StructureKind);
            TestSupport.NearlyEqual(1.15f, sample.Fuel);
            TestSupport.NearlyEqual(0.75f, sample.Moisture);
            TestSupport.NearlyEqual(0.4f, sample.Barrier);
            TestSupport.NearlyEqual(1f, sample.OxygenAvailability);
            TestSupport.Equal(FireGridExposedFaces.All, sample.ExposedFaceMask);
        }

        [Fact]
        public void FireGridEnvironmentSampler_WorldWaterOverridesProfileKind_Test()
        {
            var profile = FireGridEnvironmentSampler.FromProfile("bakery", 0.9f, 0.1f, 0.2f);
            var world = new FireGridEnvironmentSample(
              FireGridStructureKind.Water,
              0f,
              0.3f,
              0f,
              0.6f,
              0.8f,
              FireGridExposedFaces.PositiveY);

            var environment = FireGridEnvironmentSampler.Merge(profile, world).ToEnvironment();

            TestSupport.Equal(FireGridStructureKind.Water, environment.StructureKind);
            TestSupport.True(environment.IsUnderwater);
            TestSupport.NearlyEqual(0.9f, environment.Fuel);
            TestSupport.NearlyEqual(0.9f, environment.Moisture);
            TestSupport.NearlyEqual(0.8f, environment.WaterDepth);
            TestSupport.NearlyEqual(0.6f, environment.OxygenAvailability);
            TestSupport.Equal(FireGridExposedFaces.PositiveY, environment.ExposedFaceMask);
        }

        [Fact]
        public void FireGridEnvironmentSampler_MergeKeepsReadOnlyWorldConstraints_Test()
        {
            var profile = FireGridEnvironmentSampler.FromProfile("platform-barrier", 0.2f, 0.8f, 0.25f);
            var world = new FireGridEnvironmentSample(
              FireGridStructureKind.Terrain,
              0.6f,
              0.7f,
              0.5f,
              0.4f,
              0f,
              FireGridExposedFaces.PositiveY | FireGridExposedFaces.PositiveX);

            var sample = FireGridEnvironmentSampler.Merge(profile, world);

            TestSupport.Equal(FireGridStructureKind.Barrier, sample.StructureKind);
            TestSupport.NearlyEqual(0.6f, sample.Fuel);
            TestSupport.NearlyEqual(0.7f, sample.Moisture);
            TestSupport.NearlyEqual(0.5f, sample.Barrier);
            TestSupport.NearlyEqual(0.4f, sample.OxygenAvailability);
            TestSupport.Equal(FireGridExposedFaces.PositiveY | FireGridExposedFaces.PositiveX, sample.ExposedFaceMask);
        }

        [Fact]
        public void FireGridEnvironmentSampler_TerrainColumnSeparatesTopSurfaceAndMass_Test()
        {
            var belowFloor = FireGridEnvironmentSampler.FromTerrainColumn(new FireGridCoordinate(2, 3, 4), 4, 8);
            var mass = FireGridEnvironmentSampler.FromTerrainColumn(new FireGridCoordinate(2, 6, 4), 4, 8);
            var top = FireGridEnvironmentSampler.FromTerrainColumn(new FireGridCoordinate(2, 8, 4), 4, 8);
            var aboveTop = FireGridEnvironmentSampler.FromTerrainColumn(new FireGridCoordinate(2, 9, 4), 4, 8);

            TestSupport.Equal(FireGridStructureKind.Air, belowFloor.StructureKind);
            TestSupport.Equal(FireGridStructureKind.Terrain, mass.StructureKind);
            TestSupport.Equal(FireGridExposedFaces.None, mass.ExposedFaceMask);
            TestSupport.True(mass.Barrier > top.Barrier);
            TestSupport.True(mass.OxygenAvailability < top.OxygenAvailability);
            TestSupport.Equal(FireGridStructureKind.Terrain, top.StructureKind);
            TestSupport.Equal(
              FireGridExposedFaces.PositiveY
              | FireGridExposedFaces.NegativeX
              | FireGridExposedFaces.PositiveX
              | FireGridExposedFaces.NegativeZ
              | FireGridExposedFaces.PositiveZ,
              top.ExposedFaceMask);
            TestSupport.Equal(FireGridStructureKind.Air, aboveTop.StructureKind);
        }

    }
}
