using System.Threading;
using AES.Tools.UI.Core.Registry;
using AES.Tools.UI.Core.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.UI.Core.Factory
{
    public interface IUIFactory
    {
        UniTask<UIView> CreateAsync(UIRegistryEntry entry, Transform parent, CancellationToken ct = default);
        void Destroy(UIRegistryEntry entry, UIView view);
    }
}