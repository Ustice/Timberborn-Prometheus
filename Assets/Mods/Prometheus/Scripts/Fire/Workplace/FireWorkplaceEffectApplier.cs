using System.Collections.Generic;
using Bindito.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.WorkSystem;
using UnityEngine;

namespace Mods.Prometheus.Scripts {
  internal class FireWorkplaceEffectApplier : BaseComponent,
                                             IUpdatableComponent {

    private const float UpdateIntervalInSeconds = 1f;

    private FireRuntimeProjectionRuntimeState _fireRuntimeProjectionRuntimeState;
    private float _timeSinceLastUpdate;
    private Workplace _workplace;
    private readonly List<Behaviour> _workplaceSupportBehaviours = new();
    private readonly Dictionary<Behaviour, bool> _workplaceSupportOriginalEnabledState = new();
    private readonly List<Behaviour> _operationalBehaviours = new();
    private readonly Dictionary<Behaviour, bool> _operationalOriginalEnabledState = new();
    private readonly Dictionary<Worker, float> _appliedWorkerSpeedPenalties = new();
    private readonly Dictionary<Worker, float> _originalWorkerSpeedMultipliers = new();
    private readonly List<Worker> _workersToRestore = new();
    private bool _workplaceSupportDisabled;
    private bool _operationalDisabled;
    private bool _loggedWorkplaceSpeedApiResolved;
    private float _lastLoggedPenaltyDelta = float.NaN;
    private int _lastLoggedAssignedWorkerCount = -1;
    private int _lastLoggedIndoorExposedWorkerCount = -1;

    [Inject]
    public void InjectDependencies(FireRuntimeProjectionRuntimeState fireRuntimeProjectionRuntimeState) {
      _fireRuntimeProjectionRuntimeState = fireRuntimeProjectionRuntimeState;
    }

    public void Update() {
      if (!TickGate.ShouldRun(ref _timeSinceLastUpdate, UpdateIntervalInSeconds)) {
        return;
      }

      EnsureWorkplaceBound();
      EnsureWorkplaceSupportBehavioursBound();
      EnsureOperationalBehavioursBound();

      var entityId = GameObject.GetInstanceID();
      if (!_fireRuntimeProjectionRuntimeState.TryGetSnapshot(entityId, out var projection) || !projection.HasImpact) {
        RestoreWorkerSpeedPenalties();
        RestoreWorkplaceSupport();
        RestoreOperationalBehaviours();
        return;
      }

      var isWorkplaceEntity = _workplace is not null;
      var buildingDead = FireRuntimeProjectionRules.ShouldDisableWorkplaceOperations(projection, isWorkplaceEntity);
      var workingSpeedMultiplier = FireRuntimeProjectionRules.ComputeWorkplaceSpeedMultiplier(projection, isWorkplaceEntity);

      if (buildingDead) {
        DisableWorkplaceSupport();
        DisableOperationalBehaviours();
      } else {
        RestoreWorkplaceSupport();
        RestoreOperationalBehaviours();
      }

      ApplyWorkerSpeedPenalty(workingSpeedMultiplier);
      ApplyAssignedWorkerIndoorExposure(projection);
    }

    internal void DebugResetFireEffects() {
      EnsureWorkplaceBound();
      EnsureWorkplaceSupportBehavioursBound();
      EnsureOperationalBehavioursBound();

      RestoreWorkerSpeedPenalties();
      RestoreWorkplaceSupport();
      RestoreOperationalBehaviours();
      _lastLoggedPenaltyDelta = float.NaN;
      _lastLoggedAssignedWorkerCount = -1;
      _lastLoggedIndoorExposedWorkerCount = -1;
    }

    private void EnsureWorkplaceBound() {
      if (_workplace is not null) {
        return;
      }

      if (TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<Workplace>(GameObject, out var workplace)) {
        _workplace = workplace;
        LogWorkplaceSpeedApiResolved();
      }
    }

    private void ApplyWorkerSpeedPenalty(float workingSpeedMultiplier) {
      if (_workplace is null) {
        RestoreWorkerSpeedPenalties();
        return;
      }

      var penaltyDelta = Mathf.Clamp(workingSpeedMultiplier - 1f, -1f, 0f);
      var assignedWorkerCount = _workplace.AssignedWorkers.Count;
      if (Mathf.Approximately(penaltyDelta, 0f)) {
        RestoreWorkerSpeedPenalties();
        LogWorkerPenaltyState(assignedWorkerCount, penaltyDelta, 0);
        return;
      }

      _workersToRestore.Clear();
      foreach (var pair in _appliedWorkerSpeedPenalties) {
        if (pair.Key is null || !_workplace.AssignedWorkers.Contains(pair.Key)) {
          _workersToRestore.Add(pair.Key);
        }
      }

      for (var i = 0; i < _workersToRestore.Count; i++) {
        RestoreWorkerSpeedPenalty(_workersToRestore[i]);
      }

      var appliedCount = 0;
      foreach (var worker in _workplace.AssignedWorkers) {
        if (worker is null || worker._bonusManager is null) {
          continue;
        }

        if (_appliedWorkerSpeedPenalties.TryGetValue(worker, out var existingPenalty)
            && Mathf.Approximately(existingPenalty, penaltyDelta)) {
          continue;
        }

        RestoreWorkerSpeedPenalty(worker);
        _originalWorkerSpeedMultipliers[worker] = worker.WorkingSpeedMultiplier;
        worker._bonusManager.AddBonus(Worker.WorkingSpeedBonusId, penaltyDelta);
        worker.WorkingSpeedMultiplier = Mathf.Max(0f, _originalWorkerSpeedMultipliers[worker] + penaltyDelta);
        _appliedWorkerSpeedPenalties[worker] = penaltyDelta;
        appliedCount++;
      }

      LogWorkerPenaltyState(assignedWorkerCount, penaltyDelta, appliedCount);
    }

    private void ApplyAssignedWorkerIndoorExposure(FireRuntimeProjectionSnapshot projection) {
      if (_workplace is null) {
        return;
      }

      var exposedWorkerCount = 0;
      foreach (var worker in _workplace.AssignedWorkers) {
        if (worker is null || worker.GameObject == null) {
          continue;
        }

        if (FireBeaverEffectApplier.TryApplyIndoorExposure(worker.GameObject.transform, projection)) {
          exposedWorkerCount++;
        }
      }

      if (exposedWorkerCount > 0 && exposedWorkerCount != _lastLoggedIndoorExposedWorkerCount) {
        FireTelemetry.Log($"event={FireTelemetryEvents.WorkplaceIndoorExposure} entity={GameObject.name} id={GameObject.GetInstanceID()} exposedWorkers={exposedWorkerCount}");
      }

      _lastLoggedIndoorExposedWorkerCount = exposedWorkerCount;
    }

    private void RestoreWorkerSpeedPenalties() {
      if (_appliedWorkerSpeedPenalties.Count == 0) {
        return;
      }

      _workersToRestore.Clear();
      foreach (var pair in _appliedWorkerSpeedPenalties) {
        _workersToRestore.Add(pair.Key);
      }

      for (var i = 0; i < _workersToRestore.Count; i++) {
        RestoreWorkerSpeedPenalty(_workersToRestore[i]);
      }
    }

    private void RestoreWorkerSpeedPenalty(Worker worker) {
      if (!_appliedWorkerSpeedPenalties.TryGetValue(worker, out var penaltyDelta)) {
        return;
      }

      _appliedWorkerSpeedPenalties.Remove(worker);
      if (worker is null || worker._bonusManager is null) {
        return;
      }

      worker._bonusManager.RemoveBonus(Worker.WorkingSpeedBonusId, penaltyDelta);
      if (_originalWorkerSpeedMultipliers.TryGetValue(worker, out var originalWorkingSpeedMultiplier)) {
        worker.WorkingSpeedMultiplier = originalWorkingSpeedMultiplier;
        _originalWorkerSpeedMultipliers.Remove(worker);
      }
    }

    private void LogWorkplaceSpeedApiResolved() {
      if (_loggedWorkplaceSpeedApiResolved) {
        return;
      }

      _loggedWorkplaceSpeedApiResolved = true;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorkplaceSpeedApiResolved} entity={GameObject.name} id={GameObject.GetInstanceID()} api=\"Worker.BonusManager({Worker.WorkingSpeedBonusId})\"");
    }

    private void LogWorkerPenaltyState(int assignedWorkerCount, float penaltyDelta, int appliedCount) {
      if (_lastLoggedAssignedWorkerCount == assignedWorkerCount
          && !ShouldLogPenaltyDeltaChange(_lastLoggedPenaltyDelta, penaltyDelta)) {
        return;
      }

      _lastLoggedAssignedWorkerCount = assignedWorkerCount;
      _lastLoggedPenaltyDelta = penaltyDelta;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorkplaceSpeedPenaltyState} entity={GameObject.name} id={GameObject.GetInstanceID()} assignedWorkers={assignedWorkerCount} appliedWorkers={appliedCount} penaltyDelta={penaltyDelta:0.000}");
    }

    private static bool ShouldLogPenaltyDeltaChange(float previousPenaltyDelta, float currentPenaltyDelta) {
      if (float.IsNaN(previousPenaltyDelta)) {
        return true;
      }

      if (Mathf.Approximately(previousPenaltyDelta, currentPenaltyDelta)) {
        return false;
      }

      if (Mathf.Approximately(currentPenaltyDelta, 0f)
          || Mathf.Approximately(currentPenaltyDelta, -1f)) {
        return true;
      }

      return Mathf.Abs(currentPenaltyDelta - previousPenaltyDelta) >= 0.05f;
    }

    private void EnsureWorkplaceSupportBehavioursBound() {
      if (_workplaceSupportBehaviours.Count > 0 || _workplaceSupportDisabled) {
        return;
      }

      foreach (var behaviour in TimberbornComponentCacheLookup.FindBehavioursByPolicy(GameObject, TimberbornCompatibility.IsWorkplaceSupportComponentName)) {
        if (!_workplaceSupportOriginalEnabledState.ContainsKey(behaviour)) {
          _workplaceSupportOriginalEnabledState[behaviour] = behaviour.enabled;
        }

        _workplaceSupportBehaviours.Add(behaviour);
      }
    }

    private void DisableWorkplaceSupport() {
      if (_workplaceSupportDisabled) {
        return;
      }

      for (var i = 0; i < _workplaceSupportBehaviours.Count; i++) {
        var behaviour = _workplaceSupportBehaviours[i];
        if (behaviour == null) {
          continue;
        }

        behaviour.enabled = false;
      }

      _workplaceSupportDisabled = true;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorkplaceSupportDisabled} entity={GameObject.name} id={GameObject.GetInstanceID()} reason=building_dead components={_workplaceSupportBehaviours.Count}");
    }

    private void RestoreWorkplaceSupport() {
      if (!_workplaceSupportDisabled) {
        return;
      }

      foreach (var pair in _workplaceSupportOriginalEnabledState) {
        var behaviour = pair.Key;
        if (behaviour == null) {
          continue;
        }

        behaviour.enabled = pair.Value;
      }

      _workplaceSupportDisabled = false;
      FireTelemetry.Log($"event={FireTelemetryEvents.WorkplaceSupportRestored} entity={GameObject.name} id={GameObject.GetInstanceID()} components={_workplaceSupportOriginalEnabledState.Count}");
    }

    private void EnsureOperationalBehavioursBound() {
      if (_operationalBehaviours.Count > 0 || _operationalDisabled) {
        return;
      }

      foreach (var behaviour in TimberbornComponentCacheLookup.FindBehavioursByPolicy(GameObject, TimberbornCompatibility.IsOperationalComponentName)) {
        if (!_operationalOriginalEnabledState.ContainsKey(behaviour)) {
          _operationalOriginalEnabledState[behaviour] = behaviour.enabled;
        }

        _operationalBehaviours.Add(behaviour);
      }
    }

    private void DisableOperationalBehaviours() {
      if (_operationalDisabled) {
        return;
      }

      for (var i = 0; i < _operationalBehaviours.Count; i++) {
        var behaviour = _operationalBehaviours[i];
        if (behaviour == null) {
          continue;
        }

        behaviour.enabled = false;
      }

      _operationalDisabled = true;
      FireTelemetry.Log($"event={FireTelemetryEvents.BuildingOperationsDisabled} entity={GameObject.name} id={GameObject.GetInstanceID()} reason=building_dead components={_operationalBehaviours.Count}");
    }

    private void RestoreOperationalBehaviours() {
      if (!_operationalDisabled) {
        return;
      }

      foreach (var pair in _operationalOriginalEnabledState) {
        var behaviour = pair.Key;
        if (behaviour == null) {
          continue;
        }

        behaviour.enabled = pair.Value;
      }

      _operationalDisabled = false;
      FireTelemetry.Log($"event={FireTelemetryEvents.BuildingOperationsRestored} entity={GameObject.name} id={GameObject.GetInstanceID()} components={_operationalOriginalEnabledState.Count}");
    }

  }
}
