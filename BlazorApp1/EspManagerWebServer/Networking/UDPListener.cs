// // UDPListener.cs
// using System.Net;
// using System.Net.Sockets;
// using System.Text;
//
// using Microsoft.AspNetCore.Components;
//
//
// public class UDPListener : ComponentBase
// {
//     private void UdpListener()
//     {
//         using (var udpClient = new UdpClient(9124))
//         {
//             var endPoint = new IPEndPoint(IPAddress.Any, 9124);
//             Console.WriteLine("Listening for UDP broadcasts on port 9124...");
//
//             while (true)
//             {
//                 var receivedBytes = udpClient.Receive(ref endPoint);
//                 var receivedMessage = Encoding.UTF8.GetString(receivedBytes);
//                 var parts = receivedMessage.Split('|');
//                 if (parts.Length == 2)
//                 {
//                     var deviceName = parts[0];
//                     var uptime = TimeSpan.Parse(parts[1]).TrimToSecond();
//
//                     InvokeAsync(() =>
//                     {
//                         if (!devices.ContainsKey(deviceName))
//                             devices[deviceName] = new DeviceInfo { LastUpdate = DateTime.Now.TrimToSecond(), Uptime = uptime, Downtime = TimeSpan.Zero, IsDown = false };
//                         else
//                         {
//                             var device = devices[deviceName];
//
//                             var timeSinceLastUpdate = DateTime.Now - device.LastUpdate;
//
//                             if (timeSinceLastUpdate > TimeSpan.FromSeconds(10) && !device.IsDown)
//                             {
//                                 device.DarkStart = device.LastUpdate.AddSeconds(10).TrimToSecond();
//                                 device.IsDown = true;
//                                 darkPeriodsLog.Add($"{deviceName} is down since {device.DarkStart}");
//                             }
//
//                             if (uptime > TimeSpan.FromSeconds(10) && device.IsDown)
//                             {
//                                 device.DarkEnd = DateTime.Now.TrimToSecond();
//                                 darkPeriodsLog.Add($"{deviceName} is back online since {device.DarkEnd}. Downtime: {device.DarkEnd.Value - device.DarkStart.Value}");
//                                 device.Downtime += device.DarkEnd.Value - device.DarkStart.Value;
//                                 device.DarkStart = null;
//                                 device.DarkEnd = null;
//                                 device.IsDown = false;
//                             }
//
//                             device.LastUpdate = DateTime.Now.TrimToSecond();
//                             device.Uptime = uptime;
//                         }
//
//                         StateHasChanged();
//                     });
//                 }
//             }
//         }
//     }
// }
