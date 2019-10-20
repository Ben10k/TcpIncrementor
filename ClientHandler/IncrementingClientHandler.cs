using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TcpIncrementor.ClientHandler
{
    public class IncrementingClientHandler : IClientHandler
    {
        private readonly ILogger<IncrementingClientHandler> _logger;
        private readonly int _sessionTimeout;
        private bool _isSessionKilled = false;
        private CancellationTokenSource _incrementorCancellationTokenSource = new CancellationTokenSource();

        public IncrementingClientHandler(ILogger<IncrementingClientHandler> logger, IConfiguration config)
        {
            _logger = logger;
            _sessionTimeout = config.GetSection("service").GetValue<int>("SessionTimeout");
        }

        public async Task HandleClientAsync(Stream dataStream, CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested && !_isSessionKilled)
                {
                    var receivedData = ReadTextAsync(dataStream, stoppingToken);
                    if (await Task.WhenAny(receivedData, Task.Delay(_sessionTimeout, stoppingToken)) == receivedData)
                        await ProcessCommandAsync(receivedData.Result, dataStream, stoppingToken);
                    else
                        await OnTimeOutAsync(dataStream, stoppingToken);
                }

                _logger.LogInformation("Client has been disconnected");
            }
            catch (SocketException e)
            {
                _logger.LogError(e, "Exception has been thrown");
            }
        }

        private async Task ProcessCommandAsync(string command, Stream dataStream, CancellationToken token)
        {
            switch (command)
            {
                case {} when command.StartsWith("kill"):
                    _logger.LogInformation("Received a kill signal. Terminating the connection");
                    _isSessionKilled = true;
                    break;
                case {} when command.StartsWith("stop"):
                    _logger.LogInformation("Received a stop signal. Stopping the sending");
                    _incrementorCancellationTokenSource.Cancel();
                    _incrementorCancellationTokenSource = new CancellationTokenSource();
                    break;
                case {} when command.StartsWith("send") && command.Split(" ").Length == 4:
                    var strings = command.Split(" ");
                   
                        _logger.LogInformation("Received a send signal. Starting to send the information");
                        Task.Run(() =>
                                StartIncrementAsync(dataStream, Convert.ToInt32(strings[1]), Convert.ToInt32(strings[2]),
                                    Convert.ToInt32(strings[3]),
                                    _incrementorCancellationTokenSource.Token),
                            _incrementorCancellationTokenSource.Token);
                    break;
                default:
                    _logger.LogInformation("Unsupported command received");
                    await WriteTextAsync(dataStream, "Unsupported command received", token);
                    break;
            }
        }

        private async Task OnTimeOutAsync(Stream dataStream, CancellationToken token)
        {
            await WriteTextAsync(dataStream, "You have been timed-out", token);
            _incrementorCancellationTokenSource.Cancel();
            _isSessionKilled = true;
            _logger.LogInformation("Timeout");
        }

        private async Task StartIncrementAsync(Stream dataStream, int initial, int delay, int increment,
            CancellationToken token)
        {
            _logger.LogInformation(
                "Starting incremental sending to client with parameters: initial {0}, delay {1}, increment {2}",
                initial, delay, increment);
            var currentNumber = initial;
            while (!token.IsCancellationRequested)
            {
                await WriteTextAsync(dataStream, $"{currentNumber}", token);
                currentNumber += increment;
                await Task.Delay(delay * 1000, token);
            }
        }

        private async Task WriteTextAsync(Stream dataStream, string text, CancellationToken token)
        {
            _logger.LogInformation("Sending text {text} to client", text);
            await dataStream.WriteAsync(Encoding.ASCII.GetBytes(text + "\n"), token);
        }

        private async Task<string> ReadTextAsync(Stream dataStream, CancellationToken token)
        {
            var buffer = new byte[256];
            var bytesRead = dataStream.ReadAsync(buffer, token);
            var text = Encoding.ASCII.GetString(buffer, 0, await bytesRead);
            _logger.LogInformation("Received text {text} from client", text);
            return text;
        }
    }
}