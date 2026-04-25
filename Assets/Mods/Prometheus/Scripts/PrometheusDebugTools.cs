using Timberborn.Localization;
using Timberborn.ToolSystem;
using Timberborn.ToolSystemUI;

namespace Mods.Prometheus.Scripts {
  internal abstract class PrometheusDebugTool : ITool, IToolDescriptor {

    private readonly PrometheusDebugPanel _prometheusDebugPanel;
    private readonly ToolService _toolService;
    private readonly PrometheusDebugPanelTab _view;
    private readonly ILoc _loc;
    private readonly string _titleLocKey;
    private readonly string _descriptionLocKey;

    protected PrometheusDebugTool(
      PrometheusDebugPanel prometheusDebugPanel,
      ToolService toolService,
      ILoc loc,
      PrometheusDebugPanelTab view,
      string titleLocKey,
      string descriptionLocKey) {
      _prometheusDebugPanel = prometheusDebugPanel;
      _toolService = toolService;
      _loc = loc;
      _view = view;
      _titleLocKey = titleLocKey;
      _descriptionLocKey = descriptionLocKey;
    }

    public void Enter() {
      _prometheusDebugPanel.Open(_view);
      _toolService.SwitchToDefaultTool();
      _prometheusDebugPanel.RestoreSelectionIfToolSwitchClearedIt();
    }

    public void Exit() {
    }

    public ToolDescription DescribeTool() {
      return new ToolDescription.Builder(_loc.T(_titleLocKey))
        .AddSection(_loc.T(_descriptionLocKey))
        .Build();
    }

  }

  internal class PrometheusDebugActionsTool : PrometheusDebugTool {

    public PrometheusDebugActionsTool(PrometheusDebugPanel prometheusDebugPanel, ToolService toolService, ILoc loc)
      : base(prometheusDebugPanel, toolService, loc, PrometheusDebugPanelTab.Actions, "Tools.PrometheusDebugActions", "Tools.PrometheusDebugActions.Description") {
    }

  }

  internal class PrometheusDebugVisualsTool : PrometheusDebugTool {

    public PrometheusDebugVisualsTool(PrometheusDebugPanel prometheusDebugPanel, ToolService toolService, ILoc loc)
      : base(prometheusDebugPanel, toolService, loc, PrometheusDebugPanelTab.Visuals, "Tools.PrometheusDebugVisuals", "Tools.PrometheusDebugVisuals.Description") {
    }

  }

  internal class PrometheusDebugSelectionTool : PrometheusDebugTool {

    public PrometheusDebugSelectionTool(PrometheusDebugPanel prometheusDebugPanel, ToolService toolService, ILoc loc)
      : base(prometheusDebugPanel, toolService, loc, PrometheusDebugPanelTab.Selection, "Tools.PrometheusDebugSelection", "Tools.PrometheusDebugSelection.Description") {
    }

  }

  internal class PrometheusDebugLogTool : PrometheusDebugTool {

    public PrometheusDebugLogTool(PrometheusDebugPanel prometheusDebugPanel, ToolService toolService, ILoc loc)
      : base(prometheusDebugPanel, toolService, loc, PrometheusDebugPanelTab.Log, "Tools.PrometheusDebugLog", "Tools.PrometheusDebugLog.Description") {
    }

  }
}
