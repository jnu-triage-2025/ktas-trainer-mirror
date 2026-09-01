# <a id="MultiplayerInfrastructure_Entity"></a> Namespace MultiplayerInfrastructure.Entity

### Classes

 [Entity](MultiplayerInfrastructure.Entity.Entity.md)

 [MinecraftBoatLikeControl](MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl.md)

Minecraft 보트 방식의 탑승/점유/이동을 제공하는 공통 네트워크 모듈.
도메인 객체(환자 침대, 의료 장비 등)의 상태나 상호작용은 소유하지 않는다.

 [NPCBaseModelSO](MultiplayerInfrastructure.Entity.NPCBaseModelSO.md)

 [NPCScenarioInteractDefinition](MultiplayerInfrastructure.Entity.NPCScenarioInteractDefinition.md)

 [NPCSubmissionInteractDefinition](MultiplayerInfrastructure.Entity.NPCSubmissionInteractDefinition.md)

NPC 에디터(인스펙터/NPCBaseModelSO)에서 "아이템 제출" 상호작용을 사전 설정하기 위한 직렬화 데이터.

이 정의 자체는 순수 데이터이며, <xref href="MultiplayerInfrastructure.Entity.Npc" data-throw-if-not-resolved="false"></xref> 가 런타임에 이 정의로부터
<xref href="MultiplayerInfrastructure.InteractableEntity.ItemSubmissionInteractable" data-throw-if-not-resolved="false"></xref> 컴포넌트를 자동 생성/구성하여 상호작용 소스로 추가한다.
(submission 상호작용은 제출 UI 를 여는 컴포넌트가 필요하므로 순수 데이터만으로는 동작하지 않는다.)

시나리오 그래프 노드(ItemSubmissionConfig)는 이렇게 생성된 Interactable 을
<xref href="MultiplayerInfrastructure.Entity.NPCSubmissionInteractDefinition.InteractableIdentifier" data-throw-if-not-resolved="false"></xref> 로 참조하여 런타임에 요구 아이템/완료 신호를 덮어쓸 수 있다.

 [Npc](MultiplayerInfrastructure.Entity.Npc.md)

 [ReposableAttachPointObject](MultiplayerInfrastructure.Entity.ReposableAttachPointObject.md)

 [Ridable](MultiplayerInfrastructure.Entity.Ridable.md)

 [RidableAttachPointObject](MultiplayerInfrastructure.Entity.RidableAttachPointObject.md)

### Interfaces

 [IItemUseTarget](MultiplayerInfrastructure.Entity.IItemUseTarget.md)

플레이어가 들고 있는 아이템의 "사용(Use)" 대상이 될 수 있는 월드 오브젝트가 구현하는 인터페이스.

<p>
플레이어가 아이템을 사용하면 <xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 가
조준 대상(크로스헤어 레이캐스트 히트)에서 이 인터페이스를 찾아 <xref href="MultiplayerInfrastructure.Entity.IItemUseTarget.OnItemUsed(MultiplayerInfrastructure.Entity.Entity%2cSystem.String)" data-throw-if-not-resolved="false"></xref> 를 호출한다.
구현체는 사용 결과(아이템 부착/적용 등)와, 필요 시 시나리오 인터랙션 완료 신호 Raise 를 자체적으로 처리한다.
</p>

재사용성: 이 인터페이스 자체는 시나리오/트리아지에 의존하지 않는다(범용 "아이템 사용 대상"). 신호 배선
같은 도메인 동작은 구현체(예: TriageTrainer 의 PatientController) 책임이다.

 [IItemizableWorldEntity](MultiplayerInfrastructure.Entity.IItemizableWorldEntity.md)

좌클릭으로 월드 아이템으로 되돌릴 수 있는 설치형 엔티티의 규약입니다.

 [IReposable](MultiplayerInfrastructure.Entity.IReposable.md)

 [IScenarioEntityInitTarget](MultiplayerInfrastructure.Entity.IScenarioEntityInitTarget.md)

시나리오 그래프의 EntityInit 노드가 초기 표시/부착 상태를 설정할 수 있는 엔티티가 구현하는 인터페이스.

<p>
시나리오 컨트롤러는 식별자로 엔티티를 레지스트리에서 찾은 뒤, 그 GameObject 에서 이 인터페이스를
찾아 명명된 표시 상태(예: 환자의 처치 부착물)를 표시/비표시로 설정한다. 상태 이름의 해석과
실제 적용(자식 GameObject 토글 등)은 구현체 책임이다.
</p>

재사용성: 이 인터페이스 자체는 시나리오/트리아지 도메인에 의존하지 않는다(범용 "명명된 표시 상태 대상").
환자 부착물 표현 같은 도메인 매핑은 구현체(예: TriageTrainer 의 PatientController) 책임이다.

 [IScenarioEntityStateEventSource](MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource.md)

상태(state) 변경을 명명된 이벤트로 노출하여, 시나리오 그래프가 그 이벤트를 신호로 변환할 수 있게 하는
엔티티가 구현하는 범용 인터페이스.

<p>
시나리오의 <code>EntityStateSignalBinding</code> 노드는 식별자로 엔티티를 레지스트리에서 찾은 뒤,
그 GameObject 에서 이 인터페이스를 찾아 (eventName, key) 조합에 대한 리스너를 등록/해제한다.
엔티티가 해당 이벤트를 발생시키면 등록된 콜백이 호출되고, 노드는 콜백에서
<code>ScenarioInteractionSignals.Raise</code> 를 수행한다.
</p>

<p>
재사용성: 이 인터페이스 자체는 어떤 도메인(환자/처치 등)에도 의존하지 않는다. 어떤 이벤트가 있고
key 가 무엇을 의미하는지는 구현체(예: TriageTrainer 의 PatientController)가 정의한다.
<xref href="MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource.GetStateEventNames" data-throw-if-not-resolved="false"></xref> 로 지원 이벤트 목록을 조회할 수 있어, 콘텐츠 검증/문서화에 사용한다.
</p>

 [IScenarioIdentifiedEntity](MultiplayerInfrastructure.Entity.IScenarioIdentifiedEntity.md)

시나리오 식별자를 노출하는 엔티티가 구현하는 범용 인터페이스.

<p>
트리거 존(<code>ScenarioTriggerZone</code>) 등 "진입한 엔티티가 누구인지" 를 알아야 하는 컴포넌트가,
도메인 타입(예: PatientController)에 직접 의존하지 않고 엔티티의 시나리오 식별자를 얻기 위해 사용한다.
진입 콜라이더에서 <code>GetComponentInParent&lt;IScenarioIdentifiedEntity&gt;()</code> 로 조회한다.
</p>

<p>재사용성: 이 인터페이스는 어떤 도메인에도 의존하지 않는다. 식별자의 의미와 생성 방식은 구현체가 정한다.</p>

 [IScenarioTriageAssessTarget](MultiplayerInfrastructure.Entity.IScenarioTriageAssessTarget.md)

시나리오 그래프의 TriageAssessControl 노드가 트리아지 평가 인터랙션을 활성화/비활성화할 수 있는
엔티티가 구현하는 인터페이스.

<p>
시나리오 컨트롤러는 식별자로 엔티티를 레지스트리에서 찾은 뒤 이 인터페이스를 통해 트리아지 평가
가능 여부를 제어한다. 실제 인터랙션 노출/게이팅 규칙은 구현체(예: TriageTrainer 의 PatientController)
책임이다.
</p>

재사용성: 이 인터페이스 자체는 트리아지 도메인 구현에 의존하지 않는다("트리아지 평가 가능 플래그 대상").

