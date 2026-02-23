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

  internal class FireImpactRuntimeState : EntitySnapshotStore<FireImpactSnapshot> {
  }
}