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

| 액션 | 효과 / 확인 포인트 |
|---|---|
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

## 4. 관련

- 결합 프리셋 구성: [`patient-bed-combined-preset-guide.md`](./patient-bed-combined-preset-guide.md)
- 운영자 전체 절차: [`human-operator-process.md`](./human-operator-process.md)
- SO 비런타임 검증: `EntityPresetRegistryRequirementsSO` 의 "Validate Presets (Editor)" (정적 구조 검증)
