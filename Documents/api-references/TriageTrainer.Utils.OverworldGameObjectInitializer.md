# API 레퍼런스: `TriageTrainer.Utils.OverworldGameObjectInitializer`

> **네임스페이스:** `TriageTrainer.Editor.Utils`, `TriageTrainer.Utils`  
> **파일 위치:**  
> - `Assets/Modules/TriageTrainer/Editor/Utils/OverworldGameObjectInitializer/OverworldGameObjectInitializer.cs`  
> - `Assets/Modules/TriageTrainer/Editor/Utils/OverworldGameObjectInitializer/OverworldGameObjectInitializerEditor.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Utils/OverworldSpawnPoint.cs`  
> - `Assets/Modules/TriageTrainer/Scripts/Utils/GeneratedByOverworldGameObjectInitializerEditor.cs`

---

## 0. 문서 목적

Overworld 초기 세팅에서 반복적으로 필요한 웨이포인트/스폰 포인트를 에디터 도구 한 번으로 생성/정리하는 절차와 런타임 등록 동작을 정리합니다.

---

## 1. 제공 기능 요약

- 메뉴 `Tools/Triage Trainer/Overworld GameObject Initializer`에서 전용 창 제공
- `Set` 실행 시 생성 루트(`GeneratedByOverworldGameObjectInitializerEditor`) 아래에 다음 오브젝트를 재생성
  - `Waypoint_{buildingIdentifier}`
  - `Waypoint_{treatmentIdentifier}`
  - `SpawnPoint_{commonSpawnPointIdentifier}`
- `Delete` 실행 시 생성 마커가 붙은 루트/하위 오브젝트를 일괄 삭제

---

## 2. 기본 식별자/좌표

`OverworldGameObjectInitializer` 기본값:

- 건물 입구 웨이포인트
  - identifier: `building-enterance`
  - position: `(-72.525, 1.0, 2.3)`
- 치료실 입구 웨이포인트
  - identifier: `treatment-room-enterance`
  - position: `(-66.5, 1.0, -10.2)`
- 공용 스폰 포인트
  - identifier: `spawnpoint-commons`
  - position: `(-73.0, 1.0, -7.5)`

---

## 3. 생성/삭제 동작 상세

### `Set(...)`

```csharp
public static void Set(
  string buildingIdentifier,
  Vector3 buildingEnterance,
  string treatmentIdentifier,
  Vector3 treatmentRoomEnterance,
  string commonSpawnPointIdentifier,
  Vector3 commonSpawnPoint)
```

동작 순서:

1. 생성 루트를 찾거나 생성 (`GetOrCreateGeneratedRoot`)
2. 루트 하위 기존 생성물 제거 (`DeleteChildren`)
3. 웨이포인트 2개 생성 (`CreateWaypoint`)
4. 스폰 포인트 1개 생성 (`CreateSpawnPoint`)

중복 루트가 이미 존재하면 첫 번째만 남기고 나머지는 제거합니다.

### `Delete()`

```csharp
public static void Delete()
```

- `GeneratedByOverworldGameObjectInitializerEditor` 컴포넌트를 가진 루트를 찾아 모두 삭제합니다.

### Undo/PlayMode 처리

- 에디터 비재생 상태: `Undo.RegisterCreatedObjectUndo`, `Undo.DestroyObjectImmediate` 사용
- 플레이 중: `Object.Destroy` 사용

---

## 4. Waypoint 식별자 주입 방식

`CreateWaypoint`는 `WaypointAnchor`의 비공개 필드 `identifier`를 Reflection으로 설정합니다.

```csharp
var field = typeof(WaypointAnchor).GetField("identifier", BindingFlags.Instance | BindingFlags.NonPublic);
field?.SetValue(waypointAnchor, identifier);
```

의도:

- 인스펙터 수동 세팅 없이 즉시 식별자 확정
- 생성 직후 Registry 조회 가능한 일관된 키 유지

주의:

- `WaypointAnchor` 내부 필드명이 바뀌면 Reflection 설정이 실패하므로 함께 유지보수해야 합니다.

---

## 5. OverworldSpawnPoint 런타임 등록

`OverworldSpawnPoint`는 `IPlayerSpawnPointProvider` 구현체이며 Enable/Disable 수명주기에서 다음을 수행합니다.

- `PlayerSpawnPointRegistry.Register(this)` / `Unregister(this)`
- `Registry.Register(RegistryType.SpawnPoint, identifier, transform)` / `Unregister(...)`

속성:

- `Identifier` → `_identifier`
- `SpawnTransform` → `transform`
- `IsAvailable` → 활성 상태 + hierarchy 활성 여부

에디터 가시화:

- Gizmo 구/와이어/라벨 표시로 씬에서 스폰 포인트를 즉시 확인 가능

---

## 6. 에디터 UI (`OverworldGameObjectInitializerEditor`)

입력 필드:

- Building/Treatment 식별자 + 좌표
- Common SpawnPoint 식별자 + 좌표

버튼:

- `Reset Values`: 기본값 복원
- `Set`: 현재 입력값으로 생성/재생성
- `Delete`: 생성된 루트 일괄 삭제

참고:

- 창 도움말에 따라 `Set` 실행 시 FishNet `PlayerSpawner`의 Spawns 배열을 CommonSpawnPoint 하나로 맞추는 운영 규칙을 전제로 사용합니다.

---

## 7. 운영 체크리스트

- identifier는 씬 전역에서 유일하게 유지
- `Set` 후 Hierarchy에 생성 루트가 1개인지 확인
- 플레이 시작 후 Registry에서 `RegistryType.SpawnPoint` 조회 검증
- `WaypointAnchor` 내부 식별자 필드명 변경 시 Reflection 코드 동시 수정
