using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.InteractableEntity
{
  /// <summary>
  /// 인터렉션 레지스트리 디버그 창. 플레이 중 등록 항목, 출처, 조건 절, 관찰자별 판정 결과, 오버라이드를 보여 주고
  /// 오버라이드를 켜고 끌 수 있다. 편집은 Unity 에디터에서만 가능하며 프리팹·씬에 직렬화되지 않는다.
  /// 클라이언트 피어에서의 편집은 중계기를 통해 서버로 위임된다.
  /// </summary>
  public sealed class InteractionRegistryWindow : EditorWindow
  {
    private Vector2 _scroll;
    private string _filter = string.Empty;
    private bool _onlySelected;
    private bool _showHidden = true;

    [MenuItem("Tools/Multiplayer Infrastructure/Interaction Registry")]
    public static void Open()
    {
      var window = GetWindow<InteractionRegistryWindow>("Interaction Registry");
      window.minSize = new Vector2(520f, 320f);
      window.Show();
    }

    private void OnEnable()
    {
      InteractionRegistry.HintRefreshRequested += Repaint;
      Selection.selectionChanged += Repaint;
      EditorApplication.playModeStateChanged += HandlePlayModeChanged;
    }

    private void OnDisable()
    {
      InteractionRegistry.HintRefreshRequested -= Repaint;
      Selection.selectionChanged -= Repaint;
      EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
    }

    private void HandlePlayModeChanged(PlayModeStateChange change) => Repaint();

    private void OnGUI()
    {
      if (!Application.isPlaying)
      {
        EditorGUILayout.HelpBox("인터렉션 레지스트리는 런타임 상태입니다. 플레이 중에만 표시됩니다.", MessageType.None);
        return;
      }

      DrawToolbar();

      var localPlayer = Registry.Registry.GetFirstEntityComponent<PlayerController>(
        EntityType.Player, each => each != null && each.IsOwner);
      string viewerLabel = localPlayer != null ? $"{localPlayer.UserIdentifier}" : "(no local player)";
      EditorGUILayout.LabelField("Observer", viewerLabel);
      EditorGUILayout.LabelField("Entries", InteractionRegistry.Count.ToString());

      var selectedEntities = _onlySelected ? CollectSelectedEntityIdentifiers() : null;

      _scroll = EditorGUILayout.BeginScrollView(_scroll);
      foreach (var group in InteractionRegistry.AllEntries
                 .GroupBy(entry => entry.Address.EntityIdentifier, StringComparer.Ordinal)
                 .OrderBy(group => group.Key, StringComparer.Ordinal))
      {
        if (selectedEntities != null && !selectedEntities.Contains(group.Key))
          continue;

        var entries = group.Where(MatchesFilter).OrderBy(entry => entry.Sequence).ToList();
        if (entries.Count == 0)
          continue;

        EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
        foreach (var entry in entries)
          DrawEntry(entry, localPlayer);
        EditorGUILayout.Space();
      }
      EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
      EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
      _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
      _onlySelected = GUILayout.Toggle(_onlySelected, "Selected only", EditorStyles.toolbarButton, GUILayout.Width(100f));
      _showHidden = GUILayout.Toggle(_showHidden, "Show hidden", EditorStyles.toolbarButton, GUILayout.Width(90f));
      if (GUILayout.Button("Reset all overrides", EditorStyles.toolbarButton, GUILayout.Width(130f)))
      {
        foreach (var entry in InteractionRegistry.AllEntries.ToList())
          InteractionRegistry.SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Reset, InteractionVisibilityScope.Global);
      }
      EditorGUILayout.EndHorizontal();
    }

    private bool MatchesFilter(InteractionRegistryEntry entry)
    {
      if (string.IsNullOrWhiteSpace(_filter))
        return true;
      return entry.Address.Key.IndexOf(_filter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0
             || (entry.Definition?.Display?.Text?.IndexOf(_filter.Trim(), StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
    }

    private void DrawEntry(InteractionRegistryEntry entry, PlayerController localPlayer)
    {
      string reason;
      bool visible = localPlayer != null
        ? InteractionRegistry.IsVisible(entry, localPlayer, out reason)
        : InteractionRegistry.IsVisible(entry, ScenarioConditionContext.Global, out reason);
      if (!visible && !_showHidden)
        return;

      EditorGUILayout.BeginVertical("box");
      EditorGUILayout.BeginHorizontal();
      var style = new GUIStyle(EditorStyles.label) { normal = { textColor = visible ? new Color(0.35f, 0.8f, 0.4f) : new Color(0.85f, 0.45f, 0.4f) } };
      EditorGUILayout.LabelField(visible ? "●" : "○", style, GUILayout.Width(16f));
      EditorGUILayout.LabelField(entry.Address.InteractionIdentifier, EditorStyles.boldLabel);
      EditorGUILayout.LabelField(entry.Definition?.Kind.ToString() ?? "-", GUILayout.Width(100f));
      EditorGUILayout.LabelField(entry.Source, GUILayout.Width(160f));
      EditorGUILayout.EndHorizontal();

      var definition = entry.Definition;
      if (definition != null)
      {
        EditorGUILayout.LabelField("Display", definition.Display?.Text ?? "-");
        EditorGUILayout.LabelField("Handler", entry.Handler != null ? entry.Handler.GetType().Name + (entry.HandlerIsGeneric ? " (generic)" : string.Empty) : "(none)");
        EditorGUILayout.LabelField("Initial / AfterInteract", $"{definition.InitialVisible} / {definition.AfterInteract}");
        if (definition.HasVisibilityConditions)
        {
          EditorGUILayout.LabelField($"Conditions ({definition.MatchMode})");
          for (int i = 0; i < definition.VisibilityConditions.Count; i++)
            EditorGUILayout.LabelField("  " + ScenarioInspectorView.DescribeCondition(definition.VisibilityConditions[i]));
        }
        if (entry.RegisteredOutsideInitCycle)
          EditorGUILayout.HelpBox("초기화 사이클 밖에서 등록되었습니다.", MessageType.Warning);
      }
      EditorGUILayout.LabelField("Result", reason ?? "-");

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Override", GUILayout.Width(60f));
      if (GUILayout.Button("Show all", EditorStyles.miniButtonLeft))
        InteractionRegistry.SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Show, InteractionVisibilityScope.Global);
      if (GUILayout.Button("Hide all", EditorStyles.miniButtonMid))
        InteractionRegistry.SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Hide, InteractionVisibilityScope.Global);
      if (GUILayout.Button("Reset all", EditorStyles.miniButtonRight))
        InteractionRegistry.SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Reset, InteractionVisibilityScope.Global);
      using (new EditorGUI.DisabledScope(localPlayer == null || string.IsNullOrWhiteSpace(localPlayer.UserIdentifier)))
      {
        var me = localPlayer != null ? new[] { localPlayer.UserIdentifier } : Array.Empty<string>();
        if (GUILayout.Button("Show me", EditorStyles.miniButtonLeft))
          InteractionRegistry.SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Show, InteractionVisibilityScope.Player, me);
        if (GUILayout.Button("Hide me", EditorStyles.miniButtonMid))
          InteractionRegistry.SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Hide, InteractionVisibilityScope.Player, me);
        if (GUILayout.Button("Reset me", EditorStyles.miniButtonRight))
          InteractionRegistry.SetVisibilityOverride(entry.Address, InteractionVisibilityOverride.Reset, InteractionVisibilityScope.Player, me);
      }
      EditorGUILayout.EndHorizontal();
      EditorGUILayout.EndVertical();
    }

    private static HashSet<string> CollectSelectedEntityIdentifiers()
    {
      var result = new HashSet<string>(StringComparer.Ordinal);
      foreach (var selected in Selection.gameObjects)
      {
        if (selected == null)
          continue;
        foreach (var pair in Registry.Registry.GetAllEntities())
        {
          var target = pair.Value?.GameObject;
          if (target == null)
            continue;
          if (target == selected || selected.transform.IsChildOf(target.transform) || target.transform.IsChildOf(selected.transform))
            result.Add(pair.Key);
        }
      }
      return result;
    }
  }
}
