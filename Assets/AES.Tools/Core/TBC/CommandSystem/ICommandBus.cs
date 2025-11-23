using System.Threading;
using System.Threading.Tasks;


namespace AES.Tools.TBC.CommandSystem
{
    public interface ICommandBus
    {
        Task<Result> Run(ICommand command, CancellationToken ct = default);
        Task<Result> Undo(CancellationToken ct = default);
        Task<Result> Redo(CancellationToken ct = default);
        
        bool CanUndo { get; }
        bool CanRedo { get; }
        void ClearAll();
    }
}