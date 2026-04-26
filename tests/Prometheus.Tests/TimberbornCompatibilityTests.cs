using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class TimberbornCompatibilityTests
    {

        [Fact]
        public void ProbeResultNormalization_EncodesResolvedAndMissing_Test()
        {
            var resolved = TimberbornCompatibility.NormalizeProbeResult(true, "  NeedManager.AddPoints  ");
            var missing = TimberbornCompatibility.NormalizeProbeResult(false, "");

            TestSupport.Equal(TimberbornCompatibilityProbeStatus.Resolved, resolved.Status);
            TestSupport.Equal("NeedManager.AddPoints", resolved.Detail);
            TestSupport.Equal(TimberbornCompatibilityProbeStatus.Missing, missing.Status);
            TestSupport.Equal("none", missing.Detail);
        }

        [Fact]
        public void ComponentTypeClassifiers_KeepTimberbornNamesSearchable_Test()
        {
            TestSupport.True(TimberbornCompatibility.IsComponentCacheTypeName("ComponentCache"));
            TestSupport.True(TimberbornCompatibility.IsNeedManagerTypeName("NeedManager"));
            TestSupport.True(TimberbornCompatibility.IsTreeComponentName("TreeComponent"));
            TestSupport.True(TimberbornCompatibility.IsGrowableComponentName("Growable"));
            TestSupport.False(TimberbornCompatibility.IsComponentCacheTypeName("PrometheusComponentCache"));
            TestSupport.False(TimberbornCompatibility.IsNeedManagerTypeName(""));
        }

        [Fact]
        public void DamageCategoryClassifier_PreservesKnownTimberbornBoundaries_Test()
        {
            TestSupport.Equal(
                FireDamageCategory.Building,
                TimberbornCompatibility.ClassifyDamageCategory(new[] { "Growable" }, true));
            TestSupport.Equal(
                FireDamageCategory.Tree,
                TimberbornCompatibility.ClassifyDamageCategory(new[] { "Growable", "TreeComponent" }, false));
            TestSupport.Equal(
                FireDamageCategory.Crop,
                TimberbornCompatibility.ClassifyDamageCategory(new[] { "Growable" }, false));
            TestSupport.Equal(
                FireDamageCategory.Building,
                TimberbornCompatibility.ClassifyDamageCategory(new[] { "Manufactory" }, false));
            TestSupport.Equal(
                FireDamageCategory.Unknown,
                TimberbornCompatibility.ClassifyDamageCategory(new[] { "FireExposureController" }, false));
        }

        [Fact]
        public void OperationClassifiers_LiveBehindCompatibilityBoundary_Test()
        {
            TestSupport.True(TimberbornCompatibility.IsWorkplaceSupportComponentName("WorkplaceWorkerTracker"));
            TestSupport.False(TimberbornCompatibility.IsWorkplaceSupportComponentName("WorkplaceBonuses"));
            TestSupport.True(TimberbornCompatibility.IsOperationalComponentName("RecipeSelector"));
            TestSupport.True(TimberbornCompatibility.IsOperationalComponentName("SimpleManufactoryBehaviors"));
            TestSupport.False(TimberbornCompatibility.IsOperationalComponentName("FireWorkplaceEffectApplier"));
            TestSupport.False(TimberbornCompatibility.IsOperationalComponentName("Deteriorable"));
        }

        [Fact]
        public void NeedManagerProbe_NormalizesKnownShapes_Test()
        {
            var directApi = TimberbornCompatibility.ProbeNeedManagerApi(typeof(DirectNeedManager));
            var getNeedApi = TimberbornCompatibility.ProbeNeedManagerApi(typeof(GetNeedManager));
            var tryGetNeedApi = TimberbornCompatibility.ProbeNeedManagerApi(typeof(TryGetNeedManager));
            var missingApi = TimberbornCompatibility.ProbeNeedManagerApi(typeof(MissingNeedManager));

            TestSupport.True(directApi.IsResolved);
            TestSupport.Equal("NeedManager.AddPoints(string,float)", directApi.Description);
            TestSupport.True(getNeedApi.IsResolved);
            TestSupport.Equal("NeedManager.GetNeed(string) + Need.AddPoints(float)", getNeedApi.Description);
            TestSupport.True(tryGetNeedApi.IsResolved);
            TestSupport.Equal("NeedManager.TryGetNeed + Need.AddPoints(float)", tryGetNeedApi.Description);
            TestSupport.False(missingApi.IsResolved);
        }

        private sealed class DirectNeedManager
        {

            public void AddPoints(string needId, float points)
            {
            }

        }

        private sealed class GetNeedManager
        {

            public TestNeed GetNeed(string needId) => new();

        }

        private sealed class TryGetNeedManager
        {

            public bool TryGetNeed(string needId, out TestNeed need)
            {
                need = new TestNeed();
                return true;
            }

        }

        private sealed class MissingNeedManager
        {
        }

        private sealed class TestNeed
        {

            public void AddPoints(float points)
            {
            }

            public void SetPoints(float points)
            {
            }

        }

    }
}
