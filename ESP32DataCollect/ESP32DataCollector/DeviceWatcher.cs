

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
                await ProcessGridStatusAsync(packet, context);
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

            var gridStatus = new GridStatus
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
                if (currentTime - prevStatus.LastSeen > TimeSpan.FromMinutes(2) && prevStatus.IsOnline)
                {
                    // Mark the device as down
                    prevStatus.IsOnline = false;
                    var downStatus = new GridStatus
                    {
                        DeviceName = parts[0],
                        IsOnline = false,
                        LastSeen = prevStatus.LastSeen,
                        UpTime = prevStatus.UpTime,
                        Dtg = currentTime
                    };

                    currentGridStatus[parts[0]] = downStatus;
                    await SaveStatus(downStatus, context);
                }
                else if (prevStatus.IsOnline == false)
                {
                    // Device was previously offline, now it's back online
                    gridStatus.Logged = false;
                    currentGridStatus[parts[0]] = gridStatus;
                    
                    await SaveStatus(gridStatus, context);
                }
            }
            else
            {
                // New device or device status changed
                currentGridStatus[parts[0]] = gridStatus;
                await SaveStatus(gridStatus, context);
            }
        }

        private bool IsDown(string deviceName)
        {
            var found = currentGridStatus.TryGetValue(deviceName, out var status);
            if (!found)
                return false;

            return DateTime.UtcNow - status.LastSeen > TimeSpan.FromMinutes(2);
        }

        private async Task CheckDevices()
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<PostgresContext>();
                foreach (var kvp in currentGridStatus)
                {
                    string deviceName = kvp.Key;
                    GridStatus status = kvp.Value;

                    // Log the device name and status
                    var now = DateTime.UtcNow;
                    var ts = TimeSpan.FromMinutes(2);

                    if (now - status.LastSeen > ts && status.IsOnline)
                    {
                        Console.WriteLine($"down device: {deviceName} , Last Seen: {status.LastSeen}, Is Online: {status.IsOnline}");
                        var downStatus = new GridStatus
                        {
                            DeviceName = deviceName,
                            IsOnline = false,
                            LastSeen = status.LastSeen,
                            UpTime = status.UpTime,
                            Dtg = DateTime.UtcNow
                        };
                        if (downStatus.Logged == false)
                        {
                            downStatus.Logged = true;
                            await SaveStatus(downStatus, context);
                        }
                        currentGridStatus[deviceName] = downStatus;
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
