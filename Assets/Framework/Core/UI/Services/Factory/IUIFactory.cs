using System.Threading;
using AES.Tools.Core;
using AES.Tools.Registry;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Factory
{
    public interface IUIFactory
    {
        UniTask<UIView> CreateAsync(UIRegistryEntry entry, Transform parent, CancellationToken ct = default);
        void Destroy(UIRegistryEntry entry, UIView view);
    }
}