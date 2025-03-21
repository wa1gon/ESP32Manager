using ESP32DataCollector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DatabaseLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);


// Add configuration from appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "secrets.json"), optional: true, reloadOnChange: true);

var email = builder.Configuration["EmailSettings:SenderPassword"];
Console.WriteLine($"MySecret from config: {email}");
// Register the configuration for UdpListenerOptions
builder.Services.Configure<UdpListenerOptions>(builder.Configuration.GetSection("UdpListenerOptions"));

// Register the UDPListener as a hosted service
builder.Services.AddSingleton<IHostedService, UDPListener>();
builder.Services.AddScoped<DeviceWatcher, DeviceWatcher>();
builder.Services.AddDbContextPool<PostgresContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var host = builder.Build();
host.Run();
