namespace Mods.Prometheus.Scripts {
  internal class FireVisualEffectRuntimeState {

    public FireVisualEffectTuning CurrentTuning { get; private set; } = FireVisualEffectTuning.Default;

    public void ResetDefaults() {
      CurrentTuning = FireVisualEffectTuning.Default;
    }

    public void SetEmberScale(float scale) {
      CurrentTuning = CurrentTuning.With(emberScale: scale);
    }

    public void SetSmokeScale(float scale) {
      CurrentTuning = CurrentTuning.With(smokeScale: scale);
    }

    public void SetFireScale(float scale) {
      CurrentTuning = CurrentTuning.With(fireScale: scale);
    }

    public void SetSteamScale(float scale) {
      CurrentTuning = CurrentTuning.With(steamScale: scale);
    }

    public void SetCharScale(float scale) {
      CurrentTuning = CurrentTuning.With(charScale: scale);
    }

    public void SetHeightOffset(float offset) {
      CurrentTuning = CurrentTuning.With(heightOffset: offset);
    }

    public void SetDepthOffset(float offset) {
      CurrentTuning = CurrentTuning.With(depthOffset: offset);
    }

    public void SetEffectSize(float size) {
      CurrentTuning = CurrentTuning.With(effectSize: size);
    }

    public void SetEmberSpread(float spread) {
      CurrentTuning = CurrentTuning.With(emberSpread: spread);
    }

  }
}
