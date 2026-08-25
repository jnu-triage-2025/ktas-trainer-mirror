using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.InteractableEntity;
using UnityEngine;

namespace MultiplayerInfrastructure.Entity
{
  /// <summary>
  /// NPC 에디터(인스펙터/NPCBaseModelSO)에서 "아이템 제출" 상호작용을 사전 설정하기 위한 직렬화 데이터.
  ///
  /// 이 정의 자체는 순수 데이터이며, <see cref="Npc"/> 가 런타임에 이 정의로부터
  /// <see cref="ItemSubmissionInteractable"/> 컴포넌트를 자동 생성/구성하여 상호작용 소스로 추가한다.
  /// (submission 상호작용은 제출 UI 를 여는 컴포넌트가 필요하므로 순수 데이터만으로는 동작하지 않는다.)
  ///
  /// 시나리오 그래프 노드(ItemSubmissionConfig)는 이렇게 생성된 Interactable 을
  /// <see cref="InteractableIdentifier"/> 로 참조하여 런타임에 요구 아이템/완료 신호를 덮어쓸 수 있다.
  /// </summary>
  [Serializable]
  public class NPCSubmissionInteractDefinition
  {
    [Tooltip("생성될 ItemSubmissionInteractable 의 식별자(그래프 노드가 참조/오버라이드할 때 사용). 비어 있으면 자동 생성된다.")]
    [SerializeField] private string _interactableIdentifier;

    [Tooltip("상호작용 힌트에 표시할 짧은 텍스트.")]
    [SerializeField] private string _displayText = "제출하기";

    [SerializeField] private IconSpriteReference _displayIcon = new();
    [SerializeField] private Color _displayColor = Color.white;

    [Tooltip("제출 패널 제목.")]
    [SerializeField] private string _title = "아이템 제출";

    [Tooltip("제출 버튼 라벨.")]
    [SerializeField] private string _submitButtonText = "제출";

    [Tooltip("요구되는 아이템 목록(식별자 + 수량). 모든 항목이 충족되어야 제출할 수 있다.")]
    [SerializeField] private List<ItemRequirement> _requiredItems = new();

    [Tooltip("제출 성공 시 올릴 서버 세션 전역 신호 식별자('sig.' 접두사는 자동 정규화).")]
    [SerializeField] private string _completionSignalIdentifier;

    [Tooltip("한 번 제출에 성공하면 이후 상호작용을 비활성화할지 여부.")]
    [SerializeField] private bool _consumeOnce = true;

    [Tooltip("시작 시 상호작용 가능 여부.")]
    [SerializeField] private bool _enabled = true;

    public string InteractableIdentifier => _interactableIdentifier;
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon?.Resolve();
    public bool AllowDisplayIconFallback => !(_displayIcon?.IsExplicitNone ?? false);
    public Color DisplayColor => _displayColor;
    public bool Enabled => _enabled;

    /// <summary>요구 아이템이 하나 이상 정의되어 있으면 유효하다.</summary>
    public bool IsValid
    {
      get
      {
        if (_requiredItems == null)
          return false;

        for (int i = 0; i < _requiredItems.Count; i++)
        {
          if (_requiredItems[i].IsValid)
            return true;
        }

        return false;
      }
    }

    /// <summary>이 정의를 런타임 <see cref="ItemSubmissionDefinition"/> 로 변환한다.</summary>
    public ItemSubmissionDefinition ToSubmissionDefinition()
    {
      var def = new ItemSubmissionDefinition
      {
        displayText = _displayText,
        submitButtonText = _submitButtonText,
        title = _title,
        completionSignalIdentifier = _completionSignalIdentifier,
        consumeOnce = _consumeOnce,
        requiredItems = new List<ItemRequirement>()
      };

      if (_requiredItems != null)
      {
        for (int i = 0; i < _requiredItems.Count; i++)
        {
          if (_requiredItems[i].IsValid)
            def.requiredItems.Add(_requiredItems[i]);
        }
      }

      return def;
    }

    public NPCSubmissionInteractDefinition Clone()
    {
      var clone = new NPCSubmissionInteractDefinition
      {
        _interactableIdentifier = _interactableIdentifier,
        _displayText = _displayText,
        _displayIcon = _displayIcon?.Clone(),
        _displayColor = _displayColor,
        _title = _title,
        _submitButtonText = _submitButtonText,
        _completionSignalIdentifier = _completionSignalIdentifier,
        _consumeOnce = _consumeOnce,
        _enabled = _enabled,
        _requiredItems = new List<ItemRequirement>()
      };

      if (_requiredItems != null)
      {
        for (int i = 0; i < _requiredItems.Count; i++)
          clone._requiredItems.Add(_requiredItems[i]);
      }

      return clone;
    }
  }
}
