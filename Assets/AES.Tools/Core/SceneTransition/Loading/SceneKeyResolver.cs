public static class SceneKeyResolver
{
#if EFLATUN_SCENE_REFERENCE
    public static ResolvedSceneKey Resolve(Eflatun.SceneReference.SceneReference sceneRef)
    {
        var unityKey = !string.IsNullOrEmpty(sceneRef.Path) ? sceneRef.Path : sceneRef.Name;

        // Addressables 관련 프로퍼티(Address)는 Addressable 상태에서만 접근해야 함
        if (sceneRef.State == Eflatun.SceneReference.SceneReferenceState.Addressable)
        {
            var addr = sceneRef.Address;
            if (!string.IsNullOrEmpty(addr))
                return new ResolvedSceneKey(unityKey, addr, true);
        }

        if (!string.IsNullOrEmpty(sceneRef.Path))
            return new ResolvedSceneKey(sceneRef.Path, null, false);

        return new ResolvedSceneKey(sceneRef.Name, null, false);
    }

    public static LoadingScreenKey ResolveLoading(Eflatun.SceneReference.SceneReference sceneRef)
    {
        var unityKey = !string.IsNullOrEmpty(sceneRef.Path) ? sceneRef.Path : sceneRef.Name;

        // Addressables 관련 프로퍼티(Address)는 Addressable 상태에서만 접근해야 함
        if (sceneRef.State == Eflatun.SceneReference.SceneReferenceState.Addressable)
        {
            var addr = sceneRef.Address;
            if (!string.IsNullOrEmpty(addr))
                return new LoadingScreenKey(unityKey, addr, true);
        }

        return new LoadingScreenKey(unityKey, null, false);
    }
#else
    public static ResolvedSceneKey Resolve(string keyOrName)
        => new ResolvedSceneKey(keyOrName, null, false);

    public static LoadingScreenKey ResolveLoading(string nameOrPath)
        => new LoadingScreenKey(nameOrPath, null, false);
#endif
}