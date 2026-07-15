# Scenario Ingame Requirements 데이터 계약 초안

이 문서는 `Scenario Ingame Requirements Support` version 1의 외부 JSON 계약과 canonical compile 규칙을
정의한다. 구현 중 임의로 필드 의미를 바꾸지 않으며 변경이 필요하면 `schemaVersion`을 올리고 migration을
제공한다.

## 1. 파일 규칙

```text
<scenario-name>.scenario.json
<scenario-name>.scenario.requirements.json
```

- sidecar는 scenario 파일과 같은 폴더에 둔다.
- `scenarioIdentifier`는 로드된 graph identifier와 `Ordinal`로 일치해야 한다.
- 파일명은 탐색 힌트일 뿐 identity의 정본이 아니다.
- sidecar가 없으면 inferred requirements만 사용한다.
- sidecar는 UTF-8 JSON이며 comments와 trailing comma는 version 1에서 허용하지 않는다.
- unknown property, unknown enum과 duplicate JSON property는 오류다.

## 2. 승인된 sidecar 예시

```json
{
  "$schema": "scenario.requirements.schema.json",
  "format": "scenario-ingame-requirements",
  "schemaVersion": 1,
  "scenarioIdentifier": "disaster_intro",
  "source": {
    "scenarioSha256": "7b9f...",
    "generatedAtUtc": "2026-07-15T10:00:00Z"
  },
  "declarations": [
    {
      "selector": {
        "kind": "Npc",
        "identifier": "npc-1"
      },
      "operation": "Override",
      "scope": "Overworld",
      "authority": "Server",
      "binding": {
        "mode": "PrefabInstance",
        "factoryIdentifier": "triage.npc.standard"
      },
      "configuration": {
        "position": { "x": -61.5, "y": 1.0, "z": -8.0 },
        "rotationEuler": { "x": 0.0, "y": 180.0, "z": 0.0 }
      },
      "notes": "NPCMove에서 사용하는 현장 NPC"
    },
    {
      "selector": {
        "kind": "SpatialAnchor",
        "identifier": "treatment-room"
      },
      "operation": "Override",
      "scope": "Overworld",
      "authority": "Any",
      "binding": {
        "mode": "GeneratedSceneObject",
        "factoryIdentifier": "mi.waypoint-anchor"
      },
      "configuration": {
        "position": { "x": -66.5, "y": 1.0, "z": -10.2 }
      }
    }
  ],
  "suppressions": [
    {
      "selector": {
        "kind": "RuntimeSignal",
        "identifier": "sig.external-device-ready"
      },
      "reason": "외부 장비 bridge가 세션 시작 후 등록한다.",
      "owner": "simulation-team",
      "expiresOn": "2026-12-31"
    }
  ]
}
```

## 3. AI candidate 예시

AI 출력은 canonical sidecar와 별도 형식으로 import한다.

```json
{
  "format": "scenario-ingame-requirements-candidates",
  "schemaVersion": 1,
  "scenarioIdentifier": "disaster_intro",
  "candidates": [
    {
      "kind": "Npc",
      "identifier": "npc-1",
      "capabilities": ["ResolvableNpcMoveTarget", "ProvidesPosition"],
      "evidence": [
        {
          "nodeIdentifier": "M014",
          "nodeType": "NPCMove",
          "fieldPath": "npcIdentifier"
        }
      ],
      "suggestedBinding": {
        "mode": "PrefabInstance",
        "factoryIdentifier": null,
        "scope": "Overworld",
        "authority": "Server"
      },
      "confidence": 0.96,
      "reviewRequired": true,
      "notes": "사용할 prefab과 배치 좌표는 시나리오만으로 결정할 수 없음"
    }
  ]
}
```

AI candidate는 다음 규칙을 따른다.

- `reviewRequired`는 importer가 항상 true로 강제할 수 있다.
- `confidence`는 0 이상 1 이하이며 validation severity를 변경하지 않는다.
- evidence가 scenario graph와 일치하지 않으면 `SIR701` 오류다.
- 알 수 없는 factory는 승인할 수 없다.
- null 위치는 허용하지만 생성 Apply 전에는 unresolved Error다.
- candidate 파일은 canonical sidecar를 자동 덮어쓰지 않는다.

## 4. Canonical domain model

```csharp
public sealed class ScenarioRequirementManifest
{
  public int SchemaVersion { get; }
  public string ScenarioIdentifier { get; }
  public string ScenarioSha256 { get; }
  public IReadOnlyList<ScenarioRequirementDescriptor> Requirements { get; }
  public IReadOnlyList<ScenarioRequirementDiagnostic> Diagnostics { get; }
}

public sealed class ScenarioRequirementDescriptor
{
  public ScenarioRequirementKey Key { get; }
  public ScenarioRequirementKind Kind { get; }
  public string Identifier { get; }
  public ScenarioRequirementScope Scope { get; }
  public ScenarioRequirementAuthority Authority { get; }
  public ScenarioRequirementCardinality Cardinality { get; }
  public ScenarioRequirementAvailability EffectiveAvailability { get; }
  public IReadOnlySet<ScenarioRequirementCapability> Capabilities { get; }
  public IReadOnlyList<ScenarioRequirementOccurrence> Occurrences { get; }
  public ScenarioRequirementBindingHint BindingHint { get; }
}
```

구현 언어와 Unity 버전 제약상 `IReadOnlySet<T>` 사용이 부적합하면 내부 `HashSet<T>`와 정렬된
`IReadOnlyList<T>` 노출로 대체할 수 있다. JSON DTO와 domain model은 별도 타입으로 유지한다.

## 5. Requirement key

사람이 직접 조립한 문자열을 정본으로 저장하지 않는다. 다음 값으로 구조적으로 비교한다.

```text
Kind
Identifier.Trim()
```

표시와 stable report key는 다음 escape 규칙으로 만든다.

```text
<Kind>:<percent-encoded-identifier>
```

예:

```text
Npc:npc-1
SpatialAnchor:treatment-room
```

## 6. Declaration 필드

### selector

| 필드 | 필수 | 규칙 |
|---|---:|---|
| `kind` | 예 | known enum |
| `identifier` | 예 | trim 후 비어 있지 않음 |

selector 밖 declaration 필드:

| 필드 | 필수 | 규칙 |
|---|---:|---|
| `operation` | 예 | `Declare` 또는 `Override` |
| `scope` | 아니오 | known enum, 미지정 시 inferred/default 유지 |
| `authority` | 아니오 | known enum, 미지정 시 inferred/default 유지 |
| `availability` | 아니오 | occurrence 의미를 약화시키지 않아야 함 |
| `cardinality` | 아니오 | minimum 0 이상, maximum은 null 또는 minimum 이상 |
| `mustProve` | 아니오 | 기본 false, Indeterminate를 strict Error로 승격 |
| `occurrenceSelector` | 아니오 | 특정 node/field occurrence만 override할 때 사용 |

`occurrenceSelector`는 `nodeIdentifier`와 `fieldPath`를 모두 요구한다. 생략하면 descriptor 전체 제약을
override한다. 같은 requirement key의 override를 여러 개 둘 수 있지만 descriptor-wide override는 최대
하나이며 occurrence selector tuple은 중복될 수 없다.

### binding

| 필드 | 필수 | 규칙 |
|---|---:|---|
| `mode` | 예 | known binding mode |
| `factoryIdentifier` | 조건부 | Generated/Prefab일 때 필수 |
| `providerIdentifier` | 조건부 | RegistryProvided/External일 때 선택 또는 필수 |

### configuration

| 필드 | 필수 | 규칙 |
|---|---:|---|
| `position` | factory별 | finite float만 허용 |
| `rotationEuler` | 아니오 | finite float만 허용, 기본 zero |
| `scale` | 아니오 | 각 축 0 초과, 기본 one |
| `parentBindingKey` | 아니오 | cycle 금지 |
| `factoryOptions` | 아니오 | factory별 versioned option schema |

공용 schema는 arbitrary `factoryOptions` object를 허용하지 않는다. factory가 option을 필요로 하면
factory identifier별 등록 schema가 Editor importer에서 추가 검증되어야 한다.

## 7. Binding mode 규칙

| Mode | JSON 의미 | Scene binding 필요 | 시작 전 존재 |
|---|---|---:|---:|
| `ExistingSceneObject` | 기존 오브젝트에 연결 | 예 | 예 |
| `GeneratedSceneObject` | factory가 생성 | 적용 후 예 | 예 |
| `PrefabInstance` | project factory/prefab 배치 | 적용 후 예 | 예 |
| `RegistryProvided` | preloader/service가 공급 | 아니오 | 보통 예 |
| `RuntimeProduced` | scenario/gameplay가 나중에 공급 | 아니오 | 아니오 |
| `External` | 외부 runtime/provider가 공급 | 아니오 | 정책별 |
| `Suppressed` | 의도적으로 검증 제외 | 아니오 | 해당 없음 |

`Suppressed`는 일반 declaration binding으로 작성하지 않고 `suppressions` 배열에서만 작성한다.

## 8. Source hash와 stale 정책

- hash는 scenario 파일 원문 bytes의 SHA-256이다.
- 줄바꿈/format 변경도 stale로 판정한다.
- Editor는 stale 상태에서도 inferred extraction을 다시 수행하고 declaration selector를 재매칭한다.
- selector가 모두 유효하면 `SIR201 StaleDeclarationSource` Warning을 낸다.
- source occurrence가 사라진 override는 `SIR202 OrphanDeclaration` Error다.
- Production build에서 stale hash 자체는 Warning이지만 orphan/conflict는 Error다.
- 사용자가 Reconcile을 승인하면 hash와 declaration을 갱신한다.

## 9. Merge 규칙

동일 `(kind, normalizedIdentifier)` key에 대해:

1. inferred occurrence를 모두 수집한다.
2. capability를 합친다.
3. rule default cardinality와 availability를 정한다.
4. `Declare`를 적용한다. 같은 inferred key가 있으면 duplicate declaration Error다.
5. `Override`를 적용한다. 대상 key가 없으면 orphan Error다.
6. suppression의 유효성, 사유와 만료를 검사한다.
7. deterministic key 순서로 정렬한다.

자동 병합 금지 충돌:

- 다른 fixed scope
- 다른 authority override
- 서로 다른 factory
- `RuntimeProduced`와 `ExistingSceneObject` 동시 지정
- 필수 inferred requirement의 무사유 삭제
- 같은 selector의 declaration 중복

여기서 중복은 `(selector, operation, occurrenceSelector)`가 같은 항목이다. 서로 다른 occurrence를
대상으로 하는 override는 허용한다.

cardinality는 `minimum = max(all minimum)`, `maximum = min(all bounded maximum)`으로 병합한다. bounded
maximum이 없으면 unbounded다. 결과가 `minimum > maximum`이면 Error다.

## 10. AI 승인 변환

1. inferred key와 일치하면 `operation = Override`, 일치하지 않으면 사용자가 `Declare`를 명시한다.
2. scope가 없으면 inferred scope 또는 `AnyLoadedScene`을 제안하고 승인 UI에서 확정한다.
3. authority가 없으면 inferred authority 또는 `Any`를 제안하고 승인 UI에서 확정한다.
4. candidate capability는 known capability만 추가할 수 있고 extractor 결과를 제거할 수 없다.
5. Generated/Prefab mode에서 factory나 필수 위치가 null이면 unresolved 상태로 저장할 수 있으나 Apply와
   Production validation은 차단된다.
6. 승인 후 confidence는 canonical 계약에 저장하지 않는다.

## 11. Occurrence와 availability

각 occurrence는 다음 값을 갖는다.

```text
nodeIdentifier
nodeType
fieldPath
direction: Consumes | Produces
availability
expectedSupply: Scene | Scenario | Gameplay | External
usage
```

- descriptor의 `EffectiveAvailability`는 consumer occurrence만으로 계산한다.
- 우선순위는 `BeforeScenarioStart`, `WhenNodeReached`, `OptionalFallback`, `NotConsumed` 순이다.
- producer occurrence는 별도 목록으로 보존하며 availability에는 producer node가 실행되는 시점을 쓴다.
- consumer가 gameplay signal을 기다리는 경우 `direction = Consumes`, `availability = WhenNodeReached`,
  `expectedSupply = Gameplay`다. `ProducedByGameplay`를 availability 값으로 사용하지 않는다.
- 같은 key에 producer와 consumer가 있으면 1차 구현은 temporal status를 `Indeterminate`로 보고한다.
- 후속 graph analysis가 producer 선행을 증명하면 `expectedSupply = Scenario`를 충족으로 확정한다.

## 12. Suppression 규칙

- `reason`은 trim 후 10자 이상이어야 한다.
- `owner`는 비어 있지 않아야 한다.
- `expiresOn`은 ISO `yyyy-MM-dd`이며 필수다.
- 만료되면 suppression은 적용되지 않고 `SIR205 ExpiredSuppression` Error를 낸다.
- suppression은 requirement를 manifest에서 삭제하지 않는다. status를 `Suppressed`로 바꾸고 source를 유지한다.
- `Malformed`, schema 오류와 duplicate declaration은 suppress할 수 없다.
- AI candidate는 suppression을 생성하거나 수정할 수 없다.

## 13. Deterministic serialization

- declaration은 requirement key 순서로 저장한다.
- capability와 evidence 배열은 ordinal 정렬한다.
- property 순서는 DTO writer에서 고정한다.
- 숫자는 invariant culture를 사용한다.
- 줄바꿈은 repository convention을 따른다.
- 동일 domain model을 연속 serialize하면 byte-identical 결과를 내야 한다.

## 14. Version migration

- loader는 현재 version과 명시적으로 지원하는 이전 version만 읽는다.
- 미래 version은 best-effort로 읽지 않고 `SIR103 UnsupportedSchemaVersion`을 낸다.
- migration은 원본을 즉시 덮어쓰지 않고 preview와 새 파일 내용을 제공한다.
- build validator는 migration을 수행하지 않는다.
- version 변경 시 JSON Schema, DTO, domain mapping, example fixture와 migration test를 함께 갱신한다.
