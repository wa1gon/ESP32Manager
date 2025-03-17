namespace ESPModels;

public class GridStatus
{
    public int Id { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
    public TimeSpan UpTime { get; set; }
    
}
