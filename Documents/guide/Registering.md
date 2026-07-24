# 레지스터

메인 백엔드 시스템 `MultiplayerInfrastructure`에서는 게임에서 다루어지는 모든 요소들에 고유한 식별자를 부여하고 관리합니다. 때문에 게임에서 사용되는 모든 것들은 반드시 게임 시스템에게 이 식별자와 함께 전달되어야 합니다. 이 과정을 레지스터(Register)라고 하고, 레지스터를 통해 등록된 데이터들을 레지스트리(Registry)라는 저장소에서 관리합니다.

## NPC 레지스터

NPC는 캐릭터 모델과 애니메이션 데이터 뿐 아니라, 이름, 퀘스트 발생, 그 외의 상호작용 데이터들을 포함할 수 있습니다. 이러한 데이터가 게임 시스템과 연계되어 동작하기 위해서 레지스트리를 사용합니다.

```
┌──────────────NPC─────────────────┐
│ ┌───────EntityPreset NPC────────┐│
│ │ ┌───────Acting NPC──────────┐ ││
│ │ │                           │ ││
│ │ └───────────────────────────┘ ││
│ └───────────────────────────────┘│
└──────────────────────────────────┘
```
_Acting NPC의 시스템 내 위치_  

<br />

대부분의 상황에서는 애니메이션(선택)과 자체적인 컨트롤 스크립트(선택)를 부착해 프리팹 상태로 만들어놓은 NPC를 EntityPreset로서 등록하고, 시나리오에서 Acting NPC로서 소환하는 방식으로 사용합니다. 시나리오 파일을 통해 EntityPreset화된 NPC에, 다이얼로그, 대화 상호작용, 퀘스트 진행 등의 추가 데이터를 추가하여, 인게임에서 시스템이 플레이 도중 스폰할 수 있도록 지원하면 Acting NPC라고 합니다.  

- `actingNpcs[].identifier`: 시나리오 안에서 NPC를 찾고 제어하기 위한 인스턴스 식별자입니다.
- `actingNpcs[].presetIdentifier`: EntityPreset 레지스트리에 등록된 NPC 프리팹 식별자입니다.
- `spawnOnStart: true`: 시나리오 시작 전에 생성합니다.
- `spawnOnStart: false`와 `EntityPresetSpawn.actingNpcIdentifier`: 그래프가 해당 노드에 도달했을 때 생성합니다.
- `despawnOnScenarioEnd`: 정상적인 시나리오 종료 시 인스턴스를 제거할지 정합니다. 시작 실패나 중단 시에는 값과 관계없이 생성된 Acting NPC를 정리합니다.

<br />

NPC 데이터는 다음 절차로 레지스터하고 시나리오에서 사용할 수 있습니다.

1. NPC 프리팹을 준비해 `MultiplayerInfrastructure.Entity.Npc` 컴포넌트를 추가합니다.
2. 게임 로딩 과정에서 NPC 프리팹을 `MultiplayerInfrastructure.EntityPreset`에 등록할 수 있도록, 사전에 `EntityPresetRegistryRequirements`에 프리팹을 등록합니다.
  > [!NOTE]  
  > 현재는 `Assets/Modules/TriageTrainer/ScriptableObjects/EntityPreset Registry Requirements SO.asset` 파일을 유니티 에디터에서 열어, NPC 프리팹을 등록할 수 있습니다.
  ```
  identifier: npc_doctor_preset
  fallbackEntityType: Npc
  prefab: DoctorNpc.prefab
  displayName: 담당 의사
  isNetworked: true
  ```
  - 이 때 이 데이터를 시나리오 그래프 시스템과 연계해서 사용할 수 있습니다. 시나리오에서는 위의 `identifier` 필드를 `presetIdentifier`로 참조하여 NPC를 스폰할 수 있습니다.
3. 시나리오 그래프 시스템에서는 `actingNpcs` 항목으로 **Acting NPC**의 인스턴스 데이터를 정의합니다.
   프리팹이 가진 모델·Animator·Collider·기본 컴포넌트는 유지하고, 시나리오 정의는 인스턴스 식별자,
   표시 이름, transform, 상호작용을 적용합니다.
   ```json
   {
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
         "spawnOnStart": true,
         "despawnOnScenarioEnd": true,
         "interactions": [
           {
             "identifier": "doctor_submission",
             "interactionType": "ItemSubmission",
             "displayText": "물품 전달",
             "requiredItems": [
               {
                 "itemIdentifier": "laryngoscope",
                 "count": 1
               }
             ],
             "completionSignalIdentifier": "sig.doctor-item-received"
           }
         ]
       }
     ]
   }
   ```

   - `identifier`는 이번 시나리오에서 생성되는 NPC 인스턴스의 식별자입니다.
   - `presetIdentifier`는 앞 단계에서 `EntityPresetRegistryRequirements`에 등록한 NPC 프리팹 식별자입니다.
   - `displayName`, 위치, 회전, 생성·제거 정책은 시나리오 정의 값이 적용됩니다.
   - `spawnOnStart`를 생략하면 시나리오 시작 전에 자동 소환됩니다.
   - `despawnOnScenarioEnd`를 생략하면 시나리오 종료 시 자동 제거됩니다.
   - 현재 지원되는 인라인 상호작용은 다른 시나리오를 시작하는 `StartScenario`, 아이템을 전달하는 `ItemSubmission`, 상호작용 시 런타임 신호를 발생시키는 `Signal`입니다.

   NPC 프리팹에 미리 설정된 기본 상호작용은 삭제되거나 대체되지 않습니다. 시나리오의 `interactions`는 기본 상호작용에 추가됩니다. 따라서 같은 목적의 상호작용을 프리팹과 시나리오 양쪽에 중복 정의하지 않아야 합니다.

4. 중간에 소환할 Acting NPC는 `spawnOnStart: false`로 설정한 뒤 그래프의
   `EntityPresetSpawn` 노드에서 `actingNpcIdentifier`로 참조합니다. 이 경로도 시작 소환과 동일하게
   프리팹 생성, 레지스트리 재등록, 이름·상호작용 적용, 네트워크 구성을 수행합니다.

   ```json
   {
     "identifier": "spawn_doctor",
     "nodeType": "EntityPresetSpawn",
     "actingNpcIdentifier": "npc_doctor",
     "nextIdentifier": "doctor_move"
   }
   ```

   `actingNpcIdentifier`를 사용할 때에는 노드의 `presetIdentifier`, 좌표,
   `spawnedEntityIdentifier`는 사용하지 않습니다. 일반 EntityPreset만 소환할 때에만 기존
   `presetIdentifier` 경로를 사용합니다.

5. 게임이 시나리오를 시작하거나 Acting NPC 소환 노드에 도달하면 다음 순서로 처리합니다.

   1. `presetIdentifier`로 EntityPreset 레지스트리에서 NPC 프리팹을 찾습니다.
   2. NPC 프리팹을 시나리오에 지정한 위치와 회전으로 소환합니다.
   3. NPC에 시나리오의 `identifier`, 표시 이름, 상호작용 데이터를 적용합니다.
   4. NPC를 NPC 레지스트리와 Entity 레지스트리에 등록합니다.
   5. 멀티플레이 환경에서는 동일한 NPC 구성 정보를 원격 클라이언트에도 전달합니다.
   6. 시나리오 종료 시 기본적으로 해당 시나리오가 생성한 NPC를 제거합니다.

### 주의 사항

- NPC 프리팹에는 `MultiplayerInfrastructure.Entity.Npc` 컴포넌트가 반드시 있어야 합니다.
- 플레이어가 NPC를 감지하고 상호작용하려면 Collider가 필요합니다.
- 멀티플레이 NPC라면 루트에 `NetworkObject`를 추가하고 FishNet spawnable prefab으로도 등록해야 합니다.
- 시나리오 기반 NPC 소환에는 일반 `RegistryPreloadNpcSO`가 아니라 `EntityPresetRegistryRequirementsSO`를 사용합니다. 전자는 이미 씬에 존재하는 NPC GameObject를 등록하는 용도이며, 프리팹을 새로 소환할 수는 없습니다.
- 시나리오 안의 모든 NPC 상호작용 `identifier`는 서로 중복되어서는 안 됩니다.
- 같은 Acting NPC를 `spawnOnStart`와 `actingNpcIdentifier`로 동시에 지정하거나, 그래프에서 두 번
  소환할 수 없습니다. 재소환이 필요한 콘텐츠는 별도 despawn/re-spawn 기능이 제공되기 전까지
  서로 다른 식별자를 사용해야 합니다.
- 중간 소환 Acting NPC를 사용하는 `NPCControl` 노드는 반드시 해당
  `EntityPresetSpawn` 이후의 모든 실행 경로에 배치해야 합니다. 요구사항 검증은 이를 오류로 검사합니다.
- `despawnOnScenarioEnd: false`는 **정상 종료 후** NPC를 월드에 남기는 옵션입니다. 시나리오 시작이
  실패하거나 중단되면 부분 생성 NPC는 항상 제거됩니다.
