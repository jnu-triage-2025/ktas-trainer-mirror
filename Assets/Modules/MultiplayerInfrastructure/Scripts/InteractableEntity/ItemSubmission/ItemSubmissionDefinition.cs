using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// 아이템 제출 상호작용의 요구 사항/표시/완료 신호를 담는 직렬화 가능한 설정.
  ///
  /// 값은 인터렉션 레지스트리의 <c>kind: "ItemSubmission"</c> 정의(시나리오 데이터의 interactions 구역 또는
  /// 상시 카탈로그)에서 오며, 레지스트리 핸들러가 <see cref="ItemSubmissionInteractable.Configure"/> 로 이 구조에
  /// 옮겨 담는다. 컴포넌트의 직렬화 기본값은 구성 이전에만 쓰이고, 그래프 노드가 덮어쓰던 경로는 폐기했다.
  ///
  /// 완료 처리는 서버 세션 전역 신호(<see cref="Scenario.ScenarioInteractionSignals"/>)로 이루어진다.
  /// </summary>
  [Serializable]
  public sealed class ItemSubmissionDefinition
  {
    [Tooltip("상호작용 힌트에 표시할 짧은 텍스트. 비어 있으면 기본값이 사용된다.")]
    public string displayText = "제출하기";

    [Tooltip("제출 버튼 라벨.")]
    public string submitButtonText = "제출";

    [Tooltip("제출 패널 제목.")]
    public string title = "아이템 제출";

    [Tooltip("요구되는 아이템 목록(식별자 + 수량). 모든 항목이 충족되어야 제출할 수 있다.")]
    public List<ItemRequirement> requiredItems = new List<ItemRequirement>();

    [Tooltip("제출 성공 시 올릴 서버 세션 전역 신호 식별자('sig.' 접두사는 자동 정규화됨).")]
    public string completionSignalIdentifier;

    [Tooltip("한 번 제출에 성공하면 이후 상호작용을 비활성화할지 여부.")]
    public bool consumeOnce = true;

    public ItemSubmissionDefinition Clone()
    {
      var clone = new ItemSubmissionDefinition
      {
        displayText = displayText,
        submitButtonText = submitButtonText,
        title = title,
        completionSignalIdentifier = completionSignalIdentifier,
        consumeOnce = consumeOnce,
        requiredItems = new List<ItemRequirement>()
      };

      if (requiredItems != null)
      {
        for (int i = 0; i < requiredItems.Count; i++)
          clone.requiredItems.Add(requiredItems[i]);
      }

      return clone;
    }

    /// <summary>유효한(식별자/수량이 채워진) 요구 아이템만 반환한다.</summary>
    public IReadOnlyList<ItemRequirement> GetValidRequirements()
    {
      var result = new List<ItemRequirement>();
      if (requiredItems == null)
        return result;

      for (int i = 0; i < requiredItems.Count; i++)
      {
        var req = requiredItems[i];
        if (req.IsValid)
          result.Add(req);
      }

      return result;
    }
  }
}
