# <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_UxmlSerializedData"></a> Class PatientMonitorGraphElement.UxmlSerializedData

Namespace: [TriageTrainer.Entity.PatientMonitor](TriageTrainer.Entity.PatientMonitor.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[Serializable]
public class PatientMonitorGraphElement.UxmlSerializedData : VisualElement.UxmlSerializedData
```

#### Inheritance

object ← 
UxmlSerializedData ← 
VisualElement.UxmlSerializedData ← 
[PatientMonitorGraphElement.UxmlSerializedData](TriageTrainer.Entity.PatientMonitor.PatientMonitorGraphElement.UxmlSerializedData.md)

## Methods

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_UxmlSerializedData_CreateInstance"></a> CreateInstance\(\)

<p>
Returns an instance of the declaring element.
</p>

```csharp
public override object CreateInstance()
```

#### Returns

 object

<p>The new instance of the declaring element.</p>

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_UxmlSerializedData_Deserialize_System_Object_"></a> Deserialize\(object\)

<p>
Applies serialized field values to a compatible visual element.
</p>

```csharp
public override void Deserialize(object obj)
```

#### Parameters

`obj` object

The element to have the serialized data applied to.

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_UxmlSerializedData_Register"></a> Register\(\)

```csharp
[RegisterUxmlCache]
[Conditional("UNITY_EDITOR")]
public static void Register()
```

