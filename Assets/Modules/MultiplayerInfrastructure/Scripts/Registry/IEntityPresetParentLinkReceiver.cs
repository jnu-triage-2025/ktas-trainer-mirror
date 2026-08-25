namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// 엔티티 프리셋 스폰 시, 하위 프리셋 인스턴스가 <b>부모(루트) 프리셋의 런타임 식별자</b>를 전달받기 위한 인터페이스.
  ///
  /// 하위 참조(EntityPresetChildReference)에서 부모 연결이 요청되면(linkChildToParent),
  /// 엔진은 하위 스폰 직후 이 메서드를 호출해 "이 하위는 어떤 부모와 결합되어야 하는가"를 식별자로 알려준다.
  /// 구체적인 결합 의미(예: 환자침대가 그 부모 환자를 자기 위에 누인다)는 프로젝트 측 구현이 결정한다.
  /// 엔진은 "부모 식별자를 전달" 하는 일반 메커니즘만 제공한다(특정 엔티티 종류에 의존하지 않음).
  ///
  /// 서버 컨텍스트에서 호출되는 것을 전제로 한다(프리셋 스폰은 서버에서 수행). 구현체는 보통 이 식별자를
  /// SyncVar 등에 기록해 전 피어로 복제·결합한다.
  /// </summary>
  public interface IEntityPresetParentLinkReceiver
  {
    /// <summary>스폰된 부모(루트) 프리셋 인스턴스의 런타임 엔티티 식별자를 전달받는다.</summary>
    public void ApplyParentEntityIdentifier(string parentEntityIdentifier);
  }
}
