using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireResponseProfile : BaseComponent,
                                      IAwakableComponent {

    private FireResponseProfileSpec _spec;

    public string FactionApproach => _spec.FactionApproach;

    public float SuppressionSpeedMultiplier => _spec.SuppressionSpeedMultiplier;

    public float HeatResistanceBonus => _spec.HeatResistanceBonus;

    public float WaterEfficiencyMultiplier => _spec.WaterEfficiencyMultiplier;

    public float WeatherIgnitionChance => _spec.WeatherIgnitionChance > 0f ? _spec.WeatherIgnitionChance : 0.008f;

    public float IndustrialIgnitionChance => Mathf.Max(0f, _spec.IndustrialIgnitionChance);

    public float FireworksIgnitionBonus => Mathf.Max(0f, _spec.FireworksIgnitionBonus);

    public bool SupportsControlledBurns => _spec.SupportsControlledBurns;

    public float ControlledBurnIgnitionChance => Mathf.Max(0f, _spec.ControlledBurnIgnitionChance);

    public float FuelSpreadMultiplier => _spec.FuelSpreadMultiplier > 0f ? _spec.FuelSpreadMultiplier : 1f;

    public float SpreadBarrierResistance => Mathf.Max(0f, _spec.SpreadBarrierResistance);

    public float IgnitionSensitivity => _spec.IgnitionSensitivity > 0f ? _spec.IgnitionSensitivity : 1f;

    public float DispatchSeverityWeight => _spec.DispatchSeverityWeight > 0f ? _spec.DispatchSeverityWeight : 0.4f;

    public float DispatchAssetRiskWeight => _spec.DispatchAssetRiskWeight > 0f ? _spec.DispatchAssetRiskWeight : 0.3f;

    public float DispatchTravelCostWeight => _spec.DispatchTravelCostWeight > 0f ? _spec.DispatchTravelCostWeight : 0.2f;

    public float DispatchContainmentLeverageWeight => _spec.DispatchContainmentLeverageWeight > 0f ? _spec.DispatchContainmentLeverageWeight : 0.25f;

    public float DispatchAssignmentLockDurationInSeconds => _spec.DispatchAssignmentLockDurationInSeconds > 0f ? _spec.DispatchAssignmentLockDurationInSeconds : 6f;

    public float DispatchRetargetHysteresisThreshold => _spec.DispatchRetargetHysteresisThreshold > 0f ? _spec.DispatchRetargetHysteresisThreshold : 0.08f;

    public void Awake() {
      _spec = GetComponent<FireResponseProfileSpec>();
    }

  }
}