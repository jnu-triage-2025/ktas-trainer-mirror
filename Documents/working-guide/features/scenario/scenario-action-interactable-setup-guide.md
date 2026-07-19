---
title: "시나리오 액션 Interactable 설정 가이드"
doc_type: guide
status: active
updated: 2026-07-18
---

# 시나리오 액션 Interactable 설정 가이드

`ScenarioActionInteractable`은 흉부 위치, T-piece, 제세동기처럼 아이템 획득이 아닌
월드 오브젝트 상호작용을 Validator 신호로 기록한다.

1. 대상 오브젝트에 Collider와 `ScenarioActionInteractable`을 추가한다. Collider는 trigger여도 된다.
2. `Display Text`에 학습자에게 보일 행동을 입력한다.
3. `Completion Signal`에는 `sig.` 없이 조건명을 입력한다. 컴포넌트가 접두사를 정규화한다.
4. 행동 후 표시할/숨길 오브젝트가 있으면 `Activate On Interact`/`Deactivate On Interact`에 연결한다.
5. 한 번만 수행돼야 하면 `Consume Once`를 켠다.

환자 A 설정값:

| 대상 | Completion Signal | 권장 표시문구 |
|---|---|---|
| T-piece | `interact_tpiece` | T-piece 확인 |
| 환자 흉부(패드 부착 위치) | `interact_patient_chest` | 제세동 패드 부착 위치 확인 |
| 환자 흉부(압박 위치) | `interact_chest` | 흉부 압박 위치 확인 |
| 제세동기 | `interact_defib` | 제세동기 작동 |

벽 유량계는 이 컴포넌트를 추가하지 않는다. 기존 `WallAttachedOxyflowmeter`의
`Attach Completion Signal`을 `interact_oxyflow_wall`로 설정한다. 목 고정대는 월드 클릭이 아니라
환자에게 `cervical_collar`를 사용하는 동작이므로 `apply_stabilizer_patient_a` 신호를 사용한다.
