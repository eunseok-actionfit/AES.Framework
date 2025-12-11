using System;
using UnityEngine;


namespace AES.Tools.VContainer.Services.Loading
{
    /// <summary>기존 static 사용처를 위한 얇은 어댑터.</summary>
    public static class LoadingBusFacade
    {
        // DI가 주입함
        public static ILoadingBus Instance { get; set; }

        public static event Action<float> OnProgress
        {
            add => Instance.Progress += value;
            remove => Instance.Progress -= value;
        }

        public static event Action<string> OnMessage
        {
            add => Instance.Message += value;
            remove => Instance.Message -= value;
        }

        public static void Report(float v) => Instance.Report(v);
        public static void Say(string msg) => Instance.Say(msg);
        public static void Reset() => Instance.Reset();

        // 도메인 리로드 시 안전 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnDomainReload()
        {
            Instance = null;
        }
    }
}