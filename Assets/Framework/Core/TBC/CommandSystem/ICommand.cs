using System.Threading;
using System.Threading.Tasks;


namespace AES.Tools
{
    public interface ICommand
    {
        string Name { get; }
        Task<Result> Execute(CancellationToken ct = default);
        Task<Result> Undo(CancellationToken ct = default);
    }
}