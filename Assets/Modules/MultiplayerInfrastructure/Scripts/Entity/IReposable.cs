namespace MultiplayerInfrastructure.Entity
{
  // 자세를 다시 취할 수 있음
  public interface IReposable
  {
    int Weight { get; }

    bool IsMovingPatientBedAttached { get; }
    bool IsPlayerAttached { get; }

    void OnMovingPatientBedAttachedEnter();
    void OnMovingPatientBedAttachedExit();
    void OnPlayerAttachedEnter();
    void OnPlayerAttachedExit();
  }
}
