namespace ESP32DataCollector;

public class Lap
{
    public int Id { get; set; }
    public TimeSpan Duration { get; set; }

    public static Lap Parse(string lapString)
    {
        var duration = TimeSpan.ParseExact(lapString, "d\\.hh\\:mm\\:ss", null);
        return new Lap { Duration = duration };
    }
}
