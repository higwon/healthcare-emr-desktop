using System.Threading;
using System.Threading.Tasks;

namespace HealthNote.Application
{
    public interface IClientReadinessQuery
    {
        Task<bool> IsReadyAsync(CancellationToken cancellationToken);
    }
}
