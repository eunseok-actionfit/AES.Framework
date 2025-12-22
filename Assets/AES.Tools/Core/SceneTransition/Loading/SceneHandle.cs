using UnityEngine.SceneManagement;

public readonly struct SceneHandle
{
    public string Key { get; }
    public Scene Scene { get; }
    public object NativeHandle { get; } // Unity AsyncOperation or Addressables handle

    public SceneHandle(string key, Scene scene, object nativeHandle)
    {
        Key = key;
        Scene = scene;
        NativeHandle = nativeHandle;
    }

    public readonly bool HasValidLoadedScene()
        => Scene.IsValid() && Scene.isLoaded;
}