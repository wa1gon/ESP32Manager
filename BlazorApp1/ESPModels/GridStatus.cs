using System.ComponentModel.DataAnnotations.Schema;

namespace ESPModels;

public class GridStatus
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceName { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
    public TimeSpan UpTime { get; set; }
    public DateTime Dtg { get; set; }
    [NotMapped]
    public bool Logged { get; set; } = false;
}
