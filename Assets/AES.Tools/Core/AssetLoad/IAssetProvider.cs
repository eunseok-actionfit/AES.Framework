using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools
{
    public interface IAssetProvider
    {
        UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object;
        UniTask<GameObject> InstantiateAsync(string key, CancellationToken ct = default);
        
        void Release(string key);
        void Release(object asset);
        void ReleaseAll();
        bool IsLoaded(string key);

        UniTask<IReadOnlyList<T>> LoadByLabelAsync<T>(
            string label,
            CancellationToken ct = default
        ) where T : Object;
    }
}