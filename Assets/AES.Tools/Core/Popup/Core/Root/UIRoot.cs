using AES.Tools.Layer;
using UnityEngine;


namespace AES.Tools.Root
{
    public sealed class UIRoot : MonoBehaviour
    {
        [Header("Layers (top = last draws on top)")]
        public UILayer WindowLayer;
        public UILayer HudLayer;
        public UILayer PopupLayer;
        public UILayer OverlayLayer;

        [SerializeField, HideInInspector]
        private UIRootRole role = UIRootRole.Local;

        public UIRootRole Role => role;
        
        public void SetRole(UIRootRole newRole)
        {
            role = newRole;
        }
        
        private void OnEnable()
        {
            UiServiceLocator.UIRootProvider?.Register(this);
        }

        private void OnDisable()
        {
            UiServiceLocator.UIRootProvider?.Unregister(this);
        }
    }
}