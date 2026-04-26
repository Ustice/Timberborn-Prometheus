using Timberborn.SingletonSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal sealed class FireGridSimulationSingleton : IUpdatableSingleton {

    private readonly FireGridSimulationCoordinator _fireGridSimulationCoordinator;

    public FireGridSimulationSingleton(FireGridSimulationCoordinator fireGridSimulationCoordinator) {
      _fireGridSimulationCoordinator = fireGridSimulationCoordinator;
    }

    public void UpdateSingleton() {
      _fireGridSimulationCoordinator.StepFrame(Time.frameCount);
    }

  }
}
