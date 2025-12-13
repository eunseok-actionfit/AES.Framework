using System.Threading;
using AES.Tools.TBC.Result;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public interface ICommandBus
    {
        /// 단발 실행 (Undo 기록 X)
        UniTask<Result> Run(IGameCommand command, CancellationToken ct = default);

        /// 실행 + Undo 히스토리에 기록
        UniTask<Result> RunAndRecord(IUndoableGameCommand command, CancellationToken ct = default);

        UniTask<Result> Undo(CancellationToken ct = default);
        UniTask<Result> Redo(CancellationToken ct = default);

        bool CanUndo { get; }
        bool CanRedo { get; }

        void ClearAll();
    }
}