# <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement"></a> Class PatientMonitorGraphElement

Namespace: [TriageTrainer.Entity.PatientMonitor](TriageTrainer.Entity.PatientMonitor.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[UxmlElement]
public class PatientMonitorGraphElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[PatientMonitorGraphElement](TriageTrainer.Entity.PatientMonitor.PatientMonitorGraphElement.md)

## Constructors

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement__ctor"></a> PatientMonitorGraphElement\(\)

```csharp
public PatientMonitorGraphElement()
```

## Properties

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_LineColor"></a> LineColor

```csharp
[UxmlAttribute("line-color")]
public Color LineColor { get; set; }
```

#### Property Value

 Color

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_LineWidth"></a> LineWidth

```csharp
[UxmlAttribute("line-width")]
public float LineWidth { get; set; }
```

#### Property Value

 float

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_MaxPoints"></a> MaxPoints

```csharp
[UxmlAttribute("max-points")]
public int MaxPoints { get; set; }
```

#### Property Value

 int

## Methods

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_AddValue_System_Single_"></a> AddValue\(float\)

```csharp
public void AddValue(float value)
```

#### Parameters

`value` float

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_ClearAndFillHistory_System_Single_"></a> ClearAndFillHistory\(float\)

먼저 저장된 샘플을 제거한 뒤, 표시 폭 전체를 지정 값으로 채웁니다. 새 파형은
오른쪽에서 시작해 기준선을 밀어내므로, 그래프가 가로로 확대되는 연출이 발생하지 않습니다.

```csharp
public void ClearAndFillHistory(float value)
```

#### Parameters

`value` float

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_ClearHistory"></a> ClearHistory\(\)

저장된 샘플만 제거합니다. 다음 샘플이 표시 폭 전체로 확대되어 보일 수 있으므로,
연속적인 모니터 스크롤 표현에는 <xref href="TriageTrainer.Entity.PatientMonitor.PatientMonitorGraphElement.ClearAndFillHistory(System.Single)" data-throw-if-not-resolved="false"></xref>를 사용해야 합니다.

```csharp
public void ClearHistory()
```

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_SetChannel_TriageTrainer_Entity_PatientMonitor_MonitorChannelId_System_String_"></a> SetChannel\(MonitorChannelId, string\)

```csharp
public void SetChannel(MonitorChannelId channelId, string channelName)
```

#### Parameters

`channelId` [MonitorChannelId](TriageTrainer.Entity.PatientMonitor.MonitorChannelId.md)

`channelName` string

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_SetColor_UnityEngine_Color_"></a> SetColor\(Color\)

```csharp
public void SetColor(Color color)
```

#### Parameters

`color` Color

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_SetLineWidth_System_Single_"></a> SetLineWidth\(float\)

```csharp
public void SetLineWidth(float width)
```

#### Parameters

`width` float

### <a id="TriageTrainer_Entity_PatientMonitor_PatientMonitorGraphElement_SetRange_System_Single_System_Single_"></a> SetRange\(float, float\)

```csharp
public void SetRange(float min, float max)
```

#### Parameters

`min` float

`max` float

