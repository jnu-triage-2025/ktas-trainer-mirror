using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects
{
  [CreateAssetMenu(
    fileName = "New EntityPreset Registry Requirements SO",
    menuName = "Triage Trainer/Multiplayer Infrastructure/EntityPreset Registry Requirements SO")]
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

        // FishNet Spawnable Prefabs 등록 검증:
        // 네트워크 프리셋의 프리팹이 NetworkObject 를 가졌는데 PrefabId 가 미할당(UNSET=65535)이면,
        // 그 프리팹은 DefaultPrefabObjects 컬렉션에 포함되지 않은 것이다(예: 프리팹 Variant, 스캔 제외 폴더 등).
        // 이 상태로 스폰하면 런타임에 "ObjectId 65535 ... is expected to be initialized but was not" 오류가 난다.
        if (req.isNetworked && req.prefab.TryGetComponent(out NetworkObject nob))
        {
          if (nob.PrefabId == NetworkObject.UNSET_PREFABID_VALUE)
          {
            Debug.LogWarning(
              $"[EntityPresetSO] {tag} 의 프리팹 '{req.prefab.name}' 이 FishNet Spawnable Prefabs(DefaultPrefabObjects)에 " +
              "등록되어 있지 않습니다(PrefabId 미할당). 프리팹 Variant 이거나 스캔에서 제외된 위치일 수 있습니다. " +
              "원본(컬렉션에 등록된) 프리팹을 지정하거나, Fish-Networking > Utility > Reserialize Prefabs 후에도 " +
              "포함되지 않으면 Variant 대신 일반 프리팹을 사용하세요. (이대로 스폰하면 런타임 ObjectId 65535 오류 발생)", this);
            problems++;
          }
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
