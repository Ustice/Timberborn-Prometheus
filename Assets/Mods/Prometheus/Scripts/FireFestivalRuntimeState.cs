using System.Collections.Generic;

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

  internal class FireFestivalRuntimeState {

    private readonly Dictionary<int, FireFestivalSnapshot> _snapshotsByEntityId = new();

    public void SetSnapshot(int entityId, FireFestivalSnapshot snapshot) {
      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out FireFestivalSnapshot snapshot) {
      return _snapshotsByEntityId.TryGetValue(entityId, out snapshot);
    }

  }
}