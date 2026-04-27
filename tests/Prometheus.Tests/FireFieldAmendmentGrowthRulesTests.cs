using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireFieldAmendmentGrowthRulesTests
    {

        [Fact]
        public void IsEligibleCropGrowable_RequiresCropGrowableAndRejectsTreesAndBushes_Test()
        {
            TestSupport.True(FireFieldAmendmentGrowthRules.IsEligibleCropGrowable(new[] { "Growable", "Crop" }));
            TestSupport.False(FireFieldAmendmentGrowthRules.IsEligibleCropGrowable(new[] { "Growable" }));
            TestSupport.False(FireFieldAmendmentGrowthRules.IsEligibleCropGrowable(new[] { "Growable", "TreeComponent", "Crop" }));
            TestSupport.False(FireFieldAmendmentGrowthRules.IsEligibleCropGrowable(new[] { "Growable", "Bush", "Crop" }));
        }

        [Fact]
        public void ComputeBoostedGrowthTimeInDays_ReducesControlGrowthTime_Test()
        {
            var controlGrowthTime = 4f;
            var amendedGrowthTime = FireFieldAmendmentGrowthRules.ComputeBoostedGrowthTimeInDays(controlGrowthTime);

            TestSupport.True(amendedGrowthTime < controlGrowthTime);
            TestSupport.NearlyEqual(3.6363637f, amendedGrowthTime);
        }

    }
}
