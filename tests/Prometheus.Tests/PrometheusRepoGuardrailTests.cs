using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class PrometheusRepoGuardrailTests
    {

        private static readonly string RepositoryRoot = FindRepositoryRoot();
        private static readonly string ModRoot = Path.Combine(RepositoryRoot, "Assets", "Mods", "Prometheus");
        private static readonly string ScriptsRoot = Path.Combine(ModRoot, "Scripts");
        private static readonly string TestProjectPath = Path.Combine(RepositoryRoot, "tests", "Prometheus.Tests", "Prometheus.Tests.csproj");

        private static readonly string[] ExpectedTestCompileItems =
        {
            "Core/EntitySnapshotStore.cs",
            "Core/FireResetRegistry.cs",
            "Core/TimberbornCompatibility.cs",
            "Core/TimberbornOperationStateAdapter.cs",
            "Core/TickGate.cs",
            "Fire/Damage/FireDamageStateRuntimeState.cs",
            "Fire/Exposure/FireConfiguredSourceInjector.cs",
            "Fire/Exposure/FireExposureRuntimeState.cs",
            "Fire/Grid/FireGridChunk.cs",
            "Fire/Grid/FireGridEnvironmentSampler.cs",
            "Fire/Grid/FireGridFootprintSampler.cs",
            "Fire/Grid/FireGridKernel.cs",
            "Fire/Grid/FireGridPropagationPolicy.cs",
            "Fire/Grid/FireGridPropagationRules.cs",
            "Fire/Grid/FireGridRuntimeState.cs",
            "Fire/Grid/FireGridSimulationCoordinator.cs",
            "Fire/Grid/FireGridValues.cs",
            "Fire/Impact/FireImpactRuntimeState.cs",
            "Fire/Projection/FireRuntimeProjectionRuntimeState.cs",
            "Fire/Recovery/FertileAshRecoveredGoodStackSpawner.cs",
            "Fire/Recovery/FireAftermathEligibilityPolicy.cs",
            "Fire/Recovery/FireFieldAmendmentGrowthRules.cs",
            "Fire/Recovery/FireFieldAmendmentRuntimeState.cs",
            "Fire/Recovery/FireRecoveryRuntimeState.cs",
            "Fire/Visuals/FireNativeParticleSourceCatalog.cs",
            "Fire/Visuals/FireVisualEffectAuthoring.cs",
            "Fire/Visuals/FireVisualEffectPreviewRuntimeState.cs",
            "Fire/Visuals/FireVisualEffectRules.cs",
            "Fire/Workplace/FireWorkplaceRules.cs",
        };

        private static readonly string[] QaTelemetryEvents =
        {
            FireTelemetryEvents.DebugIgnitionQueued,
            FireTelemetryEvents.DebugIgnitionConsumed,
            FireTelemetryEvents.DebugStopAllFires,
            FireTelemetryEvents.DebugStopAllFiresResult,
            FireTelemetryEvents.DebugResetFireExposure,
            FireTelemetryEvents.RuntimeResetRegistryStarted,
            FireTelemetryEvents.RuntimeResetRegistryCompleted,
            FireTelemetryEvents.RuntimeResetHookFailed,
            FireTelemetryEvents.DebugClearBeaverFireEffects,
            FireTelemetryEvents.DebugClearBeaverFireEffectsResult,
            FireTelemetryEvents.DebugViewFocus,
            FireTelemetryEvents.WorkplaceIndoorExposure,
            FireTelemetryEvents.WorkplaceSpeedApiResolved,
            FireTelemetryEvents.WorkplaceSpeedPenaltyState,
            FireTelemetryEvents.WorkplaceSupportDisabled,
            FireTelemetryEvents.WorkplaceSupportRestored,
            FireTelemetryEvents.BuildingOperationsDisabled,
            FireTelemetryEvents.BuildingOperationsRestored,
            FireTelemetryEvents.GridIgnitionSeeded,
            FireTelemetryEvents.GridSourceInjected,
            FireTelemetryEvents.GridSourceSuppressed,
            FireTelemetryEvents.FertileAshSpawnQueued,
            FireTelemetryEvents.FertileAshSpawnSkipped,
            FireTelemetryEvents.FertileAshSpawnFailed,
            FireTelemetryEvents.FertileAshRecoveredGoodStackQueued,
            FireTelemetryEvents.FertileAshRecoveredGoodStackFailed,
            FireTelemetryEvents.FertileAshResetState,
            FireTelemetryEvents.VisualPreviewApply,
            FireTelemetryEvents.VisualPreviewClear,
            FireTelemetryEvents.VisualTuningJson,
            FireTelemetryEvents.NativeVisualEffectResolved,
            FireTelemetryEvents.NativeVisualEffectUnavailable,
            FireTelemetryEvents.TimberbornCompatibilitySummary,
            FireTelemetryEvents.TimberbornCompatibilityProbe,
        };

        [Fact]
        public void InternalMarkdown_DoesNotShipInModPayload_Test()
        {
            var markdownFiles = Directory.EnumerateFiles(ModRoot, "*.md", SearchOption.AllDirectories)
              .Select(ToRepositoryRelativePath)
              .OrderBy(path => path, StringComparer.Ordinal)
              .ToArray();

            TestSupport.Equal(string.Empty, string.Join(Environment.NewLine, markdownFiles));
        }

        [Fact]
        public void TestProjectCompileItems_StayInSyncWithDependencyLightSources_Test()
        {
            var compileItems = ReadPrometheusCompileItems();

            TestSupport.Equal(ExpectedTestCompileItems.Length, compileItems.Length);
            TestSupport.Equal(ExpectedTestCompileItems.Length, new HashSet<string>(compileItems, StringComparer.Ordinal).Count);

            var expected = ExpectedTestCompileItems.OrderBy(path => path, StringComparer.Ordinal).ToArray();
            var actual = compileItems.OrderBy(path => path, StringComparer.Ordinal).ToArray();

            TestSupport.Equal(string.Join(Environment.NewLine, expected), string.Join(Environment.NewLine, actual));

            var missing = compileItems
              .Where(relativePath => !File.Exists(Path.Combine(ScriptsRoot, relativePath)))
              .OrderBy(path => path, StringComparer.Ordinal)
              .ToArray();

            TestSupport.Equal(string.Empty, string.Join(Environment.NewLine, missing));
        }

        [Fact]
        public void QaTelemetryEvents_AreStableAndRegistered_Test()
        {
            var expectedTokens = new[]
            {
                "debug_ignition_queued",
                "debug_ignition_consumed",
                "debug_stop_all_fires",
                "debug_stop_all_fires_result",
                "debug_reset_fire_exposure",
                "runtime_reset_registry_started",
                "runtime_reset_registry_completed",
                "runtime_reset_hook_failed",
                "debug_clear_beaver_fire_effects",
                "debug_clear_beaver_fire_effects_result",
                "debug_view_focus",
                "workplace_indoor_exposure",
                "workplace_speed_api_resolved",
                "workplace_speed_penalty_state",
                "workplace_support_disabled",
                "workplace_support_restored",
                "building_operations_disabled",
                "building_operations_restored",
                "grid_ignition_seeded",
                "grid_source_injected",
                "grid_source_suppressed",
                "fertile_ash_spawn_queued",
                "fertile_ash_spawn_skipped",
                "fertile_ash_spawn_failed",
                "fertile_ash_recovered_good_stack_queued",
                "fertile_ash_recovered_good_stack_failed",
                "fertile_ash_reset_state",
                "visual_preview_apply",
                "visual_preview_clear",
                "visual_tuning_json",
                "native_visual_effect_resolved",
                "native_visual_effect_unavailable",
                "timberborn_compatibility_summary",
                "timberborn_compatibility_probe",
            };

            TestSupport.Equal(string.Join(Environment.NewLine, expectedTokens), string.Join(Environment.NewLine, QaTelemetryEvents));

            var registeredEvents = new HashSet<string>(FireTelemetryEvents.All, StringComparer.Ordinal);
            var missing = QaTelemetryEvents
              .Where(eventName => !registeredEvents.Contains(eventName))
              .OrderBy(eventName => eventName, StringComparer.Ordinal)
              .ToArray();

            TestSupport.Equal(string.Empty, string.Join(Environment.NewLine, missing));
        }

        private static string[] ReadPrometheusCompileItems()
        {
            var projectDirectory = Path.GetDirectoryName(TestProjectPath) ?? RepositoryRoot;
            var project = XDocument.Load(TestProjectPath);

            return project
              .Descendants("Compile")
              .Select(element => element.Attribute("Include")?.Value)
              .Where(include => !string.IsNullOrWhiteSpace(include))
              .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include)))
              .Where(fullPath => fullPath.StartsWith(ScriptsRoot, StringComparison.Ordinal))
              .Select(ToScriptsRelativePath)
              .ToArray();
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))
                  && Directory.Exists(Path.Combine(directory.FullName, "Assets", "Mods", "Prometheus")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Unable to locate repository root.");
        }

        private static string ToRepositoryRelativePath(string fullPath) =>
          Path.GetRelativePath(RepositoryRoot, fullPath).Replace(Path.DirectorySeparatorChar, '/');

        private static string ToScriptsRelativePath(string fullPath) =>
          Path.GetRelativePath(ScriptsRoot, fullPath).Replace(Path.DirectorySeparatorChar, '/');

    }
}
