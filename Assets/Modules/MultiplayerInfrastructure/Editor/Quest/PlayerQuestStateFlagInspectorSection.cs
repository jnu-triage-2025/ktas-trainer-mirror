using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Quest;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Quest
{
  /// <summary>
  /// PlayerController 인스펙터에 그리는 퀘스트 상태 플래그 풀 구역.
  ///
  /// <para>
  /// 풀은 런타임 상태라서 직렬화 필드로 미러링하지 않고 <see cref="PlayerQuestStateFlagService"/>를
  /// 직접 읽는다. 미러를 두면 갱신 주기만큼 화면이 늦고, 되돌려 쓰는 경로까지 생겨 조작 결과가
  /// 다음 갱신에 지워질 수 있다.
  /// </para>
  ///
  /// <para>
  /// 조작은 서비스가 허용하는 컨텍스트(서버 또는 오프라인)에서만 열린다. 클라이언트 피어에서 임의로
  /// 바꾸면 서버 스냅샷이 복제되는 순간 되돌아가므로, 그 상황에서는 읽기 전용으로 두고 이유를 적는다.
  /// </para>
  /// </summary>
  public static class PlayerQuestStateFlagInspectorSection
  {
    /// <summary>플레이어별 입력 상태. 인스펙터는 매 프레임 다시 그려지므로 밖에 들고 있는다.</summary>
    private static readonly Dictionary<string, string> _pendingFlagByPlayer = new(StringComparer.Ordinal);

    private static readonly List<string> _flagBuffer = new();

    public static void Draw(PlayerController player)
    {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Quest State Flags", EditorStyles.boldLabel);

      if (!Application.isPlaying)
      {
        EditorGUILayout.HelpBox(
          "퀘스트 상태 플래그 풀은 런타임 상태입니다. 플레이 중에만 표시·조작할 수 있습니다.",
          MessageType.None);
        return;
      }

      string identifier = player != null ? player.UserIdentifier : null;
      if (string.IsNullOrWhiteSpace(identifier))
      {
        EditorGUILayout.HelpBox(
          "이 PlayerController에 아직 사용자 식별자가 없습니다. 스폰이 끝나면 표시됩니다.",
          MessageType.Info);
        return;
      }

      bool canMutate = PlayerQuestStateFlagService.CanMutate;
      if (!canMutate)
      {
        EditorGUILayout.HelpBox(
          "플래그 변경은 서버 권위입니다. 이 피어는 클라이언트라 읽기만 할 수 있습니다.",
          MessageType.Info);
      }

      DrawCurrentFlags(identifier, canMutate);

      using (new EditorGUI.DisabledScope(!canMutate))
      {
        DrawFlagAdder(identifier);
        DrawKnownFlagPicker(identifier);
      }
    }

    private static void DrawCurrentFlags(string identifier, bool canMutate)
    {
      _flagBuffer.Clear();
      _flagBuffer.AddRange(PlayerQuestStateFlagService.GetFlags(identifier));
      _flagBuffer.Sort(StringComparer.Ordinal);

      if (_flagBuffer.Count == 0)
      {
        EditorGUILayout.LabelField("보유 플래그", "(none)");
        return;
      }

      EditorGUILayout.LabelField("보유 플래그", $"{_flagBuffer.Count}개");
      using (new EditorGUI.IndentLevelScope())
      {
        for (int i = 0; i < _flagBuffer.Count; i++)
        {
          string flag = _flagBuffer[i];
          using (new EditorGUILayout.HorizontalScope())
          {
            EditorGUILayout.SelectableLabel(flag, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            using (new EditorGUI.DisabledScope(!canMutate))
            {
              if (GUILayout.Button("제거", GUILayout.Width(48f)))
                PlayerQuestStateFlagService.Unset(identifier, flag);
            }
          }
        }
      }

      using (new EditorGUI.DisabledScope(!canMutate))
      {
        if (GUILayout.Button("이 플레이어의 플래그 모두 제거"))
        {
          for (int i = 0; i < _flagBuffer.Count; i++)
            PlayerQuestStateFlagService.Unset(identifier, _flagBuffer[i]);
        }
      }
    }

    /// <summary>임의 식별자를 직접 넣는 입력란. 풀은 어휘에 없는 문자열도 그대로 담는다.</summary>
    private static void DrawFlagAdder(string identifier)
    {
      _pendingFlagByPlayer.TryGetValue(identifier, out string pending);

      using (new EditorGUILayout.HorizontalScope())
      {
        pending = EditorGUILayout.TextField("추가할 플래그", pending ?? string.Empty);
        _pendingFlagByPlayer[identifier] = pending;

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(pending)))
        {
          if (GUILayout.Button("추가", GUILayout.Width(48f)))
          {
            PlayerQuestStateFlagService.Set(identifier, pending);
            _pendingFlagByPlayer[identifier] = string.Empty;
            GUI.FocusControl(null);
          }
        }
      }
    }

    /// <summary>실행 중인 콘텐츠가 등록한 플래그 어휘를 보유 여부 토글로 보여준다.</summary>
    private static void DrawKnownFlagPicker(string identifier)
    {
      var known = PlayerQuestStateFlagService.KnownFlags;
      if (known == null || known.Count == 0)
        return;

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("시나리오가 사용하는 플래그", EditorStyles.miniBoldLabel);

      using (new EditorGUI.IndentLevelScope())
      {
        foreach (string flag in known)
        {
          bool had = PlayerQuestStateFlagService.Has(identifier, flag);
          bool has = EditorGUILayout.ToggleLeft(flag, had);
          if (has == had)
            continue;

          if (has)
            PlayerQuestStateFlagService.Set(identifier, flag);
          else
            PlayerQuestStateFlagService.Unset(identifier, flag);
        }
      }
    }
  }
}
