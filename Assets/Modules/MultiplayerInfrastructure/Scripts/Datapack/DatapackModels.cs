using System;

namespace MultiplayerInfrastructure.Datapack
{
  [Serializable]
  public class DatapackDefinition
  {
    public string packId;
    public string displayName;
    public DatapackGameRule[] gameRules;
    public DatapackCommandAlias[] commandAliases;
    public DatapackPeriodicCommand[] periodicCommands;
    public DatapackEventCommandHandler[] eventHandlers;
  }

  [Serializable]
  public class DatapackPeriodicCommand
  {
    public string command;
    public float intervalSeconds = 1f;
    public bool runImmediately = true;
  }

  [Serializable]
  public class DatapackEventCommandHandler
  {
    public string eventIdentifier;
    public string command;
  }

  [Serializable]
  public class DatapackGameRule
  {
    public string name;
    public string value;
  }

  [Serializable]
  public class DatapackCommandAlias
  {
    public string name;
    public string target;
  }
}
