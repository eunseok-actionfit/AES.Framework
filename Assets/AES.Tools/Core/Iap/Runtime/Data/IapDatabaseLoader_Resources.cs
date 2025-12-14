using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;


namespace AES.Tools
{
    public static class IapDatabaseLoader_Resources
    {
        [Serializable]
        private sealed class SheetDump
        {
            public string mode;
            public JArray rows;
        }

        public static IapDatabase Load(string resourcesFolder)
        {
            if (string.IsNullOrWhiteSpace(resourcesFolder))
                throw new ArgumentException("resourcesFolder is empty");

            var texts = Resources.LoadAll<TextAsset>(resourcesFolder);
            if (texts == null || texts.Length == 0)
                throw new Exception($"No IAP JSON found under Resources/{resourcesFolder}");

            var prods = new List<IapProductRow>();
            var stores = new List<IapStoreProductRow>();
            var bundles = new List<IapBundleContentRow>();
            var enums = new List<EnumDefinitionRow>();
            var economy = new List<EconomyValueRow>();
            var limits = new List<IapLimitRow>();

            foreach (var ta in texts)
            {
                if (ta == null || string.IsNullOrWhiteSpace(ta.text)) continue;

                SheetDump dump;
                try
                {
                    dump = JObject.Parse(ta.text).ToObject<SheetDump>();
                }
                catch
                {
                    continue; // 불필요한 경고 스팸 제거
                }

                if (dump?.rows == null || string.IsNullOrWhiteSpace(dump.mode)) continue;

                switch (dump.mode)
                {
                    case "IapProductJson":
                        prods.AddRange(dump.rows.ToObject<List<IapProductRow>>());
                        break;
                    case "IapStoreProductJson":
                        stores.AddRange(dump.rows.ToObject<List<IapStoreProductRow>>());
                        break;
                    case "IapBundleContentJson":
                        bundles.AddRange(dump.rows.ToObject<List<IapBundleContentRow>>());
                        break;

                    case "EnumDefinitionJson":
                        enums.AddRange(dump.rows.ToObject<List<EnumDefinitionRow>>());
                        break;
                    case "EconomyValueJson":
                        economy.AddRange(dump.rows.ToObject<List<EconomyValueRow>>());
                        break;
                    case "IapLimitJson":
                        limits.AddRange(dump.rows.ToObject<List<IapLimitRow>>());
                        break;
                }
            }

            return new IapDatabase(prods, stores, bundles, enums, economy, limits);
        }
    }
}
