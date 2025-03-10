using ESPWorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHostedService(sp => new UDPListener(
    sp.GetRequiredService<ILogger<UDPListener>>(), 9124));

var host = builder.Build();
host.Run();
