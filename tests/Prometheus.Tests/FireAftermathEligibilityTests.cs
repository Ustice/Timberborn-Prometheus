using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireAftermathEligibilityTests
    {

        [Fact]
        public void ValidCharredTree_CanProduceFertileAsh_Test()
        {
            var result = FireAftermathEligibilityPolicy.Evaluate(
              new FireAftermathEligibilityCandidate(
                FireGridStructureKind.Vegetation,
                FireDamageCategory.Tree,
                FireDamageState.Dead,
                burnedOut: true));

            TestSupport.True(result.CanProduceFertileAsh);
            TestSupport.Equal(FireAftermathEligibilityStatus.Eligible, result.Status);
            TestSupport.Equal(FireAftermathSourceKind.CharredTree, result.SourceKind);
            TestSupport.Equal("charred_tree", result.Reason);
        }

        [Fact]
        public void ValidCharredBuilding_CanProduceFertileAsh_Test()
        {
            var result = FireAftermathEligibilityPolicy.Evaluate(
              new FireAftermathEligibilityCandidate(
                FireGridStructureKind.Building,
                FireDamageCategory.Building,
                FireDamageState.Dead,
                burnedOut: true));

            TestSupport.True(result.CanProduceFertileAsh);
            TestSupport.Equal(FireAftermathEligibilityStatus.Eligible, result.Status);
            TestSupport.Equal(FireAftermathSourceKind.CharredBuilding, result.SourceKind);
            TestSupport.Equal("charred_building", result.Reason);
        }

        [Fact]
        public void ExcludedObjects_CannotProduceFertileAsh_Test()
        {
            var excludedCandidates = new[]
            {
                new FireAftermathEligibilityCandidate(FireGridStructureKind.Vegetation, FireDamageCategory.Crop, FireDamageState.Dead, burnedOut: true),
                new FireAftermathEligibilityCandidate(FireGridStructureKind.Building, FireDamageCategory.Unknown, FireDamageState.Dead, burnedOut: true),
                new FireAftermathEligibilityCandidate(FireGridStructureKind.Barrier, FireDamageCategory.Building, FireDamageState.Dead, burnedOut: true),
                new FireAftermathEligibilityCandidate(FireGridStructureKind.Water, FireDamageCategory.Unknown, FireDamageState.Dead, burnedOut: true),
                new FireAftermathEligibilityCandidate(FireGridStructureKind.Air, FireDamageCategory.Unknown, FireDamageState.Dead, burnedOut: true),
                new FireAftermathEligibilityCandidate(FireGridStructureKind.Vegetation, FireDamageCategory.Tree, FireDamageState.Scorched, burnedOut: true),
                new FireAftermathEligibilityCandidate(FireGridStructureKind.Building, FireDamageCategory.Building, FireDamageState.Dead, burnedOut: false),
            };

            foreach (var candidate in excludedCandidates)
            {
                var result = FireAftermathEligibilityPolicy.Evaluate(candidate);

                TestSupport.False(result.CanProduceFertileAsh);
                TestSupport.Equal(FireAftermathEligibilityStatus.Ineligible, result.Status);
                TestSupport.Equal(FireAftermathSourceKind.ExcludedObject, result.SourceKind);
            }
        }

        [Fact]
        public void TerrainEligibility_IsExplicitPlaceholder_Test()
        {
            var result = FireAftermathEligibilityPolicy.Evaluate(
              new FireAftermathEligibilityCandidate(
                FireGridStructureKind.Terrain,
                FireDamageCategory.Unknown,
                FireDamageState.Dead,
                burnedOut: true,
                terrainSampleAvailable: true));

            TestSupport.False(result.CanProduceFertileAsh);
            TestSupport.True(result.IsPlaceholder);
            TestSupport.Equal(FireAftermathEligibilityStatus.Placeholder, result.Status);
            TestSupport.Equal(FireAftermathSourceKind.Terrain, result.SourceKind);
            TestSupport.Equal("terrain_adapter_pending", result.Reason);
        }

        [Fact]
        public void TopSurfaceEligibility_IsExplicitPlaceholder_Test()
        {
            var result = FireAftermathEligibilityPolicy.Evaluate(
              new FireAftermathEligibilityCandidate(
                FireGridStructureKind.Unknown,
                FireDamageCategory.Unknown,
                FireDamageState.Dead,
                burnedOut: true,
                topSurfaceSampleAvailable: true));

            TestSupport.False(result.CanProduceFertileAsh);
            TestSupport.True(result.IsPlaceholder);
            TestSupport.Equal(FireAftermathEligibilityStatus.Placeholder, result.Status);
            TestSupport.Equal(FireAftermathSourceKind.TopSurface, result.SourceKind);
            TestSupport.Equal("top_surface_adapter_pending", result.Reason);
        }

        [Fact]
        public void FertileAshSpawnPolicy_QueuesAmountsForEligibleSources_Test()
        {
            var treeDecision = FertileAshSpawnPolicy.Evaluate(
              new FireAftermathEligibilityResult(
                FireAftermathEligibilityStatus.Eligible,
                FireAftermathSourceKind.CharredTree,
                "charred_tree"));
            var buildingDecision = FertileAshSpawnPolicy.Evaluate(
              new FireAftermathEligibilityResult(
                FireAftermathEligibilityStatus.Eligible,
                FireAftermathSourceKind.CharredBuilding,
                "charred_building"));

            TestSupport.True(treeDecision.ShouldQueue);
            TestSupport.Equal(FertileAshSpawnPolicy.CharredTreeAmount, treeDecision.Amount);
            TestSupport.Equal("charred_tree", treeDecision.Reason);
            TestSupport.True(buildingDecision.ShouldQueue);
            TestSupport.Equal(FertileAshSpawnPolicy.CharredBuildingAmount, buildingDecision.Amount);
            TestSupport.Equal("charred_building", buildingDecision.Reason);
        }

        [Fact]
        public void FertileAshSpawnPolicy_DoesNotQueueIneligibleSources_Test()
        {
            var decision = FertileAshSpawnPolicy.Evaluate(
              new FireAftermathEligibilityResult(
                FireAftermathEligibilityStatus.Ineligible,
                FireAftermathSourceKind.ExcludedObject,
                "excluded_source"));

            TestSupport.False(decision.ShouldQueue);
            TestSupport.Equal(0, decision.Amount);
            TestSupport.Equal("excluded_source", decision.Reason);
        }

    }
}
