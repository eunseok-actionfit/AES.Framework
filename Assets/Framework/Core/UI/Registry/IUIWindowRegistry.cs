using System.Collections.Generic;
using Core.Systems.UI.Core;

namespace Core.Systems.UI.Registry
{
    public interface IUIWindowRegistry
    {
        bool TryGet(UIWindowKey key, out UIRegistryEntry entry);

        IEnumerable<KeyValuePair<UIWindowKey, UIRegistryEntry>> GetAll();
    }
}