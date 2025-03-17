using DatabaseLibrary;
using ESPModels;
using Microsoft.Extensions.DependencyInjection;

namespace ESP32DataCollector;

public interface IProcessPackets
{
    public Task ProcessPacketAsync(string packet, PostgresContext context);
    public Task CheckDevices(IServiceProvider serviceProvider);
}

public class DeviceWatcher : IProcessPackets
{
    private TimeSpan lastUpTime = TimeSpan.MinValue;
    private DateTime lastRecordedTime = DateTime.MinValue;
    private readonly IServiceProvider serviceProvider;

    public DeviceWatcher(IServiceProvider sp)
    {
        serviceProvider = sp;
    }

    public async Task ProcessPacketAsync(string packet, PostgresContext context)
    {
        if (packet.StartsWith("$"))
            await ProcessNamedPacketAsync(packet, context);
        else
        {
            await ProcessGridStatusAsync(packet, context);
        }
    }

    public Task ProcessNamedPacketAsync(string packet, PostgresContext context)
    {
        return Task.CompletedTask;
    }

    public async Task ProcessGridStatusAsync(string packet, PostgresContext context)
    {
        var parts = packet.Split('|');
        TimeSpan UpTime = TimeSpan.Parse(parts[1]);
        DateTime currentTime = DateTime.UtcNow;

        if (UpTime < lastUpTime || (currentTime - lastRecordedTime) >= TimeSpan.FromMinutes(1))
        {
            lastUpTime = UpTime;
            lastRecordedTime = currentTime;

            var gridStatus = new GridStatus
            {
                DeviceName = parts[0],
                UpTime = UpTime,
                LastSeen = currentTime,
                IsOnline = true
            };
            context.GridStatuses.Add(gridStatus);
            await context.SaveChangesAsync();
        }
    }

    public async Task CheckDevices(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var adbContext = scope.ServiceProvider.GetRequiredService<PostgresContext>();
            // Your logic to check devices using adbContext
        }
    }
}
