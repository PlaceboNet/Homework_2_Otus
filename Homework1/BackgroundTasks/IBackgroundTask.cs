using System.Threading.Tasks;
using System.Threading;

namespace Homework1.BackgroundTasks
{
    public interface IBackgroundTask
    {
        Task Start(CancellationToken ct);
    }
}
