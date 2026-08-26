# API 레퍼런스: `TriageTrainer.ItemDefinitions.ChecklistPaper`

## 개요

`ChecklistPaper`는 `Paper`를 상속하는 시나리오용 종이 아이템입니다. 식별자는 `checklist_paper`, 표시 이름은 `종이`, 기본 설명은 빈 문자열입니다.

활성 시나리오가 있으면 소유 플레이어의 태그에 맞는 항목을 설명에 표시합니다. 같은 아이템 식별자가 여러 태그에 있으면 수량을 합산하여 한 줄만 표시합니다.

## 시나리오 데이터 형식

`ScenarioGraph` 최상위의 `checklistItemSetsByPlayerTag` 필드를 사용합니다.

Scenario Graph Editor의 `Checklist Items` 탭에서 태그 묶음, 아이템 식별자, 수량을 직접 수정할 수 있습니다. 태그 묶음의 추가·삭제와 태그 이름 변경도 이 탭에서 처리합니다.

```json
"checklistItemSetsByPlayerTag": {
  "nurse_a": [
    { "identifier": "plaster", "count": 2 }
  ]
}
```

- 키: 플레이어 태그입니다.
- `identifier`: 레지스트리에 등록된 아이템 식별자입니다.
- `count`: 1 이상의 표시 수량입니다.

현재 역할별 항목 데이터는 `disaster_intro`, `patient_a_critical`, `patient_b_c_ct` 시나리오에 입력되어 있습니다. `tutorial`과 디버그 시나리오에는 `nurse_a`~`nurse_d` 역할별 준비물 흐름이 없으므로 항목 묶음을 추가하지 않습니다.

## 완료 표시와 수명주기

- 줄은 `(아이템명) × (개수)` 형식으로 표시됩니다.
- 플레이어가 표기된 식별자의 아이템을 한 번 인벤토리에 획득하면 해당 줄이 녹색 취소선으로 바뀝니다.
- 완료 상태는 아이템의 파생 직렬화 값에 보관되어 인벤토리 전달 과정에서 유지됩니다.
- 시나리오가 종료되거나 중단되면 설명과 완료 상태가 모두 초기화됩니다.

3D 모델과 아이콘 스프라이트는 의도적으로 없습니다.
