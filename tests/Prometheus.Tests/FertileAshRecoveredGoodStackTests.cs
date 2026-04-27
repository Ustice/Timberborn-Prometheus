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

        [Fact]
        public void TelemetryState_ClearForReset_RemovesQueuedAshEvidence_Test()
        {
            var state = new FertileAshRecoveredGoodStackTelemetryState();

            state.RecordQueuedStack(
                3,
                new FertileAshSpawnTelemetryContext("BurnedOut", "vegetation", "tree", 42));

            var snapshot = state.ClearForReset();

            TestSupport.Equal(1, snapshot.QueuedStackCount);
            TestSupport.Equal(3, snapshot.QueuedAshAmount);
            TestSupport.Equal("BurnedOut", snapshot.LastSourceAttribution);
            TestSupport.Equal("vegetation", snapshot.LastSourceKind);
            TestSupport.Equal("tree", snapshot.LastDamageCategory);
            TestSupport.Equal(0, state.QueuedStackCount);
            TestSupport.Equal(0, state.QueuedAshAmount);
            TestSupport.Equal("none", state.LastSourceAttribution);
        }

    }
}
