using AES.Tools.Core;
using UnityEngine;


namespace AES.Tools.StartKit
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