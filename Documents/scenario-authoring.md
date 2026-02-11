# 시나리오 작성 가이드

이 문서는 시나리오 텍스트를 ScenarioGraph 문서로 변환하는 기준을 정의합니다.

## 기본 원칙

- 시나리오는 진입점 단위로 분리하여 파일을 작성합니다.
- 각 파일은 [Documents/scenario/_template.md](scenario/_template.md) 형식을 따릅니다.
- 노드 식별자는 접두사 규칙을 지켜 숫자 증가 방식으로 부여합니다.

## 파일 구조

- 시나리오 파일: `Documents/scenario/<scenario_id>.md`
- 예시 참고: [Documents/scenario/_example.md](scenario/_example.md)

## 노드 Identifier 규칙

- `D###`: Dialogue
- `C###`: Choice
- `S###`: Sound
- `PM###`: PlayerMove
- `NM###`: NPCMove
- `CT###`: CameraTarget
- `P###`: Parallel
- `P###-B#`: ParallelBranch
- `E###`: InvokeEvent
- `V###`: Validator
- `Q###`: QuestControl

## 작성 절차

1. 시나리오를 사건 흐름 단위로 나눕니다.
2. 각 흐름을 NodeType으로 매핑합니다.
3. 병렬 수행이 필요한 단계는 Parallel로 구성합니다.
4. 모든 InvokeEvent는 [Documents/scenario/event-registry.md](scenario/event-registry.md)에 기록합니다.
5. 종료 조건을 마지막에 명시합니다.

## 변환 기준 예시

- 방송 안내, 지시 멘트: Dialogue
- 특정 시스템 호출: InvokeEvent
- 역할 분담/동시 수행: Parallel
- 질문/선택: Choice

## 누락 검증

- 시작 노드 Identifier가 존재해야 합니다.
- 모든 NextIdentifier는 실제 노드를 가리켜야 합니다.
- Optional 표기가 있는 경우에도 흐름이 끊기지 않아야 합니다.
- 새로운 NodeType이 필요하다면 기능 제안서를 작성합니다.

## 기능 제안이 필요한 경우

ScenarioNode 정의에 없는 기능이 필요한 경우, `Agents/Proposals/`에 제안서를 작성합니다. 템플릿은 `/.gitlab/issue_templates/Feature Proposal - detailed.md`를 따릅니다.
