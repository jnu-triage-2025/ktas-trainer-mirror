namespace MultiplayerInfrastructure.Entity
{
  // 자세를 다시 취할 수 있음
  public interface IReposable
  {
    public int Weight { get; }

    public bool IsMovingPatientBedAttached { get; }
    public bool IsPlayerAttached { get; }

    public void OnMovingPatientBedAttachedEnter();
    public void OnMovingPatientBedAttachedExit();
    public void OnPlayerAttachedEnter();
    public void OnPlayerAttachedExit();
  }
}
