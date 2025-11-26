using System.Threading;
using AES.Tools.Models;
using AES.Tools.Root;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer
{
    public sealed class SceneFlowService : ISceneFlow
    {
        readonly ISceneLoader _scenes;
        readonly ILoadingService _loading;
        readonly LifetimeScope _root;
        // readonly IUIController _ui; 

        public SceneFlowService(ISceneLoader scenes, ILoadingService loading, LifetimeScope root)
        {
            _scenes = scenes;
            _loading = loading;
            _root = root;
            //   _ui = ui;
        }

        public async UniTask LoadWithArgsAsync<T>(
            string sceneName,
            string channel,
            T payload,
            bool additive = false,
            string overlayAddressKey = null,
            bool allowActivation = true,
            CancellationToken ct = default) where T : class
        {
            //SceneArgsBus.Set(channel, payload);

            using (LifetimeScope.EnqueueParent(_root))
            using (LifetimeScope.Enqueue(b => {
                    if (payload != null) b.RegisterInstance(payload).As<T>(); // DI로도 주입
                }))
            {
                // if (!additive)
                //     await _ui.CloseAllAsync(UIRootRole.Local, ct); 
                
                var opts = new SceneLoadOptions(
                    mode: additive ? SceneLoadMode.Additive : SceneLoadMode.Single,
                    setActive: !additive,
                    allowSceneActivation: allowActivation,
                    onProgress: null,
                    externalToken: ct
                );

                if (!string.IsNullOrEmpty(overlayAddressKey))
                {
                    await _loading.RunWithLoadingAsync(async p => {
                        opts = new SceneLoadOptions(
                            mode: additive ? SceneLoadMode.Additive : SceneLoadMode.Single,
                            setActive: !additive,
                            allowSceneActivation: allowActivation,
                            onProgress: p.Report,
                            externalToken: ct
                        );

                        await _scenes.Load(new SceneRef(addressKey: sceneName), opts);
                    }, overlayAddressKey);
                }
                else { await _scenes.Load(new SceneRef(addressKey: sceneName), opts); }
            }
        }

        public UniTask GoHome(string overlayKey = null, CancellationToken ct = default) =>
            LoadWithArgsAsync<object>("Scene/Home", "Home.Load", null, false, overlayKey, true, ct);

        public UniTask GoGame(ISceneArgs args, string overlayKey = null, CancellationToken ct = default) =>
            LoadWithArgsAsync("Scene/InGame", "Game.Load", args, false, overlayKey, true, ct);
    }
}