using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using SOACS.GridWatch.Models;

namespace SOACS.GridWatch.Services
{
    public class ConfigurationService
    {
        public void Save(string path, ObservableCollection<TargetNode> nodes)
        {
            var doc=new XmlDocument(); var root=doc.CreateElement("GridWatchConfig"); doc.AppendChild(root);
            foreach(var n in nodes){ var e=doc.CreateElement("Target"); root.AppendChild(e); Add(doc,e,"Enabled",n.Enabled.ToString()); Add(doc,e,"Name",n.Name); Add(doc,e,"Address",n.Address); Add(doc,e,"Monitor",n.Monitor.ToString()); Add(doc,e,"Port",n.Port.ToString()); Add(doc,e,"Profile",n.Profile.ToString()); }
            doc.Save(path);
        }
        public ObservableCollection<TargetNode> Load(string path)
        {
            var list=new ObservableCollection<TargetNode>(); if(!File.Exists(path)) return list; var doc=new XmlDocument(); doc.Load(path);
            foreach(XmlNode x in doc.SelectNodes("/GridWatchConfig/Target")){ var n=new TargetNode(); n.Enabled=B(x,"Enabled",true); n.Name=S(x,"Name","Target"); n.Address=S(x,"Address","127.0.0.1"); MonitorType mt; if(Enum.TryParse(S(x,"Monitor","ICMP"), out mt)) n.Monitor=mt; int port; if(int.TryParse(S(x,"Port","0"),out port)) n.Port=port; ProfileType pt; if(Enum.TryParse(S(x,"Profile","Normal"),out pt)) n.Profile=pt; list.Add(n); }
            return list;
        }
        static void Add(XmlDocument d, XmlElement p, string name, string value){ var e=d.CreateElement(name); e.InnerText=value??""; p.AppendChild(e); }
        static string S(XmlNode n,string name,string def){ var x=n.SelectSingleNode(name); return x==null?def:x.InnerText; }
        static bool B(XmlNode n,string name,bool def){ bool b; return bool.TryParse(S(n,name,def.ToString()), out b)?b:def; }
    }
}
