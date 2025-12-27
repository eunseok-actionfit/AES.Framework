using UnityEngine;

public abstract class LoadingOverlayUIBase : MonoBehaviour, ILoadingOverlayUI
{
    [SerializeField] private int priority = 10; // AppOpen UI는 100 추천

    protected virtual void OnEnable() => LoadingUIRegistry.Register(this, priority);
    protected virtual void OnDisable()
    {
        Debug.Log($"[Overlay] OnDisable: {name} (scene={gameObject.scene.name})");
        LoadingUIRegistry.Unregister(this);
    }

    public abstract void SetProgress(float realtime01, float smoothed01);
    public abstract void SetMessage(string message);
}