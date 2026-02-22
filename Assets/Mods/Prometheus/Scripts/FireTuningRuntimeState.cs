namespace Mods.Prometheus.Scripts {
  internal enum FireTuningProfile {
    Low,
    Standard,
    High,
  }

  internal readonly struct FireTuningSnapshot {

    public FireTuningProfile Profile { get; }
    public float IgnitionMultiplier { get; }
    public float SpreadMultiplier { get; }
    public float QuenchingMultiplier { get; }
    public float ImpactMultiplier { get; }
    public float DamageTickMultiplier { get; }
    public float FestivalRiskMultiplier { get; }
    public float WeatherIgnitionMultiplier { get; }
    public float IndustrialIgnitionMultiplier { get; }
    public float FireworksIgnitionMultiplier { get; }
    public float ControlledBurnIgnitionMultiplier { get; }
    public float NeighborIgnitionMultiplier { get; }
    public float DrynessSpreadMultiplier { get; }
    public float FuelSpreadMultiplier { get; }
    public float BarrierResistanceMultiplier { get; }

    public FireTuningSnapshot(
      FireTuningProfile profile,
      float ignitionMultiplier,
      float spreadMultiplier,
      float quenchingMultiplier,
      float impactMultiplier,
      float damageTickMultiplier,
      float festivalRiskMultiplier,
      float weatherIgnitionMultiplier,
      float industrialIgnitionMultiplier,
      float fireworksIgnitionMultiplier,
      float controlledBurnIgnitionMultiplier,
      float neighborIgnitionMultiplier,
      float drynessSpreadMultiplier,
      float fuelSpreadMultiplier,
      float barrierResistanceMultiplier) {
      Profile = profile;
      IgnitionMultiplier = ignitionMultiplier;
      SpreadMultiplier = spreadMultiplier;
      QuenchingMultiplier = quenchingMultiplier;
      ImpactMultiplier = impactMultiplier;
      DamageTickMultiplier = damageTickMultiplier;
      FestivalRiskMultiplier = festivalRiskMultiplier;
      WeatherIgnitionMultiplier = weatherIgnitionMultiplier;
      IndustrialIgnitionMultiplier = industrialIgnitionMultiplier;
      FireworksIgnitionMultiplier = fireworksIgnitionMultiplier;
      ControlledBurnIgnitionMultiplier = controlledBurnIgnitionMultiplier;
      NeighborIgnitionMultiplier = neighborIgnitionMultiplier;
      DrynessSpreadMultiplier = drynessSpreadMultiplier;
      FuelSpreadMultiplier = fuelSpreadMultiplier;
      BarrierResistanceMultiplier = barrierResistanceMultiplier;
    }

  }

  internal class FireTuningRuntimeState {

    private static readonly FireTuningSnapshot LowSnapshot = new(
      FireTuningProfile.Low,
      0.65f,
      0.7f,
      1.2f,
      0.7f,
      0.75f,
      0.65f,
      0.8f,
      0.75f,
      0.8f,
      0.95f,
      0.85f,
      0.85f,
      0.85f,
      1.15f);

    private static readonly FireTuningSnapshot StandardSnapshot = new(
      FireTuningProfile.Standard,
      1f,
      1f,
      1f,
      1f,
      1f,
      1f,
      1f,
      1f,
      1f,
      1f,
      1f,
      1f,
      1f,
      1f);

    private static readonly FireTuningSnapshot HighSnapshot = new(
      FireTuningProfile.High,
      1.4f,
      1.35f,
      0.9f,
      1.3f,
      1.4f,
      1.35f,
      1.25f,
      1.35f,
      1.2f,
      0.9f,
      1.35f,
      1.2f,
      1.2f,
      0.85f);

    private FireTuningSnapshot _currentSnapshot = StandardSnapshot;

    public FireTuningSnapshot Current => _currentSnapshot;

    public void SetProfile(FireTuningProfile profile) {
      _currentSnapshot = profile switch {
        FireTuningProfile.Low => LowSnapshot,
        FireTuningProfile.High => HighSnapshot,
        _ => StandardSnapshot,
      };
    }

  }
}