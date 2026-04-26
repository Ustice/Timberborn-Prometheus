using ConfigurableToolGroups.Services;
using ConfigurableToolGroups.UI;
using Timberborn.ToolSystem;

namespace Mods.Prometheus.Scripts {
  internal class PrometheusDebugToolGroupElement : CustomRootToolGroupElement {

    public const string ToolGroupId = "PrometheusDebug";
    private const string ActionsIcon = "PrometheusActionsIcon";
    private const string VisualsIcon = "PrometheusVisualsIcon";
    private const string SelectionIcon = "PrometheusSelectionIcon";
    private const string QaIcon = "PrometheusQaIcon";
    private const string LogIcon = "PrometheusLogIcon";

    private readonly PrometheusDebugActionsTool _actionsTool;
    private readonly PrometheusDebugVisualsTool _visualsTool;
    private readonly PrometheusDebugSelectionTool _selectionTool;
    private readonly PrometheusDebugQaTool _qaTool;
    private readonly PrometheusDebugLogTool _logTool;

    public override string Id => ToolGroupId;

    public PrometheusDebugToolGroupElement(
      PrometheusDebugActionsTool actionsTool,
      PrometheusDebugVisualsTool visualsTool,
      PrometheusDebugSelectionTool selectionTool,
      PrometheusDebugQaTool qaTool,
      PrometheusDebugLogTool logTool,
      ToolGroupService toolGroupService,
      ModdableToolGroupButtonFactory buttonFactory)
      : base(toolGroupService, buttonFactory) {
      _actionsTool = actionsTool;
      _visualsTool = visualsTool;
      _selectionTool = selectionTool;
      _qaTool = qaTool;
      _logTool = logTool;
      Color = ToolButtonColor.Blue;
    }

    protected override void AddChildren(ModdableToolGroupButton button) {
      button.AddChildTool(_actionsTool, ActionsIcon);
      button.AddChildTool(_visualsTool, VisualsIcon);
      button.AddChildTool(_selectionTool, SelectionIcon);
      button.AddChildTool(_qaTool, QaIcon);
      button.AddChildTool(_logTool, LogIcon);
    }

  }
}
