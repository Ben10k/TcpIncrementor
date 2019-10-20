using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TcpIncrementor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _maxClientCount;
        private readonly TcpListener _tcpListener;
        private int _currentClientCount;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration config)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            var serviceConfig = config.GetSection("Service");
            _maxClientCount = serviceConfig.GetValue<int>("MaxClients");
            _currentClientCount = 0;
            _tcpListener = new TcpListener(IPAddress.Any, serviceConfig.GetValue<int>("Port"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _tcpListener.Start();
            _logger.LogInformation("TCP server has been initialized and awaiting connections");

            while (!stoppingToken.IsCancellationRequested)
                try
                {
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    if (_currentClientCount < _maxClientCount)
                    {
                        OnConnect();
                        Task.Run(() => _serviceProvider
                            .GetRequiredService<IClientHandler>()
                            .HandleClientAsync(client.GetStream(), stoppingToken)
                            .ContinueWith(arg => OnDisconnect(client), stoppingToken), stoppingToken);
                    }
                    else
                        DisconnectOnTooManyClients(client);
                }
                catch (SocketException e)
                {
                    _logger.LogError(e, "Exception has been thrown on accepting new connections");
                }
        }

        private void OnConnect()
        {
            Interlocked.Increment(ref _currentClientCount);
            _logger.LogInformation("A Client has been accepted. Current client count: {0}", _currentClientCount);
        }

        private void OnDisconnect(TcpClient client)
        {
            client.Close();
            Interlocked.Decrement(ref _currentClientCount);
            _logger.LogInformation("A Client has disconnected. Current client count: {0}", _currentClientCount);
        }

        private void DisconnectOnTooManyClients(TcpClient client)
        {
            _logger.LogInformation("A client tried to connect, but the limit {0} has been reached", _maxClientCount);
            client.GetStream().Write(Encoding.ASCII.GetBytes("Too many concurrent clients, try again later\n"));
            client.Close();
        }
    }
}