# Unity YAML 병합 및 식별자 추적 도구 제안

## 개요

Unity Smart Merge를 대체하는 독립 실행형 Unity YAML 병합 도구를 개발한다. 도구는 Unity YAML을 일반 텍스트가 아니라 문서, 오브젝트, 필드, 참조로 구성된 직렬화 그래프로 해석하고, 가능한 변경은 자동으로 합치며 실제로 충돌한 최소 필드만 사용자에게 남긴다.

핵심 병합 엔진은 Git 등 특정 버전 관리 시스템에 의존하지 않는다. `base`, `current`, `incoming` 스냅샷을 명시적으로 받으면 어디서든 결정론적으로 동작한다. Git 연동은 입력 발견, 이력 인덱싱, rename 및 GUID 재생성 추론, merge driver, 커밋 전후 캐싱을 제공하는 선택적 어댑터로 분리한다.

도구의 임시 이름은 본 제안에서 `unity-merge`로 표기한다.

## 해결하려는 문제 상황

- Unity Smart Merge는 동일 파일 안의 비충돌 변경도 충분히 보존하지 못하거나, 충돌한 컴포넌트 전체를 한쪽 버전으로 되돌려 실제 변경을 잃을 수 있다.
- `.unity`, `.prefab`, `.asset`은 문서별 `fileID`, 외부 자산 GUID, Prefab override, stripped object 등 일반 YAML보다 강한 의미 규칙을 가진다.
- 파일 이동, `.meta` 삭제 및 재생성, prefab 교체로 GUID 또는 경로가 바뀌면 과거 참조가 끊어질 수 있다.
- Git conflict marker가 이미 삽입된 파일만 전달되는 환경에서는 원래 base가 없을 수 있다.
- 자동화, CI, AI 에이전트는 대화형 병합 UI를 사용할 수 없으므로 충돌 위치와 선택지를 안정적인 기계 판독 형식으로 받아야 한다.
- 전체 Git 이력을 매번 탐색하면 커밋과 병합이 느려지므로 증분 인덱스가 필요하다.

## 사용자 경험 목표

- 다른 필드를 수정한 변경은 자동으로 모두 보존한다.
- 같은 필드를 같은 값으로 수정한 경우 충돌 없이 그 값으로 합친다.
- 같은 필드를 서로 다른 값으로 수정한 경우 명시적인 정책이 있을 때만 자동 선택한다.
- 정책이 없거나 `--ignore-conflict-policy`가 지정되면 최소 필드 범위만 충돌로 남긴다.
- 자동 처리, 충돌, 참조 재연결, 참조 null 처리의 이유와 근거를 모두 보고한다.
- TTY가 없는 환경에서는 절대 대화형 입력을 기다리지 않고 표준 출력 또는 JSON 보고서만 반환한다.
- 이력 인덱스가 없거나 손상되어도 기본 3-way 병합은 동일하게 동작한다.
- 같은 입력과 설정에는 플랫폼과 실행 순서에 관계없이 같은 결과를 생성한다.

## 비목표

초기 버전은 다음을 목표로 하지 않는다.

- Unity Editor 또는 프로젝트 코드를 실행해 직렬화된 객체를 인스턴스화하는 것
- 이름이나 hierarchy 유사도만으로 낮은 신뢰도의 참조를 조용히 자동 교체하는 것
- 모든 패키지의 사용자 정의 직렬화 의미를 이해하는 것
- 충돌이 남았는데도 Unity에서 정상 import 가능한 파일이라고 보장하는 것
- Git 이력 인덱스를 병합 정확성의 필수 조건으로 만드는 것
- 전체 nested prefab 결과를 소스 prefab 없이 완전히 실체화해 병합하는 것

## 핵심 설계 원칙

### 1. 병합 엔진과 VCS 어댑터 분리

핵심 엔진은 최소한 아래 세 snapshot과 output만으로 syntax-level 보수적 병합을 수행한다.

```text
unity-merge merge --base BASE --current CURRENT --incoming INCOMING --output OUTPUT
```

Git 어댑터는 index stage 1/2/3, merge base, commit graph, rename 정보를 찾아 위 입력 계약으로 변환한다. 다른 VCS도 같은 어댑터 인터페이스를 구현할 수 있다.

정책, revision identity, project root, side별 prefab dependency snapshot, identity map, index evidence는 명시적인 `MergeContext` 선택 입력으로 전달한다. 선택 context가 없으면 해당 기능을 추측하지 않고 보수적 충돌 또는 `not-checked` capability로 남긴다. 따라서 기본 병합의 결정론은 동일 snapshot, 동일 context, 동일 도구 버전을 전제로 한다.

### 2. Unity 전용 concrete syntax tree와 의미 그래프

일반 YAML AST만 사용하지 않는다. 다음 내용을 손실 없이 보존할 수 있는 Unity 전용 concrete syntax tree(CST)를 사용한다.

- `%YAML`, `%TAG` 지시문
- `--- !u!<classID> &<fileID>` 문서 헤더와 `stripped` 표기
- 필드 순서, 주석, 문자열 인용, multiline scalar, float 표기, 줄바꿈 방식
- 알려지지 않은 class ID, 필드, `serializedVersion`
- flow style 및 block style 컬렉션

CST에서 별도의 의미 그래프를 구성하여 오브젝트와 참조를 병합한다. 출력은 변경된 token 범위만 다시 작성하여 불필요한 전체 파일 포맷 변경을 피한다.

### 3. 보수적 자동화

잘못된 자동 참조 연결은 미해결 충돌보다 위험하다. 자동화는 근거가 유일하고 검증 가능한 경우에만 수행한다. 후보가 둘 이상이거나 구조 검증이 실패하면 충돌 또는 경고로 남긴다.

### 4. 모든 자동 결정의 감사 가능성

각 결정은 `reason`, `policy`, `confidence`, `evidence`를 기록한다. 이력 추론은 직접적인 3-way 변경 근거보다 우선할 수 없다.

## 지원 대상

초기 지원 확장자는 Unity text serialization 형식의 다음 파일이다.

- `.unity`
- `.prefab`
- `.asset`
- `.mat`
- `.controller`
- `.overrideController`
- Unity text 형식 `.meta`

파일이 Unity YAML이 아닌 binary, Git LFS pointer, 손상된 인코딩이면 명확한 unsupported 결과를 반환한다. 지원 Unity 버전 범위는 fixture와 batch-mode 검증을 통과한 버전으로 명시하며, 최초 기준 버전은 이 프로젝트의 `6000.2.8f1`로 한다.

## 식별자 모델

식별자 영역을 혼합하지 않는다.

### 자산 식별자

- `.meta`의 GUID는 한 자산 세대(asset generation)의 식별자다.
- 자산 세대는 commit DAG에서 같은 GUID 관찰이 parent edge의 경로 연속성 또는 검증된 rename으로 연결된 connected component로 정의한다.
- 동시 branch에 독립적으로 나타난 같은 GUID는 하나의 최초 시점으로 강제하지 않는다. 복수의 최소 boundary commit을 기록한다.
- shallow/partial history 때문에 세대 경계를 증명할 수 없으면 경계를 `unknown`으로 표시하며 indexing 순서나 최초 발견 시간으로 정하지 않는다.
- 같은 GUID가 두 경로에 동시에 존재하면 duplicate GUID 오류로 보고 자동 추론을 중단한다.
- 같은 경로에서 GUID가 바뀌어도 무조건 같은 자산으로 간주하지 않는다. meta 재생성인지 삭제 후 다른 자산 생성인지 추가 근거가 필요하다.

### 로컬 오브젝트 식별자

- Unity YAML 문서 오브젝트는 `(asset generation, fileID, classID)`로 식별한다.
- bare `fileID`는 파일 밖에서 전역 식별자로 사용하지 않는다.
- 음수, 매우 큰 수를 포함해 원문 정수 문자열 또는 signed 64-bit 이상 안전한 형식으로 보존한다.
- `fileID: 0`은 null이며 재연결 후보가 아니다.

### 외부 참조

- 외부 참조는 `(guid, fileID, type)` tuple로 취급한다.
- 같은 `fileID`라도 GUID가 다르면 완전히 다른 대상이다.
- built-in resource, package asset, imported model sub-asset도 별도 종류로 기록한다.

### Managed reference

- `[SerializeReference]`의 `rid`와 `references`는 YAML 문서 `fileID`와 다른 namespace로 추적한다.
- `rid` 충돌 또는 재할당 시 해당 managed reference 범위의 모든 참조를 함께 갱신한다.

## 병합 모델

### 같은 필드와 같은 값의 정의

“같은 필드”는 텍스트 line이나 단순 key 이름이 아니라 다음 semantic address가 같은 경우다.

```text
asset identity / object identity / managed-reference identity / field path / sequence element identity
```

예를 들어 두 컴포넌트에 모두 `m_Enabled`가 있어도 object identity가 다르면 다른 필드다. Prefab override는 target tuple과 `propertyPath`가 같아야 같은 필드다. sequence element는 전용 semantic key가 있으면 그 key를 사용하고, 없으면 base와의 정렬 결과 및 구조 fingerprint를 사용한다. 유일하게 정렬할 수 없으면 같은 element로 단정하지 않는다.

“같은 값”은 Unity 직렬화 의미를 해치지 않는 제한된 canonical comparison으로 판정한다.

- mapping key 순서만 다른 값은 schema상 순서가 무의미할 때 같다고 볼 수 있다.
- 정수, bool, null, GUID, fileID는 type-aware 비교를 한다.
- float는 기본적으로 정확히 파싱된 수치와 특별값 표현이 모두 같아야 한다. 임의 epsilon 비교를 사용하지 않는다.
- 문자열은 escape를 해제한 값이 같아도 재출력 시 각 side의 원문 표기를 보존한다.
- sequence는 해당 필드의 순서 의미 규칙에 따라 비교한다.
- unknown field 또는 unknown scalar tag는 보수적으로 원문 token 기준 비교를 사용한다.

비교상 같아 자동 병합하더라도 output formatting은 current를 기본 골격으로 하고 incoming의 독립 추가를 최소 token edit로 반영한다.

### 기본 3-way 규칙

필드 경로 `P`에 대해 다음 규칙을 적용한다.

| Base | Current | Incoming | 결과 |
|---|---|---|---|
| A | A | B | B |
| A | B | A | B |
| A | B | B | B |
| A | B | C | 정책 적용 또는 최소 충돌 |
| A | ∅ | A | 삭제 |
| A | A | ∅ | 삭제 |
| A | ∅ | B | delete/modify 충돌 |
| ∅ | B | B | B 추가 |
| ∅ | B | C | add/add 충돌 |

`∅`는 필드 또는 element가 존재하지 않음을 의미하는 tombstone이다. 서로 겹치지 않고 구조적 의존성이 없는 필드 경로의 변경만 모두 합친다. `P` 삭제와 `P.x` 수정처럼 ancestor/descendant 경로가 겹치거나, 다른 경로라도 ownership 및 parallel-array 관계로 연결되면 구조 충돌로 처리한다.

### 문서 및 구조 변경

다음 경우를 별도 conflict taxonomy로 다룬다.

- document add/add에서 동일 `fileID`를 서로 다른 객체에 사용
- delete/modify 및 delete/reference
- class ID 또는 `m_Script` 변경과 같은 타입 변경
- GameObject와 Component ownership 변경
- Transform reparent, sibling reorder, scene root 변경
- 한쪽의 component 추가와 다른 쪽의 GameObject 삭제
- 같은 local ID를 사용하는 독립적인 동시 생성
- prefab override 수정과 override 대상 삭제
- managed reference add/add 및 rid 재사용

add/add local ID 충돌에서 두 객체가 다르면 한쪽에 새 ID를 할당하고 해당 side에서 유입된 모든 로컬 참조를 원자적으로 다시 쓸 수 있다. 그래프 전체를 완전히 갱신할 수 없으면 자동 remap하지 않는다.

### 충돌 분류와 정책 적용 범위

| 충돌 종류 | 일반 값 정책 적용 | 요구사항 |
|---|---|---|
| scalar 또는 leaf field 값 충돌 | 허용 | 선택 후 field/type 검증 |
| mapping add/add | 동일 key의 독립 subtree일 때만 허용 | ancestor/descendant 겹침 검사 |
| field delete/modify | side 선택 정책을 명시한 경우 허용 | descendant와 참조 영향 전체 적용 |
| document 또는 GameObject delete/modify | 기본 불허 | 전용 whole-object 정책과 graph-wide 결과 필요 |
| ownership, reparent, root 및 component order | 기본 불허 | 전용 구조 정책 필요 |
| local ID/rid collision | 값 정책 적용 불가 | remap planner 또는 unresolved |
| prefab target/source 교체 | 기본 불허 | side별 dependency를 포함한 전용 정책 필요 |

구조 충돌은 taxonomy가 whole-side 선택의 영향 범위를 명시한 경우에만 `current-first` 또는 `incoming-first`를 적용한다. 단순 leaf 값 정책을 document 삭제나 ID collision에 확대 적용하지 않는다.

### sequence 병합

모든 배열을 같은 방식으로 처리하지 않는다.

- 순서가 의미인 sequence는 deterministic LCS 기반 3-way 병합을 사용한다.
- `m_Component`, scene roots, Transform children처럼 순서와 참조가 함께 의미를 가지는 필드는 전용 규칙을 사용한다.
- 집합처럼 보이는 필드도 duplicate가 허용되면 set으로 축약하지 않는다.
- 서로 인덱스로 결합된 parallel array는 하나의 병합 단위로 처리한다.
- `propertyPath`에 배열 index가 포함되면 concurrent insertion에 의해 대상이 바뀌었는지 검사한다.

중복 element 때문에 correspondence 또는 LCS가 유일하지 않으면 임의 tie-break를 사용하지 않고 sequence conflict로 남긴다. 동일 insertion anchor의 동시 추가는 current 항목 뒤 incoming 항목 순서처럼 명시된 결정적 규칙을 사용할 수 있지만, 순서 자체가 의미이고 양쪽 의도가 상충하면 unresolved로 남긴다. move/modify, move/move, delete/modify는 별도 conflict kind로 보고한다.

### Prefab 및 override 병합

다음 구조를 인식한다.

- `PrefabInstance`
- `m_SourcePrefab`
- `m_CorrespondingSourceObject`
- `m_PrefabInstance`
- stripped document
- `m_Modifications`
- `m_AddedGameObjects`, `m_AddedComponents`
- `m_RemovedGameObjects`, `m_RemovedComponents`

override는 list 위치가 아니라 record 종류별 semantic key를 사용한다. value 및 object-reference modification의 key는 full target PPtr `(guid, fileID, type) + versioned propertyPath + record kind`다. added/removed GameObject 및 Component는 각 record 형식이 가진 source target, instance target, sibling/index 정보를 포함해 별도 key를 정의한다. 같은 key의 duplicate record는 값이 의미적으로 같을 때만 축약하고 그 외에는 구조 충돌로 처리한다. 한쪽이 override를 수정하고 다른 쪽이 target을 삭제하거나 source prefab을 교체하면 자동 병합하지 않는다.

정확한 판정에 source prefab이 필요하면 base/current/incoming 각각의 revision에서 dependency snapshot을 찾는다. working tree의 최신 prefab 하나만 사용해 과거 세 side를 해석하지 않는다. dependency를 얻을 수 없으면 가능한 raw record 병합만 수행하고 제한 사항을 보고한다.

## 충돌 정책

### 지원 정책

- `latest`: 별도 provenance 기능이 두 side의 유일한 field-level last-change commit을 증명하고, 한 commit이 다른 commit의 ancestor일 때만 descendant 변경을 선택한다.
- `base-first`: current와 incoming이 충돌하면 base 값을 선택한다.
- `current-first`: 현재 branch 또는 `--current` 값을 선택한다.
- `incoming-first`: incoming 값을 선택한다.
- `fail`: 손실 선택 없이 가능한 비충돌 병합과 전체 conflict 분석을 수행하되 output을 교체하지 않고 manifest를 기록한 뒤 code 1을 반환한다.

사용자 요구의 `base-first`, `incoming-first`와 함께 현재 branch 선택을 명확히 하기 위해 `current-first`를 별도로 제공한다.

`latest`는 commit timestamp 또는 branch tip 비교를 의미하지 않는다. adapter가 `current`와 `incoming`의 revision OID를 명시적으로 제공하고 각 값의 유일한 provenance를 증명해야 한다. synthetic merge base, dirty snapshot, revert/merge로 provenance가 복수인 경우, 또는 두 provenance commit이 concurrent이면 사용할 수 없다. 초기 merge driver에서는 `latest`를 experimental opt-in으로 두고 provenance를 증명할 수 없으면 정책 미지정과 동일하게 충돌로 남긴다.

### 정책 우선순위

설정은 구체적인 규칙이 우선한다.

1. 정확한 asset GUID 및 object identity
2. file glob + class ID + semantic field path
3. class ID + field path
4. file glob
5. 전역 기본 정책

예시:

```toml
[merge]
default_policy = "unresolved"

[[merge.rules]]
file = "Assets/Scenes/**/*.unity"
class_id = 4
field = "Transform.m_LocalPosition"
policy = "incoming-first"

[[merge.rules]]
file = "Assets/**/*.prefab"
field = "MonoBehaviour.<PrefabId>k__BackingField"
policy = "latest"
```

`--ignore-conflict-policy`는 모든 자동 값 선택 정책을 무시한다. 다만 두 side의 값이 같거나 한쪽만 변경된 비손실 병합은 계속 수행한다.

정책은 값 선택 규칙이지 구조 검증 우회 수단이 아니다. `current-first` 또는 `incoming-first`로 선택한 결과가 dangling reference, ownership 모순, prefab target 손실을 만들면 성공으로 확정하지 않고 structural conflict로 승격한다. `base-first`는 양쪽 변경을 모두 버릴 수 있는 손실 정책이므로 항상 보고서에 `lossyResolution: true`를 기록한다.

## 충돌 출력

### 파일 내 최소 conflict marker

미해결 충돌은 가능한 가장 작은 필드 또는 sequence element 범위에 기록한다.

```yaml
<<<<<<< current
  rotationY: 0
||||||| base
  rotationY: 45
=======
  rotationY: -90
>>>>>>> incoming
```

marker가 포함된 파일은 Unity가 import하지 못할 수 있다. atomic temp output은 부분 파일을 방지할 뿐 Unity import를 방지하지 않으므로, 일반 CLI는 marker 결과를 project 외부 또는 명시적인 output 경로에 쓰는 것을 기본으로 한다. Git merge driver compatibility mode에서만 대상 파일에 marker를 기록한다.

`--conflict-output sidecar`는 `--sidecar-value <base|current|incoming>`을 반드시 요구한다. 지정한 materialized value로 문법상 유효한 YAML을 만들지만 상태는 계속 `conflicted`이며 code 1을 반환한다. manifest에는 각 충돌의 `materializedSide`를 기록한다. sidecar가 유실되면 해결된 파일처럼 보일 수 있으므로 merge driver의 기본값으로 사용하지 않는다.

### Conflict manifest

텍스트 보고와 별도로 versioned JSON manifest를 제공한다.

```json
{
  "schemaVersion": 1,
  "status": "conflicted",
  "output": "Assets/Scenes/OverworldScene.unity",
  "conflicts": [
    {
      "assetGuid": "...",
      "fileId": "1234",
      "classId": 4,
      "objectPath": "Root/Patient/Transform",
      "fieldPath": "m_LocalPosition.x",
      "baseRange": { "startLine": 10, "endLine": 10 },
      "outputRange": { "startLine": 12, "endLine": 18 },
      "currentValue": "0",
      "incomingValue": "1",
      "reason": "divergent-same-field-change"
    }
  ]
}
```

텍스트 모드 stdout에는 파일 경로, output line range, object path, field path, 선택 가능한 행동을 출력한다. 로그와 진행률은 stderr로 보내며 `--format json`에서는 stdout을 JSON 한 건으로 제한한다.

### 이미 marker가 삽입된 입력

```text
unity-merge resolve-markers FILE
```

- diff3 marker에 base가 있으면 해당 구간을 구조적으로 다시 3-way 병합한다.
- 일반 2-way marker만 있고 VCS에서 base를 복원할 수 있으면 복원한 base를 사용한다.
- base도 VCS도 없으면 두 side와 주변 구조만 분석한다. 같은 필드의 다른 값은 자동으로 추론하지 않고 충돌로 유지한다.
- 중첩되거나 손상된 marker는 원문을 보존하고 parse error 범위를 보고한다.

marker는 YAML 구조 경계에서만 lexical marker로 인식한다. block scalar, quoted scalar, 주석 안의 동일 문자열은 marker가 아니다. 허용 marker 길이와 label grammar를 명시하고, 들여쓰기 또는 marker collision이 모호하면 자동 재작성하지 않고 sidecar 분석만 제공한다.

## 대화형 해결

TTY이고 `--interactive`가 명시된 경우에만 준 TUI를 실행한다.

- base/current/incoming 값과 raw YAML context 표시
- hierarchy object path와 prefab source 표시
- current, incoming, base, 직접 편집 선택
- 선택에 의해 영향을 받는 참조 목록 표시
- 이력 추론 근거와 confidence 표시
- resolution plan을 `.unity-merge/resolutions/<session>.json`에 저장해 재개 가능
- 모든 선택 후 그래프 재검증

stdin/stdout이 파이프이거나 `--non-interactive`이면 TUI를 열지 않는다. 이 경우 conflict manifest와 exit code만 반환한다. TUI는 별도 병합 구현이 아니라 동일한 conflict model의 frontend여야 한다.

## 이력 식별자 인덱스

### 저장 위치와 성격

- Git repository에서는 공유 인덱스를 `git rev-parse --git-common-dir` 아래 `unity-merge/INDEX`에 저장한다. 여러 linked worktree가 같은 object database와 이 인덱스를 공유한다.
- worktree별 provisional cache와 session은 `git rev-parse --git-dir` 아래 `unity-merge/`에 둔다.
- Git이 없는 project에서는 project root의 `.unity-merge/INDEX`를 fallback 위치로 사용한다.
- `INDEX`는 schema version을 포함하는 transactional SQLite 또는 동등한 단일 파일 데이터베이스로 구현한다.
- 파생 캐시이며 언제든 rebuild할 수 있어야 한다. checkout 안의 `.unity-merge/INDEX`를 Git 환경의 authoritative cache로 사용하지 않는다.
- 팀 공유 정책과 수동 identity mapping은 별도 추적 가능한 `.unity-merge/config.toml`, `.unity-merge/identity-overrides.toml`에 둔다.
- 인덱스가 잠겼거나 손상되면 기본 병합은 `--no-index`와 같은 상태로 계속하고 경고를 반환한다.

shared index의 repository identity는 common Git directory의 private state에 생성한 random UUID다. filesystem path나 remote URL을 identity로 사용하지 않는다. 인덱스에는 policy-neutral observation만 저장하며, invoking worktree의 config hash와 정책은 merge report에만 기록한다.

### 기록 데이터

- repository UUID
- commit OID, ordered parent OID 목록, tree OID, blob OID
- asset path와 `.meta` path
- asset GUID와 asset generation
- document `fileID`, class ID, script GUID
- managed reference rid
- hierarchy path, sibling index, component order
- 구조 fingerprint와 선택된 안정 필드 fingerprint
- 외부 및 로컬 참조 edge
- 생성, 수정, 이동, 복사, 삭제, 재등장 event
- 추론된 identity transition의 confidence와 evidence

같은 blob은 OID별로 한 번만 parsing하고 commit/tree/path 관찰, parent-edge transition, ref reachability epoch를 별도 테이블로 연결한다. 전체 YAML CST가 아니라 식별 및 비교에 필요한 summary를 캐시한다. parser/schema 또는 indexing scope가 변경되면 파생 transition만 재계산할 수 있어야 한다.

### 변경 추적 모델

단일 `old ID -> new ID` map이 아니라 event graph를 사용한다.

- 같은 GUID, 다른 경로: move 후보. 동시에 두 경로에 존재하면 copy 또는 duplicate GUID로 구분한다.
- 같은 경로, 다른 GUID: meta regeneration 또는 asset replacement 후보.
- 삭제 후 같은 경로와 새 GUID 생성: 구조 fingerprint, 인접 commit, 참조 연속성으로 평가하되 자동 확정하지 않을 수 있다.
- object `fileID` 변경: 같은 asset generation 안에서 class, hierarchy, component ownership, stable fields, reference continuity로 후보를 계산한다.
- split, merge, prefab unpack/re-prefab은 일대일 transition으로 강제하지 않는다.

증거 등급 예시는 다음과 같다.

| 등급 | 예시 | 자동 재연결 |
|---|---|---|
| Exact | 검토된 수동 mapping 또는 parent/path/GUID 연속성으로 증명된 동일 세대 | 허용 |
| Strong | 같은 GUID의 rename + 유일한 object structural match | 허용 가능 |
| Probable | 같은 path GUID 재생성 + 유일한 높은 점수 후보 | 기본은 보고, opt-in 시 허용 |
| Ambiguous | 이름 또는 hierarchy 유사 후보 복수 | 금지 |

같은 blob은 content equality 증거일 뿐 identity continuity의 Exact 증거가 아니다. copy와 독립 생성에서도 같은 blob이 나타날 수 있으며, `.meta` blob이 여러 경로에 동시에 존재하면 duplicate GUID 가능성을 우선 검사한다.

### Git 이력 탐색

- linear history가 아니라 reachable commit DAG를 탐색한다.
- 기본 index 범위는 current HEAD와 설정된 primary refs다. 모든 local branch, remote-tracking branch, tag는 `--all-refs`에서만 포함한다.
- reflog에만 남은 commit과 unreachable object는 기본 범위에서 제외하고 명시 옵션으로만 포함한다.
- linked worktree의 detached HEAD는 worktree observation root로 기록하고 worktree 제거 후 grace period를 두고 prune한다.
- merge commit은 ordered parent 각각에 대해 `(parent OID, child OID, path)` transition evidence를 별도로 계산한다. 상충하는 parent-edge transition을 하나의 identity event로 자동 승격하지 않는다.
- branch, tag 포함 범위를 설정 가능하게 한다.
- shallow/partial clone, missing object, rewritten history를 감지하고 index completeness를 기록한다.
- rename/copy, directory move, case-only rename, 삭제, submodule, sparse checkout, Git LFS pointer를 구분한다.
- NUL-safe path 처리를 사용하고 locale 의존 shell parsing을 사용하지 않는다.
- incremental update는 ref reachability epoch를 갱신하고, 새 commit/blob뿐 아니라 새로 materialize된 partial-clone object, 변경된 shallow boundary, 누락 parent 복구, force-push로 unreachable된 commit을 처리한다.
- indexing은 기본적으로 network fetch를 유발하지 않는다. commit 수, 누적 blob byte, 개별 blob 크기, CPU 시간, graph edge 수에 budget을 적용하며 중단 가능한 checkpoint와 `incomplete` 상태를 남긴다.

## 끊어진 참조 복구

병합 후 모든 참조를 검증한다.

### 로컬 참조

- `{fileID}` 대상이 현재 asset 안에 없으면 side별 document map과 remap plan을 먼저 확인한다.
- add/add ID 충돌 때문에 재할당한 경우 해당 side에서 유입된 참조를 모두 원자적으로 갱신한다.
- target 삭제가 명백하면 null 허용 필드만 `{fileID: 0}`으로 바꾼다.
- null 허용 여부를 알 수 없거나 대상 후보가 모호하면 자동 변경하지 않고 unresolved reference로 보고한다.

### 외부 참조

GUID를 현재 프로젝트 및 인덱스에서 찾지 못했다는 사실만으로 삭제로 판단하지 않는다. index completeness, ref scope, partial/shallow 상태, package 및 LFS 가용성을 먼저 확인한다. 다음 순서로 판단한다.

1. 같은 asset generation의 경로 이동 확인
2. GUID 변경 event에서 유일한 Strong 대상 확인
3. 원래 GUID가 사라진 commit에서 같은 경로의 새 GUID 확인
4. 구조 fingerprint와 incoming reference continuity 확인
5. relevant side lineage와 dependency scope가 complete하고 positive deletion event가 증명되며 필드가 nullable이면 null 처리
6. 후보가 복수이거나 evidence가 약하면 충돌 유지

자동 재연결 또는 null 처리 시 이전 tuple, 새 tuple, 근거 commit, confidence, 영향받은 필드 목록을 stdout/JSON에 반환한다. 정보가 없다는 사실은 deletion evidence가 아니며 이 경우 `unknown-evidence`로 남긴다.

## VCS 비의존 동작

Git 없이도 dependency-free 보수적 병합은 동작해야 한다.

- 명시적 base/current/incoming 3-way 병합
- 이미 conflict marker가 있는 파일의 구조 분석
- 현재 project tree와 `.meta`를 이용한 가능한 범위의 참조 검증
- 사용자가 제공한 `--identity-map` 기반 GUID/fileID remap
- manifest 출력, TUI, resolution plan 적용

side별 project 또는 dependency manifest를 제공하면 Git 없이도 revision-correct prefab 검증이 가능하다. 제공하지 않으면 raw-record 병합만 수행하고 `prefabSemanticValidation: not-checked`를 기록한다. Git과 revision provenance가 없으면 사용할 수 없는 기능은 latest 판정과 과거 rename/GUID 재생성 추론이다. 이 경우 기능을 조용히 추측하지 않고 capability report에 unavailable로 표시한다.

## CLI 제안

```text
unity-merge inspect FILE
unity-merge validate FILE [--project-root ROOT]
unity-merge merge --base B --current C --incoming I --output O
  [--base-project ROOT|--base-dependencies MANIFEST]
  [--current-project ROOT|--current-dependencies MANIFEST]
  [--incoming-project ROOT|--incoming-dependencies MANIFEST]
unity-merge resolve-markers FILE [--output O]
unity-merge index build [--all-refs]
unity-merge index update
unity-merge index status
unity-merge index verify
unity-merge index rebuild
unity-merge identity trace --guid GUID [--file-id ID]
unity-merge git install-driver
unity-merge git install-hooks
unity-merge git staged-cache
```

공통 옵션:

- `--policy <latest|base-first|current-first|incoming-first|fail>`
- `--ignore-conflict-policy`
- `--format <text|json>`
- `--conflict-output <markers|sidecar>`
- `--sidecar-value <base|current|incoming>`
- `--interactive`, `--non-interactive`
- `--no-index`, `--require-index` (서로 배타적)
- `--project-root`
- `--current-revision OID`, `--incoming-revision OID`
- `--identity-map FILE`
- `--dry-run`
- `--check`
- `--max-file-bytes`, `--max-documents`, `--max-conflicts`
- `--max-depth`, `--max-token-bytes`, `--max-reference-edges`, `--timeout`
- `--validation-level <syntax|structural|unity-import>`, `--require-complete-validation`

### Exit code

| Code | 의미 |
|---|---|
| 0 | 충돌 없이 병합 또는 검증 성공 |
| 1 | 결과는 생성했으나 미해결 충돌 존재 |
| 2 | 입력 또는 YAML 구조 오류 |
| 3 | 지원하지 않는 binary/serialization 형식 |
| 4 | 참조 무결성 검증 실패 |
| 5 | index 전용 명령 실패 또는 index가 필수로 지정된 작업의 실패 |
| 10 | 내부 오류 |

기본 `merge`에서 index만 실패한 경우 no-index 모드로 계속한다. 병합 자체가 clean이면 code 0, 충돌이 남으면 code 1을 유지하고 `warnings`에 index 장애를 기록한다. `--require-index`가 지정된 경우에만 code 5를 반환한다.

우선순위는 command별로 고정한다. 지원 불가 또는 입력/내부 오류(code 3/2/10)는 output 교체 전에 종료한다. `--require-index` 실패(code 5)도 병합 전에 종료한다. 분석 가능한 merge에서 미해결 충돌은 code 1이며 marker가 의도적으로 비-YAML인 것은 추가 code 2가 아니다. marker 없이 clean 후보가 생성됐지만 graph validation이 proven invalid이면 code 4다. JSON은 하나의 `primaryStatus`와 모든 secondary warning/capability를 함께 기록한다.

## Unity Smart Merge 호환성

UnityYAMLMerge를 사용하는 기존 merge driver를 대체할 수 있도록 compatibility frontend를 제공한다.

- `base`, `left(theirs)`, `right(mine)`, `dest` argument 순서 지원
- headless 모드
- current/incoming 선택 옵션
- premerge 모드
- conflict report 파일 출력
- fallback command 실행
- 입력과 다른 output 경로
- conflict 유무를 표현하는 신뢰 가능한 exit code

UnityYAMLMerge의 `-l`, `-r`, `-p`, `-i`, `-o`, `--fallback`, `--force`, `--nomappinginoneline`, `--describe`를 호환 adapter에서 대응한다. 내부 의미가 다른 옵션은 경고와 함께 새로운 명시 옵션으로 변환한다. differential fixture를 통해 Smart Merge가 성공하는 기존 사례를 모두 수용하되, Smart Merge 결과가 항상 정답이라고 가정하지 않는다.

side orientation은 다음과 같이 고정한다.

| Frontend | Base | Current | Incoming | Output |
|---|---|---|---|---|
| Git merge driver | `%O` | `%A` | `%B` | `%A` |
| Unity compatibility | `base` | `right(mine)` | `left(theirs)` | `dest` |

내부 보고와 정책은 항상 `current`, `incoming` 명칭을 사용한다. asymmetric fixture로 방향 반전을 검증한다.

fallback command는 기본 비활성화한다. 사용자가 신뢰한 argv vector를 명시한 경우에만 shell을 거치지 않고 실행하며, repository의 과거 config가 임의 실행 명령을 제공할 수 없게 한다. hook 및 CI에서는 별도 opt-in 없이는 실행하지 않는다.

## Git merge driver 및 mergetool 연동

`.gitattributes` 예시:

```gitattributes
*.unity merge=unity-merge diff=unitydiff
*.prefab merge=unity-merge diff=unitydiff
*.asset merge=unity-merge diff=unitydiff
```

merge driver는 Git이 제공하는 ancestor/current/other 파일을 핵심 엔진에 전달한다. 자동 병합 후 conflict가 남으면 exit code 1을 반환하고, manifest는 Git protocol에 의존하지 않는 결정적 worktree-private 경로에 저장한다. mergetool은 그 manifest를 TUI에 전달한다.

Git 설정 설치는 opt-in이며 기존 `.gitattributes`와 사용자 git config를 덮어쓰기 전에 diff와 확인 내용을 출력한다. 도구가 `core.hooksPath`를 소유하거나 기존 hook을 덮어쓰지 않는다. 감지된 hook manager에 연결하거나 절대 executable path를 사용하는 chaining snippet만 생성한다.

## 커밋 전후 캐싱

pre-commit 시점에는 아직 새 commit OID가 없으므로 한 단계 훅만으로 완전한 commit index를 만들 수 없다. 다음 2단계 방식을 사용한다.

1. `pre-commit`: staged tree의 Unity asset/meta blob을 분석해 provisional cache를 만든다. record key는 repository UUID, worktree Git-dir, exact `git write-tree` OID, parser version, random invocation token이다. 전체 이력을 재탐색하지 않는다.
2. `post-commit`: 실제 commit tree OID가 provisional tree OID와 같을 때만 commit OID와 연결한다. 다르면 폐기하고 commit을 정상 indexing한다.

추가로 `unity-merge commit -- <git commit args>` wrapper를 제공할 수 있지만 일반 `git commit`을 필수로 대체하지 않는다. 훅은 repository와 함께 clone되지 않을 수 있으므로 `git install-hooks` 명령과 CI용 직접 명령을 함께 제공한다.

hook은 correctness의 원천이 아니라 latency 최적화다. commit 실패, `--no-verify`, amend, rebase, cherry-pick, 외부 ref update로 누락된 관찰은 authoritative `index update`가 reachable commit을 재조정하면서 보충한다.

훅 요구사항:

- staged asset을 수정하지 않는다.
- 명확한 timeout과 process lock을 사용한다.
- index 실패가 commit 자체를 막을지는 설정 가능하며 기본은 경고다.
- full rebuild를 synchronous hook에서 수행하지 않는다.
- 여러 worktree가 같은 `.git` object database와 worktree별 project root를 공유하는 경우를 처리한다.

## 인덱스 무결성과 운영

- schema version, parser version, repository identity를 저장한다.
- transaction, process lock, atomic replacement, crash recovery를 지원한다.
- blob checksum과 indexed refs를 저장해 stale 상태를 탐지한다.
- `index verify`, `index rebuild`, `index status`, `index prune`을 제공한다.
- force-push 또는 history rewrite 뒤 unreachable observation을 즉시 삭제하지 않고 grace period 후 prune한다.
- 파일 수, commit 수, blob 수, 용량, completeness를 표시한다.
- 인덱스 크기 상한과 오래된 parsed summary 정리 정책을 제공한다.

## 출력 및 쓰기 안전성

- output은 같은 디렉터리의 안전한 임시 파일에 기록하고 fsync 후 atomic replace한다.
- input과 output이 같아도 parsing과 검증 완료 전 원본을 덮어쓰지 않는다.
- symlink와 path traversal을 검사한다.
- mutable cache, provisional state, report 파일은 checkout 밖의 private directory에서 no-follow 방식으로 열고 open 후 소유자와 file type을 재검증한다.
- 취소, signal, crash 시 부분 결과를 대상 파일에 남기지 않는다.
- BOM, UTF-8, CRLF/LF, final newline을 가능한 한 원본 기준으로 보존한다.
- 절대 경로 및 YAML scalar 내용은 기본 telemetry에 기록하지 않는다.
- parser는 임의 YAML tag를 실행 가능한 객체로 deserialize하지 않는다.
- Unity assembly나 editor callback을 실행하지 않는다.

## 설정 파일

```text
.unity-merge/
  config.toml                 # 팀 공유 가능 정책
  identity-overrides.toml     # 검토된 수동 identity mapping
  resolutions/                # Git 비사용 fallback 또는 명시적 export
  reports/                    # 선택적 merge manifest export

$GIT_COMMON_DIR/unity-merge/
  repository-id
  INDEX                       # linked worktree 공유 파생 캐시

$GIT_DIR/unity-merge/
  provisional/               # worktree별 staged tree cache
  sessions/                  # worktree별 TUI 및 merge session
  reports/                   # merge driver manifest
```

수동 identity override에는 작성자 메모, 근거 commit, 적용 범위, 만료 조건을 기록한다. override가 현재 graph와 모순되면 적용하지 않는다.

## 검증 규칙

검증 상태는 `valid`, `invalid`, `unresolved`, `not-checked`로 구분한다. dependency나 index가 없어 검사하지 못한 상태는 invalid가 아니다. `--require-complete-validation`을 지정했을 때만 `not-checked`를 실패로 승격한다.

병합 결과에 대해 최소한 다음 invariant를 검사한다.

- document anchor가 asset 안에서 유일함
- 모든 로컬 참조가 존재하거나 명시적 null임
- external reference tuple 형식이 유효함
- GameObject `m_Component`와 Component `m_GameObject` 소유 관계가 모순되지 않음
- Transform parent/children 관계가 가능한 범위에서 일관됨
- scene root와 hierarchy가 순환하지 않음
- prefab stripped object가 유효한 PrefabInstance를 참조함
- prefab override target이 존재하거나 unresolved로 보고됨
- managed reference rid가 유일하고 references table과 일치함
- remap된 ID의 모든 참조가 갱신됨

검증 level은 `syntax`, `structural`, `unity-import`로 구분하고 JSON에 `validationLevel`과 capability별 결과를 기록한다. 구조 검증 성공은 gameplay 의미가 맞다는 보장이 아니다. 선택적으로 지원 Unity 버전의 batch-mode import/re-save 검증을 별도 명령으로 제공한다. code 0은 사용자가 요청한 검증 level이 성공했다는 뜻일 뿐 더 높은 level의 성공을 암시하지 않는다.

## 테스트 전략

### Parser 및 round-trip

- 지원 Unity 버전별 scene, prefab, asset golden corpus
- 음수 및 큰 `fileID`, null, stripped, unknown class ID
- flow/block style, quoted string, multiline scalar, CRLF/LF
- managed reference와 unknown `serializedVersion`
- parse 후 무수정 출력 byte equality 또는 허용된 최소 차이

### 병합

- 모든 기본 3-way truth table
- delete/modify, add/add ID collision, reparent, reorder
- component 추가와 GameObject 삭제
- 같은 값 동시 변경
- policy 및 `--ignore-conflict-policy`
- 결정론과 반복 실행 멱등성
- conflict marker 최소 범위와 line range 정확성

### Prefab

- nested prefab, variant, stripped object
- override 추가/수정/삭제
- added/removed GameObject 및 Component
- source prefab 교체
- side별 dependency snapshot 누락

### 참조 및 이력

- path rename, directory move, case-only rename
- GUID 재생성, copied meta, duplicate GUID
- 삭제 후 같은 경로 재생성
- local fileID 변경과 add/add collision remap
- branch, merge commit, shallow clone, force-push
- 다중 worktree와 concurrent index update

### 신뢰성

- malformed/hostile YAML fuzzing
- 최대 크기 및 깊이 제한
- crash injection과 index transaction 복구
- output atomicity
- JSON schema snapshot
- UnityYAMLMerge differential test
- Unity batch-mode import와 representative scene open

## 성능 목표

- 기본 병합은 대상 파일 크기에 대해 선형에 가까운 parsing과 변경 후보 partitioning을 목표로 한다.
- 구조 유사도 비교는 class, asset generation, hierarchy neighborhood로 후보를 제한해 전체 O(n²) 비교를 피한다.
- large scene은 document discovery를 streaming하고 필요한 CST만 메모리에 유지한다.
- 이력은 blob OID별로 한 번만 parsing한다.
- cold full-index와 incremental update를 별도 benchmark한다.
- 성능 budget을 파일 크기, document 수, commit 수 기준으로 테스트에 고정한다.

## 단계별 구현 계획

### Phase 0: 명세와 corpus

- Unity CST, identity namespace, conflict taxonomy 명세
- 실제 프로젝트에서 비식별화한 fixture corpus 구성
- `inspect`, `validate`, round-trip 구현
- CLI/JSON schema와 exit code 확정

### Phase 1: VCS 비의존 보수적 3-way 병합

- 명시적 base/current/incoming 병합
- scalar/map/document 병합
- semantic identity가 유일한 기본 sequence 처리. unknown sequence는 unresolved
- 최소 marker와 conflict manifest
- atomic output 및 syntax/document-anchor 검증
- 정책과 non-interactive 동작
- no-index Git merge driver와 CI command로 실제 통합 조기 검증

### Phase 2: Unity 오브젝트 그래프 및 Prefab

- GameObject, Component, Transform 전용 병합
- local ID collision remap planner
- managed reference
- prefab override와 stripped object
- side별 dependency resolver
- object/reference graph invariant 검증

### Phase 3A: Git 관찰 인덱스

- Git에서는 `$GIT_COMMON_DIR/unity-merge/INDEX`, 비-Git project에서는 `.unity-merge/INDEX`
- commit DAG 및 blob summary 증분 indexing
- verify/rebuild/prune 및 다중 worktree 처리
- completeness와 parent-edge path/GUID event를 report-only로 제공

### Phase 3B: Identity 추론

- GUID/path/object transition graph
- explainable identity candidate query
- 수동 override와 false-positive 측정
- Strong 이상 자동 연결은 별도 opt-in으로 시작

### Phase 4: 통합과 TUI

- UnityYAMLMerge compatibility frontend
- Git mergetool
- pre-commit provisional cache와 post-commit finalize
- resolution plan 기반 TUI
- cross-platform package와 installer

## 주요 위험과 완화

| 위험 | 완화 |
|---|---|
| 유사한 별개 오브젝트를 같은 대상으로 오인 | 유일한 Strong evidence가 아니면 자동 연결 금지 |
| 일반 YAML serializer가 파일 전체를 재포맷 | Unity CST와 token 범위 기반 출력 |
| prefab source revision 불일치 | side별 dependency snapshot, 없으면 보수적 충돌 |
| ID remap 일부 참조 누락 | graph-wide rewrite 후 invariant 검증, 실패 시 rollback |
| `latest`가 시간값 또는 tip 비교 때문에 비결정적 | 명시 revision과 유일한 field provenance가 있고 DAG ancestor 관계일 때만 인정 |
| index 손상으로 병합 불가 | index 선택화, 자동 no-index degradation |
| pre-commit에서 전체 이력 탐색으로 지연 | staged provisional cache만 생성, post-commit 증분 finalize |
| marker 파일을 Unity가 import하여 오류 | 일반 CLI는 project 외부 output, merge driver만 marker overwrite, sidecar도 code 1 유지 |
| low-confidence history inference가 사용자 변경 훼손 | confidence/evidence 보고, 기본 자동 적용 금지 |

## 자세한 달성 목표

- 지원 semantic identity로 독립성이 증명된 비충돌 필드 변경을 조용히 폐기하지 않음
- 같은 값 동시 변경의 충돌 0건
- 미해결 충돌을 object/field 경로와 output line range로 모두 보고
- 자동 ID remap은 영향 범위의 dangling local reference가 0건일 때만 확정
- 같은 입력에 byte-stable하고 멱등인 결과
- index-dependent 정책과 추론을 끈 동일 snapshot/context에서는 index 유무와 관계없이 같은 보수적 결과 생성
- TTY가 없는 실행에서 prompt 0건
- 지원 corpus에서 UnityYAMLMerge가 해결하는 clean case를 회귀 fixture로 유지하고 차이는 명시적으로 검토
- 구조적으로 잘못된 자동 병합을 성공으로 반환하지 않음

## 성공 여부 측정

- golden merge corpus의 기대 결과와 일치한다.
- property-based test에서 document anchor uniqueness와 reference preservation이 유지된다.
- 동일 명령 재실행 시 추가 diff가 발생하지 않는다.
- 실제 대형 scene/prefab conflict에서 Smart Merge 대비 미해결 범위가 같거나 작고, 보존된 독립 변경 수가 많다.
- 지원 Unity 버전 batch-mode import/re-save 검증이 통과한다.
- 동일 ref scope와 completeness에서 full index와 incremental index가 같은 identity query 결과를 낸다.
- index corruption, lock, shallow clone에서 안전하게 no-index 동작으로 전환한다.
- CI가 JSON manifest와 exit code만으로 clean/conflicted/invalid 상태를 구분한다.

## 결정이 필요한 사항

- 구현 언어 및 배포 방식: 단일 native binary 우선 여부
- 최초 지원 Unity 버전 범위
- `$GIT_COMMON_DIR/unity-merge/INDEX` 및 비-Git `.unity-merge/INDEX`의 SQLite 채택 여부
- 일반 CLI의 project 외부 marker output 위치와 manifest 보존 기간
- Strong history inference의 자동 적용을 초기부터 허용할지
- source prefab dependency snapshot 저장 범위와 용량 상한
- 수동 identity override의 코드 리뷰 및 만료 정책
- pre-commit index 실패를 경고로 둘지 commit 차단으로 둘지

## 권고 결론

첫 릴리스의 성공 기준은 Git 전체 이력을 이용한 자동 복구가 아니라, Git 없이도 안전하고 결정론적인 Unity-aware 3-way 병합을 제공하는 것이다. 이력 인덱스는 merge engine의 필수 입력이 아니라 참조 추적과 모호성 감소를 위한 선택적 증거 계층이어야 한다.

특히 낮은 신뢰도의 GUID/fileID 자동 재연결, source prefab 없이 수행하는 완전한 prefab 실체 병합, 이름과 hierarchy 유사도만을 이용한 자동 선택은 초기 범위에서 제외한다. 이 경계를 지켜야 Smart Merge보다 더 많은 것을 자동 처리하면서도 더 위험한 조용한 데이터 손상을 만들지 않을 수 있다.
