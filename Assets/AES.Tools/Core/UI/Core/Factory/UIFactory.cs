using System;
using System.Threading;
using AES.Tools.View;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace AES.Tools
{
    public class UIFactory : IUIFactory
    {
        public async UniTask<UIView> CreateAsync(UIRegistryEntry entry, Transform parent, CancellationToken ct = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var useAddr   = entry.IsAddressable;
            var usePrefab = entry.Prefab != null;
            if (!useAddr && !usePrefab)
                throw new InvalidOperationException($"No valid UI source for {entry}");

            GameObject go = null;

            try
            {
                if (useAddr)
                {
                    var handle = Addressables.InstantiateAsync(entry.AddressGuid, parent);
                    go = await handle.Task.AsUniTask().AttachExternalCancellation(ct);
                }
                else
                {
                    ct.ThrowIfCancellationRequested();
                    go = Object.Instantiate(entry.Prefab, parent, false);
                }

                var view = go.GetComponent<UIView>();
                if (!view)
                    throw new MissingComponentException(
                        $"UIView missing. source={(useAddr ? entry.AddressGuid : entry.Prefab.name)}");

                return view;
            }
            catch
            {
                if (go != null)
                {
                    if (useAddr)
                        Addressables.ReleaseInstance(go);
                    else
                        Object.Destroy(go);
                }
                throw;
            }
        }

        public void Destroy(UIRegistryEntry entry, UIView view)
        {
            if (!view) return;

            if (entry.IsAddressable)
            {
                Addressables.ReleaseInstance(view.gameObject);
            }
            else
            {
                Object.Destroy(view.gameObject);
            }
        }
    }
}
