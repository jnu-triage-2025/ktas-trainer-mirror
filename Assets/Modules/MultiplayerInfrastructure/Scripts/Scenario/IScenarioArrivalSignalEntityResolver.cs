using MultiplayerInfrastructure.Entity;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>트리거에 닿은 운반체가 대신 보고해야 할 시나리오 엔티티를 제공한다.</summary>
  public interface IScenarioArrivalSignalEntityResolver
  {
    IScenarioIdentifiedEntity ResolveArrivalSignalEntity();
  }
}
