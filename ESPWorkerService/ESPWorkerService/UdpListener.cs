namespace ESPWorkerService;

// UDPListener.cs
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class UDPListener : BackgroundService
{
    private readonly ILogger<UDPListener> _logger;
    private readonly int _port;

    public UDPListener(ILogger<UDPListener> logger, int port)
    {
        _logger = logger;
        _port = port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var udpClient = new UdpClient(_port))
        {
            var endPoint = new IPEndPoint(IPAddress.Any, _port);
            _logger.LogInformation($"Listening for UDP broadcasts on port {_port}...");

            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await udpClient.ReceiveAsync();
                var receivedMessage = Encoding.UTF8.GetString(result.Buffer);
                _logger.LogInformation($"Received message: {receivedMessage}");
            }
        }
    }
}
