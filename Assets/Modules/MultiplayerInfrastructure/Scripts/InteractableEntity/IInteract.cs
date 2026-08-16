using UnityEngine;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// IInteractable 모든 상호작용 가능 객체의 컨트롤러에서 구현해야합니다. 이후에 InteractableEntityResolver에 의해 활용됩니다. 
  /// </summary>
  public interface IInteract
  {
    /// <summary>
    /// 플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 짧은 텍스트의 내용입니다.
    /// </summary>
    public string DisplayText { get; }
    
    /// <summary>
    /// 플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 표시되는 아이콘에 해당합니다.
    /// </summary>
    public Sprite DisplayIcon { get; }

    /// <summary>
    /// DisplayIcon이 null일 때 기본 fallback 아이콘을 표시할지 여부입니다.
    /// </summary>
    public bool AllowDisplayIconFallback { get; }
    
    /// <summary>
    /// 플레이어의 화면에 상호 작용 가능한 물체로서 표시될 때, 강조하고자 싶다면 이 색을 설정합니다.
    /// 기본적으로는 하얀색으로 설정하세요.
    /// </summary>
    public Color DisplayColor { get; }
    
    /// <summary>
    /// 플레이어가 상호작용할 때, 그 처리를 정의합니다. 
    /// </summary>
    /// <param name="model"></param>
    /// <param name="interactor"></param>
    void Interact(Transform interactor);
  }

  /// <summary>
  /// 하나의 상호작용 항목에 여러 표시 아이콘을 제공할 때 구현합니다.
  /// 각 아이콘은 힌트 UI에서 독립된 정사각형 슬롯에 원본 비율을 유지해 표시됩니다.
  /// </summary>
  public interface IInteractDisplayIcons
  {
    IReadOnlyList<Sprite> DisplayIcons { get; }
  }

  /// <summary>
  /// 기존 <see cref="IInteractable"/> 컨트롤러에 같은 GameObject의 기능 컴포넌트가
  /// 조건부 상호작용 항목을 보탤 때 사용한다.
  /// </summary>
  public interface IAdditionalInteractProvider
  {
    IEnumerable<IInteract> AdditionalInteracts { get; }
  }

  /// <summary>로컬 선택 상태에 따른 표시 효과를 위한 선택적 규약입니다.</summary>
  public interface ILocalInteractionFocus
  {
    void SetLocalInteractionFocused(bool focused);
  }

  /// <summary>
  /// 같은 종류의 후보가 감지 범위에 여러 개 들어왔을 때 가장 가까운 상호작용 하나만 노출해야 하는 항목입니다.
  /// 빈 그룹 키를 반환하면 현재 상태에서는 거리 필터를 적용하지 않습니다.
  /// </summary>
  public interface INearestOnlyInteract
  {
    string NearestOnlyGroup { get; }
    Transform NearestOnlyDistanceOrigin { get; }
  }
}
