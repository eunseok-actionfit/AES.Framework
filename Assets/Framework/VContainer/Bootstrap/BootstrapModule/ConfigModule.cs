using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;


namespace AES.Tools.VContainer
{
    [CreateAssetMenu(menuName = "Game/Bootstrap Modules/Config Module", fileName = "ConfigModule")]
    public sealed class ConfigModule : BootstrapModule
    {
        [Header("설정/밸런스 데이터 ScriptableObjects")]
        // [SerializeField] GameConfig gameConfig;
        // [SerializeField] BalanceTable balanceTable;

        [Header("추가 JSON 설정 파일 (StreamingAssets 등)")]
        [SerializeField] bool loadExtraJsonConfig = false;
        [SerializeField] string extraJsonFileName = "config.json";

        public override UniTask Initialize(LifetimeScope rootScope)
        {
            Debug.Log("[ConfigModule] Initialize");

            // // DI 컨테이너에 등록된 ConfigService에 ScriptableObjects 전달
            // if (rootScope != null)
            // {
            //     var container = rootScope.Container;
            //     if (container.CanResolve<IConfigService>())
            //     {
            //         var configService = container.Resolve<IConfigService>();
            //         configService.SetGameConfig(gameConfig);
            //         configService.SetBalanceTable(balanceTable);
            //     }
            // }
            //
            // if (loadExtraJsonConfig)
            // {
            //     LoadExtraJsonConfig();
            // }
            return UniTask.CompletedTask;
        }

        void LoadExtraJsonConfig()
        {
            // 예시: StreamingAssets 또는 PersistentDataPath 등에서 JSON 읽기
            // var path = Path.Combine(Application.streamingAssetsPath, extraJsonFileName);
            // if (File.Exists(path))
            // {
            //     var json = File.ReadAllText(path);
            //     var extraConfig = JsonUtility.FromJson<ExtraConfig>(json);
            //     ...
            // }

            Debug.Log($"[ConfigModule] Extra JSON config load: {extraJsonFileName}");
        }
    }
}