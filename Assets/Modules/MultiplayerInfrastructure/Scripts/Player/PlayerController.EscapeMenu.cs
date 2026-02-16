using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [SerializeField] private GameEscapeMenuUIController _escapeMenuUIController;
    [SerializeField] private bool _escapeMenuEventsRegistered = false;

    private void OnClientStart_EscapeMenu()
    {
      EnsureEscapeMenuController();
    }

    private void EnsureEscapeMenuController()
    {
      if (_escapeMenuUIController.IsUnityNull())
      {
        _escapeMenuEventsRegistered = false;
        _escapeMenuUIController = Registry.Registry.Get<GameEscapeMenuUIController>(RegistryType.UI, Registry.Registry.TypeKey<GameEscapeMenuUIController>());
      }

      if (_escapeMenuUIController.IsUnityNull()) return;
      if (_escapeMenuEventsRegistered) return;

      _escapeMenuUIController.OverlayPushed += EscapeMenuOnOverlayPushed;
      _escapeMenuUIController.OverlayPopped += EscapeMenuOnOverlayPopped;
      _escapeMenuEventsRegistered = true;
    }

    private void EscapeMenuOnOverlayPushed()
    {
      EnterUIOverlayMode();
      Debug.Log("[PlayerController.EscapeMenu] Escape menu opened, entered UI overlay mode.");
    }

    private void EscapeMenuOnOverlayPopped()
    {
      ExitUIOverlayMode();
    }
  }
}
