# <a id="MultiplayerInfrastructure_UI_DialogueElement_UxmlSerializedData"></a> Class DialogueElement.UxmlSerializedData

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public class DialogueElement.UxmlSerializedData : VisualElement.UxmlSerializedData
```

#### Inheritance

object ← 
UxmlSerializedData ← 
VisualElement.UxmlSerializedData ← 
[DialogueElement.UxmlSerializedData](MultiplayerInfrastructure.UI.DialogueElement.UxmlSerializedData.md)

## Methods

### <a id="MultiplayerInfrastructure_UI_DialogueElement_UxmlSerializedData_CreateInstance"></a> CreateInstance\(\)

<p>
Returns an instance of the declaring element.
</p>

```csharp
public override object CreateInstance()
```

#### Returns

 object

<p>The new instance of the declaring element.</p>

### <a id="MultiplayerInfrastructure_UI_DialogueElement_UxmlSerializedData_Deserialize_System_Object_"></a> Deserialize\(object\)

<p>
Applies serialized field values to a compatible visual element.
</p>

```csharp
public override void Deserialize(object obj)
```

#### Parameters

`obj` object

The element to have the serialized data applied to.

### <a id="MultiplayerInfrastructure_UI_DialogueElement_UxmlSerializedData_Register"></a> Register\(\)

```csharp
[RegisterUxmlCache]
[Conditional("UNITY_EDITOR")]
public static void Register()
```

