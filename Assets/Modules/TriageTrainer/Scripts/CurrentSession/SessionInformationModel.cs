using System;

namespace Modules.TriageTrainer.Scripts.Connection
{
  [Serializable]
  public class SessionInformationModel
  {
    public string address;
    public ushort port;

    public SessionInformationModel(string address, ushort port)
    {
      this.address = address;
      this.port = port;
    }
  }
}
