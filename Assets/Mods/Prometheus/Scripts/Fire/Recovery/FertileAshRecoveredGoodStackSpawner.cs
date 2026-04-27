using System;

#if !PROMETHEUS_TESTS
using Bindito.Core;
using Timberborn.Goods;
using Timberborn.RecoveredGoodSystem;
using UnityEngine;
#endif

namespace Mods.Prometheus.Scripts {
  internal readonly struct FertileAshSpawnTelemetryContext {

    public static readonly FertileAshSpawnTelemetryContext Unknown = new(
      "unknown",
      "unknown",
      "unknown",
      0);

    public string SourceAttribution { get; }
    public string SourceKind { get; }
    public string DamageCategory { get; }
    public int SourceEntityId { get; }

    public FertileAshSpawnTelemetryContext(
      string sourceAttribution,
      string sourceKind,
      string damageCategory,
      int sourceEntityId) {
      SourceAttribution = string.IsNullOrWhiteSpace(sourceAttribution) ? "unknown" : sourceAttribution;
      SourceKind = string.IsNullOrWhiteSpace(sourceKind) ? "unknown" : sourceKind;
      DamageCategory = string.IsNullOrWhiteSpace(damageCategory) ? "unknown" : damageCategory;
      SourceEntityId = sourceEntityId;
    }

  }

  internal class FertileAshRecoveredGoodStackTelemetryState {

    public int QueuedStackCount { get; private set; }
    public int QueuedAshAmount { get; private set; }
    public string LastSourceAttribution { get; private set; } = "none";
    public string LastSourceKind { get; private set; } = "none";
    public string LastDamageCategory { get; private set; } = "none";

    public void RecordQueuedStack(int amount, FertileAshSpawnTelemetryContext context) {
      QueuedStackCount++;
      QueuedAshAmount += Math.Max(0, amount);
      LastSourceAttribution = context.SourceAttribution;
      LastSourceKind = context.SourceKind;
      LastDamageCategory = context.DamageCategory;
    }

    public FertileAshResetTelemetrySnapshot ClearForReset() {
      var snapshot = new FertileAshResetTelemetrySnapshot(
        QueuedStackCount,
        QueuedAshAmount,
        LastSourceAttribution,
        LastSourceKind,
        LastDamageCategory);

      QueuedStackCount = 0;
      QueuedAshAmount = 0;
      LastSourceAttribution = "none";
      LastSourceKind = "none";
      LastDamageCategory = "none";
      return snapshot;
    }

  }

  internal readonly struct FertileAshResetTelemetrySnapshot {

    public int QueuedStackCount { get; }
    public int QueuedAshAmount { get; }
    public string LastSourceAttribution { get; }
    public string LastSourceKind { get; }
    public string LastDamageCategory { get; }

    public FertileAshResetTelemetrySnapshot(
      int queuedStackCount,
      int queuedAshAmount,
      string lastSourceAttribution,
      string lastSourceKind,
      string lastDamageCategory) {
      QueuedStackCount = queuedStackCount;
      QueuedAshAmount = queuedAshAmount;
      LastSourceAttribution = lastSourceAttribution;
      LastSourceKind = lastSourceKind;
      LastDamageCategory = lastDamageCategory;
    }

  }

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
    private FertileAshRecoveredGoodStackTelemetryState _telemetryState;

    [Inject]
    public void InjectDependencies(
      RecoveredGoodStackSpawner recoveredGoodStackSpawner,
      IGoodService goodService,
      FertileAshRecoveredGoodStackTelemetryState telemetryState) {
      _recoveredGoodStackSpawner = recoveredGoodStackSpawner;
      _goodService = goodService;
      _telemetryState = telemetryState;
    }

    internal bool TryQueueFertileAsh(
      Vector3Int coordinates,
      int amount,
      FertileAshSpawnTelemetryContext context,
      out string reason) {
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
        _telemetryState?.RecordQueuedStack(amount, context);
        FireTelemetry.Log(
          $"event={FireTelemetryEvents.FertileAshRecoveredGoodStackQueued} good={FertileAshRecoveredGoodStackRules.FertileAshGoodId} amount={amount} coordinates={coordinates.x},{coordinates.y},{coordinates.z} source={context.SourceAttribution} sourceKind={context.SourceKind} damageCategory={context.DamageCategory} sourceEntityId={context.SourceEntityId}");
        return true;
      } catch (Exception exception) {
        reason = "recovered_good_stack_queue_failed";
        FireTelemetry.LogWarning(
          $"event={FireTelemetryEvents.FertileAshRecoveredGoodStackFailed} reason={reason} exception={exception.GetType().Name} source={context.SourceAttribution} sourceKind={context.SourceKind} damageCategory={context.DamageCategory} sourceEntityId={context.SourceEntityId}");
        return false;
      }
    }

  }
#endif
}
