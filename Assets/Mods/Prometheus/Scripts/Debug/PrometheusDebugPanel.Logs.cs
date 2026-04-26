using System;
using TimberUi;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mods.Prometheus.Scripts {
  internal partial class PrometheusDebugPanel {

    private void PollLogStateAndRefresh() {
      var totalCount = FireTelemetry.GetRecentInGameLogEntries().Length;

      if (totalCount < _lastObservedEntryCount) {
        _lastObservedEntryCount = totalCount;
        SetUnreadCount(0);
      } else if (totalCount > _lastObservedEntryCount) {
        var addedCount = totalCount - _lastObservedEntryCount;
        _lastObservedEntryCount = totalCount;
        if (!IsOpen) {
          SetUnreadCount(_unreadCount + addedCount);
        }
      }

      RefreshLogPanel(force: false);
    }

    private VisualElement BuildLogPanel() {
      var controls = new VisualElement();

      var filtersSection = CreateSection(controls, "Filters");
      filtersSection.Add(BuildTypeSummaryRow());

      var filterButtonsRow = filtersSection.AddHorizontalContainer();

      _allFilterButton = AddFilterButtonTo(filterButtonsRow, "All", FireLogFilter.All).SetMarginRight(8);
      _eventsFilterButton = AddFilterButtonTo(filterButtonsRow, "Events", FireLogFilter.Events).SetMarginRight(8);
      _warningsFilterButton = AddFilterButtonTo(filterButtonsRow, "Warnings", FireLogFilter.Warnings).SetMarginRight(8);
      _errorsFilterButton = AddFilterButtonTo(filterButtonsRow, "Errors", FireLogFilter.Errors);
      var filterSearchRow = filtersSection.AddHorizontalContainer();

      _searchField = AddDefaultTextFieldTo(filterSearchRow, "Search");
      _searchField.RegisterValueChangedCallback(evt => {
        _searchText = evt.newValue ?? string.Empty;
        RefreshLogPanel(force: true);
      });

      _autoScrollToggle = AddGameToggleTo(filterSearchRow, "Auto", _autoScroll);
      _autoScrollToggle.RegisterValueChangedCallback(evt => _autoScroll = evt.newValue);
      UpdateFilterButtonStyles();

      var logSection = CreateSection(controls, "Log");

      _logScrollView = AddDefaultScrollViewTo(logSection, 170, 300);

      _logLinesContainer = _logScrollView.AddChild();

      return controls;
    }

    private VisualElement BuildTypeSummaryRow() {
      var summaryRow = new VisualElement().SetAsRow().SetWrap();

      _eventsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "EVENT");
      _warningsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "WARN");
      _errorsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "ERROR");
      _assertsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "ASSERT");
      _exceptionsSummaryLabel = AddSeveritySummaryLabelTo(summaryRow, "EXCEPTION");

      return summaryRow;
    }

    private static Label AddSeveritySummaryLabelTo(VisualElement parent, string typeLabel) => parent.AddGameLabel($"{typeLabel}: 0");

    private void UpdateTypeSummary(ReadOnlySpan<FireInGameLogEntry> entries) {
      var eventsCount = 0;
      var warningsCount = 0;
      var errorsCount = 0;
      var assertsCount = 0;
      var exceptionsCount = 0;

      for (var i = 0; i < entries.Length; i++) {
        var logType = entries[i].LogType;
        switch (logType) {
          case LogType.Warning:
            warningsCount++;
            break;
          case LogType.Error:
            errorsCount++;
            break;
          case LogType.Assert:
            assertsCount++;
            break;
          case LogType.Exception:
            exceptionsCount++;
            break;
          default:
            eventsCount++;
            break;
        }
      }

      if (_eventsSummaryLabel != null) {
        _eventsSummaryLabel.text = $"EVENT: {eventsCount}";
      }

      if (_warningsSummaryLabel != null) {
        _warningsSummaryLabel.text = $"WARN: {warningsCount}";
      }

      if (_errorsSummaryLabel != null) {
        _errorsSummaryLabel.text = $"ERROR: {errorsCount}";
      }

      if (_assertsSummaryLabel != null) {
        _assertsSummaryLabel.text = $"ASSERT: {assertsCount}";
      }

      if (_exceptionsSummaryLabel != null) {
        _exceptionsSummaryLabel.text = $"EXCEPTION: {exceptionsCount}";
      }
    }

    private Button AddFilterButtonTo(VisualElement parent, string text, FireLogFilter filter) {
      var button = AddGameButtonTo(parent, text, () => {
        _filter = filter;
        UpdateFilterButtonStyles();
        RefreshLogPanel(force: true);
      });
      return button;
    }

    private void UpdateFilterButtonStyles() {
      ApplyFilterStyle(_allFilterButton, _filter == FireLogFilter.All);
      ApplyFilterStyle(_eventsFilterButton, _filter == FireLogFilter.Events);
      ApplyFilterStyle(_warningsFilterButton, _filter == FireLogFilter.Warnings);
      ApplyFilterStyle(_errorsFilterButton, _filter == FireLogFilter.Errors);
    }

    private void ApplyFilterStyle(Button button, bool selected) {
      if (button == null) {
        return;
      }

      button.SetEnabled(true);
      button.text = selected ? $"✓ {button.text.TrimStart('✓', ' ')}" : button.text.TrimStart('✓', ' ');
    }

    private void RefreshLogPanel(bool force) {
      if (!IsOpen || _logLinesContainer == null) {
        return;
      }

      var entries = FireTelemetry.GetRecentInGameLogEntries();
      UpdateTypeSummary(entries);
      var entrySignature = CreateEntrySignature(entries);
      if (!force && entrySignature == _lastRenderedEntrySignature) {
        return;
      }

      _lastRenderedEntrySignature = entrySignature;
      _logLinesContainer.Clear();

      var filteredCount = 0;
      for (var i = 0; i < entries.Length; i++) {
        var entry = entries[i];
        if (!ShouldIncludeEntry(entry)) {
          continue;
        }

        filteredCount++;
        _logLinesContainer.Add(CreatePanelLogEntryRow(entry));
      }

      if (filteredCount == 0) {
        AddEmptyLogLabelTo(_logLinesContainer, "No log entries for current filter.");
      }

      if (_autoScroll && _logScrollView != null) {
        _logScrollView.scrollOffset = new Vector2(_logScrollView.scrollOffset.x, float.MaxValue);
      }
    }

    private bool ShouldIncludeEntry(FireInGameLogEntry entry) {
      var includesBySeverity = _filter switch {
        FireLogFilter.All => true,
        FireLogFilter.Events => entry.LogType == LogType.Log,
        FireLogFilter.Warnings => entry.LogType == LogType.Warning,
        FireLogFilter.Errors => entry.LogType == LogType.Error
                                || entry.LogType == LogType.Assert
                                || entry.LogType == LogType.Exception,
        _ => true,
      };

      if (!includesBySeverity) {
        return false;
      }

      if (string.IsNullOrWhiteSpace(_searchText)) {
        return true;
      }

      var needle = _searchText.Trim();
      return entry.Message.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private VisualElement CreatePanelLogEntryRow(FireInGameLogEntry entry) {
      var row = new VisualElement();
      row.SetAsRow();

      var viewButton = AddGameButtonTo(row, "View", () => ViewPanelLogEntry(entry));
      viewButton.tooltip = "View selected log entity.";
      viewButton.SetEnabled(PrometheusFireDebugFragment.TryExtractEntityId(entry.Message, out _));

      var label = AddPanelLogEntryLabelTo(row, entry);

      return row;
    }

    private static Label AddPanelLogEntryLabelTo(VisualElement parent, FireInGameLogEntry entry) {
      var severityToken = entry.LogType switch {
        LogType.Warning => "▲ WARN",
        LogType.Error => "✖ ERROR",
        LogType.Assert => "◆ ASSERT",
        LogType.Exception => "✸ EXCEPTION",
        _ => "● EVENT",
      };

      var label = parent.AddGameLabel($"[{entry.Timestamp}] [{severityToken}] {entry.Message}");

      return label;
    }

    private static Label AddEmptyLogLabelTo(VisualElement parent, string message) {
      var label = parent.AddGameLabel(message);
      return label;
    }

    private void ViewPanelLogEntry(FireInGameLogEntry entry) {
      if (!PrometheusFireDebugFragment.TryExtractEntityId(entry.Message, out var entityId)) {
        SetAdminFeedback("No entity id in this log line.");
        return;
      }

      if (TrySelectAndFocusEntity(entityId, out var entityName)
          || PrometheusFireDebugFragment.TryFocusCameraOnEntity(entityId, out entityName)) {
        SetAdminFeedback($"Viewing {entityName} (id={entityId})");
      } else {
        SetAdminFeedback($"Entity id {entityId} not found");
      }
    }

    private bool TrySelectAndFocusEntity(int entityId, out string entityName) {
      entityName = string.Empty;
      if (!TryFindLoadedGameObject(entityId, out var gameObject)) {
        return false;
      }

      if (TimberbornComponentCacheLookup.TryGetPrometheusFireComponent<FireExposureController>(
            gameObject,
            out var fireExposureController,
            out var fromComponentCache)) {
        _entitySelectionService.SelectAndFocusOn(fireExposureController);
        TimberbornCompatibility.RecordProbe(TimberbornCompatibilityArea.Focus, true, "EntitySelectionService.SelectAndFocusOn");
        entityName = gameObject.name;
        var method = fromComponentCache ? "selection_service_cached" : "selection_service_component";
        FireTelemetry.Log($"event={FireTelemetryEvents.DebugViewFocus} entity={entityName} id={entityId} method={method}");
        return true;
      }

      return false;
    }

    private static string CreateEntrySignature(ReadOnlySpan<FireInGameLogEntry> entries) {
      if (entries.Length == 0) {
        return "0";
      }

      var lastEntry = entries[^1];
      return $"{entries.Length}|{lastEntry.Timestamp}|{lastEntry.LogType}|{lastEntry.Message}";
    }

  }
}
