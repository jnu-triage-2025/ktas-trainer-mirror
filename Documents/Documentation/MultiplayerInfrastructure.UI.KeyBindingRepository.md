# <a id="MultiplayerInfrastructure_UI_KeyBindingRepository"></a> Class KeyBindingRepository

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

키 바인딩 설정을 PlayerPrefs를 통해 로컬에 저장하고 불러오는 저장소입니다.

저장 키 형식: "KeyBinding_{actionId}" → int(KeyCode)

```csharp
public static class KeyBindingRepository
```

#### Inheritance

object ← 
[KeyBindingRepository](MultiplayerInfrastructure.UI.KeyBindingRepository.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_KeyBindingRepository_DeleteAll_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_UI_KeyBindingEntry__"></a> DeleteAll\(IReadOnlyList<KeyBindingEntry\>\)

저장된 모든 키 바인딩을 PlayerPrefs에서 삭제합니다.

```csharp
public static void DeleteAll(IReadOnlyList<KeyBindingEntry> bindings)
```

#### Parameters

`bindings` IReadOnlyList<[KeyBindingEntry](MultiplayerInfrastructure.UI.KeyBindingEntry.md)\>

### <a id="MultiplayerInfrastructure_UI_KeyBindingRepository_Flush"></a> Flush\(\)

PlayerPrefs.Save()를 명시적으로 호출합니다.

```csharp
public static void Flush()
```

### <a id="MultiplayerInfrastructure_UI_KeyBindingRepository_GetBoundKey_System_String_UnityEngine_KeyCode_"></a> GetBoundKey\(string, KeyCode\)

설정 화면과 실제 게임 입력이 같은 PlayerPrefs 바인딩을 사용하도록 현재 키를 조회한다.
아직 저장된 값이 없으면 <code class="paramref">defaultKey</code>를 사용한다.

```csharp
public static KeyCode GetBoundKey(string actionId, KeyCode defaultKey)
```

#### Parameters

`actionId` string

`defaultKey` KeyCode

#### Returns

 KeyCode

### <a id="MultiplayerInfrastructure_UI_KeyBindingRepository_LoadInto_System_Collections_Generic_IList_MultiplayerInfrastructure_UI_KeyBindingEntry__"></a> LoadInto\(IList<KeyBindingEntry\>\)

저장된 키 바인딩으로 <code class="paramref">bindings</code> 리스트의 <xref href="MultiplayerInfrastructure.UI.KeyBindingEntry.boundKey" data-throw-if-not-resolved="false"></xref>를
덮어씁니다. 저장된 값이 없는 항목은 기본값을 유지합니다.

```csharp
public static bool LoadInto(IList<KeyBindingEntry> bindings)
```

#### Parameters

`bindings` IList<[KeyBindingEntry](MultiplayerInfrastructure.UI.KeyBindingEntry.md)\>

#### Returns

 bool

하나라도 덮어쓴 항목이 있으면 true.

### <a id="MultiplayerInfrastructure_UI_KeyBindingRepository_SaveAll_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_UI_KeyBindingEntry__"></a> SaveAll\(IReadOnlyList<KeyBindingEntry\>\)

모든 바인딩을 PlayerPrefs에 저장합니다.

```csharp
public static void SaveAll(IReadOnlyList<KeyBindingEntry> bindings)
```

#### Parameters

`bindings` IReadOnlyList<[KeyBindingEntry](MultiplayerInfrastructure.UI.KeyBindingEntry.md)\>

### <a id="MultiplayerInfrastructure_UI_KeyBindingRepository_SaveEntry_MultiplayerInfrastructure_UI_KeyBindingEntry_"></a> SaveEntry\(KeyBindingEntry\)

단일 바인딩을 PlayerPrefs에 저장합니다. PlayerPrefs.Save()를 즉시 플러시하지 않으므로,
배치 저장 후에는 <xref href="MultiplayerInfrastructure.UI.KeyBindingRepository.Flush" data-throw-if-not-resolved="false"></xref>를 호출하거나 <xref href="MultiplayerInfrastructure.UI.KeyBindingRepository.SaveAll(System.Collections.Generic.IReadOnlyList%7bMultiplayerInfrastructure.UI.KeyBindingEntry%7d)" data-throw-if-not-resolved="false"></xref>을 사용하세요.

```csharp
public static void SaveEntry(KeyBindingEntry entry)
```

#### Parameters

`entry` [KeyBindingEntry](MultiplayerInfrastructure.UI.KeyBindingEntry.md)

