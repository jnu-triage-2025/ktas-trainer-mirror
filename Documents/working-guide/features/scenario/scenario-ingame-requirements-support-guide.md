---
title: "Scenario 인게임 요구사항 계약 운영 가이드"
domain: "content-definitions.scenario"
progress: "2-implementing"
flags: []
---

# Scenario 인게임 요구사항 계약 운영 가이드

## 시나리오 검사와 씬 연결

1. Unity 메뉴 **Tools > Multiplayer Infrastructure > Scenario Requirements**를 연다.
2. Scenario JSON, 선택적인 requirements sidecar, 씬 구성 프로필을 지정하고 **Compile / Validate**를 실행한다.
3. 각 오류의 kind·identifier·source node·field path를 확인한다. `MissingCapability`이면 같은 identifier를 새로 만들기보다 기존 오브젝트에 필요한 컴포넌트를 추가한다.
4. 생성이 허용된 항목만 위치와 factory를 지정해 Preview한다. Preview 결과가 맞으면 Apply한다. Apply 전체는 한 번의 Undo로 되돌릴 수 있다.

NPC·환자처럼 프로젝트 고유 prefab이 필요한 항목은 선언에서 TriageTrainer factory를 명시한다. 추측한
factory나 좌표를 확정 데이터로 저장하지 않는다.

## sidecar 작성 규칙

- 파일명은 `<scenario>.scenario.requirements.json`이고 scenario JSON과 같은 폴더에 둔다.
- `scenarioIdentifier`와 source SHA-256은 대상 scenario와 일치해야 한다.
- unknown property, unknown enum, duplicate JSON property는 오류다.
- `Override`는 추출된 requirement에만 쓴다. 새 항목은 `Declare`를 사용한다.
- AI candidate는 preview/승인 절차를 거친 뒤에만 canonical sidecar에 반영한다.

## 빌드와 런타임

구성 프로필을 Production으로 설정하면 빌드 전 validator가 모든 오류를 수집한 뒤 빌드를 실패시킨다.
이 검증은 씬이나 sidecar를 수정하지 않는다. 런타임은 기본 `ReportOnly`로 관찰하고, 준비가 검증된
환경에서만 `AbortScenarioStart`를 켠다. 이 모드에서는 누락/중복/기능 불일치뿐 아니라 provider가
아직 준비되지 않은 `NotReady` 상태도 새 scenario 시작을 막는다. Abort 실패는 현재 실행 중인
scenario를 종료하지 않고 새 scenario의 시작만 막는다.

## 빠른 점검

- 메뉴 **Tools > Multiplayer Infrastructure > Validate Scenario Requirements Phase 0-1**는 schema,
  compiler, sidecar, scene scanner와 생성/Undo smoke 검증을 실행한다.
- Phase 4 검증은 임시 씬을 만들었다가 항상 삭제한다. 실행 중 열린 씬의 저장 여부를 먼저 확인한다.
- CI에서는 Unity batch mode로 빌드 validator를 실행하고 생성된 report를 보관한다.
