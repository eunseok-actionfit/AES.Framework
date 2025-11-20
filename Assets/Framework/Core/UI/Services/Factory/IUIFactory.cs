using System.Threading;
using AES.Tools.Core.View;
using AES.Tools.Services.Registry;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Services.Factory
{
    public interface IUIFactory
    {
        UniTask<UIView> CreateAsync(UIRegistryEntry entry, Transform parent, CancellationToken ct = default);
        void Destroy(UIRegistryEntry entry, UIView view);
    }
}