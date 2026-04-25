using Timberborn.BlueprintSystem;

namespace Mods.Prometheus.Scripts {
  internal record FireProfileSpec : ComponentSpec {

    [Serialize]
    public string StructureKind { get; init; }

    [Serialize]
    public float Fuel { get; init; }

    [Serialize]
    public float MoistureResistance { get; init; }

    [Serialize]
    public float BarrierResistance { get; init; }

    [Serialize]
    public float IgnitionThreshold { get; init; }

    [Serialize]
    public float OxygenDemand { get; init; }

    [Serialize]
    public float HeatSourceIntensity { get; init; }

    [Serialize]
    public float EmberSourceIntensity { get; init; }

    [Serialize]
    public float SmokeSourceIntensity { get; init; }

    [Serialize]
    public float SourceRadius { get; init; }

    [Serialize]
    public bool RequiresOperation { get; init; }

  }
}
