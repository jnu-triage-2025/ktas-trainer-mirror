# 온보딩 가이드

## 빠른 시작

1. [Documents/README.md](README.md)를 먼저 읽고, 필요한 문서 링크를 확인합니다.
2. 수정하려는 영역이 무엇인지 결정합니다. (시나리오, 아이템, 인터랙터블, 문서)
3. 해당 영역의 가이드를 읽고, 작업 범위와 산출물을 정리합니다.
4. AI에게 문서와 템플릿을 제공하고, 필요한 추가 질문을 받습니다:
    - 지시문은 /Agents/Templates/에서 찾습니다.
    - 예시 프롬프트는 /Agents/Examples/에서 찾아서 참고합니다.
    - 실제 사용된 프롬프트는 /Agents/Actually Used/에서 찾아서 참고합니다.
    - 지시문 템플릿에 AI가 참고할 문서 링크가 포함되어있습니다. 그대로 사용하세요.
5. 결과물을 검토한 뒤 커밋합니다.

## 작업 범위와 경로 규칙

- 프로젝트 전용 구현은 반드시 `Assets/Modules/TriageTrainer/` 아래에 둡니다.
- 기반 시스템인 `Assets/Modules/MultiplayerInfrastructure/`는 원칙적으로 수정하지 않습니다.
- 기반 시스템 변경이 필요하면 `Agents/Proposals/`에 기능 제안서를 작성합니다.

## 무엇을 어디에 작성하나요

| 작업 유형 | 주로 수정/추가하는 문서 | 산출물 위치 |
|---|---|---|
| 시나리오 작성 | Documents/scenario-authoring.md | Documents/scenario/*.md |
| 아이템 구현 | Documents/item.md | Assets/Modules/TriageTrainer/Prefabs/Items/<identifier>/ |
| 인터랙터블 구현 | Agents/Actually Used/Implement - (구현한 대상의 이름).md | Assets/Modules/TriageTrainer/Prefabs/Interactables/<identifier>/ |
| 문서 보완 | Documents/*.md | Documents/ |

## 식별자 및 네이밍 규칙

- 식별자에는 소문자와 숫자, 밑줄만 사용합니다. 예: `patient_a`, `move_patient_a_to_treatment`.
- 노드 Identifier는 접두사 규칙을 따릅니다. 예: `D001`, `E010`, `P001-B1`.
- 프리팹/폴더 식별자는 `lower_snake_case`로 작성합니다.

## AI에게 전달할 핵심 정보

- 관련 문서 링크와 경로: 예) [Documents/scenario-graph.md](scenario-graph.md), [Documents/scenario/_template.md](scenario/_template.md)
- 기존 구현 또는 예시 파일: 예) [Documents/scenario/patient_a_critical.md](scenario/patient_a_critical.md)
- 작업 목표와 범위: 무엇을 추가/수정할지
- 금지 조건: `MultiplayerInfrastructure` 수정 금지

## 제출 전 체크리스트

- 변경된 파일이 올바른 경로에 있는가
- 새로운 식별자가 문서/레지스트리에 기록되었는가
- Scenario의 NextIdentifier가 모두 유효한가
- 문서에 누락된 기본 정보가 없는가
- 커밋 메시지가 규칙을 따르는가

## 자주 하는 실수

- 기반 시스템 폴더를 직접 수정함
- 식별자 규칙을 지키지 않아 참조가 끊김
- 템플릿을 그대로 남겨 불필요한 문구가 포함됨
- 시나리오에서 NextIdentifier가 비어 있거나 존재하지 않음
