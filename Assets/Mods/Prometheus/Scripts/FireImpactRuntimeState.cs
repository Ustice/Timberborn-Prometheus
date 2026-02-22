using System.Collections.Generic;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireImpactSnapshot {

    public float CropDamagePressure { get; }
    public float TreeDamagePressure { get; }
    public float BuildingDamagePressure { get; }
    public float DehydrationPressure { get; }
    public float InjuryPressure { get; }

    public FireImpactSnapshot(
      float cropDamagePressure,
      float treeDamagePressure,
      float buildingDamagePressure,
      float dehydrationPressure,
      float injuryPressure) {
      CropDamagePressure = cropDamagePressure;
      TreeDamagePressure = treeDamagePressure;
      BuildingDamagePressure = buildingDamagePressure;
      DehydrationPressure = dehydrationPressure;
      InjuryPressure = injuryPressure;
    }

  }

  internal class FireImpactRuntimeState {

    private readonly Dictionary<int, FireImpactSnapshot> _snapshotsByEntityId = new();

    public void SetSnapshot(int entityId, FireImpactSnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out FireImpactSnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
    }

  }
}