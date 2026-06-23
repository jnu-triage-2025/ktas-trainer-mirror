# 설계 명세 — TODO-SPEC-3: 역할 태그 교대(Rotation)

## 문제
`ScenarioPlayerTagNode`(`ScenarioPlayerTagNode.cs`)는 다음만 지원한다.
- `Operation`: `Add` / `Remove` / `Change`(같은 플레이어 내 FromTag→ToTag 교체)
- `Scope`: `All`(전원) / `Current`(시나리오 owner 1인)

따라서 "현재 cpr_team 보유자와 airway_team 보유자의 태그를 **서로 맞바꾼다**"를 표현할 수 없다.
환자 A CPR은 P005(1사이클)에서 A=앰부(airway), B=가슴압박(cpr), C=제세동(defib), D=에피(medication)였다가
P006(2사이클)에서 A=가슴압박(cpr), B=앰부(airway), C=에피(medication), D=제세동(defib)으로 **교대**한다.
정적 `requiredPlayerTags`로는 "같은 사람이 역할을 맞바꾼다"는 의미가 사라진다.

## 제안 (전부 optional, 하위호환)

### A. Scope 확장
```
enum ScenarioPlayerTagScope { All, Current, ByTag }   // ByTag 신규
```
- `ByTag`: 특정 태그 보유 플레이어 집합을 대상으로 Add/Remove 적용(부분 그룹 조작).

### B. Swap 연산
```
enum ScenarioPlayerTagOperationType { Add, Remove, Change, Swap }   // Swap 신규
```
- `ScenarioPlayerTagNode`에 `SwapTagA`, `SwapTagB` 필드 추가.
- 의미: `SwapTagA` 보유 플레이어(들)와 `SwapTagB` 보유 플레이어(들)의 해당 태그를 상호 교환.
- 1:1 매칭 가정(각 태그 1인). 다대다는 비결정적이므로 로드/실행 시 경고 후 안전 스킵.

## 예시 (환자 A P005→P006 교대)
P006 진입 직전 D029(의사 교대 지시) 다음에 삽입:
```json
{ "nodeType": "PlayerTag", "identifier": "T_swap_AB",
  "operation": "Swap", "swapTagA": "airway_team", "swapTagB": "cpr_team",
  "nextIdentifier": "T_swap_CD" },
{ "nodeType": "PlayerTag", "identifier": "T_swap_CD",
  "operation": "Swap", "swapTagA": "defib_team", "swapTagB": "medication_team",
  "nextIdentifier": "P006" }
```
- 이후 P006 브랜치의 `requiredPlayerTags`(N021=cpr_team, N022=airway_team …)가 교대된 플레이어와 정확히 매칭.

## 선행 의존성
- 시나리오 시작 시 **초기 역할 태그 부여**가 필요(현재 두 시나리오에 PlayerTag 노드 0개).
  - 원본 루브릭의 "역할 선택" 단계 → Choice/RoleAssignment + PlayerTag(Add) 노드로 표현.
  - 본 명세는 "이미 부여된 태그를 교대"하는 부분을 담당하며, 초기 부여는 시나리오 데이터(인트로 또는 본 시나리오 도입부)에서 선행.

## 직렬화/검증
- DTO: `ScenarioPlayerTagNodeDTO`에 `swapTagA`/`swapTagB`(string, optional) 추가.
- `scenario.schema.json`: `operation` enum에 `Swap`, `scope` enum에 `ByTag`, 신규 필드 추가(전부 optional).

## 테스트
- Swap 후 두 플레이어의 태그가 정확히 교환되는지.
- 한쪽 태그 보유자 부재 시 안전 스킵 + 경고.
- 다대다(2인 이상 동일 태그) 시 경고·미적용.
- 기존 Add/Remove/Change/All/Current 그래프 회귀 없음.

## 영향
- CPR 2사이클 교대를 데이터 2노드로 표현, 정적 태그 매칭 한계 해소.
- 다른 교대형 시뮬레이션(역할 로테이션)에 재사용 가능.
