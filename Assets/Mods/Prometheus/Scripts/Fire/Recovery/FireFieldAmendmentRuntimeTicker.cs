using Timberborn.SingletonSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal sealed class FireFieldAmendmentRuntimeTicker : IUpdatableSingleton {

    private const float SimHoursPerSecond = 0.25f;

    private readonly FireFieldAmendmentRuntimeState _fireFieldAmendmentRuntimeState;

    public FireFieldAmendmentRuntimeTicker(FireFieldAmendmentRuntimeState fireFieldAmendmentRuntimeState) {
      _fireFieldAmendmentRuntimeState = fireFieldAmendmentRuntimeState;
    }

    public void UpdateSingleton() {
      var deltaHours = Time.deltaTime * SimHoursPerSecond;
      _fireFieldAmendmentRuntimeState.Tick(deltaHours);
    }

  }
}
