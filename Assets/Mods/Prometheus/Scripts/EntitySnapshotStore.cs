using System.Collections.Generic;

namespace Mods.Prometheus.Scripts {
  internal class EntitySnapshotStore<TSnapshot> {

    private readonly Dictionary<int, TSnapshot> _snapshotsByEntityId = new();

    protected IEnumerable<KeyValuePair<int, TSnapshot>> SnapshotEntries => _snapshotsByEntityId;

    public int SnapshotCount => _snapshotsByEntityId.Count;

    public void SetSnapshot(int entityId, TSnapshot snapshot) {
      if (entityId == 0) {
        return;
      }

      _snapshotsByEntityId[entityId] = snapshot;
    }

    public bool TryGetSnapshot(int entityId, out TSnapshot snapshot) =>
      _snapshotsByEntityId.TryGetValue(entityId, out snapshot);

    public void RemoveSnapshot(int entityId) {
      _snapshotsByEntityId.Remove(entityId);
    }

    public void ClearSnapshots() {
      _snapshotsByEntityId.Clear();
    }

  }
}
