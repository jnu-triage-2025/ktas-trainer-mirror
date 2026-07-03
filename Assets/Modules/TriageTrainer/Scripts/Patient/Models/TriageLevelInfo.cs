using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Entity.Patient
{
  /// <summary>
  /// KTAS 트리아지 등급별 표준 색상/명칭 조회 유틸리티.
  ///
  /// <para>
  /// 트리아지 평가 UI(색상 사각형)와 인게임 환자 위 태그 표기가 동일한 색상/명칭 규칙을 공유하도록,
  /// 등급 → (색상, 한국어 명칭, 짧은 라벨)의 매핑을 단일 진실 공급원으로 제공한다.
  /// </para>
  /// </summary>
  public static class TriageLevelInfo
  {
    private readonly struct Entry
    {
      public readonly Color Color;
      public readonly Color TextColor;
      public readonly string DisplayName;
      public readonly string ShortLabel;

      public Entry(Color color, Color textColor, string displayName, string shortLabel)
      {
        Color = color;
        TextColor = textColor;
        DisplayName = displayName;
        ShortLabel = shortLabel;
      }
    }

    // KTAS 표준 색상. Level5(흰색)만 어두운 텍스트를 사용해 가독성을 확보한다.
    private static readonly Dictionary<TriageLevel, Entry> Entries = new()
    {
      { TriageLevel.Unassessed, new Entry(new Color(0.5f, 0.5f, 0.5f, 1f), Color.white, "미분류", "-") },
      { TriageLevel.Level1, new Entry(new Color(0.15f, 0.35f, 0.85f, 1f), Color.white, "1단계 소생", "KTAS 1") },
      { TriageLevel.Level2, new Entry(new Color(0.85f, 0.15f, 0.15f, 1f), Color.white, "2단계 긴급", "KTAS 2") },
      { TriageLevel.Level3, new Entry(new Color(0.95f, 0.80f, 0.10f, 1f), new Color(0.1f, 0.1f, 0.1f, 1f), "3단계 응급", "KTAS 3") },
      { TriageLevel.Level4, new Entry(new Color(0.20f, 0.70f, 0.25f, 1f), Color.white, "4단계 준응급", "KTAS 4") },
      { TriageLevel.Level5, new Entry(new Color(0.96f, 0.96f, 0.96f, 1f), new Color(0.1f, 0.1f, 0.1f, 1f), "5단계 비응급", "KTAS 5") },
    };

    /// <summary>플레이어가 선택 가능한 KTAS 등급 순서(1→5). Unassessed 는 제외한다.</summary>
    public static readonly TriageLevel[] SelectableLevels =
    {
      TriageLevel.Level1,
      TriageLevel.Level2,
      TriageLevel.Level3,
      TriageLevel.Level4,
      TriageLevel.Level5,
    };

    /// <summary>등급의 표준 배경 색상.</summary>
    public static Color GetColor(TriageLevel level) =>
      Entries.TryGetValue(level, out var e) ? e.Color : Entries[TriageLevel.Unassessed].Color;

    /// <summary>등급 색상 위에 얹을 텍스트 색상(가독성 고려).</summary>
    public static Color GetTextColor(TriageLevel level) =>
      Entries.TryGetValue(level, out var e) ? e.TextColor : Entries[TriageLevel.Unassessed].TextColor;

    /// <summary>등급의 한국어 명칭(예: "2단계 긴급").</summary>
    public static string GetDisplayName(TriageLevel level) =>
      Entries.TryGetValue(level, out var e) ? e.DisplayName : Entries[TriageLevel.Unassessed].DisplayName;

    /// <summary>등급의 짧은 라벨(예: "KTAS 2").</summary>
    public static string GetShortLabel(TriageLevel level) =>
      Entries.TryGetValue(level, out var e) ? e.ShortLabel : Entries[TriageLevel.Unassessed].ShortLabel;
  }
}
