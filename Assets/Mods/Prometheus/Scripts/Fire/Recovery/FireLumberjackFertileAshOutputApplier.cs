#if false
using Timberborn.BaseComponentSystem;
using Timberborn.Goods;
using Timberborn.SimpleOutputBuildings;

namespace Mods.Prometheus.Scripts {
  internal class FireLumberjackFertileAshOutputApplier : BaseComponent,
                                                        IUpdatableComponent {

    private const float RetryIntervalInSeconds = 1f;

    private float _timeSinceLastRetry;
    private bool _completed;

    public void Update() {
      if (_completed || !TickGate.ShouldRun(ref _timeSinceLastRetry, RetryIntervalInSeconds)) {
        return;
      }

      var simpleOutputInventory = GameObject.GetComponent<SimpleOutputInventory>();
      var inventory = simpleOutputInventory?.Inventory;
      if (inventory == null) {
        return;
      }

      if (inventory.Allows(FertileAshRecoveredGoodStackRules.FertileAshGoodId)) {
        _completed = true;
        FireTelemetry.Log($"event={FireTelemetryEvents.FertileAshLumberjackInventoryAllowed} entity={GameObject.name} good={FertileAshRecoveredGoodStackRules.FertileAshGoodId} capacity={inventory.Capacity} reason=already_allowed");
        return;
      }

      inventory._allowedGoods.Add(new StorableGoodAmount(
        StorableGood.CreateAsTakeable(FertileAshRecoveredGoodStackRules.FertileAshGoodId),
        inventory.Capacity));
      inventory.InvokeInventoryChangedEvent(FertileAshRecoveredGoodStackRules.FertileAshGoodId);
      _completed = true;

      FireTelemetry.Log($"event={FireTelemetryEvents.FertileAshLumberjackInventoryAllowed} entity={GameObject.name} good={FertileAshRecoveredGoodStackRules.FertileAshGoodId} capacity={inventory.Capacity} reason=added_takeable_output");
    }

  }
}
#endif
