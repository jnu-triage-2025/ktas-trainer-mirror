using System;
using MultiplayerInfrastructure.Tag;

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

    /// <summary>
    /// 팩이 활성화된 동안 유효한 플레이어 태그 정의. 태그를 추가/제거할 때 권한이 필요한지를 선언한다.
    /// 내장 정의(<c>Resources/Tag/*.tags.json</c>)와 같은 태그를 선언하면 팩이 활성인 동안 팩의 정의가 우선한다.
    /// </summary>
    public PlayerTagDefinition[] tagDefinitions;
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
