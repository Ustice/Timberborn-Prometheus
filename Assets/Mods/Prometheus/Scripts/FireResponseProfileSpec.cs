using Timberborn.BlueprintSystem;

namespace Mods.Prometheus.Scripts {
  internal record FireResponseProfileSpec : ComponentSpec {

    [Serialize]
    public string FactionApproach { get; init; }

    [Serialize]
    public float SuppressionSpeedMultiplier { get; init; }

    [Serialize]
    public float HeatResistanceBonus { get; init; }

    [Serialize]
    public float WaterEfficiencyMultiplier { get; init; }

    [Serialize]
    public float WeatherIgnitionChance { get; init; }

    [Serialize]
    public float IndustrialIgnitionChance { get; init; }

    [Serialize]
    public float FireworksIgnitionBonus { get; init; }

    [Serialize]
    public bool SupportsControlledBurns { get; init; }

    [Serialize]
    public float ControlledBurnIgnitionChance { get; init; }

    [Serialize]
    public float FuelSpreadMultiplier { get; init; }

    [Serialize]
    public float SpreadBarrierResistance { get; init; }

    [Serialize]
    public float IgnitionSensitivity { get; init; }

    [Serialize]
    public float DispatchSeverityWeight { get; init; }

    [Serialize]
    public float DispatchAssetRiskWeight { get; init; }

    [Serialize]
    public float DispatchTravelCostWeight { get; init; }

    [Serialize]
    public float DispatchContainmentLeverageWeight { get; init; }

    [Serialize]
    public float DispatchAssignmentLockDurationInSeconds { get; init; }

    [Serialize]
    public float DispatchRetargetHysteresisThreshold { get; init; }

  }
}