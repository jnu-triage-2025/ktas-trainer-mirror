using MultiplayerInfrastructure.Registry;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    private string _registeredEntityIdentifier;

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
        EntityType.Npc,
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
