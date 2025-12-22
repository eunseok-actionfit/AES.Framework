using System;
using AYellowpaper.SerializedCollections;
using UnityEngine;

#if EFLATUN_SCENE_REFERENCE
using Eflatun.SceneReference;
#endif

//[CreateAssetMenu(menuName = "Game/SceneTransition/Scene Catalog", fileName = "SceneCatalog")]

[Serializable]
public sealed class SceneCatalog
{
#if EFLATUN_SCENE_REFERENCE
    [SerializeField] private SerializedDictionary<string, SceneReference> scenes;
#else
    [SerializeField] private SerializedDictionary<string, string> scenes;
#endif

    public bool TryResolve(string key, out ResolvedSceneKey resolved)
    {
        resolved = default;
        if (string.IsNullOrEmpty(key)) return false;

#if EFLATUN_SCENE_REFERENCE
        if (scenes != null && scenes.TryGetValue(key, out var sr))
        {
            if (sr.UnsafeReason == SceneReferenceUnsafeReason.Empty) return false;
            resolved = SceneKeyResolver.Resolve(sr);
            return true;
        }
#else
        if (scenes != null && scenes.TryGetValue(key, out var nameOrPath) && !string.IsNullOrEmpty(nameOrPath))
        {
            resolved = new ResolvedSceneKey(nameOrPath, null, false);
            return true;
        }
#endif
        return false;
    }
}