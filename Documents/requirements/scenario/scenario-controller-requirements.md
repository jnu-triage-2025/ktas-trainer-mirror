---
title: "ScenarioController 실행 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

ScenarioController는 시나리오 그래프를 실행해 학습/훈련 흐름을 제어하는 핵심 기능이다. 사용자에게는 단계별 안내와 분기 진행을 안정적으로 제공해야 한다.

## 상세

- 시나리오는 시작, 진행, 종료 상태를 명확히 가져야 한다.
- 대화, 선택지, 이벤트 호출, 지연, 병렬 분기 등 노드 유형별 동작을 지원해야 한다.
- 현재 노드 변경과 선택 결과는 UI 및 외부 시스템이 구독 가능한 이벤트로 제공되어야 한다.
- 시나리오 종료 시 내부 상태(역할 배정, 임시 저장값)는 초기화되어야 한다.

## 기술적 세부 사항

- State 열거형 기반 상태 머신으로 노드 실행 상태를 관리한다.
- `StartScenario`, `Advance`, `SelectOption`, `EndScenario`가 기본 제어 API다.
- PlayTTS 노드는 TTSService 준비 상태와 캐시 상태를 고려해 재생한다.
- Parallel 노드는 배분 모드(SelfAll/RandomOneAll/SpreadRandom/SpreadOrdinary)를 지원한다.

## 참조

- [api:MultiplayerInfrastructure.Scenario.ScenarioController](../../api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md)
- [api:TextToSpeechService.TTSService](../../api-references/TextToSpeechService.TTSService.md)
- [api:scenario-graph-spec](../content-definitions/scenario/scenario-graph-spec.md)
