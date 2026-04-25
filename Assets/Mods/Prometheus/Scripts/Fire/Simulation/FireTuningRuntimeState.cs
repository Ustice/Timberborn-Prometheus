namespace Mods.Prometheus.Scripts {
  internal enum FireTuningProfile {
    Low,
    Standard,
    High,
  }

  internal readonly struct FireTuningSnapshot {

    public FireTuningProfile Profile { get; }
    public float IgnitionMultiplier { get; }
    public float ImpactMultiplier { get; }
    public float DamageTickMultiplier { get; }

    public FireTuningSnapshot(
      FireTuningProfile profile,
      float ignitionMultiplier,
      float impactMultiplier,
      float damageTickMultiplier) {
      Profile = profile;
      IgnitionMultiplier = ignitionMultiplier;
      ImpactMultiplier = impactMultiplier;
      DamageTickMultiplier = damageTickMultiplier;
    }

  }

  internal class FireTuningRuntimeState {

    private static readonly FireTuningSnapshot LowSnapshot = new(
      FireTuningProfile.Low,
      0.65f,
      0.7f,
      0.75f);

    private static readonly FireTuningSnapshot StandardSnapshot = new(
      FireTuningProfile.Standard,
      1f,
      1f,
      1f);

    private static readonly FireTuningSnapshot HighSnapshot = new(
      FireTuningProfile.High,
      1.4f,
      1.3f,
      1.4f);

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
