using System.Collections.Generic;
using AES.Tools.Core;


namespace AES.Tools.Registry
{
    public interface IUIWindowRegistry
    {
        bool TryGet(UIWindowKey key, out UIRegistryEntry entry);

        IEnumerable<KeyValuePair<UIWindowKey, UIRegistryEntry>> GetAll();
    }
}