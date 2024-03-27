using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{
    public interface IWorkDoer
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken);

        Task CloseAsync();
    }
}
