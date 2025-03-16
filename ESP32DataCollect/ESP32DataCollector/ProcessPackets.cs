namespace ESP32DataCollector;

public interface IProcessPackets
{
    public Task ProcessPacket(string packet);
}

public class ProcessPackets :  IProcessPackets
{
    public async Task ProcessPacket(string packet)
    {
        
    }
}
