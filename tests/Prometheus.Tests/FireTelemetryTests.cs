using System;
using System.Collections.Generic;
using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class FireTelemetryTests
    {

        [Fact]
        public void FireTelemetryEvents_AreCentralizedAndUnique_Test()
        {
            TestSupport.True(FireTelemetryEvents.All.Length >= 20);
            TestSupport.Equal(FireTelemetryEvents.All.Length, new HashSet<string>(FireTelemetryEvents.All).Count);
            TestSupport.True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.DebugResetFireExposure) >= 0);
            TestSupport.True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.RuntimeResetRegistryStarted) >= 0);
            TestSupport.True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.RuntimeResetRegistryCompleted) >= 0);
            TestSupport.True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.RuntimeResetHookFailed) >= 0);
            TestSupport.True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.WorkplaceIndoorExposure) >= 0);
            TestSupport.True(Array.IndexOf(FireTelemetryEvents.All, FireTelemetryEvents.GridIgnitionSeeded) >= 0);
        }

    }
}
