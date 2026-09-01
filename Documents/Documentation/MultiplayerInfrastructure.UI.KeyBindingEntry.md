# <a id="MultiplayerInfrastructure_UI_KeyBindingEntry"></a> Class KeyBindingEntry

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

키 설정 항목 하나를 나타내는 데이터 클래스입니다.
actionId와 actionDisplayName으로 어떤 기능인지 식별하고,
boundKey로 현재 할당된 키코드를 저장합니다.

```csharp
[Serializable]
public class KeyBindingEntry
```

#### Inheritance

object ← 
[KeyBindingEntry](MultiplayerInfrastructure.UI.KeyBindingEntry.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_KeyBindingEntry__ctor"></a> KeyBindingEntry\(\)

```csharp
public KeyBindingEntry()
```

### <a id="MultiplayerInfrastructure_UI_KeyBindingEntry__ctor_System_String_System_String_UnityEngine_KeyCode_"></a> KeyBindingEntry\(string, string, KeyCode\)

```csharp
public KeyBindingEntry(string actionId, string actionDisplayName, KeyCode boundKey = KeyCode.None)
```

#### Parameters

`actionId` string

`actionDisplayName` string

`boundKey` KeyCode

## Fields

### <a id="MultiplayerInfrastructure_UI_KeyBindingEntry_actionDisplayName"></a> actionDisplayName

UI에 표시될 기능 명칭 (예: "앞으로 이동")

```csharp
public string actionDisplayName
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_UI_KeyBindingEntry_actionId"></a> actionId

기능의 고유 식별자 (예: "move_forward")

```csharp
public string actionId
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_UI_KeyBindingEntry_boundKey"></a> boundKey

현재 할당된 키. KeyCode.None이면 미할당 상태입니다.

```csharp
public KeyCode boundKey
```

#### Field Value

 KeyCode

