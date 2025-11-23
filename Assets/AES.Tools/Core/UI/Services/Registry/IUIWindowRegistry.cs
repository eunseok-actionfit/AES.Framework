using System.Collections.Generic;
using AES.Tools.View;


namespace AES.Tools
{
    public interface IUIWindowRegistry
    {
        bool TryGet(UIWindowKey key, out UIRegistryEntry entry);

        IEnumerable<KeyValuePair<UIWindowKey, UIRegistryEntry>> GetAll();
    }
}