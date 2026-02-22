using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireFestivalRiskController : BaseComponent,
                                             IAwakableComponent,
                                             IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;
    private const float SimHoursPerTick = 0.25f;

    private FireFestivalRuntimeState _fireFestivalRuntimeState;
    private FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private FireWaterContextRuntimeState _fireWaterContextRuntimeState;
    private FireTuningRuntimeState _fireTuningRuntimeState;

    private float _timeSinceLastUpdate;
    private float _hoursUntilFestival;
    private float _festivalHoursRemaining;

    [Inject]
    public void InjectDependencies(
      FireFestivalRuntimeState fireFestivalRuntimeState,
      FireSuppressionRuntimeState fireSuppressionRuntimeState,
      FireWaterContextRuntimeState fireWaterContextRuntimeState,
      FireTuningRuntimeState fireTuningRuntimeState) {
      _fireFestivalRuntimeState = fireFestivalRuntimeState;
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
      _fireWaterContextRuntimeState = fireWaterContextRuntimeState;
      _fireTuningRuntimeState = fireTuningRuntimeState;
    }

    public void Awake() {
      var entityId = GameObject.GetInstanceID();
      _hoursUntilFestival = 24f + Mathf.Abs(entityId % 48);
      _festivalHoursRemaining = 0f;
    }

    public void Update() {
      _timeSinceLastUpdate += Time.deltaTime;
      if (_timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      if (_festivalHoursRemaining > 0f) {
        _festivalHoursRemaining = Mathf.Max(0f, _festivalHoursRemaining - SimHoursPerTick);
      } else {
        _hoursUntilFestival = Mathf.Max(0f, _hoursUntilFestival - SimHoursPerTick);
        if (_hoursUntilFestival <= 0f) {
          _festivalHoursRemaining = 6f;
          _hoursUntilFestival = 36f;
        }
      }

      var festivalActive = _festivalHoursRemaining > 0f;

      var suppressionPower = 0f;
      var waterExposure = 0f;

      var entityId = GameObject.GetInstanceID();
      if (_fireSuppressionRuntimeState.TryGetSnapshot(entityId, out var suppressionSnapshot)) {
        suppressionPower = suppressionSnapshot.SuppressionPower;
      }

      if (_fireWaterContextRuntimeState.TryGetSnapshot(entityId, out var waterSnapshot)) {
        waterExposure = waterSnapshot.LocalWaterExposure;
      }

      var safetyPreparation = Mathf.Clamp01((suppressionPower * 0.18f) + (waterExposure * 0.82f));
      var festivalRiskBonus = festivalActive ? Mathf.Clamp(0.04f * (1f - safetyPreparation), 0f, 0.04f) : 0f;
      festivalRiskBonus *= _fireTuningRuntimeState.Current.FestivalRiskMultiplier;
      festivalRiskBonus = Mathf.Clamp(festivalRiskBonus, 0f, 0.06f);

      var snapshot = new FireFestivalSnapshot(
        festivalActive,
        festivalRiskBonus,
        safetyPreparation,
        _hoursUntilFestival,
        _festivalHoursRemaining);

      _fireFestivalRuntimeState.SetSnapshot(entityId, snapshot);
    }

  }
}