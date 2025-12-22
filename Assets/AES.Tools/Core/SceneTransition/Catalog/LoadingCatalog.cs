using System;
using AYellowpaper.SerializedCollections;
using UnityEngine;

#if EFLATUN_SCENE_REFERENCE
using Eflatun.SceneReference;
#endif

//[CreateAssetMenu(menuName = "Game/SceneTransition/Loading Catalog", fileName = "LoadingCatalog")]
[Serializable]
public sealed class LoadingCatalog
{
#if EFLATUN_SCENE_REFERENCE
    [SerializeField] private SerializedDictionary<string, SceneReference> loadings;
#else
    [SerializeField] private SerializedDictionary<string, string> loadings;
#endif

    public bool TryResolve(string key, out LoadingScreenKey loadingKey)
    {
        if (string.IsNullOrEmpty(key))
        {
            loadingKey = default;
            return false;
        }

#if EFLATUN_SCENE_REFERENCE
        if (loadings != null && loadings.TryGetValue(key, out var sr))
        {
            if (sr.UnsafeReason == SceneReferenceUnsafeReason.Empty)
            {
                loadingKey = default;
                return false;
            }

            loadingKey = SceneKeyResolver.ResolveLoading(sr);
            return true;
        }
#else
        if (loadings != null && loadings.TryGetValue(key, out var unityName) && !string.IsNullOrEmpty(unityName))
        {
            loadingKey = new LoadingScreenKey(unityName, null, false);
            return true;
        }
#endif

        loadingKey = default;
        return false;
    }
}