using UnityEngine;


namespace Core.Systems.UI.Components.Utils
{
    [RequireComponent(typeof(Canvas))]
    public sealed class CanvasCameraBinder : MonoBehaviour
    {
        [SerializeField] bool bindOnEnable = true;

        void OnEnable()
        {
            if (!bindOnEnable) return;
            var canvas = GetComponent<Canvas>();
            if (canvas.renderMode != RenderMode.ScreenSpaceCamera) return;
            var cam = Camera.main;
            if (cam) canvas.worldCamera = cam;
        }
    }
}