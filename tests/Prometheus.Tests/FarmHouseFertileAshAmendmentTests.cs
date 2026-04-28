using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FarmHouseFertileAshAmendmentTests
    {

        [Fact]
        public void CanApply_RequiresTargetUnamendedCellAndAsh_Test()
        {
            TestSupport.True(FarmHouseFertileAshAmendmentRules.CanApply(
                hasTarget: true,
                targetAlreadyAmended: false,
                hasUnreservedAsh: true));

            TestSupport.False(FarmHouseFertileAshAmendmentRules.CanApply(
                hasTarget: false,
                targetAlreadyAmended: false,
                hasUnreservedAsh: true));
            TestSupport.False(FarmHouseFertileAshAmendmentRules.CanApply(
                hasTarget: true,
                targetAlreadyAmended: true,
                hasUnreservedAsh: true));
            TestSupport.False(FarmHouseFertileAshAmendmentRules.CanApply(
                hasTarget: true,
                targetAlreadyAmended: false,
                hasUnreservedAsh: false));
        }

        [Fact]
        public void Constants_StartWithSmallFarmhouseFirstSlice_Test()
        {
            TestSupport.Equal(1, FarmHouseFertileAshAmendmentRules.AshAmountPerApplication);
            TestSupport.Equal(3, FarmHouseFertileAshAmendmentRules.AmendmentCharges);
            TestSupport.True(FarmHouseFertileAshAmendmentRules.AmendmentDurationHours > 0f);
            TestSupport.True(FarmHouseFertileAshAmendmentRules.WorkerWaitHours > 0f);
        }

    }
}
