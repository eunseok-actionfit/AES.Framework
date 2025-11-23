using System.Threading;
using AES.Tools;
using AES.Tools.Core;
using AES.Tools.TBC.CommandSystem;
using Cysharp.Threading.Tasks;
using ICommand = AES.Tools.TBC.CommandSystem.ICommand;


public sealed class SaveAllCommand : ICommand
{
    public string Name => "SaveAll";
    private readonly ISaveCoordinator coord;

    public SaveAllCommand(ISaveCoordinator c) => coord = c;

    public async UniTask<Result> Execute(CancellationToken ct = default)
    {
        return await coord.SaveAllAsync(ct);
    }

    public UniTask<Result> Undo(CancellationToken ct = default)
        => UniTask.FromResult(Result.Fail(new Error("undo-not-supported", "Cannot undo SaveAll")));
}

public sealed class LoadAllCommand : ICommand
{
    public string Name => "LoadAll";
    private readonly ISaveCoordinator coord;

    public LoadAllCommand(ISaveCoordinator c) => coord = c;

    public async UniTask<Result> Execute(CancellationToken ct = default)
    {
        return await coord.LoadAllAsync(ct);
    }

    public UniTask<Result> Undo(CancellationToken ct = default)
        => UniTask.FromResult(Result.Fail(new Error("undo-not-supported", "Cannot undo LoadAll")));
}