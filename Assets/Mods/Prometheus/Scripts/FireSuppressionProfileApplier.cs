using Bindito.Core;
using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireSuppressionProfileApplier : BaseComponent,
                                                 IAwakableComponent,
                                                 IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireResponseProfile _fireResponseProfile;
    private FireSuppressionRuntimeState _fireSuppressionRuntimeState;
    private float _timeSinceLastUpdate;

    [Inject]
    public void InjectDependencies(FireSuppressionRuntimeState fireSuppressionRuntimeState) {
      _fireSuppressionRuntimeState = fireSuppressionRuntimeState;
    }

    public void Awake() {
      _fireResponseProfile = GetComponent<FireResponseProfile>();
    }

    public void Update() {
      _fireResponseProfile ??= GetComponent<FireResponseProfile>();
      if (_fireResponseProfile == null) {
        return;
      }

      var entityId = GameObject.GetInstanceID();
      var hasExistingSnapshot = _fireSuppressionRuntimeState.TryGetSnapshot(entityId, out _);

      _timeSinceLastUpdate += Time.deltaTime;
      if (hasExistingSnapshot && _timeSinceLastUpdate < UpdateIntervalInSeconds) {
        return;
      }

      _timeSinceLastUpdate = 0f;

      var suppressionPower = _fireResponseProfile.SuppressionSpeedMultiplier * _fireResponseProfile.WaterEfficiencyMultiplier;
      var heatMitigation = Mathf.Clamp01(_fireResponseProfile.HeatResistanceBonus);
      var waterEfficiency = _fireResponseProfile.WaterEfficiencyMultiplier;

      var snapshot = new FireSuppressionSnapshot(
        _fireResponseProfile.FactionApproach,
        suppressionPower,
        heatMitigation,
        waterEfficiency,
        _fireResponseProfile.DispatchAssignmentLockDurationInSeconds,
        _fireResponseProfile.DispatchRetargetHysteresisThreshold);

      _fireSuppressionRuntimeState.SetSnapshot(entityId, snapshot);
    }

  }
}