using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DatabaseLibrary;
using ESPModels;
using Microsoft.Extensions.DependencyInjection;

namespace ESP32DataCollector
{
    public class DeviceWatcher
    {
        private TimeSpan lastUpTime = TimeSpan.MinValue;
        private DateTime lastRecordedTime = DateTime.MinValue;
        private readonly IServiceProvider serviceProvider;
        private Timer _timer;
        private PostgresContext context;
        private const int CheckInterval = 6000; // 6 seconds
        private ConcurrentDictionary<string, GridStatus> currentGridStatus = new ConcurrentDictionary<string, GridStatus>();

        public DeviceWatcher(IServiceProvider sp)
        {
            serviceProvider = sp;
            using (var scope = serviceProvider.CreateScope())
            {
                context = scope.ServiceProvider.GetRequiredService<PostgresContext>();
            }

            _timer = new Timer(OnTimerElapsed, null, CheckInterval, CheckInterval);
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

            var gap = currentTime - lastRecordedTime;

            lastUpTime = UpTime;
            lastRecordedTime = currentTime;

            var gridStatus = new GridStatus
            {
                DeviceName = parts[0],
                UpTime = UpTime,
                LastSeen = currentTime,
                IsOnline = true,
                Dtg = currentTime
            };
            currentGridStatus[parts[0]] = gridStatus;
            if (UpTime < lastUpTime || gap >= TimeSpan.FromMinutes(1))
            {
                await SaveStatus(gridStatus);
            }
        }

        public async Task CheckDevices()
        {
            foreach (var kvp in currentGridStatus)
            {
                string deviceName = kvp.Key;
                GridStatus status = kvp.Value;

                // Perform your logic with deviceName and status
                if (DateTime.Now - status.LastSeen > TimeSpan.FromMinutes(2) && status.IsOnline == true)
                {
                    status.IsOnline = false;
                    await SaveStatus(status);
                }
            }
        }

        private async Task SaveStatus(GridStatus status)
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
