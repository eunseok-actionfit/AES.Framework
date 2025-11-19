using System.Threading;
using Core.Systems.UI.Core.UIView;
using Core.Systems.UI.Registry;
using Cysharp.Threading.Tasks;
using UnityEngine;


public interface IUIFactory
{
    UniTask<UIView> CreateAsync(UIRegistryEntry entry, Transform parent, CancellationToken ct = default);
    void Destroy(UIRegistryEntry entry, UIView view);
}