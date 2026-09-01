# <a id="MultiplayerInfrastructure_Logging_GameLogEntry"></a> Class GameLogEntry

Namespace: [MultiplayerInfrastructure.Logging](MultiplayerInfrastructure.Logging.md)  
Assembly: Assembly\-CSharp.dll  

단일 로그 엔트리.
로컬 타임 기준 타임스탬프, 컨텍스트(서버/클라이언트), 카테고리, 메시지를 포함한다.

```csharp
public sealed class GameLogEntry
```

#### Inheritance

object ← 
[GameLogEntry](MultiplayerInfrastructure.Logging.GameLogEntry.md)

## Constructors

### <a id="MultiplayerInfrastructure_Logging_GameLogEntry__ctor_System_DateTime_System_String_MultiplayerInfrastructure_Logging_GameLogCategory_System_String_System_String_"></a> GameLogEntry\(DateTime, string, GameLogCategory, string, string\)

```csharp
public GameLogEntry(DateTime timestamp, string context, GameLogCategory category, string message, string tag = null)
```

#### Parameters

`timestamp` DateTime

`context` string

`category` [GameLogCategory](MultiplayerInfrastructure.Logging.GameLogCategory.md)

`message` string

`tag` string

## Properties

### <a id="MultiplayerInfrastructure_Logging_GameLogEntry_Category"></a> Category

로그 카테고리.

```csharp
public GameLogCategory Category { get; }
```

#### Property Value

 [GameLogCategory](MultiplayerInfrastructure.Logging.GameLogCategory.md)

### <a id="MultiplayerInfrastructure_Logging_GameLogEntry_Context"></a> Context

이 엔트리가 기록된 컨텍스트.
"server", "client", "client:{uuid}" 등.

```csharp
public string Context { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Logging_GameLogEntry_Message"></a> Message

로그 메시지.

```csharp
public string Message { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Logging_GameLogEntry_Tag"></a> Tag

추가 태그/식별자 (시나리오 ID, 플레이어 UUID 등). 없으면 null.

```csharp
public string Tag { get; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Logging_GameLogEntry_Timestamp"></a> Timestamp

엔트리 생성 시각 (로컬 시간).

```csharp
public DateTime Timestamp { get; }
```

#### Property Value

 DateTime

## Methods

### <a id="MultiplayerInfrastructure_Logging_GameLogEntry_ToLogLine"></a> ToLogLine\(\)

텍스트 파일 한 줄로 직렬화한다.
형식: [yyyy-MM-dd HH:mm:ss.fff] [context] [category] [tag?] message

```csharp
public string ToLogLine()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_Logging_GameLogEntry_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 string

