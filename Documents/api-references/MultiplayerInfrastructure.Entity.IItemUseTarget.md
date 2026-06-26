# MultiplayerInfrastructure.Entity.IItemUseTarget

플레이어가 들고 있는 아이템의 "사용(Use)" 대상이 될 수 있는 월드 오브젝트가 구현하는 인터페이스입니다.
아이템 사용 입력을 조준 대상의 도메인 동작(부착/적용, 신호 배선 등)으로 연결하는 브리지의 핵심입니다.

## 정의

```csharp
namespace MultiplayerInfrastructure.Entity
{
  public interface IItemUseTarget
  {
    // user: 사용자 플레이어 Entity(null 가능), itemIdentifier: 사용된 아이템 식별자
    // 반환: 대상이 이 아이템을 의미 있게 수용했으면 true
    bool OnItemUsed(Entity user, string itemIdentifier);
  }
}
```

## 호출 경로 (브리지)

1. 플레이어가 아이템 사용을 트리거하면 `PlayerController.UseItem()` 이 호출된다.
2. `TryUseHandlingItemOnTarget()` 가 현재 크로스헤어 레이캐스트 히트(`RaycastHitObject`)에서
   `GetComponentInParent<IItemUseTarget>()` 로 대상을 찾는다(`PlayerController.Item.cs`).
3. 찾으면 `target.OnItemUsed(PlayerEntity, HandlingItem.CurrentIdentifier)` 를 호출한다.
4. 히트가 없거나 대상이 `IItemUseTarget` 이 아니면 아무 것도 하지 않는다(기존 동작, 하위호환).

> 레이캐스트는 매 프레임 `PlayerController.Update_Raycast()`(소유자 한정)가 갱신하므로, 사용 시점에
> 최신 조준 대상이 사용된다. 사용 입력은 소유자 클라이언트에서 발생하며, 신호 배선은 구현체가
> `ScenarioInteractionSignals.Raise`(서버 권한 라우팅)로 처리한다.

## 구현체 (TriageTrainer)

- `TriageTrainer.Entity.PatientController`
- `TriageTrainer.Entity.MovingPatientBedController`

두 컨트롤러는 기존 `OnItemUsed(Entity, string)` 메서드를 가지고 있었으나 **호출되지 않는 고아 메서드**였다.
본 변경으로 (1) 반환 타입을 `bool` 로 정합하고 `IItemUseTarget` 을 구현, (2) `PlayerController` 의
사용 입력과 연결되었다.

### 적용 신호(Apply Signal) 배선

두 컨트롤러의 `AttachableItemVisualPair` 에 `ApplySignal`(옵션) 필드가 추가되었다. `TryAttachItem` 성공 시
해당 아이템에 매핑된 신호를 `ScenarioInteractionSignals.Raise(applySignal)` 로 올린다. 운영자 설정 방법은
[item-apply-signal-setup-guide.md](../requirements/content-definitions/scenario/item-apply-signal-setup-guide.md) 참고.

## 재사용성 주의

`IItemUseTarget` 자체는 시나리오/트리아지에 의존하지 않는 범용 "아이템 사용 대상" 추상이다. 신호 Raise
같은 도메인 동작은 구현체(TriageTrainer) 책임으로 분리되어, `MultiplayerInfrastructure` 의 타 프로젝트
재사용성을 해치지 않는다.

## 관련 문서

- [item-apply-signal-setup-guide.md](../requirements/content-definitions/scenario/item-apply-signal-setup-guide.md)
- [인터랙션 완료 신호 연결 명세](../requirements/content-definitions/scenario/interaction-signal-integration-spec.md)
- 제안서: `Agents/Proposals/done/2026-06-25-item-use-target-signal/`
