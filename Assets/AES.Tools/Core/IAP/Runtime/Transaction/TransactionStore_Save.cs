using System;
using System.Collections.Generic;
using AES.IAP;
using Cysharp.Threading.Tasks;
using AES.Tools;
using AES.Tools.Core;

namespace Systems.IAP
{
    [Serializable]
    [SaveData("iap_tx", UseSlot = true, Backend = SaveBackend.CloudFirst)]
    public sealed class IapProcessedTransactionsData
    {
        public List<string> ids = new();
    }

    public sealed class TransactionStore_Save : IIapTransactionStore
    {
        private readonly IStorageService _storage;
        private readonly ISlotService _slot;

        private readonly HashSet<string> _cache = new(StringComparer.Ordinal);
        private bool _loaded;

        private const int MaxIds = 5000;

        public TransactionStore_Save(IStorageService storage, ISlotService slot)
        {
            _storage = storage;
            _slot = slot;
        }

        public bool IsProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return false;
            return _loaded && _cache.Contains(transactionId);
        }

        public async UniTask LoadAsync()
        {
            var slotId = _slot.CurrentSlotId;
            var result = await _storage.LoadAsync<IapProcessedTransactionsData>(slotId);

            _cache.Clear();
            var dto = result.Value;
            if (dto?.ids != null)
            {
                foreach (var id in dto.ids)
                    if (!string.IsNullOrEmpty(id)) _cache.Add(id);
            }

            _loaded = true;
        }

        public async UniTask MarkProcessedAsync(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;

            if (!_loaded)
                await LoadAsync();

            if (_cache.Add(transactionId))
                await SaveAsync();
        }

        private async UniTask SaveAsync()
        {
            var list = new List<string>(_cache);
            if (list.Count > MaxIds)
                list.RemoveRange(0, list.Count - MaxIds);

            var dto = new IapProcessedTransactionsData { ids = list };

            var slotId = _slot.CurrentSlotId;
            await _storage.SaveAsync(slotId, dto);
        }
    }
}
