using System.Collections.Generic;
using System.Linq;

namespace Mods.Prometheus.Scripts {
  internal sealed class FireGridChunk {

    private readonly Dictionary<int, FireGridCoordinate> _coordinatesByLocalIndex = new();
    private readonly Dictionary<int, FireCellEnvironment> _environmentsByLocalIndex = new();
    private readonly Dictionary<int, FireCellState> _statesByLocalIndex = new();

    public int ActiveCellCount => _statesByLocalIndex.Count(pair => pair.Value.IsActive);

    public int EnvironmentCount => _environmentsByLocalIndex.Count;

    public IEnumerable<KeyValuePair<FireGridCoordinate, FireCellState>> StateEntries =>
      _statesByLocalIndex.Select(pair => new KeyValuePair<FireGridCoordinate, FireCellState>(_coordinatesByLocalIndex[pair.Key], pair.Value));

    public FireCellState GetState(FireGridCoordinate coordinate) {
      var index = FireGridChunkCoordinate.LocalIndex(coordinate);
      return _statesByLocalIndex.TryGetValue(index, out var state) ? state : FireCellState.Cold;
    }

    public bool TryGetState(FireGridCoordinate coordinate, out FireCellState state) {
      var index = FireGridChunkCoordinate.LocalIndex(coordinate);
      return _statesByLocalIndex.TryGetValue(index, out state);
    }

    public void SetState(FireGridCoordinate coordinate, FireCellState state) {
      var index = FireGridChunkCoordinate.LocalIndex(coordinate);
      if (!state.IsActive) {
        ClearState(coordinate);
        return;
      }

      _statesByLocalIndex[index] = state;
      _coordinatesByLocalIndex[index] = coordinate;
    }

    public void ClearState(FireGridCoordinate coordinate) {
      var index = FireGridChunkCoordinate.LocalIndex(coordinate);
      _statesByLocalIndex.Remove(index);
      if (!_environmentsByLocalIndex.ContainsKey(index)) {
        _coordinatesByLocalIndex.Remove(index);
      }
    }

    public void ClearStates() {
      _statesByLocalIndex.Clear();
      var stateOnlyCoordinates = _coordinatesByLocalIndex
        .Where(pair => !_environmentsByLocalIndex.ContainsKey(pair.Key))
        .Select(pair => pair.Key)
        .ToArray();
      for (var i = 0; i < stateOnlyCoordinates.Length; i++) {
        _coordinatesByLocalIndex.Remove(stateOnlyCoordinates[i]);
      }
    }

    public FireCellEnvironment GetEnvironment(FireGridCoordinate coordinate) {
      var index = FireGridChunkCoordinate.LocalIndex(coordinate);
      return _environmentsByLocalIndex.TryGetValue(index, out var environment) ? environment : FireCellEnvironment.OpenAir;
    }

    public void SetEnvironment(FireGridCoordinate coordinate, FireCellEnvironment environment) {
      var index = FireGridChunkCoordinate.LocalIndex(coordinate);
      _environmentsByLocalIndex[index] = environment;
      _coordinatesByLocalIndex[index] = coordinate;
    }

  }
}
