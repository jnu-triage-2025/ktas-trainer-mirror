using UnityEngine;

namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// IInteractable 모든 상호작용 가능 객체의 컨트롤러에서 구현해야합니다. 이후에 InteractableEntityResolver에 의해 활용됩니다. 
  /// </summary>
  public interface IInteractable
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
}
