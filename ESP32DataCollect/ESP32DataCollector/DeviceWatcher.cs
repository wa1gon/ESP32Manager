using System;
using System.Collections.Concurrent;
using System.Timers;
using DatabaseLibrary;
using ESPModels;
using Timer = System.Timers.Timer;

namespace ESP32DataCollector
{
    public class DeviceWatcher
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Timer _timer;
        private const int CheckInterval = 6000; // 6 seconds
        private readonly ConcurrentDictionary<string, GridStatus> currentGridStatus = new();

        public DeviceWatcher(IServiceProvider sp)
        {
            serviceProvider = sp;
            _timer = new Timer(CheckInterval);
            _timer.Elapsed += async (s, e) => await OnTimerElapsed();
            _timer.AutoReset = true;
            _timer.Start();
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
            TimeSpan upTime = TimeSpan.Parse(parts[1]);
            DateTime currentTime = DateTime.UtcNow;

            var incomingPacket = new GridStatus
            {
                DeviceName = parts[0],
                UpTime = upTime,
                LastSeen = currentTime,
                IsOnline = true,
                Dtg = currentTime,
                Logged = false
            };

            bool found = currentGridStatus.TryGetValue(parts[0], out var prevStatus);
            if (found && prevStatus != null)
            {
                if (!prevStatus.IsOnline)
                {
                    // Device was down, now up
                    incomingPacket.Logged = false; // Reset logged status for new up event
                    currentGridStatus[parts[0]] = incomingPacket;
                    await SaveStatus(incomingPacket, context);
                    Console.WriteLine($"Device '{parts[0]}' is back online");
                }
                else
                {
                    // Update existing online device
                    prevStatus.LastSeen = currentTime;
                    prevStatus.UpTime = upTime;
                    prevStatus.Dtg = currentTime;
                    currentGridStatus[parts[0]] = prevStatus;
                }
            }
            else
            {
                // New device
                currentGridStatus[parts[0]] = incomingPacket;
                await SaveStatus(incomingPacket, context);
                Console.WriteLine($"New device '{parts[0]}' detected");
            }
        }

        private bool IsDown(string deviceName, out GridStatus status)
        {
            bool found = currentGridStatus.TryGetValue(deviceName, out status);
            if (!found || status == null)
                return false; // Unknown devices aren't "down" yet

            TimeSpan ts = DateTime.UtcNow - status.LastSeen;
            TimeSpan threshold = TimeSpan.FromMinutes(1);
            Console.WriteLine($"Device '{deviceName}': Time since last seen: {ts}, Threshold: {threshold}");
            return ts > threshold;
        }

        private async Task CheckDevices()
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PostgresContext>();

            foreach (var deviceName in currentGridStatus.Keys)
            {
                if (deviceName.Contains("Bedroom"))
                    continue;

                if (IsDown(deviceName, out var currentStatus) && currentStatus.IsOnline)
                {
                    Console.WriteLine($"Device '{deviceName}' is offline");
                    var downStatus = new GridStatus
                    {
                        DeviceName = deviceName,
                        IsOnline = false,
                        LastSeen = currentStatus.LastSeen,
                        UpTime = currentStatus.UpTime,
                        Dtg = DateTime.UtcNow,
                        Logged = false
                    };

                    currentGridStatus[deviceName] = downStatus;
                    await SaveStatus(downStatus, context);
                    Console.WriteLine($"Device down record saved for '{deviceName}'");
                }
                else if (!IsDown(deviceName, out _))
                {
                    Console.WriteLine($"Device '{deviceName}' is online");
                }
            }
        }

        private async Task SaveStatus(GridStatus status, PostgresContext context)
        {
            context.GridStatuses.Add(status);
            await context.SaveChangesAsync();
        }

        private async Task OnTimerElapsed()
        {
            await CheckDevices();
        }
    }
}
