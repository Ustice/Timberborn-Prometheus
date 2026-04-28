using Timberborn.SingletonSystem;
#if !PROMETHEUS_TESTS
using UnityEngine;
#endif

namespace Mods.Prometheus.Scripts {
  internal sealed class PrometheusWorldLoadState : ILoadableSingleton,
                                                  IPostLoadableSingleton,
                                                  IUnloadableSingleton {

    private const float PostLoadSettleDelaySeconds = 4f;

    private bool _postLoadComplete;
#if !PROMETHEUS_TESTS
    private float _postLoadRealtime;
#endif

    public bool WorldReady {
      get {
#if PROMETHEUS_TESTS
        return _postLoadComplete;
#else
        return _postLoadComplete
               && Time.realtimeSinceStartup - _postLoadRealtime >= PostLoadSettleDelaySeconds;
#endif
      }
    }

    public void Load() {
      _postLoadComplete = false;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorldLoadStateChanged} ready=false stage=load");
    }

    public void PostLoad() {
      _postLoadComplete = true;
#if !PROMETHEUS_TESTS
      _postLoadRealtime = Time.realtimeSinceStartup;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorldLoadStateChanged} ready=false stage=post_load_settling delaySeconds={PostLoadSettleDelaySeconds:0.###}");
#else
      FireTelemetry.Log($"event={FireTelemetryEvents.WorldLoadStateChanged} ready=true stage=post_load");
#endif
    }

    public void Unload() {
      _postLoadComplete = false;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorldLoadStateChanged} ready=false stage=unload");
    }

  }
}
