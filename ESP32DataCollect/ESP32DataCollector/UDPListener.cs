using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DatabaseLibrary;
using Microsoft.EntityFrameworkCore;

namespace ESP32DataCollector
{
    public class UDPListener : IHostedService
    {
        private readonly ILogger<UDPListener> _logger;
        private readonly UdpListenerOptions _options;
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource _stoppingCts;

        public UDPListener(ILogger<UDPListener> logger, IOptions<UdpListenerOptions> options, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _options = options.Value;
            _serviceProvider = serviceProvider;
            _stoppingCts = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("UDPListener starting...");
            Task.Run(() => ExecuteAsync(_stoppingCts.Token));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("UDPListener stopping...");
            _stoppingCts.Cancel();
            return Task.CompletedTask;
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var udpClient = new UdpClient(_options.Port))
                {
                    var endPoint = new IPEndPoint(IPAddress.Any, _options.Port);
                    _logger.LogInformation($"Listening for UDP broadcasts on port {_options.Port}...");

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Waiting for a packet...");
                        var result = await udpClient.ReceiveAsync();
                        var receivedMessage = Encoding.UTF8.GetString(result.Buffer);
                        _logger.LogInformation($"Received message: {receivedMessage}");

                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var adbContext = scope.ServiceProvider.GetRequiredService<PostgresContext>();
                            adbContext.Database.EnsureCreated(); 
                            // Use dbContext here
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while receiving UDP packets.");
            }
        }
    }
}
