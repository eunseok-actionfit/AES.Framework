using Cysharp.Threading.Tasks;

public interface IIapTransactionStore
{
    bool IsProcessed(string transactionId);
    UniTask LoadAsync();
    UniTask MarkProcessedAsync(string transactionId);
}