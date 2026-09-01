# <a id="TriageTrainer_Entity_IAttachCompletionSignalConfigurable"></a> Interface IAttachCompletionSignalConfigurable

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

배치 데이터가 설치 완료 신호를 지정할 수 있는 벽면 설치 장비입니다.
이 신호를 프리팹 오버라이드로만 남기면 레이아웃을 다시 생성할 때 함께 지워지므로,
배치 데이터가 재생성 시점마다 이 인터페이스를 통해 값을 다시 주입한다.

```csharp
public interface IAttachCompletionSignalConfigurable
```

## Methods

### <a id="TriageTrainer_Entity_IAttachCompletionSignalConfigurable_SetAttachCompletionSignalForEditor_System_String_"></a> SetAttachCompletionSignalForEditor\(string\)

```csharp
void SetAttachCompletionSignalForEditor(string signal)
```

#### Parameters

`signal` string

