# Feature Proposal: 아이템 사용 타깃 해석 활성화 및 처치(apply/use) 완료 신호 배선

> 구현 완료(2026-06-25, 브랜치 `feat/scenario-validator-gate-timeout`): 부착형 적용/착용
> (`apply_*`/`wear_glove`) 범위로 구현됨. `IItemUseTarget` 인터페이스 신설 + `PlayerController.UseItem`
> 브리지 + `PatientController`/`MovingPatientBedController` 구현 + `AttachableItemVisualPair.ApplySignal`
> 설정. 삽입(`insert_iv_*`)·주입(`push_*`)·흡인/제거는 전용 메커닉이 추가로 필요하여 본 구현 범위에서 제외.
> 레퍼런스: `Documents/api-references/MultiplayerInfrastructure.Entity.IItemUseTarget.md`,
> 설정 가이드: `Documents/requirements/content-definitions/scenario/item-apply-signal-setup-guide.md`.

- 작성일: 2026-06-25
- 대상 모듈: `Assets/Modules/MultiplayerInfrastructure/Scripts/Player/` (시스템), `Assets/Modules/TriageTrainer/Scripts/Items/` (프로젝트)
- 관련 평가: `Agents/Proposals/done/2026-06-25-scenario-validator-gate-timeout/Evaluation - Scenario JSON vs Original Plan.md` (S-2)
- 관련 명세: `Documents/requirements/content-definitions/scenario/interaction-signal-integration-spec.md` §2

### 개요

`아이템 사용 타깃 해석 활성화 및 처치 완료 신호 배선` 기능은 현재 **스텁으로 비활성화된** 아이템
"사용/공격 대상 해석"(`PlayerController.RaycastTargetEntity`)을 실제 구현하여, 이미 존재하지만
호출되지 못하는 "들고 있는 아이템을 대상(환자/침대)에 적용" 경로를 살리고, 그 완료 지점에서
시나리오 게이팅용 완료 신호(`sig.apply_*`, `sig.wear_glove`, `sig.push_*`)를 올리는 구현입니다.

- 요약: `RaycastTargetEntity()` 의 `// TODO return null;` 을 카메라 레이캐스트 → `Entity` 해석으로
  구현하고, `MedicalItem.OnUse` 에서 대상 기반으로 `ScenarioInteractionSignals.Raise(...)` 를 호출한다.
- 의도/목표: 평가 루브릭의 핵심 "처치 수행" 게이트(지혈/고정/플라스터/장갑/약물 주입)를
  **실제 게임플레이 수행으로 통과**시켜, "수행해야 진행"(G-2)과 평가 기록(G-3)을 실효화한다.
- 주요 맥락: 부착 시각화(`PatientController.TryAttachItem`, `MovingPatientBedController.TryAttachItem`)와
  대상측 핸들러(`OnItemUsed(Entity, itemId)` / `OnAttacked(Entity, dmg)`)는 **이미 구현되어 있으나**,
  현재 **누구도 호출하지 않는 고아(orphan) 메서드**다. 막힌 지점이 두 개다:
  (1) `RaycastTargetEntity()` 스텁(=null), (2) `PlayerController.UseItem()`/`Attack()` 가 해석된 대상의
  `OnItemUsed`/`OnAttacked` 를 **호출하는 브리지 자체가 없음**(`Entity.Attack` 은 플레인 `Entity`의
  health 만 깎고 `PatientController` 로 전달되지 않음). 따라서 본 제안은 단순 스텁 채움이 아니라
  **"아이템 사용 대상 해석 + 대상 핸들러 호출 브리지"** 신설을 포함한다.
- 설계 메모: 레이캐스트는 Collider→GameObject→`MonoBehaviour` 를 맞히므로, 대상 핸들러를 공통
  인터페이스(예: `IItemUseTarget { void OnItemUsed(Entity user, string itemIdentifier); }`)로 추상화하고
  `PatientController`/`MovingPatientBedController` 가 이를 구현하도록 한다. `RaycastTargetEntity` 또는
  신규 `RaycastUseTarget()` 가 이 인터페이스를 반환하고, `UseItem()` 이 `target.OnItemUsed(PlayerEntity, id)` 를 호출한다.
- 기술적 제약: `RaycastTargetEntity` 는 `MultiplayerInfrastructure.Player.PlayerController` 의 일부로,
  타 프로젝트에서도 재사용되는 핵심 입력/아이템 경로다. 신중한 변경과 하위호환이 필요하다.

### 해결하려는 문제 상황

나는 **시나리오 콘텐츠 운영자/교육 설계자**로서, 학습자가 거즈로 지혈하고, 경추 고정기를 적용하고,
장갑을 착용하고, 약물을 주입하는 등 **실제 처치 동작을 수행해야 다음 단계로 진행**되도록 하고 싶다.
왜냐하면 그것이 원본 평가 루브릭의 핵심 교육 의도이기 때문이다.

그러나 현재 엔진은 아이템을 대상에 "사용"하는 입력 경로의 타깃 해석이 스텁(`RaycastTargetEntity()`
→ `return null;`)이라, `InvokeUseItem`/`InvokeAttack` 이 항상 `target=null` 로 호출된다
(`PlayerController.Item.cs:124,132,136-140`). 따라서 `MedicalItem.OnUse`(현재 no-op)에서 대상별
처치 완료를 판정할 수 없고, `apply_*`/`wear_glove`/`push_*` 신호를 올릴 지점이 존재하지 않는다.

결과적으로 두 시나리오의 해당 게이트(예: `V016_2 sig.apply_gauze`, `V013_1 sig.apply_stabilizer_patient_a`,
`V016_1 sig.wear_glove`, `V026_2 sig.push_epi`)는 게임플레이로 통과시킬 방법이 없어, 현재는
`/scenario signal` 수동 주입이나 게이트 타임아웃(G-6)에 의존해야 한다.

### 사용자 경험 목표

- 학습자: 거즈/플라스터/고정기/장갑/약물 아이템을 들고 환자(또는 환부)에 **사용(클릭)** 하면
  처치가 적용되고 시나리오가 다음 단계로 진행된다.
- 운영자: 별도 코드 없이, 아이템 식별자/환자 식별자/환부 식별자만 시나리오 조건명과 맞추면
  처치 게이트가 자동 통과된다.
- 감독/평가자: 처치 수행이 게이트 통과로 기록되어 평가 루브릭(G-3, `RubricRecorder`)의 "수행" 판정 근거가 된다.

### 제안

1. **사용 대상 핸들러 인터페이스 신설** (`MultiplayerInfrastructure`):
   `IItemUseTarget { void OnItemUsed(Entity user, string itemIdentifier); }` (필요 시 `OnAttacked` 포함)를
   추가하고, `PatientController`/`MovingPatientBedController` 가 이를 구현(이미 동명 메서드 보유 → 시그니처 정합만).

2. **타깃 해석 + 브리지 구현** (`PlayerController.Item.cs`):
   소유자 카메라 기준 레이캐스트로 사거리(`MinReach`~`MaxReach`) 내 콜라이더에서 `IItemUseTarget` 을 찾고,
   `UseItem()`/`Attack()` 에서 `target.OnItemUsed(PlayerEntity, HandlingItem.CurrentIdentifier)` 를 호출한다.
   - 히트 없음/대상 아님이면 아무 것도 하지 않음(기존 동작) → 하위호환.
   - 기존 `RaycastTargetEntity`(Entity 반환)는 전투용으로 보존하거나, 신규 `RaycastUseTarget()` 로 분리.

3. **처치 완료 신호 배선** (`MedicalItem.OnUse` 또는 대상 핸들러, TriageTrainer):
   대상이 환자/환부일 때 아이템 종류·환부 식별자에 따라 `Raise("apply_gauze")`,
   `Raise("apply_stabilizer_" + targetId)`, `Raise("wear_glove")`, `Raise("push_epi")` 등을 올린다.
   - 부착 시각화는 기존 `TryAttachItem` 경로를 재사용한다(신규 시각 작업 없음).

4. (선택) 약물 주입(`push_*`)은 부착이 아니라 "투여 동작"이므로, 중심정맥관/IV 라인 대상일 때만
   신호를 올리도록 대상 타입 가드를 둔다.

### 자세한 달성 목표

1. `RaycastTargetEntity` 미구현 상태의 기존 동작(타깃 없음)은 명시적 히트 실패 시 그대로 유지(회귀 0).
2. 거즈/플라스터/고정기/장갑/약물 아이템을 환자/환부에 사용하면 대응 `sig.*` 가 1회 발생.
3. 발생한 신호로 해당 Validator 게이트가 통과되고, `RubricRecorder` 가 "수행"으로 기록.
4. CPR 반복 구간의 신호 리셋(`Clear`)과 충돌하지 않음.

### 문서화

- `interaction-signal-integration-spec.md` §2 `apply_*/wear_*/insert_*/push_*` 절을 "[없음]"→"[계측]" 으로 갱신.
- `MedicalItem` 사용 규약(어떤 아이템이 어떤 대상에서 어떤 신호를 올리는지) 표를 운영자 가이드로 추가.
- 채택 시 `Documents/requirements` 색인/링크 검증(`Tools/validate-documentation-links.sh`).

### 가용성과 테스트

- 위험: `RaycastTargetEntity` 는 모든 아이템 사용/공격의 공통 입력 경로다. 잘못 구현하면 무관한
  아이템 상호작용·전투(타 프로젝트)에 영향. → 레이어/사거리/`Entity` 타입 가드로 한정하고,
  히트 실패 시 기존 `null` 동작을 보장하여 회귀를 최소화한다.
- 테스트:
  - 기존 입력 경로(아이템 들기/뷰모델/획득 신호)가 영향 없이 동작.
  - 거즈→환자 사용 시 `sig.apply_gauze` 발생 및 게이트 통과 검증.
  - 레이캐스트 미스 시 신호 미발생·`OnUse(target=null)` 안전 처리.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 성공 지표: 미배선이던 `apply_*`/`wear_glove`/`push_*` 게이트가 게임플레이 수행만으로 통과된다.
- 수용 기준:
  1. 타깃 미해석(히트 실패) 시 런타임 동작이 변경 전과 동일(회귀 0).
  2. 거즈로 환자 환부를 사용하면 `sig.apply_gauze` 가 1회 발생하고 `V016_2` 게이트 통과.
  3. 장갑 착용/약물 주입이 각 신호로 정확히 매핑.

### 링크, 참고사항

- 타깃 해석 스텁: `PlayerController.Item.cs:136-140` (`RaycastTargetEntity` → `return null;`).
- 죽어있는 적용 경로(이미 구현된 부착): `PatientController.cs:78-110`(`TryAttachCurrentHandlingItem`),
  `MovingPatientBedController.cs:331-363`.
- 사용 입력 흐름: `PlayerController.Item.cs:120-134` (`InvokeAttack`/`InvokeUseItem`).
- 신호 헬퍼: `ScenarioInteractionSignals.Raise` (서버 권한 라우팅).
- 미배선 분류 근거: `interaction-signal-integration-spec.md` §2/§5.3, 각 `*.unsupported.flags.json` `notWired_gameplayMissing`.
- 선행/연계: G-6(게이트 타임아웃) 및 G-3(`RubricRecorder`)은 이미 구현됨. 본 제안은 "수동 통과/타임아웃"
  의존을 "실제 수행 통과"로 대체한다.
