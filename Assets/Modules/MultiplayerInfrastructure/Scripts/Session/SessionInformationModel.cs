using System;

namespace MultiplayerInfrastructure.Session
{
  [Serializable]
  public class SessionInformationModel
  {
    public string Name { get; set; }
    public string Address { get; set; }
    public ushort Port { get; set; }
    public DateTime LastSeenUtc { get; set; }

    public SessionInformationModel(string address, ushort port, string? sessionName = null, DateTime? lastSeenUtc = null)
    {
      Address = address;
      Port = port;
      Name = sessionName ?? "Unknown Session";
      LastSeenUtc = lastSeenUtc ?? DateTime.UtcNow;
    }

    public override string ToString() => $"{Name} ({Address}:{Port})";
  }
}
