# <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver"></a> Class AudioDeviceSelectionResolver

Namespace: [MultiplayerInfrastructure.Audio](MultiplayerInfrastructure.Audio.md)  
Assembly: Assembly\-CSharp.dll  

저장된 장치 선택값을 현재 장치 목록과 대조하는 순수 함수 모음입니다.

Unity에 기대지 않으므로 에디터 테스트에서 그대로 검증할 수 있습니다.

```csharp
public static class AudioDeviceSelectionResolver
```

#### Inheritance

object ← 
[AudioDeviceSelectionResolver](MultiplayerInfrastructure.Audio.AudioDeviceSelectionResolver.md)

## Fields

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_SystemDefaultId"></a> SystemDefaultId

"시스템 설정을 따름"을 뜻하는 예약 식별자입니다.

```csharp
public const string SystemDefaultId = ""
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_SystemDefaultLabelBase"></a> SystemDefaultLabelBase

시스템 설정 항목에 붙는 이름입니다.

```csharp
public const string SystemDefaultLabelBase = "시스템 설정"
```

#### Field Value

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_UnknownDeviceLabel"></a> UnknownDeviceLabel

장치 이름을 아직 알아내지 못했을 때 괄호 안에 넣는 문구입니다.

```csharp
public const string UnknownDeviceLabel = "확인할 수 없음"
```

#### Field Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_BuildChoiceLabels_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Audio_AudioDeviceDescriptor__"></a> BuildChoiceLabels\(IReadOnlyList<AudioDeviceDescriptor\>\)

드롭다운에 넣을 문구 목록을 만듭니다. 0번은 항상 시스템 설정 항목입니다.
목록의 순서는 <code class="paramref">devices</code>와 1칸씩 어긋나므로 인덱스로 짝지어 쓰세요.

항상 <code>devices.Count + 1</code>개를 돌려줍니다. 중간에서 항목을 건너뛰면 그 뒤 장치가
한 칸씩 밀려 <xref href="MultiplayerInfrastructure.Audio.AudioDeviceSelectionResolver.ChoiceIdAt(System.Int32%2cSystem.Collections.Generic.IReadOnlyList%7bMultiplayerInfrastructure.Audio.AudioDeviceDescriptor%7d)" data-throw-if-not-resolved="false"></xref>가 엉뚱한 장치를 짚게 되므로, 비어 있는 자리도
자리표시자 문구로 채웁니다.

```csharp
public static List<string> BuildChoiceLabels(IReadOnlyList<AudioDeviceDescriptor> devices)
```

#### Parameters

`devices` IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

#### Returns

 List<string\>

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_BuildSystemDefaultLabel_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Audio_AudioDeviceDescriptor__"></a> BuildSystemDefaultLabel\(IReadOnlyList<AudioDeviceDescriptor\>\)

시스템 설정 항목의 표시 문구를 만듭니다.

요구사항대로 지금 운영체제가 쓰고 있는 장치 이름을 괄호에 함께 적습니다.
기본 장치를 알아내지 못한 플랫폼에서는 "시스템 설정(확인할 수 없음)"이 됩니다.

```csharp
public static string BuildSystemDefaultLabel(IReadOnlyList<AudioDeviceDescriptor> devices)
```

#### Parameters

`devices` IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

#### Returns

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_BuildSystemDefaultLabel_System_String_"></a> BuildSystemDefaultLabel\(string\)

시스템 설정 항목의 표시 문구를 만듭니다.

요구사항대로 지금 운영체제가 쓰고 있는 장치 이름을 괄호에 함께 적습니다.
기본 장치를 알아내지 못한 플랫폼에서는 "시스템 설정(확인할 수 없음)"이 됩니다.

```csharp
public static string BuildSystemDefaultLabel(string systemDefaultName)
```

#### Parameters

`systemDefaultName` string

#### Returns

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_ChoiceIdAt_System_Int32_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Audio_AudioDeviceDescriptor__"></a> ChoiceIdAt\(int, IReadOnlyList<AudioDeviceDescriptor\>\)

드롭다운 인덱스를 장치 식별자로 되돌립니다.

```csharp
public static string ChoiceIdAt(int index, IReadOnlyList<AudioDeviceDescriptor> devices)
```

#### Parameters

`index` int

`devices` IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

#### Returns

 string

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_Find_System_String_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Audio_AudioDeviceDescriptor__"></a> Find\(string, IReadOnlyList<AudioDeviceDescriptor\>\)

저장된 식별자에 해당하는 장치를 찾습니다. 없으면 null입니다.

```csharp
public static AudioDeviceDescriptor Find(string deviceId, IReadOnlyList<AudioDeviceDescriptor> devices)
```

#### Parameters

`deviceId` string

`devices` IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

#### Returns

 [AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_FindSystemDefault_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Audio_AudioDeviceDescriptor__"></a> FindSystemDefault\(IReadOnlyList<AudioDeviceDescriptor\>\)

목록에서 운영체제 기본 장치를 찾습니다. 없으면 null입니다.

```csharp
public static AudioDeviceDescriptor FindSystemDefault(IReadOnlyList<AudioDeviceDescriptor> devices)
```

#### Parameters

`devices` IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

#### Returns

 [AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_IndexOfChoice_System_String_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Audio_AudioDeviceDescriptor__"></a> IndexOfChoice\(string, IReadOnlyList<AudioDeviceDescriptor\>\)

선택된 식별자에 해당하는 드롭다운 인덱스를 구합니다. 못 찾으면 0(시스템 설정)입니다.

```csharp
public static int IndexOfChoice(string deviceId, IReadOnlyList<AudioDeviceDescriptor> devices)
```

#### Parameters

`deviceId` string

`devices` IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

#### Returns

 int

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_IsSystemDefault_System_String_"></a> IsSystemDefault\(string\)

```csharp
public static bool IsSystemDefault(string deviceId)
```

#### Parameters

`deviceId` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Audio_AudioDeviceSelectionResolver_Reconcile_System_String_System_Collections_Generic_IReadOnlyList_MultiplayerInfrastructure_Audio_AudioDeviceDescriptor__"></a> Reconcile\(string, IReadOnlyList<AudioDeviceDescriptor\>\)

저장된 식별자가 현재 목록에 남아 있으면 그대로, 사라졌으면 시스템 설정으로 되돌립니다.

```csharp
public static string Reconcile(string storedId, IReadOnlyList<AudioDeviceDescriptor> devices)
```

#### Parameters

`storedId` string

저장되어 있던 장치 식별자

`devices` IReadOnlyList<[AudioDeviceDescriptor](MultiplayerInfrastructure.Audio.AudioDeviceDescriptor.md)\>

지금 인식된 장치 목록

#### Returns

 string

실제로 적용할 식별자. 시스템 설정이면 <xref href="MultiplayerInfrastructure.Audio.AudioDeviceSelectionResolver.SystemDefaultId" data-throw-if-not-resolved="false"></xref>.

