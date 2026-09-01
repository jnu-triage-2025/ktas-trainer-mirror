# <a id="TriageTrainer_Scenario"></a> Namespace TriageTrainer.Scenario

### Namespaces

 [TriageTrainer.Scenario.Rubric](TriageTrainer.Scenario.Rubric.md)

### Classes

 [PatientA18gLeftVisualMarker](TriageTrainer.Scenario.PatientA18gLeftVisualMarker.md)

 [PatientA18gRightVisualMarker](TriageTrainer.Scenario.PatientA18gRightVisualMarker.md)

 [PatientAAmbuConnectedVisualMarker](TriageTrainer.Scenario.PatientAAmbuConnectedVisualMarker.md)

 [PatientACentralLineVisualMarker](TriageTrainer.Scenario.PatientACentralLineVisualMarker.md)

 [PatientACriticalQuestStateFlags](TriageTrainer.Scenario.PatientACriticalQuestStateFlags.md)

`patient_a_critical` 시나리오가 사용하는 퀘스트 상태 플래그와, 그 플래그가 여닫는 상호작용 표를 담는다.

<p>
이 시나리오는 상호작용 노출을 엔티티에 저장된 활성 플래그가 아니라
<xref href="MultiplayerInfrastructure.Quest.PlayerQuestStateFlagService" data-throw-if-not-resolved="false"></xref>의 플레이어별 플래그 풀로 판정한다. 엔티티 활성 플래그는
환자 인스턴스 하나에 공유되어 있어서, 어떤 플레이어의 퀘스트 단계가 다른 플레이어의 상호작용
목록까지 바꿔 버린다. 퀘스트가 플레이어별로 발행되므로 노출 판정도 플레이어별이어야 한다.
</p>

<p>
게이트는 이 시나리오가 실행 중일 때만(<xref href="TriageTrainer.Scenario.PatientACriticalQuestStateFlags.IsArmed" data-throw-if-not-resolved="false"></xref>) 동작한다. 표에 없는 상호작용과
다른 시나리오는 기존 판정 경로를 그대로 쓴다.
</p>

 [PatientAEtTubeInsertedVisualMarker](TriageTrainer.Scenario.PatientAEtTubeInsertedVisualMarker.md)

 [PatientAEtTubePreparedVisualMarker](TriageTrainer.Scenario.PatientAEtTubePreparedVisualMarker.md)

 [PatientAGauzeVisualMarker](TriageTrainer.Scenario.PatientAGauzeVisualMarker.md)

 [PatientAGauzeWithPlasterVisualMarker](TriageTrainer.Scenario.PatientAGauzeWithPlasterVisualMarker.md)

 [PatientATPieceConnectedVisualMarker](TriageTrainer.Scenario.PatientATPieceConnectedVisualMarker.md)

 [PatientB20gRightVisualMarker](TriageTrainer.Scenario.PatientB20gRightVisualMarker.md)

 [PatientBGauzeVisualMarker](TriageTrainer.Scenario.PatientBGauzeVisualMarker.md)

 [PatientBGauzeWithPlasterVisualMarker](TriageTrainer.Scenario.PatientBGauzeWithPlasterVisualMarker.md)

 [PatientC20gLeftVisualMarker](TriageTrainer.Scenario.PatientC20gLeftVisualMarker.md)

 [PatientCGauzeVisualMarker](TriageTrainer.Scenario.PatientCGauzeVisualMarker.md)

 [PatientCGauzeWithPlasterVisualMarker](TriageTrainer.Scenario.PatientCGauzeWithPlasterVisualMarker.md)

 [ScenarioActionInteractable](TriageTrainer.Scenario.ScenarioActionInteractable.md)

시나리오에서 요구하는 물체/부위 상호작용을 위한 경량 Interactable 입니다.
상호작용이 확정되면 완료 신호를 올리고, 필요하면 연결된 시각 오브젝트를 표시 또는 숨깁니다.

 [TriageScenarioEventBootstrap](TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)

Registers TriageTrainer scenario event handlers without modifying base infrastructure.

NOTE:
- Current handlers are safe placeholders for MVP wiring.
- Replace each coroutine body with real presentation/interaction logic incrementally.

 [TriageScenarioReferenceMarker](TriageTrainer.Scenario.TriageScenarioReferenceMarker.md)

 [TriageWorldInteractionSignals](TriageTrainer.Scenario.TriageWorldInteractionSignals.md)

트리아지 월드 오브젝트가 자동으로 발생시키는 시나리오 신호의 규칙.

 [TutorialDecoyInteractable](TriageTrainer.Scenario.TutorialDecoyInteractable.md)

튜토리얼의 오답 배송 물품에 사용하는 로컬 안내 상호작용.
실행 중인 메인 시나리오를 중단하지 않고 비점유 다이얼로그만 표시한다.

### Structs

 [TriageScenarioEventBootstrap.PatientBedPair](TriageTrainer.Scenario.TriageScenarioEventBootstrap.PatientBedPair.md)

### Enums

 [TriageScenarioReferenceRole](TriageTrainer.Scenario.TriageScenarioReferenceRole.md)

