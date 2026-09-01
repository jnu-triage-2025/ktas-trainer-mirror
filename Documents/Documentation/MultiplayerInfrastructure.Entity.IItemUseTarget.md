# <a id="MultiplayerInfrastructure_Entity_IItemUseTarget"></a> Interface IItemUseTarget

Namespace: [MultiplayerInfrastructure.Entity](MultiplayerInfrastructure.Entity.md)  
Assembly: Assembly\-CSharp.dll  

플레이어가 들고 있는 아이템의 "사용(Use)" 대상이 될 수 있는 월드 오브젝트가 구현하는 인터페이스.

<p>
플레이어가 아이템을 사용하면 <xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 가
조준 대상(크로스헤어 레이캐스트 히트)에서 이 인터페이스를 찾아 <xref href="MultiplayerInfrastructure.Entity.IItemUseTarget.OnItemUsed(MultiplayerInfrastructure.Entity.Entity%2cSystem.String)" data-throw-if-not-resolved="false"></xref> 를 호출한다.
구현체는 사용 결과(아이템 부착/적용 등)와, 필요 시 시나리오 인터랙션 완료 신호 Raise 를 자체적으로 처리한다.
</p>

재사용성: 이 인터페이스 자체는 시나리오/트리아지에 의존하지 않는다(범용 "아이템 사용 대상"). 신호 배선
같은 도메인 동작은 구현체(예: TriageTrainer 의 PatientController) 책임이다.

```csharp
public interface IItemUseTarget
```

## Methods

### <a id="MultiplayerInfrastructure_Entity_IItemUseTarget_OnItemUsed_MultiplayerInfrastructure_Entity_Entity_System_String_"></a> OnItemUsed\(Entity, string\)

플레이어가 이 대상에 아이템을 사용했을 때 호출된다.

```csharp
bool OnItemUsed(Entity user, string itemIdentifier)
```

#### Parameters

`user` [Entity](MultiplayerInfrastructure.Entity.Entity.md)

사용자 플레이어의 Entity (null 가능).

`itemIdentifier` string

사용된 아이템의 현재 식별자.

#### Returns

 bool

사용이 의미 있게 처리되었으면 true(대상이 이 아이템을 수용).

