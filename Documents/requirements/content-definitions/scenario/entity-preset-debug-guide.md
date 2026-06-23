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

1. `Selected Preset Identifier` = `patient_a_bed_group` 으로 두고 **Spawn Selected Preset**.
2. **Report Registered Entities** 로 `patient_a`(Patient), `bed_a`(MovingPatientBed) 가 등록되었는지 확인
   → 자식 분리(ungroup)가 동작했음을 의미.
3. (선택) 결합 동작은 `attach_patient_bed_pairs` 시나리오 이벤트 또는 별도 호출로 확인.
4. **Despawn Debug Spawns** 로 정리.

> 참고: `/entitypreset list` / `/entitypreset spawn <id> <x> <y> <z>` 채팅 명령으로도 동일 확인이 가능하다.
> 디버거 컴포넌트는 그 위에 "에디터에서 클릭 한 번 + 오버레이 가시화" 편의를 더한 것이다.

## 3-1. 식별자 개념 정리 (childPath vs identifier — 혼동 주의)

프리셋에는 **두 단계**의 식별자가 있다.

- **프리셋(컨테이너) `identifier`** — SO 항목 최상단 필드(예 `patient_a_bed_group`).
  **스폰할 때 사용하는 키** 다. `TrySpawnEntityPreset("patient_a_bed_group", ...)` 처럼 이걸로 스폰한다.
- **자식 `childDetachments[]`** — 컨테이너 안에서 분리할 자식 지정.
  - `childPath`: **프리팹 위계상의 경로/이름**(예 `PatientTypeA`, `PatientMovingBed`). 어떤 자식을 분리할지 가리킨다.
  - `spawnedEntityIdentifier`: 분리되어 독립 객체가 된 그 자식이 **가질 식별자**(예 `patient_a`, `patient_a_moving_bed`).

즉 사용자의 이해가 맞다: **path = 위계 경로, (자식)identifier = 생성된 대상이 가질 식별자.**
"Path 가 왜 있나"의 답: **스폰은 프리셋(컨테이너) identifier 로 하고**, path 는 그 컨테이너 *내부에서*
어떤 자식을 분리할지 지목하는 용도다(두 개는 다른 계층의 식별자).

위 오류(`patient_a_bed_group' is not registered`)는 식별자 개념 문제가 아니라 **컨테이너 프리셋이
레지스트리에 등록되지 않은** 상태(부트스트랩 미실행/SO 미연결)였다. 위 §2 "등록 선행" 으로 해결한다.

## 3-2. 자주 나는 오류와 해결

### (A) `Entity preset '...' is not registered.`
- 원인: 프리셋이 레지스트리에 등록되지 않음(부트스트랩 미실행/SO 미연결).
- 해결: §2 "등록 선행" — 디버거 `_autoRegisterOnStart`(기본 on) 또는 "Register Presets From SO".

### (B) `NetworkObject Name [...] ObjectId [65535] ... is expected to be initialized but was not. ... Reserialize Prefabs ...`
- 원인: **FishNet 프리팹 직렬화 누락**. 새로 만든(또는 자식으로 NetworkObject 를 가진) 프리팹이
  FishNet 의 프리팹 컬렉션(`DefaultPrefabObjects.asset`)에 직렬화/등록되지 않아, `Instantiate` 시
  `NetworkObject.Awake` 가 초기화되지 않았다고 판단해 오류를 낸다. (ObjectId 65535 = 미설정)
- **해결(에디터 작업, 필수)**:
  1. 플레이모드를 종료한다.
  2. Unity 상단 메뉴 **Fish-Networking > Utility > Reserialize NetworkObjects** 실행
     (또는 **Refresh Default Prefabs** / **Reserialize Prefabs**). 씬 오브젝트면 **Reserialize Scenes** 도 함께.
  3. 컨테이너 프리팹과 그 **자식 NetworkObject(환자/침대)** 가 `DefaultPrefabObjects.asset` 에 포함되었는지 확인.
  4. 다시 플레이모드로 스폰 테스트.
- 참고: 컨테이너(분리용) 프리팹은 **자식 NetworkObject 가 nested** 된 구조다. FishNet 에서 nested
  NetworkObject 는 반드시 프리팹 직렬화가 되어 있어야 인스턴스화/스폰이 정상 동작한다. 새 프리팹을
  만들거나 자식 NetworkObject 구성을 바꿀 때마다 위 Reserialize 를 수행해야 한다.

## 4. 관련

- 결합 프리셋 구성: [`patient-bed-combined-preset-guide.md`](./patient-bed-combined-preset-guide.md)
- 운영자 전체 절차: [`human-operator-process.md`](./human-operator-process.md)
- SO 비런타임 검증: `EntityPresetRegistryRequirementsSO` 의 "Validate Presets (Editor)" (정적 구조 검증)
