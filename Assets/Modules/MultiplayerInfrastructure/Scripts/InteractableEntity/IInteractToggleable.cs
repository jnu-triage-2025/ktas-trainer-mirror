namespace MultiplayerInfrastructure.InteractableEntity
{
  /// <summary>
  /// 상호작용을 런타임에 활성/비활성 전환할 수 있는 Interactable 이 구현한다.
  ///
  /// <para>
  /// 인터렉션 레지스트리 도입 뒤 시나리오가 노출을 바꾸는 경로는 레지스트리의 가시성
  /// (정의의 조건 절과 InteractionVisibility 노드)뿐이다. 이 인터페이스를 호출하던 그래프 노드
  /// (NPCControl 의 인터렉트 조작, ItemSubmissionConfig, NpcInteractControl)는 모두 폐기했으므로
  /// 지금은 호출자가 없고, 컴포넌트가 자기 잠금을 코드로 다룰 때 쓰는 규약으로만 남아 있다.
  /// </para>
  /// </summary>
  public interface IInteractToggleable
  {
    public void SetEnabled(bool enabled);
  }
}
