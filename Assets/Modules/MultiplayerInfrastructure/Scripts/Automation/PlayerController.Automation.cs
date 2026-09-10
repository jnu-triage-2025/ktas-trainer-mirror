#if UNITY_E2E || UNITY_EDITOR
using Newtonsoft.Json.Linq;
using System.Linq;
using MultiplayerInfrastructure.InteractableEntity;
namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    internal JObject AutomationInventory()
    {
      bool available = IsOwner || IsServerInitialized;
      var slots = new JArray();
      if (available && InventorySlots != null)
        for (int index = 0; index < InventorySlots.Count; index++)
        {
          var item = InventorySlots[index]?.ItemInstance;
          if (item == null) continue;
          var value = new JObject { ["slot"] = index, ["itemId"] = item.CurrentIdentifier,
            ["count"] = item.CurrentStackCount, ["description"] = item.CurrentDescription };
          if (item.CurrentIdentifier == "checklist_paper")
          {
            string serialized = item.GetCurrentSerializedDerivedAttributes();
            try { value["checklist"] = string.IsNullOrEmpty(serialized)
              ? new JObject { ["CompletedIdentifiers"] = new JArray() } : JObject.Parse(serialized); }
            catch (System.Exception error) { value["observationError"] = error.GetType().Name; }
          }
          slots.Add(value);
        }
      return new JObject { ["available"] = available, ["slots"] = slots };
    }
    internal JArray AutomationInteractions()
    {
      // Keep the device-input surface and the exported observation on the
      // same interaction generation. Scenario signals can replace a quest
      // binding (for example gauze -> plaster) between normal hint refreshes;
      // without this sync the MCP sees the new target but scroll/F still
      // operates on the stale or empty UI list.
      RefreshInteractableHintsNow();
      var available = CollectAvailableInteracts(_detector?.Nearby);
      if (_detector != null) KeepNearestExclusiveInteracts(available, _detector.DetectionPosition);
      OrderInteractsByDisplayPriority(available);
      // CollectAvailableInteracts can rebuild interaction wrapper objects for
      // this observation.  The hint UI retains the semantic selected slot, so
      // reference equality against the freshly collected wrappers can report
      // no selection even while the player has one.  Export the stable index.
      int selectedIndex = _interactableHintUI?.GetSelectedIndex() ?? -1;
      return new JArray(available.Select((interaction,index) => new JObject {
        ["index"] = index, ["text"] = interaction.DisplayText, ["selected"] = index == selectedIndex,
        ["entityId"] = (interaction as IQuestPresentationTarget)?.PresentationEntityIdentifier,
        ["interactionId"] = (interaction as IQuestPresentationTarget)?.InteractionIdentifier
      }));
    }
    internal void AutomationSelectInteraction(int index)
    {
      RefreshInteractableHintsNow();
      _interactableHintUI?.SetSelected(index);
      RefreshLocalInteractionFocus();
    }
    internal void AutomationExecuteInteraction(int index, string expectedEntityId = null, string expectedInteractionId = null)
    {
      AutomationSelectInteraction(index);
      // Observation and input are separate frames. Never execute a different
      // nearby target when that slot has changed in the meantime.
      if (expectedEntityId != null || expectedInteractionId != null)
      {
        var target = _interactableHintUI?.GetSelected() as IQuestPresentationTarget;
        if (target == null
          || (expectedEntityId != null && target.PresentationEntityIdentifier != expectedEntityId)
          || (expectedInteractionId != null && target.InteractionIdentifier != expectedInteractionId))
          throw new System.InvalidOperationException("TARGET_NOT_INTERACTABLE");
        if ((_dialoguePanelUIController != null && UI.UIOverlayStack.IsTop(_dialoguePanelUIController))
          || (_interactableHintUI != null && _interactableHintUI.IsDialogueMode))
          throw new System.InvalidOperationException("STATE_CONFLICT");
      }
      TryInteractWithSelection();
    }
    internal void AutomationSelectHotbarSlot(int index)
    {
      if (_hotbarUI == null)
        _hotbarUI = Registry.Registry.Get<UI.HotbarUIController>(Registry.RegistryType.UI,
          Registry.Registry.TypeKey<UI.HotbarUIController>());
      if (_hotbarUI == null || index < 0 || index >= _hotbarUI.SlotCount)
        throw new System.ArgumentOutOfRangeException(nameof(index));
      _hotbarUI.SetSelectedIndex(index);
      // Inventory mutation can replace a same-index ItemInstance without a
      // slot-change event, so always resolve after an automation selection.
      ResolveHandledItem();
    }
    internal int AutomationSelectedHotbarSlot => _hotbarUI?.SelectedSlot ?? -1;
    internal JObject AutomationDialogueBinding => new JObject {
      ["bound"] = _dialoguePanelUIController != null,
      ["isTop"] = _dialoguePanelUIController != null && UI.UIOverlayStack.IsTop(_dialoguePanelUIController),
      ["controllerId"] = _dialoguePanelUIController == null ? 0 : _dialoguePanelUIController.GetInstanceID(),
      ["enabled"] = isActiveAndEnabled
    };
    internal JObject AutomationBindings => new JObject {
      ["chat"] = _keyToggleChat.ToString(), ["command"] = _keyToggleCommand.ToString(),
      ["inventory"] = _keyToggleInventory.ToString(), ["quest"] = _keyOpenQuestUI.ToString(),
      ["interact"] = _keyInteractInteractableObject.ToString(), ["menu"] = _keyOpenEscMenu.ToString(),
      ["dialogueAdvance"] = _keyAdvanceDialogue.ToString(), ["dialogueConfirm"] = _keyConfirmDialogue.ToString()
    };
    internal bool AutomationScriptedMovement => _scriptedMovementActive;
    internal float AutomationCameraPitch => _rotationX;
    internal float AutomationRotationSensitivity => _rotatingSpeed;
  }
}
#endif
