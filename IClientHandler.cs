using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TcpIncrementor
{
    public interface IClientHandler
    {
        Task HandleClientAsync(Stream dataStream, CancellationToken stoppingToken);
    }
}