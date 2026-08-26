### 개요

체크리스트 종이 기능은 역할별로 준비해야 할 아이템을 시나리오 재생 중에 표시하기 위한 구현입니다. 시나리오 graph에 플레이어 태그별 아이템 묶음을 선언하고, `checklist_paper`가 현재 태그에 해당하는 묶음을 읽어 설명으로 표시합니다.

### 해결하려는 문제 상황

교육 참가자는 시나리오를 진행하면서 자신의 역할에 필요한 물품과 준비 수량을 별도로 기억해야 합니다. 역할을 겸임하는 참가자는 여러 목록을 동시에 확인할 수 있어야 합니다.

### 사용자 경험 목표

참가자는 체크리스트 종이 하나로 자신의 태그에 해당하는 모든 준비물을 확인하고, 실제로 획득한 물품을 즉시 완료 상태로 구분할 수 있습니다. 시나리오가 끝나면 다음 실행에 이전 목록과 완료 표시가 남지 않습니다.

### 제안

`ScenarioGraph`에 `checklistItemSetsByPlayerTag` 읽기 전용 맵을 추가합니다. JSON에서는 다음 형식을 사용합니다.

```json
"checklistItemSetsByPlayerTag": {
  "nurse_a": [{ "identifier": "plaster", "count": 2 }]
}
```

로더와 저장기는 이 값을 보존하고 JSON Schema는 빈 식별자와 0 이하 수량을 거부합니다. `PlayerController.InventorySlots`는 프로젝트 특화 아이템이 현재 보유한 체크리스트 인스턴스만 찾을 수 있도록 읽기 전용으로 노출합니다. 인벤토리 툴팁 설명에는 rich text를 명시적으로 활성화하여 완료 행의 색상과 취소선이 표시되게 합니다.

Scenario Graph Editor에는 `Checklist Items` 탭을 추가합니다. 이 탭에서 태그 묶음의 추가·삭제·이름 변경과 아이템 식별자·수량 변경을 지원하며, 기존 저장 절차로 graph JSON에 반영합니다.

### 자세한 달성 목표

- 여러 태그의 아이템을 합쳐 표시합니다.
- 같은 식별자는 하나의 행으로 합산합니다.
- 아이템을 한 번 획득하면 해당 행을 완료 표시로 바꿉니다.
- 시나리오 종료 또는 중단 시 설명과 완료 상태를 초기화합니다.

### 문서화

아이템 설정 안내와 API 레퍼런스에 JSON 작성 형식, 의도적으로 누락된 모델·아이콘, 확인 절차를 기록합니다.

### 가용성과 테스트

기존 시나리오는 새 필드를 생략할 수 있으므로 기존 동작이 유지됩니다. 로더의 JSON Schema 검증과 graph 저장·재로딩 검증에서 새 필드가 보존되는지 확인해야 합니다. 인게임 검증에서는 여러 태그, 최초 획득 완료 표시, 종료 초기화를 확인해야 합니다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

유효한 `checklistItemSetsByPlayerTag` JSON을 읽고 저장한 뒤 같은 태그·식별자·수량이 유지되어야 합니다. 활성 시나리오에서 체크리스트 설명이 요구된 줄 형식으로 보이고, 최초 획득 뒤 해당 줄만 녹색 취소선으로 변해야 합니다.

### 링크, 참고사항

- `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/Models/ScenarioGraphNodes/ScenarioGraph.cs`
- `Assets/Modules/TriageTrainer/Scripts/Items/Definitions/ChecklistPaper.cs`
