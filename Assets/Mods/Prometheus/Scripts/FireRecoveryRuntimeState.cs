namespace Mods.Prometheus.Scripts {
  internal readonly struct FireRecoverySnapshot {

    public bool ControlledBurn { get; }
    public bool FertileAshAvailable { get; }
    public float FertilityBoost { get; }
    public float GrowthSpeedBonus { get; }
    public float YieldBonus { get; }
    public float RemainingHours { get; }

    public FireRecoverySnapshot(
      bool controlledBurn,
      bool fertileAshAvailable,
      float fertilityBoost,
      float growthSpeedBonus,
      float yieldBonus,
      float remainingHours) {
      ControlledBurn = controlledBurn;
      FertileAshAvailable = fertileAshAvailable;
      FertilityBoost = fertilityBoost;
      GrowthSpeedBonus = growthSpeedBonus;
      YieldBonus = yieldBonus;
      RemainingHours = remainingHours;
    }

  }

  internal class FireRecoveryRuntimeState : EntitySnapshotStore<FireRecoverySnapshot> {
  }
}
