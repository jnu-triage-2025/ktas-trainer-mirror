using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [SerializeField] private GameEscapeMenuUIController _escapeMenuUIController;

    private void OnClientStart_EscapeMenu()
    {
      EnsureEscapeMenuController();
    }

    private void EnsureEscapeMenuController()
    {
      if (_escapeMenuUIController.IsUnityNull())
      {
        _escapeMenuUIController = Registry.Registry.Get<GameEscapeMenuUIController>(RegistryType.UI, Registry.Registry.TypeKey<GameEscapeMenuUIController>());
      }
    }
  }
}
