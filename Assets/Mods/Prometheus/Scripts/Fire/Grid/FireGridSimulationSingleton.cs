using Timberborn.SingletonSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal sealed class FireGridSimulationSingleton : IPrometheusWorldReadyUpdatableSingleton {

    private readonly FireGridSimulationCoordinator _fireGridSimulationCoordinator;
    private readonly PrometheusWorldLoadState _worldLoadState;

    public FireGridSimulationSingleton(
      FireGridSimulationCoordinator fireGridSimulationCoordinator,
      PrometheusWorldLoadState worldLoadState) {
      _fireGridSimulationCoordinator = fireGridSimulationCoordinator;
      _worldLoadState = worldLoadState;
    }

    public void UpdateSingleton() {
      if (!_worldLoadState.WorldReady) {
        return;
      }

      _fireGridSimulationCoordinator.StepFrame(Time.frameCount);
    }

  }
}
