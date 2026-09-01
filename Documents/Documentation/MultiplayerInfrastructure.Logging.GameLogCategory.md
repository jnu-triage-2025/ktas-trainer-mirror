# <a id="MultiplayerInfrastructure_Logging_GameLogCategory"></a> Enum GameLogCategory

Namespace: [MultiplayerInfrastructure.Logging](MultiplayerInfrastructure.Logging.md)  
Assembly: Assembly\-CSharp.dll  

로그 엔트리의 카테고리.

```csharp
public enum GameLogCategory
```

## Fields

`Chat = 2` 

채팅 메시지



`Command = 3` 

커맨드 실행 (서버 또는 플레이어)



`Interaction = 6` 

인터랙션 발생



`Misc = 7` 

기타



`PlayerJoin = 1` 

플레이어 접속/퇴장



`ScenarioGraph = 4` 

시나리오 그래프 실행 (시작/종료/노드 진행)



`ScenarioSignal = 5` 

시나리오 신호 발생 (interaction signal raise/clear)



`System = 0` 

시스템 수준 이벤트 (세션 시작/종료, 서버/클라이언트 연결 등)



