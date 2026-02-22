using System.Collections.Generic;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireWaterContextSnapshot {

    public bool IsFlooded { get; }
    public float WaterAboveBase { get; }
    public bool WaterNeedsMet { get; }
    public float LocalWaterExposure { get; }
    public float QuenchingBonus { get; }
    public float SpreadReduction { get; }

    public FireWaterContextSnapshot(
      bool isFlooded,
      float waterAboveBase,
      bool waterNeedsMet,
      float localWaterExposure,
      float quenchingBonus,
      float spreadReduction) {
      IsFlooded = isFlooded;
      WaterAboveBase = waterAboveBase;
      WaterNeedsMet = waterNeedsMet;
      LocalWaterExposure = localWaterExposure;
      QuenchingBonus = quenchingBonus;
      SpreadReduction = spreadReduction;
    }

  }

  internal class FireWaterContextRuntimeState {

    private readonly Dictionary<int, FireWaterContextSnapshot> _snapshotsByEntityId = new();

    public void SetSnapshot(int entityId, FireWaterContextSnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out FireWaterContextSnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
    }

  }
}