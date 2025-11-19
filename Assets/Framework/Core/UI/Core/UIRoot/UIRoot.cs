using UnityEngine;
using Core.Systems.UI;              // UiServices
// using VContainer;                // 제거

namespace Core.Systems.UI.Core.UIRoot
{
    public sealed class UIRoot : MonoBehaviour
    {
        [Header("Layers (top = last draws on top)")]
        public UILayer.UILayer WindowLayer;
        public UILayer.UILayer HudLayer;
        public UILayer.UILayer PopupLayer;
        public UILayer.UILayer OverlayLayer;

        [SerializeField, HideInInspector]
        private UIRootRole role = UIRootRole.Local;

        public UIRootRole Role => role;
        
        public void SetRole(UIRootRole newRole)
        {
            role = newRole;
        }
        
        private void OnEnable()
        {
            UiServices.UIRootProvider?.Register(this);
        }

        private void OnDisable()
        {
            UiServices.UIRootProvider?.Unregister(this);
        }
    }
}