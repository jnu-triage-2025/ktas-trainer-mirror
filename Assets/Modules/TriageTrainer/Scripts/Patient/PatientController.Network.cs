using MultiplayerInfrastructure.Registry;

namespace TriageTrainer.Entity
{
  public partial class PatientController : ISpawnedEntityIdentifierReceiver
  {
    private string _registeredEntityIdentifier;

    /// <summary>
    /// 엔티티 프리셋 스폰 시 인스턴스 식별자를 주입받는다(ISpawnedEntityIdentifierReceiver).
    /// 자가 등록(OnStartClient → RegisterPatientEntity) 전에 호출되면 그 식별자로 등록되며,
    /// 이미 등록된 뒤라면 재등록한다. 이로써 하나의 프리셋에서 patient_a/_b/_c 인스턴스를 구분한다.
    /// </summary>
    public void ApplySpawnedEntityIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      _identifier = identifier.Trim();

      // 이미 다른 식별자로 등록되어 있었다면 갱신.
      if (!string.IsNullOrWhiteSpace(_registeredEntityIdentifier)
          && !string.Equals(_registeredEntityIdentifier, _identifier, System.StringComparison.Ordinal))
      {
        UnregisterPatientEntity();
        RegisterPatientEntity();
      }
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      RegisterPatientEntity();
    }

    public override void OnStopClient()
    {
      UnregisterPatientEntity();
      base.OnStopClient();
    }

    private void OnDestroy()
    {
      UnregisterPatientEntity();
    }

    private void RegisterPatientEntity()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      _registeredEntityIdentifier = _identifier;
      Registry.RegisterEntity(
        _registeredEntityIdentifier,
        EntityType.Patient,
        gameObject,
        displayName: _identifier,
        ownerUserIdentifier: null,
        clientId: null,
        isNetworked: true);
    }

    private void UnregisterPatientEntity()
    {
      if (string.IsNullOrWhiteSpace(_registeredEntityIdentifier))
        return;

      Registry.UnregisterEntity(_registeredEntityIdentifier);
      _registeredEntityIdentifier = null;
    }
  }
}
