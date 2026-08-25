using System;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Entity
{
  /// <summary>
  /// 상태(state) 변경을 명명된 이벤트로 노출하여, 시나리오 그래프가 그 이벤트를 신호로 변환할 수 있게 하는
  /// 엔티티가 구현하는 범용 인터페이스.
  ///
  /// <para>
  /// 시나리오의 <c>EntityStateSignalBinding</c> 노드는 식별자로 엔티티를 레지스트리에서 찾은 뒤,
  /// 그 GameObject 에서 이 인터페이스를 찾아 (eventName, key) 조합에 대한 리스너를 등록/해제한다.
  /// 엔티티가 해당 이벤트를 발생시키면 등록된 콜백이 호출되고, 노드는 콜백에서
  /// <c>ScenarioInteractionSignals.Raise</c> 를 수행한다.
  /// </para>
  ///
  /// <para>
  /// 재사용성: 이 인터페이스 자체는 어떤 도메인(환자/처치 등)에도 의존하지 않는다. 어떤 이벤트가 있고
  /// key 가 무엇을 의미하는지는 구현체(예: TriageTrainer 의 PatientController)가 정의한다.
  /// <see cref="GetStateEventNames"/> 로 지원 이벤트 목록을 조회할 수 있어, 콘텐츠 검증/문서화에 사용한다.
  /// </para>
  /// </summary>
  public interface IScenarioEntityStateEventSource
  {
    /// <summary>이 엔티티가 지원하는 상태 이벤트 이름 목록(검토/검증용).</summary>
    public IReadOnlyList<string> GetStateEventNames();

    /// <summary>
    /// 명명된 상태 이벤트에 대한 리스너를 등록한다.
    /// </summary>
    /// <param name="eventName">이벤트 이름(구현체가 정의; <see cref="GetStateEventNames"/> 중 하나).</param>
    /// <param name="key">
    /// 이벤트 세부 대상 필터(구현체가 해석). null/빈 문자열이면 해당 이벤트의 모든 발생에 매칭된다.
    /// 콜백에는 실제 발생 key 가 전달된다.
    /// </param>
    /// <param name="onFired">이벤트 발생 시 호출되는 콜백. 인자는 실제 발생 key(없으면 null).</param>
    /// <returns>이벤트 이름을 인식하여 등록했으면 true.</returns>
    public bool RegisterStateEventListener(string eventName, string key, Action<string> onFired);

    /// <summary>
    /// 등록된 상태 이벤트 리스너를 해제한다.
    /// <paramref name="eventName"/> 가 비어 있으면 이 엔티티의 모든 리스너를 해제한다.
    /// </summary>
    public void UnregisterStateEventListeners(string eventName);
  }
}
