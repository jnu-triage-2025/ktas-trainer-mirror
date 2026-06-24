---
title: "EntityPreset 개발 디버깅 가이드 (IndevScene)"
doc_type: requirement
domain: content-definitions
progress: "1-designed"
status: active
updated: 2026-06-23
flags: []
---

# EntityPreset 개발 디버깅 가이드 (IndevScene)

개발 환경(IndevScene)에서 `EntityPresetRegistryRequirementsSO` 에 등록한 프리셋이
**스폰되는지 / 동작하는지** 를 빠르게 확인하기 위한 디버그 수단과 사용법.

확인 목표(범위): "프리셋이 등록되었는가 / 스폰이 성공하는가 / 자식 분리(ungroup)가 동작하는가 /
결과 엔티티가 레지스트리에 올라오는가" 정도. 정식 게임플레이 검증이 아니라 개발 보조용이다.

## 1. 디버그 컴포넌트: `EntityPresetDebugger`

위치: `Assets/Modules/TriageTrainer/Scripts/Utils/EntityPresetDebugger.cs`

IndevScene 의 빈 GameObject(예 `__EntityPresetDebugger`)에 붙이고 인스펙터를 설정한다.

| 필드 | 설명 |
|---|---|
| `Requirements SO` | 디버그할 `EntityPresetRegistryRequirementsSO` |
| `Spawn Origin` | 스폰 기준 위치(비우면 이 오브젝트 위치) |
| `Spawn Spacing` | 전체 스폰 시 항목 간 간격(m) |
| `Selected Preset Identifier` | 단일 스폰할 프리셋 식별자 |
| `Show Overlay` | 플레이 중 좌상단 디버그 오버레이 표시 |

## 2. 사용 절차

1. **호스트/서버로 플레이모드 진입** (네트워크 프리셋은 서버 컨텍스트에서만 복제 스폰됨).
2. 컴포넌트 우클릭 **ContextMenu** 또는 화면 좌상단 **오버레이 버튼** 으로 다음을 실행한다.

> **중요 — 등록 선행**: 프리셋은 먼저 레지스트리에 **등록** 되어야 스폰된다. 등록은 원래
> `RegisteringMultiplayerInfrastructureSupport`(부트스트랩) 가 `Awake` 에서 수행한다. 만약 IndevScene 에
> 그 컴포넌트가 없거나 SO 가 연결되지 않았다면, `Entity preset '...' is not registered.` 오류가 난다.
> 디버거는 이를 위해 **`_autoRegisterOnStart`(기본 on)** 와 **"Register Presets From SO"** 액션을 제공한다.
> (디버거의 `Requirements SO` 에 동일 SO 를 지정해두면, 부트스트랩 없이도 디버거가 단독 등록한다. Spawn 액션도
> 미등록 시 자동으로 SO 등록을 시도한다.)

| 액션 | 효과 / 확인 포인트 |
|---|---|
| **Register Presets From SO** | SO 의 프리셋을 레지스트리에 등록(미등록분만). 부트스트랩 없을 때 선행 실행. |
| **List Registered Presets** | 등록된 프리셋 수 + 각 항목(type/networked/detach 개수/prefab 유무). 등록 자체 확인. |
| **Spawn Selected Preset** | `Selected Preset Identifier` 1개 스폰. 성공/실패 + 결과 식별자 로그. |
| **Spawn All Presets** | SO 의 모든 프리셋을 간격을 두고 스폰. `성공/전체` 요약. |
| **Report Registered Entities** | 현재 레지스트리 엔티티 목록(type/networked/alive). 스폰 후 `patient_a`/`bed_a` 등이 올라왔는지 확인. |
| **Despawn Debug Spawns** | 디버그로 스폰한 객체 정리(서버면 FishNet Despawn, 아니면 Destroy + 등록 해제). |

3. 오버레이에는 `Server` 여부, `Presets`/`Entities`/`DebugSpawned` 개수, **마지막 실행 결과(Last)** 가 표시되어
   콘솔 없이도 스폰/동작 여부를 즉시 확인할 수 있다.

## 3. 결합 프리셋(환자+침대) 확인 흐름 (예)

1. `Selected Preset Identifier` = `patient_a` 으로 두고 **Spawn Selected Preset**.
2. **Report Registered Entities** 로 `patient_a`(Patient), `bed_a`(MovingPatientBed) 가 등록되었는지 확인
   → 하위 프리셋(`bed_a`)이 `unwrapOnSpawn` 으로 형제 루트 스폰되었음을 의미.
3. (선택) 결합 동작은 `attach_patient_bed_pairs` 시나리오 이벤트 또는 별도 호출로 확인.
4. **Despawn Debug Spawns** 로 정리.

> 참고: `/entitypreset list` / `/entitypreset spawn <id> <x> <y> <z>` 채팅 명령으로도 동일 확인이 가능하다.
> 디버거 컴포넌트는 그 위에 "에디터에서 클릭 한 번 + 오버레이 가시화" 편의를 더한 것이다.

## 3-1. 식별자 개념 정리 (childPresetIdentifier vs spawnedEntityIdentifier)

프리셋에는 **두 종류**의 식별자가 있다.

- **프리셋 `identifier`** — SO 항목 최상단 필드(예 `patient_a`, `bed_a`).
  **스폰할 때 사용하는 키** 다. `TrySpawnEntityPreset("patient_a", ...)` 처럼 이걸로 스폰한다.
- **하위 참조 `childReferences[]`** — 이 프리셋과 함께 스폰할 다른 프리셋 지정.
  - `childPresetIdentifier`: **함께 스폰할 다른 EntityPreset 의 식별자**(예 `bed_a`). 원본 프리팹의 자식 경로가 아니다.
  - `spawnedEntityIdentifier`: 그 하위가 스폰되어 가질 식별자(예 `bed_a`).
  - `unwrapOnSpawn`: true 면 하위를 루트의 자식이 아니라 **동일 계층(형제 루트)** 으로 둔다.

위 오류(`'patient_a' is not registered`)는 **프리셋이 레지스트리에 등록되지 않은** 상태(부트스트랩 미실행/SO 미연결)다.
아래 §2 "등록 선행" 으로 해결한다.

## 3-2. 자주 나는 오류와 해결

### (A) `Entity preset '...' is not registered.`
- 원인: 프리셋이 레지스트리에 등록되지 않음(부트스트랩 미실행/SO 미연결). 또는 `childPresetIdentifier` 가 가리키는
  하위 프리셋이 등록되지 않음.
- 해결: §2 "등록 선행" — 디버거 `_autoRegisterOnStart`(기본 on) 또는 "Register Presets From SO". 하위 프리셋도 같은
  SO 에 함께 등록되었는지 확인한다.

### (B) `NetworkObject Name [...] ObjectId [65535] ... is expected to be initialized but was not. ... Reserialize Prefabs ...`
- 원인: **FishNet 프리팹 직렬화 누락**. 프리팹이 FishNet 프리팹 컬렉션(`DefaultPrefabObjects.asset`)에 등록되지 않음.
- **해결(에디터 작업)**: 플레이모드 종료 → Unity 메뉴 **Fish-Networking > Utility > Reserialize NetworkObjects**
  (또는 **Refresh Default Prefabs**) 실행 → 환자/침대 프리팹이 컬렉션에 포함되었는지 확인 → 다시 스폰 테스트.
- 참고(중요): 새 EntityPreset 모델에서는 **컨테이너 프리팹/ nested NetworkObject 가 없다.** 환자/침대는 각각 독립
  프리팹이므로 일반 프리팹과 동일한 직렬화 규칙만 따른다. 따라서 (B) 오류 발생 빈도가 크게 줄어든다.

## 3-3. FishNet 스폰 규칙 준수 (Instantiate → ServerManager.Spawn)

엔진 스폰 로직(`Registry.TrySpawnEntityPreset`)은 FishNet 공식 가이드의 패턴을 따른다.
참고: [Spawnable Prefabs](https://fish-networking.gitbook.io/docs/fishnet-building-blocks/scriptableobjects/spawnableprefabs),
[Spawning](https://fish-networking.gitbook.io/docs/guides/features/networked-gameobjects-and-scripts/spawning).

- **전제(필수)**: 스폰할 프리팹(환자/침대 등)은 NetworkManager 의 **Spawnable Prefabs**(`DefaultPrefabObjects.asset`)에
  등록되어 있어야 한다. 위 (B) 해결의 Reserialize 가 이 등록을 보장한다. 미등록 시 (B) 오류가 난다.
- **서버 컨텍스트**: `NetworkSpawnIfServer` 는 `InstanceFinder.IsServerStarted` 일 때만 `ServerManager.Spawn(go)` 를
  호출한다(공식 예시와 동일: `Instantiate` 후 `ServerManager.Spawn`). 서버가 아니면 복제되지 않으며 경고를 남긴다.
- **unwrap 하위(환자+침대의 기본 사례)**: 환자(루트)와 침대(하위)는 **각각 독립 루트 NetworkObject** 로 따로
  Instantiate·Spawn 된다. 한 프리팹에 nested 되지 않으므로, 과거 "prefab-nested NetworkObject 를 런타임에 unparent"
  하던 방식(FishNet 기술 제약상 금지)의 문제가 원천적으로 사라진다.
- **비-unwrap 하위(드문 경우)**: 하위를 루트의 자식으로 두는 경우, 엔진은 하위를 **스폰 전에 먼저 루트 아래로 reparent**
  한다(루트는 prefab-nested 가 아닌 "루트" 상태라 런타임 reparent 가 허용됨). 스폰된 루트 아래로 사후 nested 되어 스폰될
  때는 소유권이 자동 추론되지 않으므로 ownerless(null owner)로 스폰된다(프로젝트 기본값과 동일).

## 4. 관련

- 결합 프리셋 구성: [`patient-bed-combined-preset-guide.md`](./patient-bed-combined-preset-guide.md)
- 운영자 전체 절차: [`human-operator-process.md`](./human-operator-process.md)
- SO 비런타임 검증: `EntityPresetRegistryRequirementsSO` 의 "Validate Presets (Editor)" (정적 구조 검증)
