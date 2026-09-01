# <a id="MultiplayerInfrastructure_Registry_RegistryType"></a> Enum RegistryType

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public enum RegistryType
```

## Fields

`Entity = 7` 

월드에 존재하는 엔티티 저장소. 값은 EntityDescriptor 이며, Get&lt;GameObject&gt; / Get&lt;Component&gt; 해석을 지원합니다.



`EntityPreset = 13` 

`IconSprite = 3` 

`InteractableEntity = 10` 

`Item = 0` 

ItemSystem.Item 파생 클래스의 System.Type을 등록합니다. 나중에 인스턴스화할 수 있도록 클래스 자체를 값으로 가집니다.



`Npc = 4` 

`PlayerModel = 12` 

`PlayerQuestStateFlag = 17` 

플레이어별 퀘스트 상태 플래그 풀. 키는 UserDescriptor.Identifier(UUID),
값은 HashSet&lt;string&gt;입니다. 역할 태그와 달리 퀘스트 진행 중에만 유지되는 임시 상태를 담습니다.



`PlayerTag = 16` 

플레이어 태그 레지스트리. 키는 UserDescriptor.Identifier(UUID), 값은 List&lt;string&gt;입니다.



`ProblemFigure = 15` 

`ProblemSet = 14` 

`RuntimeState = 9` 

씬 전환 의도, 접속 정보 등 전역 상태값을 등록합니다.



`ScenarioEvent = 1` 

`ScenarioGraph = 2` 

`Service = 8` 

씬/런타임 서비스 저장소. QuestManager, MainCameraController 등의 싱글턴성 컨트롤러를 등록합니다.



`SpawnPoint = 6` 

`UI = 11` 

`Waypoint = 5` 

