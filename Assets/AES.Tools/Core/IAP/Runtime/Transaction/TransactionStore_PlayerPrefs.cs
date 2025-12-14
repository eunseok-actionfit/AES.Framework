using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AES.IAP.Transaction
{
    /// <summary>
    /// Simple transaction store backed by PlayerPrefs.
    /// Good default if you don't have a cloud save system.
    /// </summary>
    public sealed class TransactionStore_PlayerPrefs : IIapTransactionStore
    {
        private const string PrefKey = "AES.IAP.ProcessedTxIds";
        private const int MaxIds = 5000;

        [Serializable]
        private sealed class Dto { public List<string> ids = new(); }

        private readonly HashSet<string> _cache = new(StringComparer.Ordinal);
        private bool _loaded;

        public bool IsProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return false;
            return _loaded && _cache.Contains(transactionId);
        }

        public UniTask LoadAsync()
        {
            _cache.Clear();
            var json = PlayerPrefs.GetString(PrefKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var dto = JsonUtility.FromJson<Dto>(json);
                    if (dto?.ids != null)
                    {
                        foreach (var id in dto.ids)
                            if (!string.IsNullOrEmpty(id)) _cache.Add(id);
                    }
                }
                catch (Exception)
                {
                    // ignore corrupted data
                }
            }

            _loaded = true;
            return UniTask.CompletedTask;
        }

        public async UniTask MarkProcessedAsync(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            if (!_loaded) await LoadAsync();

            if (_cache.Add(transactionId))
                Save();
        }

        private void Save()
        {
            var list = new List<string>(_cache);
            if (list.Count > MaxIds)
                list.RemoveRange(0, list.Count - MaxIds);

            var json = JsonUtility.ToJson(new Dto { ids = list });
            PlayerPrefs.SetString(PrefKey, json);
            PlayerPrefs.Save();
        }
    }
}
