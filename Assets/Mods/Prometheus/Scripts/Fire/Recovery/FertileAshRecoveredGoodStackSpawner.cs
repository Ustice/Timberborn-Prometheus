using System;

#if !PROMETHEUS_TESTS
using Bindito.Core;
using Timberborn.Goods;
using Timberborn.RecoveredGoodSystem;
using UnityEngine;
#endif

namespace Mods.Prometheus.Scripts {
  internal static class FertileAshRecoveredGoodStackRules {

    internal const string FertileAshGoodId = "FertileAsh";
    internal const string ReadyReason = "ready";
    internal const string InvalidAmountReason = "invalid_amount";
    internal const string GoodMissingReason = "fertile_ash_good_missing";

    internal static string ValidateRequest(int amount, bool fertileAshGoodRegistered) {
      if (amount <= 0) {
        return InvalidAmountReason;
      }

      return fertileAshGoodRegistered ? ReadyReason : GoodMissingReason;
    }

  }

#if !PROMETHEUS_TESTS
  internal class FertileAshRecoveredGoodStackSpawner {

    private RecoveredGoodStackSpawner _recoveredGoodStackSpawner;
    private IGoodService _goodService;

    [Inject]
    public void InjectDependencies(
      RecoveredGoodStackSpawner recoveredGoodStackSpawner,
      IGoodService goodService) {
      _recoveredGoodStackSpawner = recoveredGoodStackSpawner;
      _goodService = goodService;
    }

    internal bool TryQueueFertileAsh(Vector3Int coordinates, int amount, out string reason) {
      var fertileAshGoodRegistered = _goodService is not null
                                      && _goodService.HasGood(FertileAshRecoveredGoodStackRules.FertileAshGoodId);
      reason = FertileAshRecoveredGoodStackRules.ValidateRequest(amount, fertileAshGoodRegistered);
      if (!string.Equals(reason, FertileAshRecoveredGoodStackRules.ReadyReason, StringComparison.Ordinal)) {
        return false;
      }

      if (_recoveredGoodStackSpawner is null) {
        reason = "recovered_good_stack_spawner_missing";
        return false;
      }

      try {
        _recoveredGoodStackSpawner.AddAwaitingGoods(
          coordinates,
          new[] { new GoodAmount(FertileAshRecoveredGoodStackRules.FertileAshGoodId, amount) });
        reason = "queued_recovered_good_stack";
        FireTelemetry.Log(
          $"event={FireTelemetryEvents.FertileAshRecoveredGoodStackQueued} good={FertileAshRecoveredGoodStackRules.FertileAshGoodId} amount={amount} coordinates={coordinates.x},{coordinates.y},{coordinates.z}");
        return true;
      } catch (Exception exception) {
        reason = "recovered_good_stack_queue_failed";
        FireTelemetry.LogWarning(
          $"event={FireTelemetryEvents.FertileAshRecoveredGoodStackFailed} reason={reason} exception={exception.GetType().Name}");
        return false;
      }
    }

  }
#endif
}
