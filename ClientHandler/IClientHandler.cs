using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TcpIncrementor.ClientHandler
{
    public interface IClientHandler
    {
        Task HandleClientAsync(Stream dataStream, CancellationToken stoppingToken);
    }
}