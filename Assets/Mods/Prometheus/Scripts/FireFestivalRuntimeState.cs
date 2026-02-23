namespace Mods.Prometheus.Scripts {
  internal readonly struct FireFestivalSnapshot {

    public bool FestivalActive { get; }
    public float FestivalRiskBonus { get; }
    public float SafetyPreparation { get; }
    public float HoursUntilFestivalStart { get; }
    public float FestivalHoursRemaining { get; }

    public FireFestivalSnapshot(
      bool festivalActive,
      float festivalRiskBonus,
      float safetyPreparation,
      float hoursUntilFestivalStart,
      float festivalHoursRemaining) {
      FestivalActive = festivalActive;
      FestivalRiskBonus = festivalRiskBonus;
      SafetyPreparation = safetyPreparation;
      HoursUntilFestivalStart = hoursUntilFestivalStart;
      FestivalHoursRemaining = festivalHoursRemaining;
    }

  }

  internal class FireFestivalRuntimeState : EntitySnapshotStore<FireFestivalSnapshot> {
  }
}