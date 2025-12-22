using UnityEngine;

public sealed class AppOpenLoadingOverlaySpawner
{
    private readonly GameObject _prefab;
    private GameObject _instance;

    public AppOpenLoadingOverlaySpawner(GameObject prefab) => _prefab = prefab;

    public void Show()
    {
        if (_prefab == null) return;
        if (_instance != null) return;

        _instance = Object.Instantiate(_prefab);
        Object.DontDestroyOnLoad(_instance);
    }

    public void Hide()
    {
        if (_instance == null) return;
        Object.Destroy(_instance);
        _instance = null;
    }
}