using DatabaseLibrary;
using ESPModels;

namespace ESP32DataCollector;

public interface IProcessPackets
{
    public Task ProcessPacketAsync(string packet, PostgresContext context);

}

public class ProcessPackets :  IProcessPackets
{
    private TimeSpan lastUpTime =TimeSpan.MinValue;
    public async Task ProcessPacketAsync(string packet, PostgresContext context)
    {
        if (packet.StartsWith("$"))
            await ProcessNamedPacketAsync(packet, context);
        else
        {
            await ProcessGridStatusAsync(packet,context);
        }
    }
    
    public Task ProcessNamedPacketAsync(string packet,PostgresContext context)
    {
        return Task.CompletedTask;
    }

    public Task ProcessGridStatusAsync(string packet,PostgresContext context)
    {

        var parts = packet.Split('|');
        TimeSpan UpTime = TimeSpan.Parse(parts[1]);
        if (UpTime > lastUpTime)
        {
            lastUpTime = UpTime;
        }
        else
        {
            var gridStatus = new GridStatus
            {
                DeviceName = parts[0],
                UpTime = UpTime,
                LastSeen = DateTime.UtcNow,
            };
            context.GridStatuses.Add(gridStatus);
            context.SaveChanges();
        }

        return Task.CompletedTask;
    }
}
