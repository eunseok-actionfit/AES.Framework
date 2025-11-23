using System;
using AES.Tools.VContainer.Scope;
using UnityEngine;
using VContainer.Unity;


namespace AES.Tools.VContainer
{
    //  Namespace Properties ------------------------------
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class AppBootstrapper : PersistentSingleton<AppBootstrapper>
    {
        [SerializeField] private AppLifetimeScope appLifetimeScopePrefab;

        private LifetimeScope _rootScope;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private async static void Init()
        {
            try {
                // _rootScope = Instantiate(appLifetimeScopePrefab);
                //
                // await BootstrapAsync(); // 내부에서 Lifetime.InitializeAsync(), Adapter.Construct() 등
                //
                // await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                //
                //
                // DontDestroyOnLoad(gameObject);
                // DontDestroyOnLoad(_rootScope.gameObject);
                //
                // Debug.Log("[App] Bootstrap done + moved to DDOL");
            }
            catch (Exception e) { Debug.LogError($"[App] Bootstrap failed: {e}"); }
        }

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;   // VSync 끄기
            UnityEngine.Application.targetFrameRate = 60; // 60프레임 타깃
        }

        // AppBootstrapper.cs
        private async void Start()
        {

        }

        private async static void BootstrapAsync()
        {
            // var lifetime = _rootScope.Container.Resolve<ApplicationLifetime>();
            // await lifetime.InitializeAsync();
            //
            // var save = _rootScope.Container.Resolve<ISaveCoordinator>();
            // await save.LoadAllAsync();
            //
            // var adapter = GetComponent<ApplicationLifetimeAdapter>();
            // adapter.Construct(lifetime);
            
            
            // var sceneFlow = _rootScope.Container.Resolve<ISceneFlow>();
            // UniTask.Create(async () => { await sceneFlow.GoHome(/*overlayKey: AssetKeys.UI_LoadingBoot*/); }).Forget();
            //
            Debug.Log("[App] Bootstrap done");
        }

    }
}