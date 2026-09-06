---
title: "시나리오 Action 인터렉션 설정 가이드"
doc_type: guide
status: active
updated: 2026-09-06
---

# 시나리오 Action 인터렉션 설정 가이드

흉부 위치, T-piece, 스타일렛처럼 아이템 획득이 아닌 월드 오브젝트 조작을 Validator 신호로 기록하는 상호작용은
시나리오 JSON 최상위 `interactions` 구역에 `kind: "Action"` 정의로 작성한다. 이전의 `ScenarioActionInteractable`
컴포넌트와 프리팹 필드는 폐기됐다(2026-09-06).

1. 대상 엔티티(예: `patient_a`)가 레지스트리에 등록되는 식별자를 확인한다. 정의는 이 식별자 아래에 붙는다.
2. `interactions`에 항목을 추가한다. `interaction`은 퀘스트 표시 바인딩이 참조할 인터렉션 식별자다.
3. `display.text`에 학습자에게 보일 행동을, `display.iconIdentifiers`에 아이콘 식별자를 적는다.
4. `completionSignal`에는 `sig.` 없이 조건명을 적는다. 레지스트리가 접두사를 정규화한다.
5. 행동 후 켜거나 끌 하위 오브젝트가 있으면 `activateObjects` / `deactivateObjects`에 엔티티 기준 경로를 적는다.
6. 한 번만 수행돼야 하면 `afterInteract`를 `HideForAll`(전원) 또는 `HideForPlayer`(수행자만)로 둔다.
7. 노출 조건은 `visibility.conditions`로 쓴다. 담당 간호사의 퀘스트 단계가 현재일 때만 열려면 `PlayerHasQuest`에
   `completionCriteriaIdentifier`를 함께 적고, 역할 제한은 `PlayerHasTag`를 더한다.

```json
{
  "entity": { "id": "patient_a" },
  "interaction": "interact_patient_chest",
  "kind": "Action",
  "display": { "text": "제세동 패드 부착" },
  "completionSignal": "interact_patient_chest",
  "afterInteract": "HideForAll",
  "activateObjects": [ "defibrillatorpad_midaxillary_A", "defibrillatorpad_subclavicle_A" ],
  "visibility": {
    "conditions": [
      { "type": "PlayerHasQuest", "questIdentifier": "Quest_Defibrillator_C",
        "completionCriteriaIdentifier": "attach-defibrillator-pad-patient-a" }
    ]
  }
}
```

환자 A 설정값(`patient_a_critical.scenario.json`):

| `interaction` | `completionSignal` | 표시 문구 | 켜는 오브젝트 |
|---|---|---|---|
| `interact_tpiece` | `interact_tpiece` | T-Piece 연결 | `TPieceSet_A` |
| `interact_patient_chest` | `interact_patient_chest` | 제세동 패드 부착 | 제세동 패드 두 개 |
| `click_to_start_comp` / `interact_chest` | 같은 이름 | 가슴압박 수행 | — (담당 간호사 태그 `nurse_b` / `nurse_a`) |
| `remove_intu_stylet` | `remove_intu_stylet` | 스타일렛 제거 | `endotracheal_tube_A` |

벽 유량계와 흡인기는 코드 리터럴(`oxyflowmeter`, `wall_suction_install`)로 선언되며, 시나리오 데이터는
`zone_a:oxyflowmeter` 같은 식별자 아래에 완료 신호와 `extras.attachedInteractSignal`만 덮어쓴다. 목 고정대는
월드 클릭이 아니라 환자에게 `cervical_collar`를 사용하는 동작이므로 `item_apply` 계열 정의를 쓴다.

수동 진입(ManualEntrypoint)으로 앞 단계에 다시 들어갈 때는 `InteractionRegistry.ResetOverridesForEntity`로
수행 뒤 숨김 오버라이드만 지운다. 노출 여부는 퀘스트 조건이 계속 결정하므로 단계 밖 상호작용이 열리지 않는다.
