using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FertileAshRecoveredGoodStackTests
    {

        [Fact]
        public void ValidateRequest_AcceptsRegisteredPositiveAsh_Test()
        {
            var reason = FertileAshRecoveredGoodStackRules.ValidateRequest(3, fertileAshGoodRegistered: true);

            TestSupport.Equal(FertileAshRecoveredGoodStackRules.ReadyReason, reason);
            TestSupport.Equal("FertileAsh", FertileAshRecoveredGoodStackRules.FertileAshGoodId);
        }

        [Fact]
        public void ValidateRequest_RejectsInvalidAmount_Test()
        {
            var zeroReason = FertileAshRecoveredGoodStackRules.ValidateRequest(0, fertileAshGoodRegistered: true);
            var negativeReason = FertileAshRecoveredGoodStackRules.ValidateRequest(-1, fertileAshGoodRegistered: true);

            TestSupport.Equal(FertileAshRecoveredGoodStackRules.InvalidAmountReason, zeroReason);
            TestSupport.Equal(FertileAshRecoveredGoodStackRules.InvalidAmountReason, negativeReason);
        }

        [Fact]
        public void ValidateRequest_RejectsMissingFertileAshGood_Test()
        {
            var reason = FertileAshRecoveredGoodStackRules.ValidateRequest(1, fertileAshGoodRegistered: false);

            TestSupport.Equal(FertileAshRecoveredGoodStackRules.GoodMissingReason, reason);
        }

    }
}
