using System.Collections.Generic;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireRecoverySnapshot {

    public bool ControlledBurn { get; }
    public bool AshenFertilityActive { get; }
    public float FertilityBoost { get; }
    public float GrowthSpeedBonus { get; }
    public float YieldBonus { get; }
    public float RemainingHours { get; }

    public FireRecoverySnapshot(
      bool controlledBurn,
      bool ashenFertilityActive,
      float fertilityBoost,
      float growthSpeedBonus,
      float yieldBonus,
      float remainingHours) {
      ControlledBurn = controlledBurn;
      AshenFertilityActive = ashenFertilityActive;
      FertilityBoost = fertilityBoost;
      GrowthSpeedBonus = growthSpeedBonus;
      YieldBonus = yieldBonus;
      RemainingHours = remainingHours;
    }

  }

  internal class FireRecoveryRuntimeState {

    private readonly Dictionary<int, FireRecoverySnapshot> _snapshotsByEntityId = new();

    public void SetSnapshot(int entityId, FireRecoverySnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out FireRecoverySnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
    }

  }
}