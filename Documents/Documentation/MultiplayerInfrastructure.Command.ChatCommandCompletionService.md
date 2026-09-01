# <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService"></a> Class ChatCommandCompletionService

Namespace: [MultiplayerInfrastructure.Command](MultiplayerInfrastructure.Command.md)  
Assembly: Assembly\-CSharp.dll  

채팅 입력창의 Tab 키 자동완성을 담당하는 서비스.

<p>
핵심 설계: Tab Cycling 시 <xref href="MultiplayerInfrastructure.Command.ChatCommandCompletionService._completionOriginalText" data-throw-if-not-resolved="false"></xref>를 보존하여
자동완성된 텍스트가 다음 검색 쿼리로 사용되는 것을 방지합니다.
</p>

```csharp
public class ChatCommandCompletionService
```

#### Inheritance

object ← 
[ChatCommandCompletionService](MultiplayerInfrastructure.Command.ChatCommandCompletionService.md)

## Constructors

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService__ctor_MultiplayerInfrastructure_Command_ChatCommandService_"></a> ChatCommandCompletionService\(ChatCommandService\)

```csharp
public ChatCommandCompletionService(ChatCommandService commandService)
```

#### Parameters

`commandService` [ChatCommandService](MultiplayerInfrastructure.Command.ChatCommandService.md)

## Methods

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_CollectEntityPresetIdentifiers"></a> CollectEntityPresetIdentifiers\(\)

등록된 모든 엔티티 프리셋 식별자를 반환합니다.

```csharp
public static List<string> CollectEntityPresetIdentifiers()
```

#### Returns

 List<string\>

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_CollectItemIdentifiers"></a> CollectItemIdentifiers\(\)

등록된 모든 아이템 식별자를 반환합니다.

```csharp
public static List<string> CollectItemIdentifiers()
```

#### Returns

 List<string\>

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_CollectManualEntrypointIdentifiers"></a> CollectManualEntrypointIdentifiers\(\)

등록된 모든 시나리오 식별자를 반환합니다.

```csharp
public static List<string> CollectManualEntrypointIdentifiers()
```

#### Returns

 List<string\>

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_CollectPlayerModelIdentifiers"></a> CollectPlayerModelIdentifiers\(\)

등록된 모든 플레이어 모델 식별자를 반환합니다.

```csharp
public static List<string> CollectPlayerModelIdentifiers()
```

#### Returns

 List<string\>

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_CollectPlayersAndSelectors"></a> CollectPlayersAndSelectors\(\)

현재 접속 중인 플레이어 이름과 타겟 셀렉터를 함께 반환합니다.

```csharp
public static List<string> CollectPlayersAndSelectors()
```

#### Returns

 List<string\>

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_CollectProblemSetIdentifiers"></a> CollectProblemSetIdentifiers\(\)

등록된 모든 문제 세트 식별자를 반환합니다.

```csharp
public static List<string> CollectProblemSetIdentifiers()
```

#### Returns

 List<string\>

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_CollectScenarioIdentifiers"></a> CollectScenarioIdentifiers\(\)

```csharp
public static List<string> CollectScenarioIdentifiers()
```

#### Returns

 List<string\>

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_CollectWaypointIdentifiers"></a> CollectWaypointIdentifiers\(\)

등록된 모든 웨이포인트 식별자를 반환합니다.

```csharp
public static List<string> CollectWaypointIdentifiers()
```

#### Returns

 List<string\>

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_HandleTabPress_System_String_System_Int32_"></a> HandleTabPress\(string, int\)

Tab 키 입력 시 호출. 자동완성 결과를 반환합니다.

```csharp
public (string text, int cursorPos)? HandleTabPress(string currentText, int cursorPos)
```

#### Parameters

`currentText` string

현재 입력창의 전체 텍스트

`cursorPos` int

현재 커서 위치

#### Returns

 \(string text, int cursorPos\)?

(완성된 텍스트, 새 커서 위치) 또는 null (후보 없음)

### <a id="MultiplayerInfrastructure_Command_ChatCommandCompletionService_ResetSession"></a> ResetSession\(\)

Tab 외의 키가 입력되었을 때 호출하여 자동완성 세션을 리셋합니다.

```csharp
public void ResetSession()
```

