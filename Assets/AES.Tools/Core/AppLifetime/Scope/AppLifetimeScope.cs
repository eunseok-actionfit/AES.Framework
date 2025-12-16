using AES.Tools.VContainer.Bootstrap.Framework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AES.Tools.VContainer.Scope
{
    public sealed class AppLifetimeScope : LifetimeScope
    {
        [SerializeField] private BootstrapGraph graph;
        [SerializeField] private string profile = "Dev";

        protected override void Configure(IContainerBuilder builder)
        {
            BootstrapRunner.InstallAll(
                graph, profile,
                builder,
                Application.platform,
#if UNITY_EDITOR
                true
#else
                false
#endif
            );
        }
    }
}