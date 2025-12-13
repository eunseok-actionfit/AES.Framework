// AES.Tools.TBC.CommandSystem/IGameCommand.cs
using System.Threading;
using AES.Tools.TBC.Result;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    /// <summary>
    /// 도메인(게임) 전용 커맨드. UI와 분리된 계층.
    /// </summary>
    public interface IGameCommand
    {
        /// 커맨드 이름 (디버깅/로그/이벤트용)
        string Name { get; }

        /// 실제 실행 로직. 성공/실패/노옵 등 Result 로 표현.
        UniTask<Result> ExecuteAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// Undo 가능한 도메인 커맨드
    /// </summary>
    public interface IUndoableGameCommand : IGameCommand
    {
        /// 되돌리기 로직
        UniTask<Result> UndoAsync(CancellationToken ct = default);
    }
}