using Timberborn.ModManagerScene;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  public class PrometheusLogger : IModStarter {

    public void StartMod(IModEnvironment modEnvironment) {
      Debug.Log("Prometheus loaded: fire system foundation initialized.");
    }

  }
}