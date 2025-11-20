using AES.Tools.Core;
using AES.Tools.Services.Registry;
using UnityEngine;


namespace AES.Tools.Config
{
    [CreateAssetMenu(menuName = "App/AppConfig")]
    public class AppConfig : ScriptableObject
    {
        [Header("시스템 설정")]
        public InputConfig inputConfig;

        [Header("UI Registry")]
        public UIRegistrySO uiRegistrySO;
    }
}