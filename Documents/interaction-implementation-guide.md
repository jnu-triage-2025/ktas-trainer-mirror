# 상호작용 기능 구현 가이드

이 문서는 AI로 상호작용 기능(예: 침대 이동, 앰부백 산소화)을 구현할 때 필요한 기준을 제공합니다. 구현은 반드시 `Assets/Modules/TriageTrainer/` 아래에서만 수행합니다.

## 구현 원칙

- 시나리오의 흐름은 `InvokeEvent`로 트리거합니다.
- 실질 동작은 Interactable/Item/Animator/FX/UI로 나누어 구성합니다.
- 기반 시스템(`MultiplayerInfrastructure`)은 수정하지 않습니다. 변경이 필요하면 제안서를 작성합니다.

## 작업 순서

1. 기능 카드 작성 (필수)
2. EventIdentifier 정의 및 [Documents/scenario/event-registry.md](scenario/event-registry.md) 기록
3. Interactable/Item 필요 여부 판단
4. 프리팹/스크립트 배치
5. 시나리오 문서와 연결 확인

## 기능 카드 템플릿

| 항목 | 내용 |
|---|---|
| 기능명 | |
| EventIdentifier | |
| 실행 주체 | 플레이어/의사 NPC/환자 등 |
| 대상 | 베드/환자/의료기기 등 |
| 선행 조건 | 장비 연결 여부, 역할 등 |
| 입력 | 클릭/홀드/선택지 |
| 출력 | 애니메이션, 상태 변화, UI 메시지 |
| 완료 조건 | N회 수행, 상태 플래그 등 |
| 실패 처리 | 조건 미충족 시 안내 |

## 프리팹/스크립트 규칙

- 스크립트는 `(PascalCaseIdentifier)Controller.cs`로 작성합니다.
- 프리팹은 `Assets/Modules/TriageTrainer/Prefabs/Interactables/<identifier>/` 또는 `Assets/Modules/TriageTrainer/Prefabs/Items/<identifier>/`에 둡니다.

## 예시 1: 침대 이동

| 항목 | 내용 |
|---|---|
| 기능명 | 환자 베드 이동 |
| EventIdentifier | move_patient_a_to_treatment |
| 실행 주체 | 플레이어 B/C/D + 의사 NPC |
| 대상 | 환자 A + 베드 |
| 선행 조건 | 베드 손잡이 2인 이상 잡기 |
| 입력 | 베드 손잡이 상호작용 |
| 출력 | 베드 이동 애니메이션, 환자 위치 변경 |
| 완료 조건 | 처치실 웨이포인트 도착 |
| 실패 처리 | 인원 부족 알림 |

구현 포인트:
- 베드 이동은 Waypoint 기반 이동으로 단순화합니다.
- 이동 중 플레이어 위치는 베드에 부착하거나 이동을 잠시 제한합니다.
- 이동 완료 시 시나리오 이벤트 완료 신호를 보냅니다.

## 예시 2: 앰부백 산소화

| 항목 | 내용 |
|---|---|
| 기능명 | 앰부백 산소화 |
| EventIdentifier | perform_bvm_oxygenation_a |
| 실행 주체 | 플레이어 A |
| 대상 | 환자 A |
| 선행 조건 | T-piece 제거 및 앰부백 연결 |
| 입력 | 앰부백 클릭/홀드 |
| 출력 | 앰부백 압박 애니메이션, 산소화 효과 |
| 완료 조건 | 정해진 횟수(예: 6~10회) 수행 |
| 실패 처리 | 연결 미완료 경고 |

구현 포인트:
- 앰부백은 Interactable로 구현하고, 연결 상태를 플래그로 관리합니다.
- 산소화 속도는 UI 안내(메뉴 질문)와 연동합니다.
- 완료 조건은 카운터 기반으로 단순화합니다.

## AI 요청 예시

- 목표: 앰부백 Interactable 구현
- 경로: Assets/Modules/TriageTrainer/Prefabs/Interactables/ambu_bag/
- 요구 사항: 연결 상태 체크, 클릭 시 압박 애니메이션 트리거, N회 수행 시 이벤트 완료
- 금지: MultiplayerInfrastructure 수정
