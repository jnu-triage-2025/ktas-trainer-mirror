using System.Collections;
using System.Reflection;
using MultiplayerInfrastructure.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Tests.UI
{
  public sealed class UIPickingRegressionTests
  {
    private sealed class PickingWindow : EditorWindow { }

    [UnityTest]
    public IEnumerator RestoredDialoguePassesClicksOutsideItsVisiblePanel()
    {
      var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
        "Assets/Modules/MultiplayerInfrastructure/UIDocuments/DialoguePanelUI.uxml");
      var window = ScriptableObject.CreateInstance<PickingWindow>();
      var controllerObject = new GameObject("Dialogue picking test");
      try
      {
        window.position = new Rect(0, 0, 1000, 700);
        window.Show();
        var root = window.rootVisualElement;
        var selectionButton = new Button { name = "selection-button" };
        Stretch(selectionButton);
        root.Add(selectionButton);

        var documentRoot = new VisualElement { pickingMode = PickingMode.Ignore };
        Stretch(documentRoot);
        asset.CloneTree(documentRoot);
        root.Add(documentRoot);
        var dialogue = documentRoot.Q<DialogueElement>("dialogue-element");
        dialogue.Show();

        var controller = controllerObject.AddComponent<DialoguePanelUIController>();
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(DialoguePanelUIController).GetField("_root", fields).SetValue(controller, documentRoot);
        typeof(DialoguePanelUIController).GetField("_dialoguePanel", fields).SetValue(controller, dialogue);

        // Restoring interactive dialogue after a noninteractive message must not cover another document's choices.
        UIDocumentInteractionPolicy.SetSubtreePickingMode(documentRoot, PickingMode.Ignore);
        typeof(DialoguePanelUIController).GetMethod("RestoreInteractivePresentation", fields).Invoke(controller, null);
        yield return null;
        yield return null;

        AssertHitsButton(root, selectionButton, root.worldBound.center, "Dialogue document background");
        Assert.That(dialogue.worldBound.height, Is.GreaterThan(0));
        var hit = root.panel.Pick(dialogue.worldBound.center);
        Assert.That(hit == dialogue || dialogue.Contains(hit), Is.True, "Visible dialogue must remain interactive.");
      }
      finally
      {
        Object.DestroyImmediate(controllerObject);
        window.Close();
      }
    }

    [UnityTest]
    public IEnumerator RecreatedHotbarPassesThroughEmptyAreasAndKeepsSlotSelection()
    {
      var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
        "Assets/Modules/MultiplayerInfrastructure/UIDocuments/HotbarUI.uxml");
      Assert.That(asset, Is.Not.Null);

      var window = ScriptableObject.CreateInstance<PickingWindow>();
      try
      {
        window.position = new Rect(0, 0, 1000, 700);
        window.Show();
        var root = window.rootVisualElement;
        var underlyingButton = new Button { name = "underlying-button" };
        Stretch(underlyingButton);
        root.Add(underlyingButton);

        for (int entry = 0; entry < 2; entry++)
        {
          // Put the recreated document above other UI, as in the standalone re-entry failure.
          var hotbarRoot = new VisualElement { pickingMode = PickingMode.Ignore };
          Stretch(hotbarRoot);
          asset.CloneTree(hotbarRoot);
          root.Add(hotbarRoot);

          var hotbar = hotbarRoot.Q<HotbarControl>("hotbar-root");
          var itemName = hotbarRoot.Q<Label>("hotbar-item-name");
          itemName.text = "Item name";
          itemName.RemoveFromClassList("hotbar__item-name--hidden");
          yield return null;
          yield return null;

          Assert.That(hotbar.worldBound.width, Is.GreaterThan(0));
          Assert.That(itemName.worldBound.height, Is.GreaterThan(0));
          AssertHitsButton(root, underlyingButton, root.worldBound.center, "Fullscreen container");
          AssertHitsButton(root, underlyingButton,
            new Vector2(root.worldBound.xMin + 4, hotbar.worldBound.center.y), "Empty hotbar margin");
          AssertHitsButton(root, underlyingButton, itemName.worldBound.center, "Item-name label");

          var slot = hotbar.Q<VisualElement>("hotbar-slot-1");
          var hit = root.panel.Pick(slot.worldBound.center);
          Assert.That(hit == slot || slot.Contains(hit), Is.True, "Hotbar slots must remain clickable.");
          int selectedSlot = -1;
          hotbar.OnSlotSelected += index => selectedSlot = index;
          using (var down = PointerDownEvent.GetPooled(new Event
          {
            type = EventType.MouseDown,
            button = 0,
            mousePosition = slot.worldBound.center
          }))
          {
            down.target = hit;
            hit.SendEvent(down);
          }
          Assert.That(selectedSlot, Is.EqualTo(1));

          hotbarRoot.RemoveFromHierarchy();
          yield return null;
        }
      }
      finally
      {
        window.Close();
      }
    }

    private static void AssertHitsButton(VisualElement root, Button button, Vector2 position, string area)
    {
      var hit = root.panel.Pick(position);
      Assert.That(hit == button || button.Contains(hit), Is.True,
        $"{area} intercepted the click: {hit}");
    }

    private static void Stretch(VisualElement element)
    {
      element.style.position = Position.Absolute;
      element.style.left = 0;
      element.style.right = 0;
      element.style.top = 0;
      element.style.bottom = 0;
    }
  }
}
