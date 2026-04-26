using System.Collections.Generic;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal readonly struct FireGridKernelEntry {

    public FireGridOffset Offset { get; }
    public float HeatWeight { get; }
    public float EmberWeight { get; }
    public float SmokeWeight { get; }
    public bool IsSelf => Offset.Dx == 0 && Offset.Dy == 0 && Offset.Dz == 0;

    public FireGridKernelEntry(FireGridOffset offset, float heatWeight, float emberWeight, float smokeWeight) {
      Offset = offset;
      HeatWeight = Mathf.Max(0f, heatWeight);
      EmberWeight = Mathf.Max(0f, emberWeight);
      SmokeWeight = Mathf.Max(0f, smokeWeight);
    }

  }

  internal sealed class FireGridKernel {

    public static FireGridKernel Full27 { get; } = CreateFull27();

    public IReadOnlyList<FireGridKernelEntry> Entries { get; }

    private FireGridKernel(IReadOnlyList<FireGridKernelEntry> entries) {
      Entries = entries;
    }

    private static FireGridKernel CreateFull27() {
      var entries = new List<FireGridKernelEntry>(27);
      for (var dx = -1; dx <= 1; dx++) {
        for (var dy = -1; dy <= 1; dy++) {
          for (var dz = -1; dz <= 1; dz++) {
            entries.Add(CreateEntry(dx, dy, dz));
          }
        }
      }

      return new FireGridKernel(entries);
    }

    private static FireGridKernelEntry CreateEntry(int dx, int dy, int dz) {
      if (dx == 0 && dy == 0 && dz == 0) {
        return new FireGridKernelEntry(
          new FireGridOffset(dx, dy, dz),
          FireGridPropagationPolicy.SelfHeatWeight,
          FireGridPropagationPolicy.SelfEmberWeight,
          FireGridPropagationPolicy.SelfSmokeWeight);
      }

      var distancePenalty = FireGridPropagationPolicy.DistancePenalty(dx, dy, dz);
      return new FireGridKernelEntry(
        new FireGridOffset(dx, dy, dz),
        FireGridPropagationPolicy.NeighborHeatBaseWeight * distancePenalty * FireGridPropagationPolicy.HeatDirectionMultiplier(dy),
        FireGridPropagationPolicy.NeighborEmberBaseWeight * distancePenalty * FireGridPropagationPolicy.EmberDirectionMultiplier(dy),
        FireGridPropagationPolicy.NeighborSmokeBaseWeight * distancePenalty * FireGridPropagationPolicy.SmokeDirectionMultiplier(dy));
    }

  }
}
