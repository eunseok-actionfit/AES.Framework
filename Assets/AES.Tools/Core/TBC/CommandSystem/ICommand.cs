using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools.TBC.CommandSystem
{
    public interface ICommand
    {
        string Name { get; }
        UniTask<Result> Execute(CancellationToken ct = default);
        UniTask<Result> Undo(CancellationToken ct = default);
    }
}