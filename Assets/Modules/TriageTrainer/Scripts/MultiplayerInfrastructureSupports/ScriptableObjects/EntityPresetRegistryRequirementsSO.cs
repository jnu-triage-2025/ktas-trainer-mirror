using System.Collections.Generic;
using FishNet.Object;
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
    // 이를 통해 "환자+침대 결합 프리팹 + 분리(ungroup) 설정" 을 플레이 없이 확인할 수 있다.

    private void OnValidate() => ValidatePresetsEditor(logWhenOk: false);

    [ContextMenu("Validate Presets (Editor)")]
    private void ValidatePresetsContextMenu() => ValidatePresetsEditor(logWhenOk: true);

    /// <summary>
    /// 각 프리셋 요구사항을 에디터에서 검증한다.
    /// 검사 항목:
    ///  - identifier/prefab 누락, identifier 중복
    ///  - childDetachments 의 각 childPath 가 프리팹에서 해석되는지
    ///  - 분리 대상 자식이 NetworkObject 인지(아니면 분리 불가)
    ///  - 컨테이너 결합(예: 침대-환자) 시 권장 구조 경고
    /// </summary>
    private void ValidatePresetsEditor(bool logWhenOk)
    {
      if (entityPresetRegistryRequirements == null)
        return;

      int problems = 0;
      var seen = new HashSet<string>(System.StringComparer.Ordinal);

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

        // 분리 설정 검증
        if (req.childDetachments != null && req.childDetachments.Length > 0)
        {
          foreach (var det in req.childDetachments)
          {
            if (string.IsNullOrWhiteSpace(det.childPath))
            {
              Debug.LogWarning($"[EntityPresetSO] {tag} childDetachments 에 빈 childPath 가 있습니다.", this);
              problems++;
              continue;
            }

            Transform child = ResolveChild(req.prefab.transform, det.childPath);
            if (child == null)
            {
              Debug.LogWarning($"[EntityPresetSO] {tag} 의 분리 대상 childPath '{det.childPath}' 를 프리팹에서 찾을 수 없습니다.", this);
              problems++;
              continue;
            }

            if (child.GetComponent<NetworkObject>() == null)
            {
              Debug.LogWarning($"[EntityPresetSO] {tag} 의 분리 대상 '{det.childPath}' 에 NetworkObject 가 없습니다. NetworkObject 만 분리 가능합니다.", this);
              problems++;
            }
          }

          // 권장 구조: 컨테이너 루트(분리 대상이 있는 프리팹) 자체는 NetworkObject 가 아닌 편이 안전
          // (자식 NetworkObject 가 nested 되지 않도록). 루트가 NetworkObject 이면 경고.
          if (req.prefab.GetComponent<NetworkObject>() != null)
          {
            Debug.LogWarning(
              $"[EntityPresetSO] {tag} 는 자식 분리를 사용하는데 컨테이너 루트에 NetworkObject 가 있습니다. " +
              "컨테이너 루트는 비-NetworkObject 로 두고, 분리할 환자/침대를 각각 자식 NetworkObject 로 두는 것을 권장합니다.", this);
            problems++;
          }
        }
      }

      if (logWhenOk && problems == 0)
      {
        Debug.Log($"[EntityPresetSO] 검증 완료 — 문제 없음 ({entityPresetRegistryRequirements.Length} 항목).", this);
      }
    }

    private static Transform ResolveChild(Transform root, string childPath)
    {
      if (root == null || string.IsNullOrWhiteSpace(childPath))
        return null;

      Transform byPath = root.Find(childPath);
      if (byPath != null)
        return byPath;

      // 이름 기준 1단계 폴백
      for (int i = 0; i < root.childCount; i++)
      {
        if (root.GetChild(i).name == childPath)
          return root.GetChild(i);
      }

      return null;
    }
#endif
  }
}
