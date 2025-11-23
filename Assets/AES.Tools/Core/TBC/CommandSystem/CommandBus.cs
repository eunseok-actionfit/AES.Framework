using System.Threading;
using System.Threading.Tasks;


namespace AES.Tools.TBC.CommandSystem
{
    public sealed class CommandBus : ICommandBus
    {
        readonly ICommandHistory _hist;

        public CommandBus(ICommandHistory hist) { _hist = hist; }

        public bool CanUndo => _hist.UndoCount > 0;
        public bool CanRedo => _hist.RedoCount > 0;

        public async Task<Result> Run(ICommand command, CancellationToken ct = default)
        {
            var r = await command.Execute(ct);

            if (r.IsSuccess)
            {
                _hist.PushDone(command);
                _hist.ClearRedo();
            }

            return r;
        }

        public async Task<Result> Undo(CancellationToken ct = default)
        {
            if (!_hist.TryPopUndo(out var cmd)) return Result.Fail(new("UNDO_EMPTY", "no command"));
            var r = await cmd.Undo(ct);
            if (r.IsSuccess) _hist.PushUndone(cmd);
            return r;
        }

        public async Task<Result> Redo(CancellationToken ct = default)
        {
            if (!_hist.TryPopRedo(out var cmd)) return Result.Fail(new("REDO_EMPTY", "no command"));
            var r = await cmd.Execute(ct);
            if (r.IsSuccess) _hist.PushDone(cmd);
            return r;
        }

        public void ClearAll() => _hist.ClearAll();
    }
}