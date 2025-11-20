using System.Collections.Generic;
using AES.Tools.Core.View;


namespace AES.Tools.Services.Registry
{
    public interface IUIWindowRegistry
    {
        bool TryGet(UIWindowKey key, out UIRegistryEntry entry);

        IEnumerable<KeyValuePair<UIWindowKey, UIRegistryEntry>> GetAll();
    }
}