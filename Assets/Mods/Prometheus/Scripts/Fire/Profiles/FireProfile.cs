using Timberborn.BaseComponentSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireProfile : BaseComponent,
                               IAwakableComponent {

    private FireProfileSpec _spec;

    public float Fuel => _spec.Fuel > 0f ? _spec.Fuel : 1f;

    public float MoistureResistance => Mathf.Clamp01(_spec.MoistureResistance);

    public float BarrierResistance => Mathf.Clamp01(_spec.BarrierResistance);

    public float IgnitionThreshold => _spec.IgnitionThreshold > 0f ? _spec.IgnitionThreshold : 0.45f;

    public float OxygenDemand => _spec.OxygenDemand > 0f ? _spec.OxygenDemand : 0.35f;

    public float HeatSourceIntensity => Mathf.Max(0f, _spec.HeatSourceIntensity);

    public float EmberSourceIntensity => Mathf.Max(0f, _spec.EmberSourceIntensity);

    public float SmokeSourceIntensity => Mathf.Max(0f, _spec.SmokeSourceIntensity);

    public float SourceRadius => _spec.SourceRadius > 0f ? _spec.SourceRadius : 1f;

    public bool RequiresOperation => _spec.RequiresOperation;

    public string StructureKind => string.IsNullOrWhiteSpace(_spec.StructureKind) ? "Unknown" : _spec.StructureKind;

    public void Awake() {
      _spec = GetComponent<FireProfileSpec>();
    }

  }
}
