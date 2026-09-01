# <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorDisplayView"></a> Class PatientMonitorDisplayView

Namespace: [TriageTrainer.Entity.PatientMonitor](TriageTrainer.Entity.PatientMonitor.md)  
Assembly: Assembly\-CSharp.dll  

모니터 콘텐츠를 출력 대상(월드 UIDocument 또는 UI UIDocument)과 분리하는 표시 뷰입니다.

```csharp
public sealed class PatientMonitorDisplayView
```

#### Inheritance

object ← 
[PatientMonitorDisplayView](TriageTrainer.Entity.PatientMonitor.PatientMonitorDisplayView.md)

## Constructors

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorDisplayView__ctor_TriageTrainer_Entity_PatientMonitor_PatientMonitorPlaneType_"></a> PatientMonitorDisplayView\(PatientMonitorPlaneType\)

```csharp
public PatientMonitorDisplayView(PatientMonitorPlaneType type)
```

#### Parameters

`type` [PatientMonitorPlaneType](TriageTrainer.Entity.PatientMonitor.PatientMonitorPlaneType.md)

## Properties

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorDisplayView_ContentRoot"></a> ContentRoot

```csharp
public VisualElement ContentRoot { get; }
```

#### Property Value

 VisualElement

## Methods

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorDisplayView_AddSamples_System_Single_System_Single_System_Single_System_Single_"></a> AddSamples\(float, float, float, float\)

```csharp
public void AddSamples(float ecg, float pleth, float art, float cvp)
```

#### Parameters

`ecg` float

`pleth` float

`art` float

`cvp` float

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorDisplayView_Build_UnityEngine_UIElements_UIDocument_UnityEngine_Color___System_Single_System_Int32_"></a> Build\(UIDocument, Color\[\], float, int\)

```csharp
public void Build(UIDocument document, Color[] colors, float lineThickness, int points)
```

#### Parameters

`document` UIDocument

`colors` Color\[\]

`lineThickness` float

`points` int

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorDisplayView_ResetGraphHistoryToValue_System_Single_"></a> ResetGraphHistoryToValue\(float\)

```csharp
public void ResetGraphHistoryToValue(float value)
```

#### Parameters

`value` float

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorDisplayView_RestoreContentToMonitor"></a> RestoreContentToMonitor\(\)

```csharp
public void RestoreContentToMonitor()
```

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorDisplayView_SetGraphValues_System_String_System_String_System_String_System_String_"></a> SetGraphValues\(string, string, string, string\)

```csharp
public void SetGraphValues(string ecg, string pleth, string art, string cvp)
```

#### Parameters

`ecg` string

`pleth` string

`art` string

`cvp` string

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorDisplayView_SetMetricValues_System_String___"></a> SetMetricValues\(params string\[\]\)

```csharp
public void SetMetricValues(params string[] values)
```

#### Parameters

`values` string\[\]

