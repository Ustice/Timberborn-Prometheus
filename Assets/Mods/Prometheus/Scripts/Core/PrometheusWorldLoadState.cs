using Timberborn.SingletonSystem;

namespace Mods.Prometheus.Scripts {
  internal sealed class PrometheusWorldLoadState : ILoadableSingleton,
                                                  IPostLoadableSingleton,
                                                  IUnloadableSingleton {

    public bool WorldReady { get; private set; }

    public void Load() {
      WorldReady = false;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorldLoadStateChanged} ready=false stage=load");
    }

    public void PostLoad() {
      WorldReady = true;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorldLoadStateChanged} ready=true stage=post_load");
    }

    public void Unload() {
      WorldReady = false;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorldLoadStateChanged} ready=false stage=unload");
    }

  }
}
