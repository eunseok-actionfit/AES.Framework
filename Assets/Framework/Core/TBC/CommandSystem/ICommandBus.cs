using System.Threading;
using System.Threading.Tasks;


namespace Core.Engine.Command
{
    public interface ICommandBus
    {
        Task<Result.Result> Run(ICommand command, CancellationToken ct = default);
        Task<Result.Result> Undo(CancellationToken ct = default);
        Task<Result.Result> Redo(CancellationToken ct = default);
        
        bool CanUndo { get; }
        bool CanRedo { get; }
        void ClearAll();
    }
}