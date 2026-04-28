using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal sealed class PrometheusQaCommandSingleton : IPrometheusWorldReadyUpdatableSingleton {

    private const string QaCommandFileName = "command.txt";
    private const string IgniteFirstTreeCommand = "ignite-first-tree";
    private const string IgniteFirstCropCommand = "ignite-first-crop";
    private const string IgniteFirstBuildingCommand = "ignite-first-building";
    private const float PollIntervalSeconds = 1f;

    private readonly FireExposureRuntimeState _fireExposureRuntimeState;
    private readonly PrometheusWorldLoadState _worldLoadState;
    private readonly PrometheusQaExchange _qaExchange = new();
    private string _lastQaCommandSignature = string.Empty;
    private float _timeSinceLastPoll;

    public PrometheusQaCommandSingleton(
      FireExposureRuntimeState fireExposureRuntimeState,
      PrometheusWorldLoadState worldLoadState) {
      _fireExposureRuntimeState = fireExposureRuntimeState;
      _worldLoadState = worldLoadState;
    }

    public void UpdateSingleton() {
      if (!_worldLoadState.WorldReady || !TickGate.ShouldRun(ref _timeSinceLastPoll, PollIntervalSeconds)) {
        return;
      }

      PollQaCommand();
    }

    private void PollQaCommand() {
      var commandPath = Path.Combine(_qaExchange.DirectoryPath, QaCommandFileName);
      if (!File.Exists(commandPath)) {
        return;
      }

      var command = File.ReadAllText(commandPath).Trim();
      var signature = $"{File.GetLastWriteTimeUtc(commandPath).Ticks}|{command}";
      if (string.IsNullOrWhiteSpace(command) || signature == _lastQaCommandSignature) {
        return;
      }

      _lastQaCommandSignature = signature;
      if (string.Equals(command, IgniteFirstTreeCommand, StringComparison.OrdinalIgnoreCase)) {
        IgniteFirstLoadedCategory(command, FireDamageCategory.Tree);
        return;
      }

      if (string.Equals(command, IgniteFirstCropCommand, StringComparison.OrdinalIgnoreCase)) {
        IgniteFirstLoadedCategory(command, FireDamageCategory.Crop);
        return;
      }

      if (string.Equals(command, IgniteFirstBuildingCommand, StringComparison.OrdinalIgnoreCase)) {
        IgniteFirstLoadedCategory(command, FireDamageCategory.Building);
        return;
      }

      FireTelemetry.LogWarning($"event={FireTelemetryEvents.QaCommandResult} command=\"{FireResetRegistry.EscapeToken(command)}\" result=unknown_command");
    }

    private void IgniteFirstLoadedCategory(string command, FireDamageCategory category) {
      var target = TimberbornComponentCacheLookup.FindLoadedPrometheusFireEntityGameObjects()
        .Where(IsQaIgnitionCandidate)
        .Select(gameObject => new {
          GameObject = gameObject,
          Category = TimberbornCompatibility.ClassifyDamageCategory(
            TimberbornComponentCacheLookup.EnumerateGameObjectAndCachedComponentTypeNames(gameObject),
            hasWorkplaceComponent: false),
        })
        .FirstOrDefault(candidate => candidate.Category == category);

      if (target == null) {
        FireTelemetry.LogWarning($"event={FireTelemetryEvents.QaCommandResult} command={command} result=no_loaded_target category={category.ToString().ToLowerInvariant()}");
        return;
      }

      var entityId = target.GameObject.GetInstanceID();
      var queued = _fireExposureRuntimeState.RequestForcedIgnition(entityId);
      FireTelemetry.Log(
        $"event={FireTelemetryEvents.QaCommandResult} command={command} result={(queued ? "success" : "rejected")} category={category.ToString().ToLowerInvariant()} id={entityId} entity=\"{FireResetRegistry.EscapeToken(target.GameObject.name)}\"");
    }

    private static bool IsQaIgnitionCandidate(GameObject gameObject) =>
      gameObject != null
      && gameObject.activeInHierarchy
      && gameObject.name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) < 0
      && TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<FireExposureController>(gameObject, out _);

  }
}
