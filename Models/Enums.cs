namespace SOACS.GridWatch.Models
{
    public enum MonitorType { ICMP, TCP, UDP }
    public enum ProfileType { Critical, High, Normal, Low }
    public enum NodeState { Disabled, Unknown, Online, Warning, Offline, Checking }
}
