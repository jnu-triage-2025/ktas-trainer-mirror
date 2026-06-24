using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects
{
  [CreateAssetMenu(
    fileName = "New EntityPreset Registry Requirements SO",
    menuName = "TriageTrainer/Multiplayer Infrastructure/EntityPreset Registry Requirements SO")]
  public class EntityPresetRegistryRequirementsSO : ScriptableObject
  {
    public EntityPresetRegistryRequirement[] entityPresetRegistryRequirements;

#if UNITY_EDITOR
    // ── 비런타임(에디터) 검증 ────────────────────────────────────────────────
    // 인스펙터에서 값이 바뀔 때 자동 검증하고, 우클릭 ContextMenu 로 수동 검증도 가능하다.
    // 새 모델에서는 하위를 "다른 EntityPreset 식별자" 로 참조하므로, 참조 무결성/순환/누락을 검사한다.

    private void OnValidate() => ValidatePresetsEditor(logWhenOk: false);

    [ContextMenu("Validate Presets (Editor)")]
    private void ValidatePresetsContextMenu() => ValidatePresetsEditor(logWhenOk: true);

    /// <summary>
    /// 각 프리셋 요구사항을 에디터에서 검증한다.
    /// 검사 항목:
    ///  - identifier/prefab 누락, identifier 중복
    ///  - 각 childReferences 의 childPresetIdentifier 가 비어있지 않은지
    ///  - 해당 하위 프리셋 식별자가 이 SO 내에 정의되어 있는지(없으면 다른 SO/코드 등록 가능성을 안내)
    ///  - 자기 자신을 하위로 참조하는 순환 여부
    /// </summary>
    private void ValidatePresetsEditor(bool logWhenOk)
    {
      if (entityPresetRegistryRequirements == null)
        return;

      int problems = 0;
      var seen = new HashSet<string>(System.StringComparer.Ordinal);
      var knownIdentifiers = new HashSet<string>(System.StringComparer.Ordinal);

      foreach (var r in entityPresetRegistryRequirements)
      {
        if (!string.IsNullOrWhiteSpace(r.identifier))
          knownIdentifiers.Add(r.identifier);
      }

      for (int i = 0; i < entityPresetRegistryRequirements.Length; i++)
      {
        var req = entityPresetRegistryRequirements[i];
        string tag = $"EntityPreset[{i}] '{req.identifier}'";

        if (string.IsNullOrWhiteSpace(req.identifier))
        {
          Debug.LogWarning($"[EntityPresetSO] EntityPreset[{i}] identifier 가 비어 있습니다.", this);
          problems++;
          continue;
        }

        if (!seen.Add(req.identifier))
        {
          Debug.LogWarning($"[EntityPresetSO] {tag} identifier 가 중복됩니다.", this);
          problems++;
        }

        if (req.prefab == null)
        {
          Debug.LogWarning($"[EntityPresetSO] {tag} prefab 이 비어 있습니다.", this);
          problems++;
          continue;
        }

        if (req.childReferences == null || req.childReferences.Length == 0)
          continue;

        foreach (var child in req.childReferences)
        {
          if (string.IsNullOrWhiteSpace(child.childPresetIdentifier))
          {
            Debug.LogWarning($"[EntityPresetSO] {tag} childReferences 에 빈 childPresetIdentifier 가 있습니다.", this);
            problems++;
            continue;
          }

          if (string.Equals(child.childPresetIdentifier, req.identifier, System.StringComparison.Ordinal))
          {
            Debug.LogWarning($"[EntityPresetSO] {tag} 가 자기 자신을 하위 프리셋으로 참조합니다(순환).", this);
            problems++;
            continue;
          }

          if (!knownIdentifiers.Contains(child.childPresetIdentifier))
          {
            Debug.LogWarning(
              $"[EntityPresetSO] {tag} 의 하위 프리셋 '{child.childPresetIdentifier}' 가 이 SO 안에 정의되어 있지 않습니다. " +
              "다른 SO/코드에서 등록되는 프리셋이면 무시해도 되지만, 오타가 아닌지 확인하세요.", this);
            problems++;
          }
        }
      }

      if (logWhenOk && problems == 0)
      {
        Debug.Log($"[EntityPresetSO] 검증 완료 — 문제 없음 ({entityPresetRegistryRequirements.Length} 항목).", this);
      }
    }
#endif
  }
}
