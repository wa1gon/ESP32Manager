using ESP32DataCollector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Register the configuration for UdpListenerOptions
builder.Services.Configure<UdpListenerOptions>(options =>
{
    options.Port = 9125;
});

// Register the UDPListener as a hosted service
builder.Services.AddHostedService<UDPListener>();

var host = builder.Build();
host.Run();
