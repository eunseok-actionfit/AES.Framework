using System.Threading;
using System.Threading.Tasks;


namespace Core.Engine.Command
{
    public interface ICommand
    {
        string Name { get; }
        Task<Result.Result> Execute(CancellationToken ct = default);
        Task<Result.Result> Undo(CancellationToken ct = default);
    }
}