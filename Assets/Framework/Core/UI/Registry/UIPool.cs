using System.Threading;
using Core.Engine.Factory;
using Core.Systems.UI.Core.UIView;
using Core.Systems.UI.Factory;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace Core.Systems.UI.Registry
{
    //  Namespace Properties ------------------------------

    public sealed class UIPoolFactory : IAsyncFactory<UIView>
    {
        private readonly IUIFactory _factory;
        private readonly UIRegistryEntry _entry;
        private readonly Transform _parent;


        public UIPoolFactory(IUIFactory factory, UIRegistryEntry entry, Transform parent)
        {
            _factory = factory;
            _entry = entry;
            _parent = parent;
        }

        public UniTask<UIView> CreateAsync(CancellationToken ct = default) =>
            _factory.CreateAsync(_entry, _parent, ct);

        public void Destroy(UIView view) =>
            _factory.Destroy(_entry, view);
    }
}