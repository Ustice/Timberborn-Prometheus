using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireFieldAmendmentRuntimeStateTests
    {

        [Fact]
        public void SetAmendment_StoresByGridCoordinate_Test()
        {
            var state = new FireFieldAmendmentRuntimeState();
            var coordinate = new FireGridCoordinate(4, 0, -2);

            state.SetAmendment(coordinate, 24f, 3);

            TestSupport.Equal(1, state.ActiveAmendmentCount);
            TestSupport.True(state.TryGetAmendment(coordinate, out var snapshot));
            TestSupport.Equal(coordinate, snapshot.Coordinate);
            TestSupport.NearlyEqual(24f, snapshot.RemainingHours);
            TestSupport.Equal(3, snapshot.RemainingCharges);
        }

        [Fact]
        public void Tick_ExpiresElapsedAmendments_Test()
        {
            var state = new FireFieldAmendmentRuntimeState();
            var expiredCoordinate = new FireGridCoordinate(1, 0, 1);
            var activeCoordinate = new FireGridCoordinate(2, 0, 2);
            state.SetAmendment(expiredCoordinate, 2f, 2);
            state.SetAmendment(activeCoordinate, 5f, 2);

            state.Tick(3f);

            TestSupport.False(state.TryGetAmendment(expiredCoordinate, out _));
            TestSupport.True(state.TryGetAmendment(activeCoordinate, out var active));
            TestSupport.NearlyEqual(2f, active.RemainingHours);
            TestSupport.Equal(1, state.ActiveAmendmentCount);
        }

        [Fact]
        public void ConsumeCharge_RemovesWhenChargesReachZero_Test()
        {
            var state = new FireFieldAmendmentRuntimeState();
            var coordinate = new FireGridCoordinate(0, 0, 7);
            state.SetAmendment(coordinate, 10f, 2);

            TestSupport.True(state.ConsumeCharge(coordinate));
            TestSupport.True(state.TryGetAmendment(coordinate, out var remaining));
            TestSupport.Equal(1, remaining.RemainingCharges);

            TestSupport.True(state.ConsumeCharge(coordinate));
            TestSupport.False(state.TryGetAmendment(coordinate, out _));
            TestSupport.False(state.ConsumeCharge(coordinate));
            TestSupport.Equal(0, state.ActiveAmendmentCount);
        }

        [Fact]
        public void ClearAmendments_RemovesAllAmendments_Test()
        {
            var state = new FireFieldAmendmentRuntimeState();
            state.SetAmendment(new FireGridCoordinate(1, 0, 1), 10f, 1);
            state.SetAmendment(new FireGridCoordinate(2, 0, 2), 20f, 2);

            state.ClearAmendments();

            TestSupport.Equal(0, state.ActiveAmendmentCount);
            TestSupport.False(state.TryGetAmendment(new FireGridCoordinate(1, 0, 1), out _));
            TestSupport.False(state.TryGetAmendment(new FireGridCoordinate(2, 0, 2), out _));
        }

    }
}
