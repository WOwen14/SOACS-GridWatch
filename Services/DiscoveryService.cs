using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SOACS.GridWatch.Services
{
    public class DiscoveryDevice { public bool Add { get; set; } public string Address { get; set; } public string Hostname { get; set; } public string Mac { get; set; } }
    public class DiscoveryService
    {
        public async Task<List<DiscoveryDevice>> DiscoverLocalAsync()
        {
            var local = GetLocalIPv4();
            var results = new List<DiscoveryDevice>();
            if (local == null) return results;
            string prefix = local.Substring(0, local.LastIndexOf('.') + 1);
            var tasks = new List<Task<DiscoveryDevice>>();
            for(int i=1;i<255;i++) tasks.Add(PingHostAsync(prefix+i));
            var found = await Task.WhenAll(tasks);
            return found.Where(x=>x!=null).OrderBy(x=>ParseLast(x.Address)).ToList();
        }
        async Task<DiscoveryDevice> PingHostAsync(string ip)
        {
            try { using(var p=new Ping()) { var r=await p.SendPingAsync(ip,250); if(r.Status!=IPStatus.Success) return null; return new DiscoveryDevice{Add=true,Address=ip,Hostname=TryDns(ip),Mac=""}; } } catch { return null; }
        }
        static string TryDns(string ip){ try { return Dns.GetHostEntry(ip).HostName; } catch { return ""; } }
        static int ParseLast(string ip){ int n; return int.TryParse(ip.Split('.').Last(), out n)?n:0; }
        static string GetLocalIPv4(){ foreach(var ni in NetworkInterface.GetAllNetworkInterfaces()){ if(ni.OperationalStatus!=OperationalStatus.Up) continue; if(ni.NetworkInterfaceType==NetworkInterfaceType.Loopback) continue; var props=ni.GetIPProperties(); foreach(var ua in props.UnicastAddresses){ if(ua.Address.AddressFamily==AddressFamily.InterNetwork){ var s=ua.Address.ToString(); if(!s.StartsWith("169.254.")) return s; } } } return null; }
    }
}
