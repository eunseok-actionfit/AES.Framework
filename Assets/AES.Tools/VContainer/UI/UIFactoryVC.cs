using System;
using System.Threading;
using AES.Tools.View;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace AES.Tools
{
    public class UIFactoryVC : IUIFactory
    {
        readonly IObjectResolver _resolver;

        public UIFactoryVC(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public async UniTask<UIView> CreateAsync(
            UIRegistryEntry entry,
            Transform parent,
            CancellationToken ct = default)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            bool useAddr   = entry.IsAddressable;
            bool usePrefab = entry.Prefab != null;

            if (!useAddr && !usePrefab)
                throw new InvalidOperationException(
                    $"No valid UI source for {entry.Kind}");

            GameObject go = null;

            try
            {
                if (useAddr)
                {
                    // Addressables instantiate
                    var handle = Addressables.InstantiateAsync(entry.AddressGuid, parent);
                    go = await handle.Task.AsUniTask().AttachExternalCancellation(ct);

                    // DI 주입
                    _resolver.InjectGameObject(go);
                }
                else
                {
                    ct.ThrowIfCancellationRequested();

                    
                    go = _resolver.Instantiate(entry.Prefab, parent);
                    
                }

                var view = go.GetComponent<UIView>();
                if (!view)
                {
                    throw new MissingComponentException(
                        $"UIView missing. source=" +
                        (useAddr ? entry.AddressGuid : entry.Prefab.name));
                }

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
                Addressables.ReleaseInstance(view.gameObject);
            else
                Object.Destroy(view.gameObject);
        }
    }
}
