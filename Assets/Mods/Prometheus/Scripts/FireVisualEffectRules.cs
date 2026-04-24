using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireVisualEffectTuning {

    public static FireVisualEffectTuning Default { get; } = new(
      1.0f,
      1.0f,
      1.0f,
      1.0f,
      1.0f);

    public float EmberScale { get; }
    public float SmokeScale { get; }
    public float FireScale { get; }
    public float SteamScale { get; }
    public float CharScale { get; }

    public FireVisualEffectTuning(
      float emberScale,
      float smokeScale,
      float fireScale,
      float steamScale,
      float charScale) {
      EmberScale = Mathf.Max(0f, emberScale);
      SmokeScale = Mathf.Max(0f, smokeScale);
      FireScale = Mathf.Max(0f, fireScale);
      SteamScale = Mathf.Max(0f, steamScale);
      CharScale = Mathf.Max(0f, charScale);
    }

  }

  internal readonly struct FireVisualEffectIntensity {

    public float Embers { get; }
    public float Smoke { get; }
    public float Fire { get; }
    public float Steam { get; }
    public float Char { get; }
    public bool HasAnyVisibleEffect => Embers > 0f || Smoke > 0f || Fire > 0f || Steam > 0f || Char > 0f;

    public FireVisualEffectIntensity(
      float embers,
      float smoke,
      float fire,
      float steam,
      float charAmount) {
      Embers = Mathf.Clamp01(embers);
      Smoke = Mathf.Clamp01(smoke);
      Fire = Mathf.Clamp01(fire);
      Steam = Mathf.Clamp01(steam);
      Char = Mathf.Clamp01(charAmount);
    }

  }

  internal static class FireVisualEffectRules {

    internal static FireVisualEffectIntensity ComputeIntensity(
      FireSimulationSnapshot simulation,
      FireWaterContextSnapshot waterContext,
      FireDamageStateSnapshot damageState,
      FireVisualEffectTuning tuning) {
      var heatPressure = Mathf.Clamp01(Mathf.Max(
        simulation.Intensity,
        simulation.SpreadPressure,
        simulation.NeighborSpreadPressure,
        simulation.HeatExposure));
      var waterExposure = Mathf.Clamp01(waterContext.LocalWaterExposure);
      var moistureDampening = Mathf.Clamp01(waterExposure + waterContext.QuenchingBonus + waterContext.SpreadReduction);
      var severity = Mathf.Clamp01(damageState.Severity);

      var embers = Mathf.Clamp01(((simulation.SpreadPressure * 1.6f) + (simulation.NeighborSpreadPressure * 0.8f) + (simulation.Intensity * 0.55f)) * tuning.EmberScale);
      var smokeBase = damageState.State switch {
        FireDamageState.Scorched => Mathf.Max(0.25f, severity),
        FireDamageState.Burning => Mathf.Max(0.45f, simulation.Intensity),
        FireDamageState.Dead => 0.15f,
        _ => 0f,
      };
      var smoke = Mathf.Clamp01(smokeBase * (1f - (moistureDampening * 0.25f)) * tuning.SmokeScale);
      var fire = simulation.Burning && damageState.State != FireDamageState.Dead
        ? Mathf.Clamp01(Mathf.Max(0.15f, simulation.Intensity) * (1f - (moistureDampening * 0.35f)) * tuning.FireScale)
        : 0f;
      var steam = Mathf.Clamp01(heatPressure * moistureDampening * tuning.SteamScale);
      var charAmount = damageState.State switch {
        FireDamageState.Scorched => Mathf.Clamp01(severity * 0.35f * tuning.CharScale),
        FireDamageState.Burning => Mathf.Clamp01((0.2f + (severity * 0.45f)) * tuning.CharScale),
        FireDamageState.Dead => Mathf.Clamp01(Mathf.Max(0.75f, severity) * tuning.CharScale),
        _ => 0f,
      };

      return new FireVisualEffectIntensity(embers, smoke, fire, steam, charAmount);
    }

  }
}
