using System.Linq;
using Mods.Prometheus.Scripts;
using UnityEngine;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireBurnedGroundAshDepositTests
    {

        [Fact]
        public void TryRecordDeposit_RecordsOneDepositPerSourceEntity_Test()
        {
            var state = new FireBurnedGroundAshDepositRuntimeState();
            var context = new FertileAshSpawnTelemetryContext("BurnedOut", "charredtree", "tree", 42);

            var firstRecorded = state.TryRecordDeposit(new Vector3Int(1, 0, 2), 42, 1, context, out var firstDeposit);
            var secondRecorded = state.TryRecordDeposit(new Vector3Int(2, 0, 3), 42, 1, context, out var secondDeposit);

            TestSupport.True(firstRecorded);
            TestSupport.False(secondRecorded);
            TestSupport.Equal(1, state.DepositCount);
            TestSupport.Equal(new Vector3Int(1, 0, 2), firstDeposit.Coordinates);
            TestSupport.Equal(firstDeposit.SourceEntityId, secondDeposit.SourceEntityId);
        }

        [Fact]
        public void TryRecordDeposit_RejectsInvalidAmountOrSource_Test()
        {
            var state = new FireBurnedGroundAshDepositRuntimeState();
            var context = new FertileAshSpawnTelemetryContext("BurnedOut", "charredcrop", "crop", 8);

            var missingSource = state.TryRecordDeposit(new Vector3Int(1, 0, 2), 0, 1, context, out _);
            var missingAmount = state.TryRecordDeposit(new Vector3Int(1, 0, 2), 8, 0, context, out _);

            TestSupport.False(missingSource);
            TestSupport.False(missingAmount);
            TestSupport.Equal(0, state.DepositCount);
        }

        [Fact]
        public void GetDeposits_ReturnsStableSourceOrder_Test()
        {
            var state = new FireBurnedGroundAshDepositRuntimeState();

            state.TryRecordDeposit(
                new Vector3Int(3, 0, 3),
                12,
                1,
                new FertileAshSpawnTelemetryContext("BurnedOut", "charredbuilding", "building", 12),
                out _);
            state.TryRecordDeposit(
                new Vector3Int(1, 0, 1),
                3,
                1,
                new FertileAshSpawnTelemetryContext("BurnedOut", "charredcrop", "crop", 3),
                out _);

            var ids = state.GetDeposits().Select(deposit => deposit.SourceEntityId).ToArray();

            TestSupport.Equal("3,12", string.Join(",", ids));
        }

        [Fact]
        public void ClearDeposits_RemovesRecordedDeposits_Test()
        {
            var state = new FireBurnedGroundAshDepositRuntimeState();
            state.TryRecordDeposit(
                new Vector3Int(1, 0, 1),
                3,
                1,
                new FertileAshSpawnTelemetryContext("BurnedOut", "charredcrop", "crop", 3),
                out _);

            var cleared = state.ClearDeposits();

            TestSupport.Equal(1, cleared);
            TestSupport.Equal(0, state.DepositCount);
        }

    }
}
