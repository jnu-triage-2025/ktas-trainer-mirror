# <a id="MultiplayerInfrastructure_UI_ProblemSheetElement"></a> Class ProblemSheetElement

Namespace: [MultiplayerInfrastructure.UI](MultiplayerInfrastructure.UI.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
[UxmlElement]
public class ProblemSheetElement : VisualElement
```

#### Inheritance

object ← 
CallbackEventHandler ← 
Focusable ← 
VisualElement ← 
[ProblemSheetElement](MultiplayerInfrastructure.UI.ProblemSheetElement.md)

## Constructors

### <a id="MultiplayerInfrastructure_UI_ProblemSheetElement__ctor"></a> ProblemSheetElement\(\)

```csharp
public ProblemSheetElement()
```

## Methods

### <a id="MultiplayerInfrastructure_UI_ProblemSheetElement_Bind_MultiplayerInfrastructure_Problem_ProblemDefinition_System_Boolean_System_Boolean_System_Int32_System_Int32_System_Boolean_"></a> Bind\(ProblemDefinition, bool, bool, int, int, bool\)

```csharp
public void Bind(ProblemDefinition problem, bool canProgressToNext, bool isLastProblem, int problemOrder, int totalCount, bool retryOnWrong)
```

#### Parameters

`problem` [ProblemDefinition](MultiplayerInfrastructure.Problem.ProblemDefinition.md)

`canProgressToNext` bool

`isLastProblem` bool

`problemOrder` int

`totalCount` int

`retryOnWrong` bool

### <a id="MultiplayerInfrastructure_UI_ProblemSheetElement_Bind_MultiplayerInfrastructure_Problem_ProblemDefinition_"></a> Bind\(ProblemDefinition\)

```csharp
public void Bind(ProblemDefinition problem)
```

#### Parameters

`problem` [ProblemDefinition](MultiplayerInfrastructure.Problem.ProblemDefinition.md)

### <a id="MultiplayerInfrastructure_UI_ProblemSheetElement_OnCloseRequested"></a> OnCloseRequested

```csharp
public event Action OnCloseRequested
```

#### Event Type

 Action

### <a id="MultiplayerInfrastructure_UI_ProblemSheetElement_OnGraded"></a> OnGraded

```csharp
public event Action<bool, int, string> OnGraded
```

#### Event Type

 Action<bool, int, string\>

### <a id="MultiplayerInfrastructure_UI_ProblemSheetElement_OnNextRequested"></a> OnNextRequested

```csharp
public event Action OnNextRequested
```

#### Event Type

 Action

