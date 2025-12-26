using System;

namespace MultiplayerInfrastructure.Registry
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
