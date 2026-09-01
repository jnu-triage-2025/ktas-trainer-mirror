# <a id="MultiplayerInfrastructure_Datapack_DatapackRuntimeService"></a> Class DatapackRuntimeService

Namespace: [MultiplayerInfrastructure.Datapack](MultiplayerInfrastructure.Datapack.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public class DatapackRuntimeService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[DatapackRuntimeService](MultiplayerInfrastructure.Datapack.DatapackRuntimeService.md)

## Properties

### <a id="MultiplayerInfrastructure_Datapack_DatapackRuntimeService_DatapackRootPath"></a> DatapackRootPath

런타임 데이터팩 폴더의 경로입니다.
내장 데이터팩이 이 폴더로 복사되고, 외부에서 추가한 데이터팩도 이 폴더에서 읽습니다.
기본값은 <xref href="MultiplayerInfrastructure.Logging.GameLogService.DatapackRootPath" data-throw-if-not-resolved="false"></xref>이며,
<xref href="MultiplayerInfrastructure.Datapack.DatapackRuntimeService.TrySetDatapackRootOverride(System.String%2cSystem.String%40)" data-throw-if-not-resolved="false"></xref>로 다른 폴더를 지정할 수 있습니다.

```csharp
public static string DatapackRootPath { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Datapack_DatapackRuntimeService_EnsureRuntimeDatapackFolder"></a> EnsureRuntimeDatapackFolder\(\)

```csharp
public static void EnsureRuntimeDatapackFolder()
```

### <a id="MultiplayerInfrastructure_Datapack_DatapackRuntimeService_GetLoadedDatapackIds"></a> GetLoadedDatapackIds\(\)

```csharp
public IReadOnlyCollection<string> GetLoadedDatapackIds()
```

#### Returns

 IReadOnlyCollection<string\>

### <a id="MultiplayerInfrastructure_Datapack_DatapackRuntimeService_RegisterDatapackFromJson_System_String_System_String_System_String__"></a> RegisterDatapackFromJson\(string, string, out string\)

```csharp
public bool RegisterDatapackFromJson(string json, string sourceName, out string error)
```

#### Parameters

`json` string

`sourceName` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Datapack_DatapackRuntimeService_ScanDatapacks"></a> ScanDatapacks\(\)

```csharp
public static List<DatapackFileInfo> ScanDatapacks()
```

#### Returns

 List<[DatapackFileInfo](MultiplayerInfrastructure.Datapack.DatapackFileInfo.md)\>

### <a id="MultiplayerInfrastructure_Datapack_DatapackRuntimeService_TrySetDatapackRootOverride_System_String_System_String__"></a> TrySetDatapackRootOverride\(string, out string\)

런타임 데이터팩 폴더를 다른 경로로 지정합니다.
데디케이티드 서버처럼 persistentDataPath가 운영자에게 드러나지 않는 실행 환경에서,
실행 파일 옆의 폴더를 사용하기 위한 진입점입니다.
폴더를 만들 수 없으면 기본 경로를 유지하고 <code>false</code>를 반환합니다.

```csharp
public static bool TrySetDatapackRootOverride(string path, out string error)
```

#### Parameters

`path` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Datapack_DatapackRuntimeService_UnregisterDatapack_System_String_"></a> UnregisterDatapack\(string\)

```csharp
public bool UnregisterDatapack(string packId)
```

#### Parameters

`packId` string

#### Returns

 bool

