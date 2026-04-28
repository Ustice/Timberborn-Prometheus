using Mods.Prometheus.Scripts;
using Xunit;

namespace Prometheus.Tests
{
    public sealed class PrometheusWorldLoadStateTests
    {

        [Fact]
        public void Lifecycle_TogglesWorldReadiness_Test()
        {
            var state = new PrometheusWorldLoadState();

            TestSupport.False(state.WorldReady);

            state.Load();
            TestSupport.False(state.WorldReady);

            state.PostLoad();
            TestSupport.True(state.WorldReady);

            state.Unload();
            TestSupport.False(state.WorldReady);
        }

    }
}
