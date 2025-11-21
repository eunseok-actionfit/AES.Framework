using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;


namespace AES.Tools.VContainer
{
    public sealed class LoadingService : ILoadingService
    {
        private readonly ISceneLoader _scenes;
        private readonly IAssetProvider _assets;
        private readonly ILoadingBus _bus;

        public LoadingService(ISceneLoader scenes, IAssetProvider assets, ILoadingBus bus)
        {
            _scenes = scenes;
            _assets = assets;
            _bus = bus;
        }

        // 호환: 진행률 없는 기존 흐름
        public async UniTask RunWithLoadingAsync(Func<UniTask> loadFlow, string overlayAddressKey, float minShowSeconds = 0.35f)
            => await RunWithLoadingAsync(async _ => await loadFlow(), overlayAddressKey, minShowSeconds, null);

        // 신규: 진행률·메시지 지원
        public async UniTask RunWithLoadingAsync(
            Func<IProgress<float>, UniTask> loadFlow,
            string overlayAddressKey,
            float minShowSeconds = 0.35f,
            IProgress<string> message = null)
        {
            if (loadFlow == null) return;

            var started = Time.realtimeSinceStartup;

            // 버스로 브리지
            var progress = new Progress<float>(_bus.Report);
            var msg = message ?? new Progress<string>(_bus.Say);

            var go = await _assets.LoadAsync<GameObject>(overlayAddressKey);
            Object.Instantiate(go);
            UnityEngine.Object.DontDestroyOnLoad(go);

            Exception caught = null;
            try
            {
                _bus.Report(0f);
                await loadFlow(progress);
                _bus.Report(1f);

                var remain = minShowSeconds - (Time.realtimeSinceStartup - started);
                if (remain > 0f) await UniTask.Delay(TimeSpan.FromSeconds(remain));
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            finally
            {
                try
                {
                    if (go != null) _assets.Release(go);
                }
                finally
                {
                    _bus.Reset(); // 씬 앤드 시 구독 정리
                }
            }

            if (caught != null) throw caught;
        }
    }
}
