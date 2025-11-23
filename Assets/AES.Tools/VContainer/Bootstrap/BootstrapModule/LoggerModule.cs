using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;


namespace AES.Tools.VContainer
{
    [CreateAssetMenu(menuName = "Game/Bootstrap Modules/Logger Module", fileName = "LoggerModule")]
    public sealed class LoggerModule : BootstrapModule
    {
        [Header("최소 로그 레벨")]
        [SerializeField]
        private LogType minimumLogType = LogType.Log;

        [Header("파일 로그 활성화")]
        [SerializeField]
        private bool enableFileLog = false;

        private bool initialized;
        
        public override UniTask Initialize(LifetimeScope rootScope)
        {
            // if (initialized)
            //     return;
            //
            // initialized = true;
            //
            // Debug.Log("[LoggerModule] Initialize");
            //
            // // 예: 글로벌 로그 핸들러
            // Application.logMessageReceived += OnLogMessageReceived;
            //
            // if (enableFileLog)
            // {
            //     // FileLogger.Initialize(...) 같은 식으로 구현
            //     // FileLogger.Initialize(minimumLogType);
            // }
            //
            // // DI 기반 로그 서비스 설정
            // if (rootScope != null)
            // {
            //     var container = rootScope.Container;
            //     if (container.CanResolve<ILogService>())
            //     {
            //         var logService = container.Resolve<ILogService>();
            //         logService.SetMinimumLevel(minimumLogType);
            //     }
            // }
            return UniTask.CompletedTask;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // if (type < minimumLogType)
            //     return;

            // 필요하다면 파일로그 / 원격 로깅 등으로 전달
            // FileLogger.Log(type, condition, stackTrace);
        }
    }
}