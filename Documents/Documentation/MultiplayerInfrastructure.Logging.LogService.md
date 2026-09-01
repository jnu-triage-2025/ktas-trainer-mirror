# <a id="MultiplayerInfrastructure_Logging_LogService"></a> Class LogService

Namespace: [MultiplayerInfrastructure.Logging](MultiplayerInfrastructure.Logging.md)  
Assembly: Assembly\-CSharp.dll  

로그 서비스의 생명주기를 관리하는 MonoBehaviour.

씬에 배치하면 FishNet 서버/클라이언트 연결 상태에 따라
<xref href="MultiplayerInfrastructure.Logging.GameLogService" data-throw-if-not-resolved="false"></xref>를 자동으로 초기화·종료한다.

## 동작
- 서버 시작(Started) → context="server"로 GameLogService 초기화
- 클라이언트 시작(Started) → context="client:{uuid}"로 GameLogService 초기화
  (단, 서버와 같은 프로세스인 호스트는 서버 컨텍스트를 우선한다)
- 서버/클라이언트 중단(Stopped) → GameLogService 종료
- OnApplicationQuit → 안전하게 Shutdown 호출

## 씬 배치
씬의 적절한 오브젝트에 컴포넌트로 추가하거나, NetworkManager 오브젝트에 함께 붙인다.

```csharp
public class LogService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[LogService](MultiplayerInfrastructure.Logging.LogService.md)

