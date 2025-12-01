using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.TBC.CommandSystem
{
    public sealed class CommandBus : ICommandBus
    {
        private readonly ICommandHistory _hist;

        public CommandBus(ICommandHistory history)
        {
            _hist = history ?? throw new ArgumentNullException(nameof(history));
        }

        public bool CanUndo => _hist.UndoCount > 0;
        public bool CanRedo => _hist.RedoCount > 0;

        static string GetName(IGameCommand cmd) => cmd.Name ?? cmd.GetType().Name;

        public async UniTask<Result> Run(IGameCommand command, CancellationToken ct = default)
        {
            if (command == null) return Result.Fail(new Error("NullCommand", "Command is null"));

            var name = GetName(command);
            EventBus<CommandStartedEvent>.Raise(new CommandStartedEvent
            {
                Command = command,
                Name = name
            });

            try
            {
                var result = await command.ExecuteAsync(ct);

                EventBus<CommandCompletedEvent>.Raise(new CommandCompletedEvent
                {
                    Command = command,
                    Name = name,
                    Result = result
                });

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandBus] Run failed on {name}: {ex}");
                EventBus<CommandFailedEvent>.Raise(new CommandFailedEvent
                {
                    Command = command,
                    Name = name,
                    Reason = ex.Message
                });
                return Result.Fail(new Error("Exception", ex.Message));
            }
        }

        public async UniTask<Result> RunAndRecord(IUndoableGameCommand command, CancellationToken ct = default)
        {
            var result = await Run(command, ct);
            if (result.IsSuccess)
            {
                _hist.PushDone(command);
                _hist.ClearRedo();
            }
            return result;
        }

        public async UniTask<Result> Undo(CancellationToken ct = default)
        {
            if (!_hist.TryPopUndo(out var cmd))
                return Result.Fail(new Error("UndoEmpty", "No command to undo"));

            var name = GetName(cmd);
            EventBus<CommandStartedEvent>.Raise(new CommandStartedEvent
            {
                Command = cmd,
                Name = $"Undo:{name}"
            });

            try
            {
                var result = await cmd.UndoAsync(ct);

                if (result.IsSuccess)
                    _hist.PushUndone(cmd); // Redo 스택에 쌓는다

                EventBus<CommandUndoEvent>.Raise(new CommandUndoEvent
                {
                    Command = cmd,
                    Name = name,
                    Result = result
                });

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandBus] Undo failed on {name}: {ex}");
                EventBus<CommandFailedEvent>.Raise(new CommandFailedEvent
                {
                    Command = cmd,
                    Name = $"Undo:{name}",
                    Reason = ex.Message
                });
                return Result.Fail(new Error("Exception", ex.Message));
            }
        }

        public async UniTask<Result> Redo(CancellationToken ct = default)
        {
            if (!_hist.TryPopRedo(out var cmd))
                return Result.Fail(new Error("RedoEmpty", "No command to redo"));

            var name = GetName(cmd);
            EventBus<CommandStartedEvent>.Raise(new CommandStartedEvent
            {
                Command = cmd,
                Name = $"Redo:{name}"
            });

            try
            {
                var result = await cmd.ExecuteAsync(ct);

                if (result.IsSuccess)
                    _hist.PushDone(cmd);

                EventBus<CommandRedoEvent>.Raise(new CommandRedoEvent
                {
                    Command = cmd,
                    Name = name,
                    Result = result
                });

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CommandBus] Redo failed on {name}: {ex}");
                EventBus<CommandFailedEvent>.Raise(new CommandFailedEvent
                {
                    Command = cmd,
                    Name = $"Redo:{name}",
                    Reason = ex.Message
                });
                return Result.Fail(new Error("Exception", ex.Message));
            }
        }

        public void ClearAll() => _hist.ClearAll();
    }
}
