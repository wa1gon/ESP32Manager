

using System.Collections.Concurrent;
using DatabaseLibrary;
using ESPModels;


namespace ESP32DataCollector
{
    public class DeviceWatcher
    {
        private readonly IServiceProvider serviceProvider;
        private Timer _timer;
        private const int CheckInterval = 6000; // 6 seconds
        private ConcurrentDictionary<string, GridStatus> currentGridStatus = new ConcurrentDictionary<string, GridStatus>();

        public DeviceWatcher(IServiceProvider sp)
        {
            serviceProvider = sp;
            _timer = new Timer(OnTimerElapsed, null, CheckInterval, CheckInterval);
        }

        public async Task ProcessPacketAsync(string packet, PostgresContext context)
        {
            if (packet.StartsWith("$"))
                await ProcessNamedPacketAsync(packet, context);
            else
                await ProcessGridPacketAsync(packet, context);
        }

        public Task ProcessNamedPacketAsync(string packet, PostgresContext context)
        {
            return Task.CompletedTask;
        }

        public async Task ProcessGridPacketAsync(string packet, PostgresContext context)
        {
            var parts = packet.Split('|');
            TimeSpan UpTime = TimeSpan.Parse(parts[1]);
            DateTime currentTime = DateTime.UtcNow;

            var incomingPacket = new GridStatus
            {
                DeviceName = parts[0],
                UpTime = UpTime,
                LastSeen = currentTime,
                IsOnline = true,
                Dtg = currentTime
            };

            var found = currentGridStatus.TryGetValue(parts[0], out var prevStatus);
            if (found)
            {
                // var ts = currentTime - prevStatus.LastSeen;
                if ( prevStatus.IsOnline == false)
                {
                    // Mark the device as up
                    prevStatus.IsOnline = false;
                    var changeToUp = new GridStatus
                    {
                        DeviceName = parts[0],
                        IsOnline = true,
                        LastSeen = currentTime,
                        UpTime = incomingPacket.UpTime,
                        Dtg = currentTime,
                        Logged = false
                    };

                    currentGridStatus[parts[0]] = changeToUp;
                    await SaveStatus(changeToUp, context);
                    changeToUp.Logged = true;
                }
                else
                {
                    prevStatus.LastSeen = currentTime;
                    prevStatus.UpTime = incomingPacket.UpTime;
                    currentGridStatus[parts[0]] = prevStatus;
                }
            }
            else
            {
                // New device 

                await SaveStatus(incomingPacket, context);
                incomingPacket.Logged = true;
                currentGridStatus[parts[0]] = incomingPacket;
                
            }
        }

        private bool IsDown(string deviceName)
        {
            var found = currentGridStatus.TryGetValue(deviceName, out var status);
            if (!found || status is null)
                return false;

            var ts = (DateTime.UtcNow - status.LastSeen);
            var from = TimeSpan.FromMinutes(1);
            Console.WriteLine($"time from now TS {from}, Time since last seen: {ts}");
            if (ts > from)
                return true;
            return false;
        }

        private async Task CheckDevices()
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<PostgresContext>();
                foreach (var kvp in currentGridStatus)
                {
                    string deviceName = kvp.Key;
                    GridStatus device = kvp.Value;
                    if (deviceName.Contains("Bedroom"))
                        continue;
                    var down = IsDown(deviceName);

                    if (down == true)
                    {
                        Console.WriteLine($"Device '{deviceName}' is offline");
                        var downStatus = new GridStatus
                        {
                            DeviceName = device.DeviceName,
                            IsOnline = false,
                            Logged = false,
                            LastSeen = device.LastSeen,
                            Dtg = DateTime.UtcNow
                        };

                        if (device.Logged == false)
                        {
                            await SaveStatus(downStatus, context);
                            downStatus.Logged = true;
                            currentGridStatus[deviceName] = downStatus;
                            Console.WriteLine($"Device down record saved and logged set to true for '{deviceName}'");
                        }
                        else
                        {
                            Console.WriteLine($"Device '{deviceName}' is already logged as down");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Device '{deviceName}' is online");
                    }
                }
            }
        }
        private async Task SaveStatus(GridStatus status, PostgresContext context)
        {
            context.GridStatuses.Add(status);
            await context.SaveChangesAsync();
        }

        private void OnTimerElapsed(object state)
        {
            CheckDevices().Wait();
        }
    }
}
