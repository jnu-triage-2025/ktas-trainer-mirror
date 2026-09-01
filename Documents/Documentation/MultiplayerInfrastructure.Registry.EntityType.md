# <a id="MultiplayerInfrastructure_Registry_EntityType"></a> Enum EntityType

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

Registry.Entity 저장소에 등록되는 런타임 엔티티 종류입니다.
UI, 서비스, 전역 상태값은 포함하지 않습니다.

```csharp
public enum EntityType
```

## Fields

`DefibrillatorCart = 11` 

제세동 카트. 1인 조종 이동체(DefibrillatorCartController)로 동작한다.



`ItemObject = 9` 

`Level1RapidInfuser = 5` 

`MovingPatientBed = 4` 

`Npc = 2` 

`Patient = 3` 

`Player = 1` 

`ScenarioInteractable = 7` 

`ScenarioTriggerZone = 8` 

`StaticPlacedItem = 10` 

에디터에 사전 배치되는 정적 아이템(<code>StaticPlacedItem</code>). 물리 스폰 없이 맵의 일부처럼 취급된다.



`Undefined = 0` 

미지정(기본값). 폴백 등록에서 종류가 명시되지 않았음을 의미한다.



`Waypoint = 6` 

