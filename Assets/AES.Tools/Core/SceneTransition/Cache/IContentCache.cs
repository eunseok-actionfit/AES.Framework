using System.Threading;
using Cysharp.Threading.Tasks;

public interface IContentCache
{
    UniTask ClearAllAsync(CancellationToken ct);
    UniTask ClearByKeyAsync(object keyOrLabel, CancellationToken ct);
    UniTask CleanUnusedAsync(CancellationToken ct);
}