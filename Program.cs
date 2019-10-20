using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TcpIncrementor.ClientHandler;
using TcpIncrementor.Worker;

namespace TcpIncrementor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => { logging.ClearProviders(); })
                .UseSerilog((hostingContext, loggerConfiguration) =>
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration))
                .ConfigureServices((hostContext, services) => services
                    .AddHostedService<TcpServiceWorker>()
                    .AddTransient<IClientHandler, IncrementingClientHandler>()
                );
    }
}