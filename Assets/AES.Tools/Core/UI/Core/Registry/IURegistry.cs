using System;
using System.Collections.Generic;
using AES.Tools.UI_Regacy_.Core.View;


namespace AES.Tools.UI.Core.Registry
{
    public interface IURegistry
    {
        bool TryGet(UIWindowKey key, out UIRegistryEntry entry);
        public bool TryGetByViewType(Type viewType, out UIRegistryEntry entry);

        IEnumerable<KeyValuePair<UIWindowKey, UIRegistryEntry>> GetAll();
    }
}