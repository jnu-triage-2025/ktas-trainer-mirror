---
title: "환자 A/B/C 시나리오 인게임 실행 셋업 가이드 (운영자용)"
doc_type: requirement
domain: content-definitions
progress: "2-implementing"
status: active
updated: 2026-06-23
flags: ["refactor-required"]
---

# 환자 A/B/C 시나리오 인게임 실행 셋업 가이드 (운영자용)

이 문서는 변환된 시나리오(`disaster_intro` → `patient_a_critical` → `patient_b_c_ct`)를
실제 플레이모드에서 실행하기 위해 **비개발 운영자**가 Unity 에디터에서 수행할 셋업 절차를 정리한다.
코드/JSON 작업(이벤트 키 정합, 역할 태그 부여)은 이미 완료되었고, 남은 것은 **씬 배치 + 인스펙터 연결**이다.

관련 문서:
- 변환/검토 노트: [`patient-a-b-c-conversion-notes.md`](./patient-a-b-c-conversion-notes.md)
- 상세 인스펙터 연결 체크리스트: [`implementation-prep.md`](./implementation-prep.md) (특히 §11)
- 부트스트랩 기술 레퍼런스: [`TriageScenarioEventBootstrap`](../../../api-references/TriageTrainer.Scenario.TriageScenarioEventBootstrap.md)

---

## 0. 현재 상태 (코드/데이터)

- 시나리오 JSON 3종은 `Assets/Modules/TriageTrainer/Resources/Scenario/` 에 있으며,
  파일명으로 자동 등록된다(별도 SO 등록 불필요).
- 이벤트 식별자: JSON(소문자 스네이크)과 핸들러 등록 문자열이 일치하도록 정합 완료
  → 비-`todo.validate.*` 실제 이벤트는 전부 핸들러로 해소된다.
- 역할 태그: `disaster_intro` 도입부에 역할 선택(Choice) + `PlayerTag(Add)` 가 들어가 있어,
  인트로를 거치면 각 플레이어에게 역할 태그가 부여된다.

> 중요: `patient_a_critical` / `patient_b_c_ct` 는 역할 태그가 **이미 부여된 상태**를 가정한다.
> 따라서 정상 플레이 흐름은 반드시 **`disaster_intro` → 환자 A → 환자 B/C 순서**로 진행해야 한다.
> 환자 시나리오만 단독 실행하려면 아래 §4 의 `/tag` 수동 부여가 필요하다.

---

## 1. 부트스트랩 컴포넌트 배치

1. 시나리오 씬에 빈 GameObject 1개 생성(예: `ScenarioBootstrap`).
2. `TriageScenarioEventBootstrap` 컴포넌트를 추가한다.
3. `DialoguePanelUIController` 등 시나리오 UI 가 씬/레지스트리에 존재하는지 확인한다
   (없으면 Dialogue/Choice 가 화면에 표시되지 않고 자동 진행된다).

---

## 2. 필수 씬 오브젝트와 식별자

부트스트랩은 다음 식별자를 **Registry(Entity/Npc) → Npc 컴포넌트 → `GameObject.Find(name)`** 순으로 해석한다.
아래 식별자와 **정확히 같은 이름**으로 씬 오브젝트를 두거나, 인스펙터의 대응 필드에 직접 연결한다.

| 구분 | 식별자 | 비고 |
|---|---|---|
| 환자 | `patientA`, `patientB`, `patientC` | 본 처치 대상 |
| 더미 환자 | `dummyA`, `dummyB` | 분류용 더미 |
| 침대(분류) | `patientABed`, `dummyABed` | 초기 이송용 |
| 침대(처치) | `patientATreatmentBed`, `patientBTreatmentBed`, `patientCTreatmentBed` | 처치실 베드 |
| 모니터 | `patientA_monitor`, `patientB_monitor`, `patientC_monitor` | 활력징후 모니터 |
| 간호사(NPC) | `NurseA`, `NurseB`, `NurseC`, `NurseD` | 이동/연출 대상 |

> 참고: 본 시나리오 JSON 은 Registry **Waypoint** 를 직접 참조하지 않는다(이동은 InvokeEvent 핸들러 +
> 인스펙터의 Transform/스폰포인트 필드로 처리). 따라서 Waypoint 레지스트리 등록은 필수가 아니다.

식별자 등록 방법(택1):
- (권장) `RegistryPreloaderController` 의 NPC/Entity Preload SO 에 위 식별자와 오브젝트를 등록.
- 또는 위 이름 그대로 씬 오브젝트를 두어 `GameObject.Find` fallback 으로 해석.
- 또는 부트스트랩 인스펙터에서 각 `_xObject` / `_xEntityIdentifier` 필드에 직접 연결.

상세한 인스펙터 필드 목록은 [`implementation-prep.md` §11](./implementation-prep.md) 을 그대로 따른다.

---

## 3. 안전 기본값 (인스펙터)

연출 오브젝트가 아직 없을 때도 흐름이 멈추지 않도록 [`implementation-prep.md` §11 "안전 기본값 권장"](./implementation-prep.md)
값을 적용한다. 핵심:
- `_autoResolveReferencesFromRegistry = true`
- `_logRegistrySnapshotOnEnable = true` (플레이모드 Console 에서 실제 등록 key 확인용)
- 이동/스폰 시간(`_spawnMoveDurationSeconds` 등) = 0 또는 짧게
- 참조 누락 항목은 **예외 없이 건너뛰고 로그만 출력**되어야 한다(흐름 무중단).

검증 도구(컴포넌트 우클릭 ContextMenu):
- `Log Registry Snapshot` — 등록된 NPC/Entity key 목록 출력.
- `Validate Event Wiring` — 핵심 참조 누락 필드 보고.
- `Run Core Smoke Test` — 핵심 이벤트 순차 호출(플레이모드).

---

## 4. 실행 방법

서버(호스트) 채팅/콘솔에서:

```
/scenario list                         # 등록된 시나리오 확인 (patient_a_critical 등 보여야 함)
/scenario execute @s disaster_intro    # 권장: 인트로부터 (역할 선택 → 태그 부여)
```

- 인트로의 "역할을 선택하세요" Choice 에서 각 플레이어가 한 명씩(A/B/C/D) 선택하면 역할 태그가 부여된다.
- 인트로 종료 후 환자 A, 환자 B/C 시나리오를 순서대로 실행한다:
  ```
  /scenario execute @s patient_a_critical
  /scenario execute @s patient_b_c_ct
  ```

환자 시나리오만 단독 검증할 때(태그 수동 부여):
```
/tag add @self triage_lead            # nurse A 역할 예시
/tag add @self airway_team            # nurse B …
```
(역할별 태그 집합은 conversion-notes "초기 역할 태그 부여" 표 참조.)

---

## 5. 알려진 제약 (엔진 확장 대기 = TODO-SPEC)

아래는 셋업으로 해결되지 않으며, `Agents/Proposals/Feature Proposal - ScenarioNode Expressiveness`
승인·구현 후 동작한다.

- **도메인 인터랙션 게이트(`todo.validate.*`)**: 현재 핸들러 미등록 → 학습자 수행 검증 없이 자동 진행(스킵).
  실제 "수행해야 진행" 게이트는 TODO-SPEC-2 구현 후 동작.
- **2인 협업 브랜치**(환자A P004/N008, 환자B/C P009/V040_A·B): `matchMode:All` 로 한 명이 두 태그를
  요구받아 단일 간호사로 매칭되지 않음 → 해당 브랜치는 스킵될 수 있음. `matchMode:Any` 전환 또는
  멀티플레이어 할당(TODO-SPEC-3) 필요.
- **CPR 역할 교대(P005→P006)**: 정적 태그로는 교대 불가 → TODO-SPEC-3(`PlayerTag Swap`) 대기.
- **Dialogue 자동 진행 시간**: 변환 시 드롭 → TODO-SPEC-1 대기.

---

## 6. 빠른 점검 체크리스트

- [ ] `TriageScenarioEventBootstrap` 컴포넌트가 씬에 1개 있다.
- [ ] DialoguePanel UI 가 씬/레지스트리에 있다.
- [ ] 환자/더미/침대/모니터/간호사 오브젝트가 위 식별자로 존재(또는 인스펙터 연결)한다.
- [ ] `_logRegistrySnapshotOnEnable = true` 로 두고 플레이모드 진입 후 Console 의 "Registry snapshot" 에서 key 확인.
- [ ] `/scenario list` 에 `disaster_intro`, `patient_a_critical`, `patient_b_c_ct` 가 보인다.
- [ ] `/scenario execute @s disaster_intro` 실행 시 역할 선택 → 태그 부여가 진행된다.
- [ ] Console 에 `No handler registered for event '...'` 경고가 `todo.validate.*` 외에는 없다.
