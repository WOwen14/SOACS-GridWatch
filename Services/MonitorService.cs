using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using SOACS.GridWatch.Models;

namespace SOACS.GridWatch.Services
{
    public class MonitorResult
    {
        public bool Online;
        public string Response;
        public int RxIncrement;
    }

    public class MonitorService
    {
        public Task<MonitorResult> CheckAsync(TargetNode node, int timeoutMs)
        {
            if (node.Monitor == MonitorType.TCP) return CheckTcpAsync(node.Address, node.Port, timeoutMs);
            if (node.Monitor == MonitorType.UDP) return CheckUdpAsync(node.Address, node.Port, timeoutMs);
            return CheckPingAsync(node.Address, timeoutMs);
        }

        async Task<MonitorResult> CheckPingAsync(string address, int timeoutMs)
        {
            try
            {
                using (var p = new Ping())
                {
                    var r = await p.SendPingAsync(address, timeoutMs);
                    if (r.Status == IPStatus.Success)
                    {
                        long ms = r.RoundtripTime;
                        return new MonitorResult { Online = true, Response = ms + " ms", RxIncrement = 1 };
                    }
                    if (r.Status == IPStatus.TimedOut)
                        return new MonitorResult { Online = false, Response = "Timeout" };
                    return new MonitorResult { Online = false, Response = r.Status.ToString() };
                }
            }
            catch (PingException) { return new MonitorResult { Online = false, Response = "Ping failed" }; }
            catch (Exception ex) { return new MonitorResult { Online = false, Response = ex.GetType().Name }; }
        }

        async Task<MonitorResult> CheckTcpAsync(string address, int port, int timeoutMs)
        {
            if (port <= 0) return new MonitorResult { Online = false, Response = "Port required" };
            var sw = Stopwatch.StartNew();
            try
            {
                using (var c = new TcpClient())
                {
                    var t = c.ConnectAsync(address, port);
                    var done = await Task.WhenAny(t, Task.Delay(timeoutMs));
                    sw.Stop();
                    if (done != t) return new MonitorResult { Online = false, Response = "Timeout" };
                    await t;
                    return new MonitorResult { Online = true, Response = "Connected (" + sw.ElapsedMilliseconds + " ms)", RxIncrement = 1 };
                }
            }
            catch (SocketException ex)
            {
                sw.Stop();
                if (ex.SocketErrorCode == SocketError.ConnectionRefused) return new MonitorResult { Online = false, Response = "Refused" };
                if (ex.SocketErrorCode == SocketError.TimedOut) return new MonitorResult { Online = false, Response = "Timeout" };
                return new MonitorResult { Online = false, Response = "TCP " + ex.SocketErrorCode };
            }
            catch { sw.Stop(); return new MonitorResult { Online = false, Response = "TCP failed" }; }
        }

        async Task<MonitorResult> CheckUdpAsync(string address, int port, int timeoutMs)
        {
            if (port <= 0) return new MonitorResult { Online = false, Response = "Port required" };
            try
            {
                using (var u = new UdpClient())
                {
                    u.Client.ReceiveTimeout = timeoutMs;
                    u.Connect(address, port);
                    var data = new byte[] { 0x47, 0x57 };
                    await u.SendAsync(data, data.Length);

                    var receiveTask = u.ReceiveAsync();
                    var done = await Task.WhenAny(receiveTask, Task.Delay(timeoutMs));
                    if (done == receiveTask)
                    {
                        var packet = await receiveTask;
                        return new MonitorResult { Online = true, Response = "Received", RxIncrement = 1 };
                    }

                    return new MonitorResult { Online = false, Response = "No Reply" };
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut) return new MonitorResult { Online = false, Response = "No Reply" };
                return new MonitorResult { Online = false, Response = "UDP " + ex.SocketErrorCode };
            }
            catch { return new MonitorResult { Online = false, Response = "UDP failed" }; }
        }
    }
}
