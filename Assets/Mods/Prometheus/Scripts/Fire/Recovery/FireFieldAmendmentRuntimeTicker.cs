using Timberborn.SingletonSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal sealed class FireFieldAmendmentRuntimeTicker : IPrometheusWorldReadyUpdatableSingleton {

    private const float SimHoursPerSecond = 0.25f;

    private readonly FireFieldAmendmentRuntimeState _fireFieldAmendmentRuntimeState;
    private readonly PrometheusWorldLoadState _worldLoadState;

    public FireFieldAmendmentRuntimeTicker(
      FireFieldAmendmentRuntimeState fireFieldAmendmentRuntimeState,
      PrometheusWorldLoadState worldLoadState) {
      _fireFieldAmendmentRuntimeState = fireFieldAmendmentRuntimeState;
      _worldLoadState = worldLoadState;
    }

    public void UpdateSingleton() {
      if (!_worldLoadState.WorldReady) {
        return;
      }

      var deltaHours = Time.deltaTime * SimHoursPerSecond;
      _fireFieldAmendmentRuntimeState.Tick(deltaHours);
    }

  }
}
