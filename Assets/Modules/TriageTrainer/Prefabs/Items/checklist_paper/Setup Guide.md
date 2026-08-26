# 종이 및 체크리스트 종이 설정 안내

`paper`와 `checklist_paper`는 3D 모델과 아이콘 스프라이트가 없는 상태가 정상입니다. 두 아이템의 표시 이름은 모두 `종이`이고 기본 설명은 비어 있습니다.

## 1. 아이템 사용 준비

1. Unity에서 `RegisteringMultiplayerInfrastructureSupport`가 배치된 초기화 오브젝트가 활성화되어 있는지 확인합니다.
2. 아이템 생성, 월드 배치, 지급 명령에서 각각 `paper` 또는 `checklist_paper` 식별자를 사용합니다.
3. 모델이 없다는 경고는 해당 클래스에 선언된 의도적 누락 특성으로 억제됩니다. 텍스처 작업이 끝난 뒤에는 `Resources/Textures/Items/`에 각 식별자와 같은 이름의 스프라이트를 추가합니다.

## 2. 시나리오 체크리스트 작성

Scenario Graph Editor 상단의 `Checklist Items` 탭에서도 같은 데이터를 직접 추가, 삭제, 이름 변경, 수량 변경할 수 있습니다. 수정 후에는 일반 시나리오 저장 절차를 사용하여 JSON 파일에 반영합니다.

시나리오 JSON 최상위에 `checklistItemSetsByPlayerTag`를 추가합니다. 키는 플레이어 태그이고, 값은 `identifier`와 `count`를 가진 배열입니다.

```json
{
  "checklistItemSetsByPlayerTag": {
    "nurse_a": [
      { "identifier": "plaster", "count": 2 },
      { "identifier": "gauze", "count": 1 }
    ],
    "team_leader": [
      { "identifier": "penlight", "count": 1 }
    ]
  }
}
```

같은 플레이어가 `nurse_a`와 `team_leader` 태그를 함께 보유하면 세 항목을 모두 확인할 수 있습니다. 같은 식별자가 여러 태그에 있으면 한 줄로 합쳐지고 수량도 합산됩니다.

## 3. 플레이 중 동작 확인

1. `checklist_paper`를 인벤토리에 넣고 시나리오를 시작합니다.
2. 인벤토리에서 종이의 설명을 열어 `(아이템명) × (개수)` 형식의 줄을 확인합니다.
3. 표기된 아이템을 한 번 획득하면 해당 줄이 녹색 취소선으로 바뀌는지 확인합니다. 요구 수량이 2 이상이어도 첫 획득에서 완료로 표시됩니다.
4. 시나리오를 중단하거나 종료한 뒤 설명이 빈 문자열로 초기화되는지 확인합니다.

시나리오가 시작된 뒤 태그가 변경되는 경우에는 다음 시나리오 노드 변경 시점에 목록이 다시 계산됩니다.
