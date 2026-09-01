# <a id="MultiplayerInfrastructure_Scenario"></a> Namespace MultiplayerInfrastructure.Scenario

### Classes

 [ScenarioTTSBakeScanner.InlineTTSJob](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.InlineTTSJob.md)

bake 가능한 인라인 TTS 작업 항목.

 [ScenarioTTSBakeScanner.ScanResult](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.ScanResult.md)

스캔 결과 요약.

 [ScenarioActingNpcDefinition](MultiplayerInfrastructure.Scenario.ScenarioActingNpcDefinition.md)

시나리오가 시작 또는 그래프 진행 중 preset으로 만들고 종료 시 선택적으로 정리할 Acting NPC 정의.
외형과 네트워크 프리팹은 preset catalog가 담당하고, 인스턴스별 데이터는 이 정의가 담당한다.

 [ScenarioActingNpcInteractionDefinition](MultiplayerInfrastructure.Scenario.ScenarioActingNpcInteractionDefinition.md)

 [ScenarioActingNpcItemRequirement](MultiplayerInfrastructure.Scenario.ScenarioActingNpcItemRequirement.md)

 [ScenarioBedSnapNode](MultiplayerInfrastructure.Scenario.ScenarioBedSnapNode.md)

이동식 환자 침대를 지정한 포지셔닝 포인트에 붙이는 노드.

<p>평소 플레이에서는 플레이어가 침대를 밀어 포인트 근처까지 가져가야 스냅이 걸린다.
이 노드는 시나리오가 그 결과만 필요할 때 쓴다. 주로 ManualEntrypoint 의 준비 체인에서
건너뛴 구간이 만들어 놨어야 할 침대 배치를 맞추는 용도다.</p>

<p>침대에 환자가 결합돼 있으면 환자도 함께 따라온다(결합은 attach_patient_bed_pairs 등이
먼저 처리한다). 서버 권위에서만 적용되고, 결과는 침대 자신의 RPC 로 각 피어에 전파된다.</p>

<p><b>대상 포인트가 이미 점유된 경우:</b> 포인트의 점유 정책을 그대로 따른다. 기본 설정에서는
환자가 실린 다른 침대가 있으면 배치가 거부되고(빈 침대는 치워진다), 이 노드는 실패로 끝난다.
여러 침대를 연달아 배치할 때는 서로 자리를 맞바꾸는 순서가 되지 않게 주의한다.
예를 들어 A 를 1번 자리로, B 를 0번 자리로 옮기는데 B 가 이미 1번에 있다면 A 가 먼저 막힌다.</p>

<p>대상 포인트가 침대 프리팹의 허용 목록에 없으면 실행 시 목록에 보정해 넣는다. 허용 목록은
플레이어가 밀어서 붙일 수 있는 곳을 제한하는 값이고, 시나리오 지시는 그보다 우선한다.</p>

 [ScenarioCameraTargetNode](MultiplayerInfrastructure.Scenario.ScenarioCameraTargetNode.md)

 [ScenarioChatPrintNode](MultiplayerInfrastructure.Scenario.ScenarioChatPrintNode.md)

시나리오 진행 중 임의의 텍스트를 인게임 채팅창 및/또는 Unity 콘솔에 출력하는 노드.
시그널/이벤트 발생을 눈으로 확인하는 디버깅·데모 용도로 사용한다.

 [ScenarioChecklistItemRequirement](MultiplayerInfrastructure.Scenario.ScenarioChecklistItemRequirement.md)

체크리스트에 표시할 아이템 종류와 수량입니다.

 [ScenarioChoiceNode](MultiplayerInfrastructure.Scenario.ScenarioChoiceNode.md)

 [ScenarioChoiceOption](MultiplayerInfrastructure.Scenario.ScenarioChoiceOption.md)

IInteractable과 호환 가능하도록 설계됨

 [ScenarioClientSignalAuthorization](MultiplayerInfrastructure.Scenario.ScenarioClientSignalAuthorization.md)

Client-origin generic signal RPC의 서버측 capability 집합. 그래프가 소비하는 입력만 허용하고,
그래프가 생성하는 출력은 동일 식별자가 validator에 있어도 client 입력에서 제외한다.

 [ScenarioCombineItemNode](MultiplayerInfrastructure.Scenario.ScenarioCombineItemNode.md)

 [ScenarioConcurrencyConflictPolicyExtensions](MultiplayerInfrastructure.Scenario.ScenarioConcurrencyConflictPolicyExtensions.md)

 [ScenarioConditionalSignalListeners](MultiplayerInfrastructure.Scenario.ScenarioConditionalSignalListeners.md)

게임플레이가 올린 신호를 관찰해, 선언된 전제 신호가 모두 있을 때만 후속 신호를 발생시킨다.

 [ScenarioController](MultiplayerInfrastructure.Scenario.ScenarioController.md)

시나리오 흐름을 제어합니다.
UI 제어는 ScenarioPanelUIController에 위임합니다.

 [ScenarioDelayNode](MultiplayerInfrastructure.Scenario.ScenarioDelayNode.md)

 [ScenarioDialogueNode](MultiplayerInfrastructure.Scenario.ScenarioDialogueNode.md)

 [ScenarioDisinteractableDialogueNode](MultiplayerInfrastructure.Scenario.ScenarioDisinteractableDialogueNode.md)

 [ScenarioEntityInitNode](MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.md)

시나리오 그래프에 엔티티를 준비(생성 또는 참조)하고 초기 상태를 설정하는 노드.

<p>대상 엔티티는 두 가지 방식으로 결정한다(<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.PresetIdentifier" data-throw-if-not-resolved="false"></xref> 가 지정되면 스폰 우선).</p>
<ol><li><b>프리셋 스폰</b>: <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.PresetIdentifier" data-throw-if-not-resolved="false"></xref> 로 등록된 엔티티 프리셋을 스폰한다.
  스폰 위치는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.PositionSourceEntityIdentifier" data-throw-if-not-resolved="false"></xref> 또는 좌표로 지정할 수 있다.</li><li><b>기존 엔티티 참조</b>: <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.TargetEntityIdentifier" data-throw-if-not-resolved="false"></xref>(직접) 또는
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.TargetEntityStateKey" data-throw-if-not-resolved="false"></xref>(상태 저장소 조회)로 이미 레지스트리에 등록된 엔티티를 가리킨다.</li></ol>

<p>
<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.EntityIdentifier" data-throw-if-not-resolved="false"></xref> 를 지정하면 이후 시나리오 그래프가 그 식별자로 동일 엔티티를 계속 제어할 수 있다
(프리셋 스폰 시에는 인스턴스에 부여할 식별자, 기존 엔티티 참조 시에는 결과 식별자로 사용).
결과 식별자는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.ResultStateKey" data-throw-if-not-resolved="false"></xref> 가 지정되면 상태 저장소에도 기록되어 후속 노드가 참조할 수 있다.
</p>

<p>
<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode.StateOperations" data-throw-if-not-resolved="false"></xref> 로 대상 엔티티의 초기 상태를 설정한다. 1차 목표는 환자 엔티티에 부착된
처치 부착물(주사기/거즈/경부보호대 등)의 초기 표시 여부 설정이다(DisplayState 종류).
</p>

 [ScenarioEntityPresetSpawnNode](MultiplayerInfrastructure.Scenario.ScenarioEntityPresetSpawnNode.md)

등록된 엔티티 프리셋을 식별자로 스폰하는 시나리오 노드.

하위 오브젝트 구성(어떤 하위 EntityPreset 을 함께 스폰할지, unwrap 여부 등)은 프리셋 정의에 사전 설정되어 있으므로,
이 노드는 프리셋 식별자/위치/결과 식별자만 다룬다(노드에서 하위 구성을 재정의하지 않는다).

 [ScenarioEntityStateOperation](MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.md)

<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode" data-throw-if-not-resolved="false"></xref> 가 대상 엔티티에 적용하는 단일 초기 상태 항목.

<p><xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.Kind" data-throw-if-not-resolved="false"></xref> 에 따라 의미가 달라진다.</p>
<ul><li><xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperationKind.StateStore" data-throw-if-not-resolved="false"></xref>:
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.Key" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.Value" data-throw-if-not-resolved="false"></xref> 를 시나리오 상태 저장소에 기록한다.</li><li><xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperationKind.DisplayState" data-throw-if-not-resolved="false"></xref>:
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.Key" data-throw-if-not-resolved="false"></xref> 가 표시/부착 상태 이름(예: 환자의 <code>CervicalCollarOnNeck</code>)이고
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperation.DisplayActive" data-throw-if-not-resolved="false"></xref> 로 표시(true)/비표시(false)를 정한다.</li></ul>

 [ScenarioEntityStateSignalBindingNode](MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.md)

엔티티의 명명된 상태(state) 이벤트를 시나리오 신호로 변환하는 바인딩을 제어하는 노드.

<p>
대상 엔티티(<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.TargetEntityIdentifier" data-throw-if-not-resolved="false"></xref> 또는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.TargetEntityStateKey" data-throw-if-not-resolved="false"></xref>)가 구현한
<xref href="MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource" data-throw-if-not-resolved="false"></xref> 에 리스너를 등록한다.
엔티티가 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.EventName" data-throw-if-not-resolved="false"></xref> 이벤트를 발생시키고(선택적 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.EventKey" data-throw-if-not-resolved="false"></xref> 필터에 매칭되면)
<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.OutputSignalIdentifier" data-throw-if-not-resolved="false"></xref> 신호를 <code>ScenarioInteractionSignals.Raise</code> 로 발신한다.
</p>

<p>
예: 환자 A의 처치 표시 <code>EndotrachealTubeInsertDone</code> 가 적용되면 <code>et_tube_done_patient_a</code> 신호를 올린다.
EventKey 를 비우면 해당 이벤트의 모든 발생에 매칭된다(예: 활력 변경 <code>VitalChanged</code>).
</p>

<p>바인딩은 시나리오 종료 시 정리되며, 동일 <xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingNode.BindingIdentifier" data-throw-if-not-resolved="false"></xref> 재등록은 기존 바인딩을 교체한다.</p>

 [ScenarioEntityStateSignalBindings](MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindings.md)

<code>EntityStateSignalBinding</code> 노드가 등록한 "엔티티 상태 이벤트 → 시나리오 신호" 바인딩을 추적·정리한다.

<p>
실제 리스너 등록은 대상 엔티티의 <xref href="MultiplayerInfrastructure.Entity.IScenarioEntityStateEventSource" data-throw-if-not-resolved="false"></xref> 에 이루어지며,
이 레지스트리는 각 바인딩의 콜백까지 보관하여 시나리오 종료 시 또는 동일 식별자 재등록 시
정확히 재구성한다. 소스는 이벤트명 단위 해제만 제공하므로, 특정 바인딩을 제거할 때는
해당 (소스, 이벤트) 리스너를 모두 지운 뒤 남은 바인딩을 다시 등록한다.
</p>

<p>
콜백에서 <xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(System.String)" data-throw-if-not-resolved="false"></xref> 를 호출한다. 신호 발신 자체가 서버 권위
라우팅이므로, 이벤트가 서버(호스트) 컨텍스트에서 발생하는 한 모든 피어에 일관되게 전파된다.
</p>

 [ScenarioEntityTagNode](MultiplayerInfrastructure.Scenario.ScenarioEntityTagNode.md)

 [ScenarioEventIdentifierRegistry](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.md)

Maps scenario event identifiers to handlers that can be invoked by scenario nodes.
Handlers may return a coroutine to allow asynchronous execution; returning null is treated as an immediate completion.

 [ScenarioExecuteCommandNode](MultiplayerInfrastructure.Scenario.ScenarioExecuteCommandNode.md)

시나리오 진행 중 인게임 채팅 명령어를 서버 권한으로 실행하는 노드.
명령 문자열 자체에 대상 선택자(<code>@s</code>/<code>@a</code>/<code>@n</code>/<code>fish:&lt;id&gt;</code>)와
파이프라인(<code>|</code>)을 포함할 수 있으므로, "특정 플레이어/서버 기준 실행"을
명령 문자열로 표현한다. 시나리오 전용 <code>give-if-missing &lt;item&gt; [count] [target]</code>
명령은 대상별 인벤토리를 확인해 해당 아이템을 보유하지 않은 대상에게만 지급한다.

 [ScenarioGameRules](MultiplayerInfrastructure.Scenario.ScenarioGameRules.md)

시나리오 실행에 적용되는 서버 게임 규칙.

 [ScenarioGraph](MultiplayerInfrastructure.Scenario.ScenarioGraph.md)

 [ScenarioGraphLoader](MultiplayerInfrastructure.Scenario.ScenarioGraphLoader.md)

 [ScenarioInteractable](MultiplayerInfrastructure.Scenario.ScenarioInteractable.md)

상호작용 시 시나리오를 시작하는 컴포넌트입니다.
NPC 또는 오브젝트에 부착합니다.

 [ScenarioInteractionNode](MultiplayerInfrastructure.Scenario.ScenarioInteractionNode.md)

 [ScenarioInteractionSignals](MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.md)

시나리오 도메인 인터랙션 "완료 신호"를 다루는 얇은 헬퍼.

설계 메모(TODO-SPEC-2):
- 엔진의 Validator 는 RegistryContains 조건 + RegistryType.RuntimeState 로
  "특정 식별자가 등록되어 있는가"를 이미 검사할 수 있다. 따라서 별도의 신규
  ScenarioValidatorCondition/RuleType 없이도 인터랙션 게이팅이 가능하다.
- 이 클래스는 그 위에 의도를 드러내는 얇은 래퍼만 제공한다(신규 저장소 없음).
  게임플레이 코드가 인터랙션 완료 시 Raise(signalId) 를 호출하고,
  시나리오 JSON 은 Validator(RegistryContains, RuntimeState, signalId) 로 검사한다.

변환 규칙(요약): JSON 의 `todo.validate.<cond>` (InvokeEvent 스텁) 은
  Validator 노드(condition=RegistryContains, rule.registryType=RuntimeState,
  rule.registryIdentifier=`sig.<cond>`) 로 치환한다.

 [ScenarioInvokeEventNode](MultiplayerInfrastructure.Scenario.ScenarioInvokeEventNode.md)

 [ScenarioItemRequirement](MultiplayerInfrastructure.Scenario.ScenarioItemRequirement.md)

시나리오 그래프에서 요구 아이템을 (식별자 + 수량)으로 표현하는 도메인 모델.
런타임에는 <xref href="MultiplayerInfrastructure.InteractableEntity.ItemRequirement" data-throw-if-not-resolved="false"></xref> 로 변환되어 사용된다.

 [ScenarioItemSubmissionConfigNode](MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.md)

아이템 제출 Interactable 을 사전 설정하는 시나리오 노드.

두 가지 방식으로 대상 Interactable 을 확보한다:
 1) 프리셋 스폰: <xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.PresetIdentifier" data-throw-if-not-resolved="false"></xref> 로 등록된 EntityPreset(ItemSubmissionInteractable 프리팹)을 스폰한다.
    스폰은 서버 권한이 필요하므로 서버/오프라인 컨텍스트에서만 수행된다.
 2) 기존 참조: <xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.TargetIdentifier" data-throw-if-not-resolved="false"></xref> (또는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.TargetStateKey" data-throw-if-not-resolved="false"></xref>)로 이미 배치/스폰된
    ItemSubmissionInteractable 을 식별자로 찾는다(예: 의사 NPC 에 부착된 것).

확보한 Interactable 에 대해 다음을 오버라이드한다(프리셋 기본값 위에 노드 값이 우선):
 - 요구 아이템 목록(<xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.RequiredItems" data-throw-if-not-resolved="false"></xref>): 비어 있지 않으면 덮어쓴다.
 - 완료 시 올릴 서버 세션 전역 신호(<xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.CompletionSignalIdentifier" data-throw-if-not-resolved="false"></xref>).
 - 활성/비활성(<xref href="MultiplayerInfrastructure.Scenario.ScenarioItemSubmissionConfigNode.Enabled" data-throw-if-not-resolved="false"></xref>).

제출이 완료되면 ItemSubmissionInteractable 이 완료 신호를 서버 권한 경로로 올리며,
이를 Validator(RegistryContains, RuntimeState, sig.&lt;signal&gt;) 노드로 게이팅할 수 있다.

 [ScenarioLifecycleNode](MultiplayerInfrastructure.Scenario.ScenarioLifecycleNode.md)

시나리오 실행 수명주기를 그래프 안에서 명시적으로 제어한다.
Cleanup은 다음 노드로 진행하고, End와 Restart는 현재 흐름을 종료한다.

 [ScenarioManualEntrypointNode](MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.md)

시나리오 흐름의 특정 지점에 별칭을 붙이는 표식 노드.

<p>평상시에는 아무 일도 하지 않고 곧바로 <xref href="MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.NextIdentifier" data-throw-if-not-resolved="false"></xref> 로 넘어간다.
운영자가 <code>/scenario enter &lt;identifier&gt;</code> 명령을 실행하면 재생 위치가 이 노드로
건너뛴다.</p>

<p>명령으로 진입한 경우에 한해 <xref href="MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.ManualEnterSetupIdentifier" data-throw-if-not-resolved="false"></xref> 체인을 먼저
실행한다. 건너뛴 구간에서 만들어졌어야 할 인게임 상황(엔티티 스폰, 퀘스트 발행, 신호 등)을
여기서 맞춰 놓는 용도다. 체인이 끝나면 이 노드로 돌아와 <xref href="MultiplayerInfrastructure.Scenario.ScenarioManualEntrypointNode.NextIdentifier" data-throw-if-not-resolved="false"></xref> 로
진행한다.</p>

 [ScenarioNPCControlNode](MultiplayerInfrastructure.Scenario.ScenarioNPCControlNode.md)

NPC의 런타임 데이터/Interact를 갱신하거나 이동을 지시하는 통합 노드.

 [ScenarioNPCMoveNode](MultiplayerInfrastructure.Scenario.ScenarioNPCMoveNode.md)

 [ScenarioNetworkRelay](MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay.md)

시나리오 도메인의 서버 권한(authoritative) 신호 중계기.

설계 근거(G-8, P1 단계):
- 시나리오 게이팅에 쓰이는 완료 신호는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals" data-throw-if-not-resolved="false"></xref> 를 통해
  <code>RegistryType.RuntimeState</code> 레지스트리(정적·비네트워크)에 기록된다.
- 인터랙션은 각 클라이언트 컨텍스트에서 일어나므로, 신호가 클라이언트 로컬에만 남으면
  "한 플레이어의 행동이 다른 플레이어 브랜치의 게이트를 통과시키는" 다인 협력이 성립하지 않는다.
- 이 중계기는 클라이언트가 올린 신호를 ServerRpc 로 서버에 보고하여
  서버의 단일 권위 RuntimeState 에 기록되게 한다. (Validator 판정은 서버에서 수행)

사용:
- 게임플레이 코드는 그대로 <xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(System.String)" data-throw-if-not-resolved="false"></xref> 만 호출하면 된다.
  서버 컨텍스트면 직접 기록, 클라이언트 컨텍스트면 이 중계기를 통해 서버로 보고된다.

추가 범위(P3 1차): 서버가 단일 그래프 상태기를 실행하고, 클라이언트는 시작/표시/입력 보고만
수행하도록 Dialogue·Choice의 표현 RPC를 제공한다. 이 경로는 지원 노드 집합으로 검증된
그래프에만 사용하며, 병렬 역할 브랜치와 미구현 표현 노드는 기존 호환 경로로 폴백한다.
단일 플레이어(호스트 단독)에서는 서버=클라 이므로 동작이 기존과 동일하다.

 [ScenarioNpcInteractControlNode](MultiplayerInfrastructure.Scenario.ScenarioNpcInteractControlNode.md)

NPC 에 부착된(혹은 참조로 연결할) Interactable 을 추가/제거하거나 활성/비활성 전환하는 시나리오 노드.

예: 의사 NPC 에게 "아이템 제출"(ItemSubmissionInteractable) 상호작용을 시나리오 진행 시점에 활성화하거나,
시나리오 종료 후 비활성화한다.

- <xref href="MultiplayerInfrastructure.Scenario.ScenarioNpcInteractControlNode.NpcIdentifier" data-throw-if-not-resolved="false"></xref>: 대상 NPC(Registry 의 Npc 식별자).
- <xref href="MultiplayerInfrastructure.Scenario.ScenarioNpcInteractControlNode.InteractableIdentifier" data-throw-if-not-resolved="false"></xref>: 대상 Interactable 의 식별자.
  Add 시 Registry(InteractableEntity)에서 해당 식별자의 IInteract 컴포넌트를 찾아 NPC 의 커스텀 소스로 추가한다.
  Enable/Disable 시 대상 Interactable 이 <xref href="MultiplayerInfrastructure.InteractableEntity.IInteractToggleable" data-throw-if-not-resolved="false"></xref> 을 구현하면 활성 상태를 전환한다.

 [ScenarioParallelAssignmentState](MultiplayerInfrastructure.Scenario.ScenarioParallelAssignmentState.md)

서버가 확정해 TargetRpc로 전달한 병렬 브랜치 배정의 클라이언트측 읽기 모델.

그래프 실행 권위는 보유하지 않는다. 표현/퀘스트/상호작용 어댑터가 "이 클라이언트가
이 parallel node의 어느 branch를 맡았는가"를 읽는 유일한 복제 결과다.

 [ScenarioParallelBranch](MultiplayerInfrastructure.Scenario.ScenarioParallelBranch.md)

 [ScenarioParallelNode](MultiplayerInfrastructure.Scenario.ScenarioParallelNode.md)

 [ScenarioParallelRoleAllocator](MultiplayerInfrastructure.Scenario.ScenarioParallelRoleAllocator.md)

서버 권위 병렬 실행에서 사용하는 결정적 역할 배정기.

후보 목록은 호출자가 서버의 세션/태그 상태로 계산해 전달한다. 이 타입은 Unity·FishNet·Registry에
의존하지 않으므로, 서버와 테스트가 같은 배정 규칙을 사용한다.

 [ScenarioPatientMedicalStatePresetNode](MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.md)

환자 엔티티의 의료 상태(PatientDescriptor 및 PatientMedicalState)를 일괄 초기화(프리셋)하는 노드.

<p>
지정한 엔티티 식별자(<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TargetEntityIdentifier" data-throw-if-not-resolved="false"></xref>)에 해당하는 <xref href="TriageTrainer.Entity.PatientController" data-throw-if-not-resolved="false"></xref>를
찾아 아래 필드를 덮어쓴다. <b>null 인 항목은 현재 값을 유지</b>하므로,
설정이 필요한 필드만 기입하면 된다.
</p>

<p>새 환자 상태 필드를 추가할 때도 이 노드를 통해 프리셋 값을 설정할 수 있다.
아래 프로퍼티 목록에 필드를 추가하고, DTO(<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNodeDTO" data-throw-if-not-resolved="false"></xref>),
로더(<code>ScenarioGraphLoader.ConvertPatientMedicalStatePreset</code>),
컨트롤러(<code>PatientController.ApplyMedicalStatePreset</code>) 세 곳에도 동일하게 추가한다.</p>

<p>
── 프리셋 가능 필드 목록 ──

<table><thead><tr><th class="term">필드 그룹</th><th class="term">프로퍼티</th><th class="term">타입</th></tr></thead><tbody><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.Sex" data-throw-if-not-resolved="false"></xref></td><td class="term">Sex?</td></tr><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.Age" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.Name" data-throw-if-not-resolved="false"></xref></td><td class="term">string</td></tr><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodType" data-throw-if-not-resolved="false"></xref></td><td class="term">BloodType?</td></tr><tr><td class="term">환자 기술자</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.IntendedTriage" data-throw-if-not-resolved="false"></xref></td><td class="term">TriageLevel?</td></tr><tr><td class="term">의료 상태</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessGcs" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessEyeOpening" data-throw-if-not-resolved="false"></xref></td><td class="term">EyeOpeningResponse?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessVerbal" data-throw-if-not-resolved="false"></xref></td><td class="term">VerbalResponse?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessMotor" data-throw-if-not-resolved="false"></xref></td><td class="term">MotorResponse?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessLocLabel" data-throw-if-not-resolved="false"></xref></td><td class="term">LOCLabel?</td></tr><tr><td class="term">의료 상태/의식</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessPupillaryResponse" data-throw-if-not-resolved="false"></xref></td><td class="term">PupillaryResponse?</td></tr><tr><td class="term">의료 상태/호흡</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.RespirationAwRR" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/호흡</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.RespirationTypeValue" data-throw-if-not-resolved="false"></xref></td><td class="term">RespirationType?</td></tr><tr><td class="term">의료 상태/맥박</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.PulseRate" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/맥박</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.PulseForceType" data-throw-if-not-resolved="false"></xref></td><td class="term">BloodPulseForceType?</td></tr><tr><td class="term">의료 상태/혈압</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodPressureSystolic" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/혈압</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodPressureDiastolic" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태/피부</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.SkinColorHue" data-throw-if-not-resolved="false"></xref></td><td class="term">SkinColorHue?</td></tr><tr><td class="term">의료 상태/피부</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.SkinTemperatureType" data-throw-if-not-resolved="false"></xref></td><td class="term">SkinTemperatureType?</td></tr><tr><td class="term">의료 상태/체온</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BodyTemperatureCelsius" data-throw-if-not-resolved="false"></xref></td><td class="term">float?</td></tr><tr><td class="term">의료 상태/모니터</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.Spo2" data-throw-if-not-resolved="false"></xref></td><td class="term">int?</td></tr><tr><td class="term">의료 상태</td><td class="term"><xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.IsCardiacArrest" data-throw-if-not-resolved="false"></xref></td><td class="term">bool?</td></tr></tbody></table>
</p>

<p>
── 측정 불가/무의식/없음 표현 ──
수치 필드(<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.ConsciousnessGcs" data-throw-if-not-resolved="false"></xref>, <xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.RespirationAwRR" data-throw-if-not-resolved="false"></xref>, <xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.PulseRate" data-throw-if-not-resolved="false"></xref>,
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodPressureSystolic" data-throw-if-not-resolved="false"></xref>, <xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.BloodPressureDiastolic" data-throw-if-not-resolved="false"></xref>)에 <b>-1</b>을 지정하면
"무의식 / 호흡 없음 / 측정 불가" 등 <b>값이 존재하지 않는 상태</b>를 의미한다.
이 경우 환자 상태 모니터에는 해당 수치가 <code>-?-</code> 로 표시된다.
(null은 "현재 값 유지", -1은 "측정 불가"로 서로 다른 의미임에 유의한다.)
</p>

<p>
── 전이(Transition) 방식 ──
<xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TransitionMode" data-throw-if-not-resolved="false"></xref> 로 프리셋 값이 적용되는 방식을 지정한다.
<xref href="MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.Immediate" data-throw-if-not-resolved="false"></xref> 는 즉시 적용,
<xref href="MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.Gradual" data-throw-if-not-resolved="false"></xref> 은 <xref href="MultiplayerInfrastructure.Scenario.ScenarioPatientMedicalStatePresetNode.TransitionDurationSeconds" data-throw-if-not-resolved="false"></xref> 동안
수치 값을 현재 값에서 대상 값으로 점차 보간한다.
</p>

 [ScenarioPlayTTSNode](MultiplayerInfrastructure.Scenario.ScenarioPlayTTSNode.md)

TTSService를 통해 지정된 identifier의 음성을 재생합니다.
Variables에 지정된 값으로 동적 세그먼트를 오버라이드합니다.

 [ScenarioPlayerMoveNode](MultiplayerInfrastructure.Scenario.ScenarioPlayerMoveNode.md)

 [ScenarioPlayerTagNode](MultiplayerInfrastructure.Scenario.ScenarioPlayerTagNode.md)

플레이어 태그를 추가·제거·변경하는 노드.
Scope가 Current일 때는 시나리오 실행 중인 플레이어(_scenarioOwnerClientId)를 대상으로 합니다.
Scope가 All일 때는 현재 접속한 모든 플레이어에게 적용됩니다.

 [ScenarioQuestControlNode](MultiplayerInfrastructure.Scenario.ScenarioQuestControlNode.md)

 [ScenarioQuestMarkNode](MultiplayerInfrastructure.Scenario.ScenarioQuestMarkNode.md)

퀘스트 마크(상호작용 아이콘 대체 / NPC 머리 위 아이콘)를 시나리오 그래프에서 명시적으로 켜고 끈다.
퀘스트 정의의 presentationBindings 와 동일한 표시 경로(<xref href="MultiplayerInfrastructure.Quest.QuestPresentationService" data-throw-if-not-resolved="false"></xref>)를 사용하므로
대상 종류·아이콘·우선순위 규칙이 퀘스트가 만든 마크와 같다. 표시 수명이 퀘스트 수명과 다를 때 사용한다.

 [ScenarioQuestWaypointHighlightNode](MultiplayerInfrastructure.Scenario.ScenarioQuestWaypointHighlightNode.md)

 [ScenarioQuizNode](MultiplayerInfrastructure.Scenario.ScenarioQuizNode.md)

 [ScenarioReturnToOriginNode](MultiplayerInfrastructure.Scenario.ScenarioReturnToOriginNode.md)

곁가지 체인의 종료 표식. 여기 닿으면 체인을 닫고 원래 흐름으로 돌아간다.

<ul><li>ManualEntrypoint 준비 체인이면 진입 지점으로 돌아가 그 노드의 NextIdentifier 로 이어진다.</li><li>병렬 브랜치면 그 브랜치만 완료 처리된다.</li><li>메인 흐름에서 만나면 아무 일도 하지 않고 NextIdentifier 로 넘어간다.</li></ul>

<p><b>왜 노드인가:</b> 예전에는 곁가지의 끝을 "NextIdentifier 가 비어 있음"으로 표시했다.
종료 의도가 데이터에 드러나지 않아, 나중에 누군가 그 노드에 다음 노드를 연결하는 순간
곁가지가 원래 흐름으로 그대로 흘러가 버렸다. 표식을 노드로 두면 그래프에서 눈에 보이고,
출력 포트가 없어 실수로 이어 붙이는 것 자체가 불가능하다.</p>

<p>이 노드는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioReturnToOriginNode.NextIdentifier" data-throw-if-not-resolved="false"></xref> 를 쓰지 않는다. 그래프 에디터도 출력 포트를
만들지 않으며, 값이 남아 있으면 진단이 경고한다.</p>

 [ScenarioSchemaValidationException](MultiplayerInfrastructure.Scenario.ScenarioSchemaValidationException.md)

예외: 시나리오 JSON이 스키마 검증에 실패했을 때 사용합니다.

 [ScenarioServerInternalSignalNode](MultiplayerInfrastructure.Scenario.ScenarioServerInternalSignalNode.md)

 [ScenarioServerInternalSignalRegistry](MultiplayerInfrastructure.Scenario.ScenarioServerInternalSignalRegistry.md)

서버 권위 내부 신호를 관리하는 FIFO 레지스트리.

register/resolve 어느 쪽이 먼저 오더라도 나중에 들어온 반대쪽과 매칭된다.
targetId 는 `@s`(self) 또는 `@m`(server) 같은 서버 내부 대상 식별자와
일반 플레이어 식별자 모두를 허용한다.

 [ScenarioSignalCounterNode](MultiplayerInfrastructure.Scenario.ScenarioSignalCounterNode.md)

접두사(prefix)로 시작하는 서로 다른(distinct) 시나리오 신호가 몇 개나 올라왔는지 세어,
임계치에 도달하면 출력 신호를 발신하는 카운터를 제어하는 노드.

<p>
시나리오 신호는 sticky(존재 여부만, 최초 1회만 <code>OnSignalRegistered</code> 발생)이므로,
"같은 신호가 N번" 을 셀 수는 없다. 대신 <xref href="MultiplayerInfrastructure.Scenario.ScenarioSignalCounterNode.SourceSignalPrefix" data-throw-if-not-resolved="false"></xref> 로 시작하는
<b>서로 다른 신호 식별자</b>의 개수를 센다. 예:
</p>
<ul><li>트리아지 구역 도착 3명: prefix <code>enter_triage_zone_</code> 로
  <code>enter_triage_zone_patient_b</code> / <code>_patient_c</code> / <code>_patient_dummy_d_b</code> 3개를 세어 threshold=3.</li><li>환자 A 18G 2개: prefix <code>insert_iv_patient_a_</code> 로
  <code>insert_iv_patient_a_left</code> / <code>_right</code> 2개를 세어 threshold=2.</li></ul>

<p>
등록 시점에 이미 올라와 있는(정규화 후 prefix 매칭) 신호도 초기 카운트에 포함한다.
임계치 도달 시 <xref href="MultiplayerInfrastructure.Scenario.ScenarioSignalCounterNode.OutputSignalIdentifier" data-throw-if-not-resolved="false"></xref> 를 <code>Raise</code> 하고, 카운터는 자동 해제된다(1회성).
시나리오 종료 시 모든 카운터가 정리된다. 동일 <xref href="MultiplayerInfrastructure.Scenario.ScenarioSignalCounterNode.CounterIdentifier" data-throw-if-not-resolved="false"></xref> 재등록은 교체된다.
</p>

 [ScenarioSignalCounters](MultiplayerInfrastructure.Scenario.ScenarioSignalCounters.md)

<code>SignalCounter</code> 노드가 등록한 카운터를 관리한다. 접두사로 시작하는 서로 다른(distinct)
시나리오 신호의 개수를 세어, 임계치에 도달하면 출력 신호를 발신한다.

<p>
시나리오 신호는 sticky 이며 <code>OnSignalRegistered</code> 는 각 신호의 최초 등록 시 1회만 발생한다.
따라서 "같은 신호 N번" 이 아니라 "접두사 매칭 신호의 distinct 개수" 를 센다. 등록 시점에 이미
올라와 있던 매칭 신호도 초기 카운트에 포함한다.
</p>

<p>
임계치 도달 시 출력 신호를 <code>Raise</code> 하고 해당 카운터를 자동 제거(1회성)한다.
재진입/중복 발신 방지를 위해 <xref href="MultiplayerInfrastructure.Scenario.ScenarioConditionalSignalListeners" data-throw-if-not-resolved="false"></xref> 와 동일한 큐 기반
디스패치 방어를 사용한다.
</p>

 [ScenarioSignalListenerNode](MultiplayerInfrastructure.Scenario.ScenarioSignalListenerNode.md)

실제 gameplay 신호를 조건부 시나리오 신호로 변환하는 리스너를 제어한다.

 [ScenarioSignalParameterStore](MultiplayerInfrastructure.Scenario.ScenarioSignalParameterStore.md)

시나리오 신호의 마지막 JSON 파라미터를 신호·발신 플레이어별로 보관한다.
서버가 권위 원본을 기록하고, 클라이언트는 네트워크 중계기로 받은 미러만 갱신한다.

 [ScenarioSignalPlayerContext](MultiplayerInfrastructure.Scenario.ScenarioSignalPlayerContext.md)

서버 권위 콜백 안에서 실제 행동 플레이어를 신호 발신자로 보존하는 일시적 컨텍스트.
네트워크 요청자 정보를 잃는 하위 콜백은 이 범위 안에서 <xref href="MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(System.String)" data-throw-if-not-resolved="false"></xref>를 호출한다.

 [ScenarioSoundNode](MultiplayerInfrastructure.Scenario.ScenarioSoundNode.md)

 [ScenarioStateUpdateNode](MultiplayerInfrastructure.Scenario.ScenarioStateUpdateNode.md)

 [ScenarioTTSBakeScanner](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.md)

프로젝트 내 모든 시나리오 그래프(JSON)를 스캔하여, PlayTTS 플래그가 켜져 있고
변수를 포함하지 않는(=bake 가능한) 인라인 텍스트(Dialogue/DisinteractableDialogue/Choice/Quiz 콘텐츠)를 수집하고
사전 합성(bake)한다.

이 클래스는 에디터 전용(<code>#if UNITY_EDITOR</code>)이지만 <code>Editor</code> 폴더 밖(런타임 어셈블리)에
위치한다. 이렇게 하면 <code>Assembly-CSharp</code>(에디터 정의 포함)로 컴파일되어,
동일 어셈블리의 에디터 훅(TTSPlayModeValidator, TTSBuildPreprocessor)에서도 참조할 수 있다.

수집 결과는 다음 두 곳에서 사용된다.
  · 인라인 오디오 Baker (사전 합성)
  · 플레이 모드 진입 / 빌드 시 bake 상태(미bake/dirty) 검사

 [ScenarioTTSVoiceProfile](MultiplayerInfrastructure.Scenario.ScenarioTTSVoiceProfile.md)

노드와 시나리오 정의 탭에서 공유하는 TTS 프로필입니다. Preset을 지정하면 내장된
voice_styles 값이 사용되며, null이면 아래 JSON 필드로 사용자 프로필을 정의합니다.

 [ScenarioTextResolver](MultiplayerInfrastructure.Scenario.ScenarioTextResolver.md)

시나리오 표시 문자열의 플레이어 지정자(@s, @t=[tag, fallback])를 해석한다.

 [ScenarioTimeControlNode](MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.md)

시간 표시(HUD)를 제어하는 시나리오 노드.

<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.Operation" data-throw-if-not-resolved="false"></xref> 에 따라 생성/흐름/표시/삭제를 각각 수행하며, 실행 즉시
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeRelay" data-throw-if-not-resolved="false"></xref> 를 통해 서버 권한으로 모든 클라이언트에 전파하고
곧바로 다음 노드로 진행한다(대기하지 않는다).

식별자(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeControlNode.TimerId" data-throw-if-not-resolved="false"></xref>)로 여러 타이머를 동시에 보유할 수 있으나, 화면에 표시되는
타이머는 항상 최대 1개다(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeOperationType.Show" data-throw-if-not-resolved="false"></xref> 로 전환).
카운트다운이 0 에 도달해도 자동으로 숨겨지지 않는다(표시 전환은 Show/Hide/Remove 로만).

 [ScenarioTimeRelay](MultiplayerInfrastructure.Scenario.ScenarioTimeRelay.md)

다중 시간(스톱워치/카운트다운)의 서버 권한(authoritative) 동기화 중계기.

설계 근거:
- 시간 표시는 모든 클라이언트가 동일한 값을 봐야 한다. 그러나 <xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref> 는
  클라이언트마다 독립 실행되고 동기화된 커서가 없으므로, 서버가 각 연산(생성/시작/표시 등)을
  모든 클라이언트에 push 해야 한다.
- 타이머 연산의 유일한 출처는 서버에서 실행되는 시나리오 로직(<xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref>)이다.
  따라서 클라이언트→서버 보고 경로(ServerRpc)는 두지 않는다. 서버 컨텍스트면
  ObserversRpc 로 전 클라이언트에 미러링하고, 네트워크 비활성/중계기 부재면
  로컬(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState" data-throw-if-not-resolved="false"></xref>)에만 적용한다.
- 흐름 시작(Start/Resume) 연산에는 서버 tick 을 함께 실어, 늦게 접속한 클라이언트가
  "발행 이후 이미 흐른 시간"을 계산해 현재 진행 지점부터 표시하도록 보정한다.

단일 플레이어(호스트 단독)에서는 서버=클라 이므로 로컬 적용과 동일하게 동작한다.

 [ScenarioTimeState](MultiplayerInfrastructure.Scenario.ScenarioTimeState.md)

다중 시간(스톱워치/카운트다운)의 로컬 스냅샷 모델.

설계 개요:
- 식별자(timerId)로 여러 타이머를 동시에 보유한다. 다만 화면에 표시되는 타이머는 항상 최대 1개이며,
  <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState._shownTimerId" data-throw-if-not-resolved="false"></xref> 가 그 대상을 가리킨다.
- 생성(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Create(System.String%2cMultiplayerInfrastructure.Scenario.ScenarioTimeDirection%2cSystem.Double%2cSystem.Double)" data-throw-if-not-resolved="false"></xref>)은 "정지" 상태로 만들고, 흐름은 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Start(System.String%2cSystem.Double)" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Resume(System.String%2cSystem.Double)" data-throw-if-not-resolved="false"></xref> 로,
  화면 표시는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Show(System.String)" data-throw-if-not-resolved="false"></xref>/<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeState.Hide" data-throw-if-not-resolved="false"></xref> 로 "별도 제어"한다. 카운트다운이 0 에 도달해도
  자동으로 숨기지 않는다(표시 전환은 오직 Show/Hide/Remove 로만 발생).

멀티플레이어:
- <xref href="MultiplayerInfrastructure.Scenario.ScenarioController" data-throw-if-not-resolved="false"></xref> 는 클라이언트마다 독립 실행되므로, 서버가 각 연산을 push 하고
  각 클라이언트가 로컬 기준점(TimerInstance._localAnchorRealtime, realtime)에서 tick 한다.
  서버 동기화는 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeRelay" data-throw-if-not-resolved="false"></xref> 가, 표시는
  <xref href="MultiplayerInfrastructure.UI.TimeDisplayUIController" data-throw-if-not-resolved="false"></xref> HUD 가 담당한다.

 [ScenarioTimeSyncSettings](MultiplayerInfrastructure.Scenario.ScenarioTimeSyncSettings.md)

시간 표시(스톱워치/카운트다운)의 주기적 재동기화 밀도를 담는 서버 측 설정.

설계:
- 타이머 값은 연산 시점(Create/Start/Show 등)에만 전파되고 그 후엔 각 클라이언트가 로컬로 tick 하므로
  시간이 지날수록 드리프트가 누적될 수 있다. 서버는 이 설정에 따라 "현재 표시 중인 타이머"의
  권위 값을 주기적으로 재전파하여 모든 클라이언트를 다시 맞춘다(<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeRelay" data-throw-if-not-resolved="false"></xref>).
- 밀도는 tick / milliseconds / seconds 세 단위로 지정할 수 있으며, 기본값은 1초에 1회이다.
- 이 설정은 서버 권위이며, 조정은 오직 명령어(<code>timesync</code>)로만 수행한다(런타임 UI/인스펙터 노출 없음).

내부적으로는 원본 값+단위를 보존(명령 에코/조회용)하되, 실제 재전파 간격 판정은
<xref href="MultiplayerInfrastructure.Scenario.ScenarioTimeSyncSettings.GetIntervalTicks" data-throw-if-not-resolved="false"></xref> 가 반환하는 tick 수로 수행한다.

 [ScenarioTriageAssessControlNode](MultiplayerInfrastructure.Scenario.ScenarioTriageAssessControlNode.md)

특정 환자(엔티티)에 대해 트리아지(Triage) 평가 인터랙션을 활성화/비활성화하는 시나리오 노드.

<p>
시나리오 진행 중 플레이어가 특정 환자를 트리아지 분류할 수 있는 시점을 제어하는 데 사용한다.
대상 엔티티는 레지스트리에서 <xref href="MultiplayerInfrastructure.Scenario.ScenarioTriageAssessControlNode.TargetEntityIdentifier" data-throw-if-not-resolved="false"></xref> 로 조회되며, 해당 엔티티가
트리아지 평가 제어를 지원하는 대상(<xref href="MultiplayerInfrastructure.Entity.IScenarioTriageAssessTarget" data-throw-if-not-resolved="false"></xref>)이어야 한다.
</p>

 [ScenarioTriggerZone](MultiplayerInfrastructure.Scenario.ScenarioTriggerZone.md)

트리거 존 진입 시 자동으로 시나리오를 시작합니다.
신호 계열 존(그래프 미지정)은 재생 중인 시나리오가 있을 때만 감지/발신합니다
(<xref href="MultiplayerInfrastructure.Scenario.ScenarioController.HasActiveScenario" data-throw-if-not-resolved="false"></xref> 기준).

 [ScenarioValidatorNode](MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.md)

 [ScenarioValidatorRootCondition](MultiplayerInfrastructure.Scenario.ScenarioValidatorRootCondition.md)

 [ScenarioValidatorRule](MultiplayerInfrastructure.Scenario.ScenarioValidatorRule.md)

 [ScenarioWaypointDefinition](MultiplayerInfrastructure.Scenario.ScenarioWaypointDefinition.md)

시나리오 수명주기 동안 사용할 waypoint anchor 정의.
시작 전에 등록되어 이동, 퀘스트, 하이라이트 노드에서 같은 identifier로 참조할 수 있다.

 [TTSVoiceProfileDefinition](MultiplayerInfrastructure.Scenario.TTSVoiceProfileDefinition.md)

내장 TTS 음성 리터럴을 노출하는 변경 불가 정의 객체입니다.

 [TTSVoiceProfileDefinitions](MultiplayerInfrastructure.Scenario.TTSVoiceProfileDefinitions.md)

배포된 모든 voice_styles 파일의 사전 준비된 정의입니다. enum은 이 테이블의 키이며,
TTS 처리 코드는 스타일 이름을 추측하지 않고 여기의 완성된 리터럴을 사용해야 합니다.

 [Waypoint](MultiplayerInfrastructure.Scenario.Waypoint.md)

### Structs

 [ScenarioNetworkRelay.LineTopologyPair](MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay.LineTopologyPair.md)

 [ScenarioNodeVisitTiming](MultiplayerInfrastructure.Scenario.ScenarioNodeVisitTiming.md)

단일 노드 방문의 진입 및 이탈 시각.

 [ScenarioSignalParameter](MultiplayerInfrastructure.Scenario.ScenarioSignalParameter.md)

신호 식별자·발신 플레이어별 마지막 JSON 파라미터의 읽기 모델.

 [ScenarioTimeValue](MultiplayerInfrastructure.Scenario.ScenarioTimeValue.md)

단위와 값을 함께 보존하는 시나리오 공통 시간 값.

### Interfaces

 [IScenarioArrivalSignalEntityResolver](MultiplayerInfrastructure.Scenario.IScenarioArrivalSignalEntityResolver.md)

트리거에 닿은 운반체가 대신 보고해야 할 시나리오 엔티티를 제공한다.

 [IScenarioNode](MultiplayerInfrastructure.Scenario.IScenarioNode.md)

### Enums

 [CareZoneMissingEquipmentFallback](MultiplayerInfrastructure.Scenario.CareZoneMissingEquipmentFallback.md)

 [PatientMedicalStateTransitionMode](MultiplayerInfrastructure.Scenario.PatientMedicalStateTransitionMode.md)

프리셋 값이 대상 환자에게 적용되는 방식.

 [ScenarioActingNpcInteractionType](MultiplayerInfrastructure.Scenario.ScenarioActingNpcInteractionType.md)

 [ScenarioActingNpcType](MultiplayerInfrastructure.Scenario.ScenarioActingNpcType.md)

 [ScenarioChatPrintTarget](MultiplayerInfrastructure.Scenario.ScenarioChatPrintTarget.md)

<xref href="MultiplayerInfrastructure.Scenario.ScenarioChatPrintNode" data-throw-if-not-resolved="false"></xref> 가 텍스트를 어디에 출력할지 지정하는 대상 플래그.
(Validator 의 <xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorFailureReportTarget" data-throw-if-not-resolved="false"></xref> 와 동일한 패턴)

 [ScenarioConcurrencyConflictPolicy](MultiplayerInfrastructure.Scenario.ScenarioConcurrencyConflictPolicy.md)

두 개 이상의 시나리오 흐름이 동시에 대화창 계열 UI(Dialogue/Choice/Quiz)를
점유하려 할 때의 처리 정책.

 [ScenarioDelayWaitUntil](MultiplayerInfrastructure.Scenario.ScenarioDelayWaitUntil.md)

 [ScenarioEntityStateOperationKind](MultiplayerInfrastructure.Scenario.ScenarioEntityStateOperationKind.md)

<xref href="MultiplayerInfrastructure.Scenario.ScenarioEntityInitNode" data-throw-if-not-resolved="false"></xref> 가 엔티티에 적용하는 초기 상태 항목의 종류.

 [ScenarioEntityStateSignalBindingOperation](MultiplayerInfrastructure.Scenario.ScenarioEntityStateSignalBindingOperation.md)

 [ScenarioInteractionActorScope](MultiplayerInfrastructure.Scenario.ScenarioInteractionActorScope.md)

 [ScenarioInteractionType](MultiplayerInfrastructure.Scenario.ScenarioInteractionType.md)

 [ScenarioInvokeEventMoveNextBehavior](MultiplayerInfrastructure.Scenario.ScenarioInvokeEventMoveNextBehavior.md)

 [ScenarioLifecycleOperation](MultiplayerInfrastructure.Scenario.ScenarioLifecycleOperation.md)

 [ScenarioMoveDestinationType](MultiplayerInfrastructure.Scenario.ScenarioMoveDestinationType.md)

 [ScenarioMoveMode](MultiplayerInfrastructure.Scenario.ScenarioMoveMode.md)

 [ScenarioNPCControlMode](MultiplayerInfrastructure.Scenario.ScenarioNPCControlMode.md)

 [ScenarioNPCInteractCrudOperation](MultiplayerInfrastructure.Scenario.ScenarioNPCInteractCrudOperation.md)

 [ScenarioNodeType](MultiplayerInfrastructure.Scenario.ScenarioNodeType.md)

 [ScenarioNpcInteractControlOperation](MultiplayerInfrastructure.Scenario.ScenarioNpcInteractControlOperation.md)

 [ScenarioParallelAllocationType](MultiplayerInfrastructure.Scenario.ScenarioParallelAllocationType.md)

 [ScenarioParallelMismatchHandling](MultiplayerInfrastructure.Scenario.ScenarioParallelMismatchHandling.md)

 [ScenarioPlayerTagMatchMode](MultiplayerInfrastructure.Scenario.ScenarioPlayerTagMatchMode.md)

ParallelBranch의 RequiredPlayerTags 매칭 방식.

 [ScenarioPlayerTagOperationType](MultiplayerInfrastructure.Scenario.ScenarioPlayerTagOperationType.md)

PlayerTag 노드의 태그 조작 타입.

 [ScenarioPlayerTagScope](MultiplayerInfrastructure.Scenario.ScenarioPlayerTagScope.md)

PlayerTag 노드의 대상 플레이어 범위.

 [ScenarioQuestFailureStrategy](MultiplayerInfrastructure.Scenario.ScenarioQuestFailureStrategy.md)

 [ScenarioQuestMarkOperationType](MultiplayerInfrastructure.Scenario.ScenarioQuestMarkOperationType.md)

시나리오 그래프가 퀘스트 마크 표시를 켜거나 끄는 연산.

 [ScenarioQuestOperationType](MultiplayerInfrastructure.Scenario.ScenarioQuestOperationType.md)

 [ScenarioServerInternalSignalOperationType](MultiplayerInfrastructure.Scenario.ScenarioServerInternalSignalOperationType.md)

 [ScenarioSignalCounterOperation](MultiplayerInfrastructure.Scenario.ScenarioSignalCounterOperation.md)

 [ScenarioSignalListenerOperation](MultiplayerInfrastructure.Scenario.ScenarioSignalListenerOperation.md)

 [ScenarioTimeDirection](MultiplayerInfrastructure.Scenario.ScenarioTimeDirection.md)

시간 표시 UI(스톱워치/타이머)가 흘러가는 방향.

 [ScenarioTimeOperationType](MultiplayerInfrastructure.Scenario.ScenarioTimeOperationType.md)

시간 표시(스톱워치/카운트다운)에 가할 연산.
생성/흐름/표시가 각각 분리되어 있어, 하나의 노드 타입으로 다중 타이머를 유연하게 제어한다.

 [ScenarioTimeUnit](MultiplayerInfrastructure.Scenario.ScenarioTimeUnit.md)

 [ScenarioValidatorBlockLogTarget](MultiplayerInfrastructure.Scenario.ScenarioValidatorBlockLogTarget.md)

 [ScenarioValidatorCondition](MultiplayerInfrastructure.Scenario.ScenarioValidatorCondition.md)

 [ScenarioValidatorFailureReportTarget](MultiplayerInfrastructure.Scenario.ScenarioValidatorFailureReportTarget.md)

 [ScenarioValidatorMatchMode](MultiplayerInfrastructure.Scenario.ScenarioValidatorMatchMode.md)

RegistryContains root condition의 ValidationRules 결합 방식이다.
기본값인 <xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorMatchMode.All" data-throw-if-not-resolved="false"></xref>은 기존의 AND 동작을 유지한다.

 [ScenarioValidatorOnFailure](MultiplayerInfrastructure.Scenario.ScenarioValidatorOnFailure.md)

 [ScenarioValidatorPlayerScope](MultiplayerInfrastructure.Scenario.ScenarioValidatorPlayerScope.md)

 [ScenarioValidatorRuleCondition](MultiplayerInfrastructure.Scenario.ScenarioValidatorRuleCondition.md)

 [ScenarioValidatorRuleType](MultiplayerInfrastructure.Scenario.ScenarioValidatorRuleType.md)

 [ScenarioValidatorWaitTimeoutBehavior](MultiplayerInfrastructure.Scenario.ScenarioValidatorWaitTimeoutBehavior.md)

<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.WaitForCondition" data-throw-if-not-resolved="false"></xref> 게이트가 <xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorNode.WaitTimeoutSeconds" data-throw-if-not-resolved="false"></xref>
동안 조건을 충족하지 못했을 때의 행동 정책. 기본값(<xref href="MultiplayerInfrastructure.Scenario.ScenarioValidatorWaitTimeoutBehavior.KeepWaiting" data-throw-if-not-resolved="false"></xref>)은 기존 동작(무한 대기)과 동일하다.

 [ScenarioWaitMode](MultiplayerInfrastructure.Scenario.ScenarioWaitMode.md)

 [ScenarioController.State](MultiplayerInfrastructure.Scenario.ScenarioController.State.md)

 [TTSVoiceStyle](MultiplayerInfrastructure.Scenario.TTSVoiceStyle.md)

시나리오 JSON에서 사용하는 음성 스타일 사전 선택지.

### Delegates

 [ScenarioEventIdentifierRegistry.ScenarioEventHandler](MultiplayerInfrastructure.Scenario.ScenarioEventIdentifierRegistry.ScenarioEventHandler.md)

