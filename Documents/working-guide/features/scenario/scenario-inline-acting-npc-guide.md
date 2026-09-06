# 시나리오 인라인 NPC ActingNpc 설정 가이드

## 지원 범위

시나리오 JSON의 최상위 `actingNpcs`에서 NPC의 인스턴스 식별자, 표시 이름, 위치, 회전과 종료 정리를
정의한다. NPC 상호작용은 최상위 `interactions` 구역에 `entity.id`를 actingNpc 식별자로 두고 정의한다
(`actingNpcs[].interactions`는 폐기됐으며, 남아 있으면 로더가 `interactions`로 옮기고 에디터에서 경고한다). `actingNpcs`는 NPC 정의 카탈로그이며, 실제 생성 시점은
`spawnOnStart` 또는 `EntityPresetSpawn` 노드의 `actingNpcIdentifier`가 결정한다. 캐릭터 메시, Animator, Collider,
`NetworkObject`는 Unity 프리팹이므로 기존 EntityPreset에 한 번 등록해야 한다.

## Unity 준비

1. NPC 프리팹에 `Npc`, Collider와 필요한 Animator를 추가한다.
2. 멀티플레이에서 사용할 경우 루트에 `NetworkObject`를 추가하고 FishNet spawnable prefab으로 등록한다.
3. `Assets/Modules/TriageTrainer/ScriptableObjects/EntityPreset Registry Requirements SO.asset`에
   다음 항목을 추가한다.
   - `identifier`: 예: `npc_doctor_preset`
   - `fallbackEntityType`: `Npc`
   - `prefab`: 준비한 NPC 프리팹
   - `isNetworked`: 네트워크 NPC이면 활성화
4. 실행 씬에 `TT_ RegistryMonoBehaviourSupport`와 `ScenarioController`가 존재하는지 확인한다.

## JSON 작성

`actingNpcs`는 `nodes`와 같은 최상위 수준에 둔다.

```json
{
  "identifier": "doctor-training",
  "actingNpcs": [
    {
      "identifier": "npc_doctor",
      "actingNpcType": "Npc",
      "presetIdentifier": "npc_doctor_preset",
      "displayName": "담당 의사",
      "positionX": 12.5,
      "positionY": 0.0,
      "positionZ": 8.0,
      "rotationY": 180.0,
      "spawnOnStart": false,
      "despawnOnScenarioEnd": true
    }
  ],
  "interactions": [
    {
      "entity": { "id": "npc_doctor" },
      "interaction": "doctor_submission",
      "kind": "ItemSubmission",
      "display": { "text": "물품 전달" },
      "itemSubmission": {
        "title": "의사에게 물품 전달",
        "submitButtonText": "전달",
        "requiredItems": [ { "itemIdentifier": "laryngoscope", "count": 1 } ]
      },
      "completionSignal": "doctor-item-received",
      "afterInteract": "HideForAll",
      "visibility": { "initial": false }
    }
  ],
  "defaultEntrypoint": "spawn-doctor",
  "nodes": {
    "spawn-doctor": {
      "identifier": "spawn-doctor",
      "nodeType": "EntityPresetSpawn",
      "actingNpcIdentifier": "npc_doctor",
      "nextIdentifier": "start"
    },
    "start": {
      "identifier": "start",
      "nodeType": "NPCControl",
      "mode": "Control",
      "npcIdentifier": "npc_doctor",
      "destinationType": "Position",
      "destinationX": 15,
      "destinationY": 0,
      "destinationZ": 9,
      "moveMode": "BySpeed",
      "moveSpeed": 1.8,
      "nextIdentifier": null
    }
  }
}
```

`EntityPresetSpawn.actingNpcIdentifier`를 지정하면 actingNpc의 preset, 위치, 회전, 이름을 한꺼번에 적용한다.
상호작용은 시나리오 시작 시 레지스트리에 등록돼 있다가 NPC가 등록되는 순간 붙는다. 이때 기존 `presetIdentifier`, 위치 관련 필드와 `spawnedEntityIdentifier`는 사용하지 않는다.
기존처럼 일반 EntityPreset만 스폰하려면 `actingNpcIdentifier` 없이 `presetIdentifier`를 사용한다.

`StartScenario` 상호작용은 `interactions`에 다음처럼 작성한다.

```json
{
  "entity": { "id": "npc_doctor" },
  "interaction": "doctor_order",
  "kind": "StartScenario",
  "display": { "text": "지시 듣기", "iconIdentifiers": [ "message-circle" ] },
  "scenarioIdentifier": "doctor-order-dialogue",
  "startNodeIdentifier": "intro",
  "visibility": { "initial": true }
}
```

현재 시나리오의 진행 신호만 발생시키는 대화형 NPC는 `Signal`을 사용한다.

```json
{
  "entity": { "id": "npc_doctor" },
  "interaction": "npc-talk",
  "kind": "Signal",
  "display": { "text": "말 걸기" },
  "completionSignal": "npc_talk_started",
  "afterInteract": "HideForAll",
  "visibility": { "initial": true }
}
```

특정 시점부터 보여야 하는 상호작용은 `visibility.initial`을 `false`로 두고 `InteractionVisibility` 노드로 열거나,
`visibility.conditions`(퀘스트 단계·역할 태그)로 조건을 쓴다.

## 실행과 정리

- `spawnOnStart`가 생략되면 `true`다. 시작 소환이 필요하면 `true`로 두고, 중간 소환은 `false`와
  `EntityPresetSpawn.actingNpcIdentifier` 조합을 사용한다.
- actingNpc는 첫 노드 실행 전에 생성되므로 첫 노드에서 바로 `NPCControl`로 찾을 수 있다.
- 같은 actingNpc를 시작 소환과 노드 소환에 동시에 지정하거나, 노드에서 두 번 스폰할 수는 없다.
- `despawnOnScenarioEnd`가 생략되면 `true`다.
- preset 누락, 중복 actingNpc identifier 또는 `Npc` 컴포넌트 누락 시 시작을 중단하고 부분 생성물을 정리한다.
- `despawnOnScenarioEnd: false`인 actingNpc는 시나리오 종료 후 월드에 남는다. 이후 수명주기를 담당할
  다른 시스템이 있을 때만 사용한다.
- 네트워크 preset인 경우 서버가 actingNpc를 spawn·구성한 뒤 원격 클라이언트에도 actingNpc 식별자를
  전달한다. 상호작용 정의는 각 피어가 같은 시나리오 데이터에서 만들고, 가시성 오버라이드만 서버가 복제한다. 실행 씬에는 spawn 시점부터 활성화된 `ScenarioNetworkRelay`가
  있어야 한다.

## 검증

1. `Tools > Multiplayer Infrastructure > Validate Scenario Requirements`를 실행한다.
2. Play Mode에서 시나리오를 시작한다.
3. Hierarchy에서 NPC 위치와 회전을 확인한다.
4. 상호작용 UI에 선언한 항목이 나타나는지 확인한다(`Tools > Multiplayer Infrastructure > Interaction Registry`에서 판정 사유를 볼 수 있다).
5. 아이템 제출 후 `completionSignalIdentifier` 신호를 기다리는 노드가 진행되는지 확인한다.
6. 시나리오 종료 후 NPC가 Hierarchy와 `RegistryType.Npc`에서 제거되는지 확인한다.

## 문제 해결

- `Entity preset ... is not registered`: EntityPreset SO 등록과 시스템 support prefab 연결을 확인한다.
- `does not contain an Npc component`: preset이 가리키는 프리팹에 `Npc`를 추가한다.
- 제출 항목이 보이지 않음: `itemSubmission.requiredItems`의 identifier와 count, 그리고 `visibility`(초기값·조건·`InteractionVisibility` 노드)를 확인한다.
- 다른 시나리오 시작 실패: 대상 `.scenario.json`이 `Resources/Scenario`에 있고 identifier가 일치하는지 확인한다.
