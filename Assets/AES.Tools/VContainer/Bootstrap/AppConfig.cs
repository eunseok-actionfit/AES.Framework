using UnityEngine;


namespace AES.Tools.VContainer.Bootstrap
{
    [CreateAssetMenu(menuName = "App/AppConfig (VContainer)")]
    public class AppConfig : ScriptableObject
    {
        // [Header("시스템 설정")]
        // public InputConfig inputConfig;

        //[Header("UI Registry")]
        //public UIRegistrySO uiRegistrySO;

        public StorageProfile storageProfile;
        
        [Header("부트스트랩 모듈 (세이브/로거/SDK 등)")]
        [SerializeField]
        public BootstrapModule[] modules;
    }
}