# <a id="TriageTrainer_DebugTools"></a> Namespace TriageTrainer.DebugTools

### Classes

 [DebugSignalEmitter](TriageTrainer.DebugTools.DebugSignalEmitter.md)

IndevScene 등 검증용으로, 임의의 시나리오 인터랙션 신호(sig.*)를 손쉽게 발생/해제하는 디버그 컴포넌트.

<p>사용처</p>
<ul><li>평면 월드맵에 빈 GameObject 로 임시 배치하여 게이트(Validator/Parallel) 통과를 검증한다.</li><li>구역 진입을 흉내내는 트리거 박스로 쓰거나, 키 입력으로 특정 신호를 즉시 올린다.</li></ul>

<p>주의</p>

디버그 전용이다. 실제 게임플레이 배선(아이템 사용/사정/연결 등)을 대체하지 않으며,
빌드/프로덕션 씬에는 배치하지 않는 것을 전제로 한다. 신호는
<xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(System.String)" data-throw-if-not-resolved="false"></xref>(서버 권한 라우팅)로 올린다.

 [ScenarioDevStub](TriageTrainer.DebugTools.ScenarioDevStub.md)

IndevScene 등 개발 씬에서, 시나리오가 요구하는 인터랙션 대상/트리거존을 간이로 대체하는
디버그 스텁. ScenarioDevStubSpawner 가 자동 생성한다.

<p>두 가지 모드</p>
<ul><li><b>Interactable</b>: 원기둥(Cylinder). 플레이어가 상호작용하면 신호를 올린다.</li><li><b>TriggerZone</b>: 통과형 넓은 큐브(isTrigger). 플레이어가 들어오면 신호를 올린다.</li></ul>

어느 모드든 지정한 식별자로 레지스트리에 엔티티를 등록하여, 사전 검증(Preflight)의
InteractionTarget 점검을 충족시키고, 인터랙션/통과 시 게이트 신호(sig.*)를 올린다.

디버그 전용이며 빌드/프로덕션 씬에는 배치하지 않는 것을 전제로 한다.

### Enums

 [ScenarioDevStub.StubMode](TriageTrainer.DebugTools.ScenarioDevStub.StubMode.md)

