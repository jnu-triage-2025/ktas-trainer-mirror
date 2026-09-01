# <a id="TriageTrainer_Entity_PatientSupportExternalRefs"></a> Struct PatientSupportExternalRefs

Namespace: [TriageTrainer.Entity](TriageTrainer.Entity.md)  
Assembly: Assembly\-CSharp.dll  

환자에 외부 장비가 연결된 상태를 모아 보관하는 참조 묶음.

```csharp
[Serializable]
public struct PatientSupportExternalRefs
```

## Properties

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_IntravenousFluids"></a> IntravenousFluids

```csharp
public IReadOnlyList<MonoBehaviour> IntravenousFluids { get; }
```

#### Property Value

 IReadOnlyList<MonoBehaviour\>

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_Oxyflowmeter"></a> Oxyflowmeter

```csharp
public WallAttachedOxyflowmeter Oxyflowmeter { get; set; }
```

#### Property Value

 [WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_Oxyflowmeters"></a> Oxyflowmeters

```csharp
public IReadOnlyList<WallAttachedOxyflowmeter> Oxyflowmeters { get; }
```

#### Property Value

 IReadOnlyList<[WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)\>

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_PatientBed"></a> PatientBed

```csharp
public MovingPatientBedController PatientBed { get; set; }
```

#### Property Value

 [MovingPatientBedController](TriageTrainer.Entity.MovingPatientBedController.md)

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_SuctionWall"></a> SuctionWall

```csharp
public WallAttachedWallSuction SuctionWall { get; set; }
```

#### Property Value

 [WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_SuctionWalls"></a> SuctionWalls

```csharp
public IReadOnlyList<WallAttachedWallSuction> SuctionWalls { get; }
```

#### Property Value

 IReadOnlyList<[WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)\>

## Methods

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_GetIntravenousFluid_System_Int32_"></a> GetIntravenousFluid\(int\)

```csharp
public MonoBehaviour GetIntravenousFluid(int index)
```

#### Parameters

`index` int

#### Returns

 MonoBehaviour

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_InitializeEmptyCollections"></a> InitializeEmptyCollections\(\)

Inspector Reset 직후 이전 프리팹 기본값과 같은 빈 장비 컬렉션을 만든다.

```csharp
public void InitializeEmptyCollections()
```

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_SetIntravenousFluid_System_Int32_UnityEngine_MonoBehaviour_"></a> SetIntravenousFluid\(int, MonoBehaviour\)

```csharp
public void SetIntravenousFluid(int index, MonoBehaviour fluidSource)
```

#### Parameters

`index` int

`fluidSource` MonoBehaviour

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_SetOxyflowmeters_System_Collections_Generic_IReadOnlyList_TriageTrainer_Entity_WallAttachedOxyflowmeter__"></a> SetOxyflowmeters\(IReadOnlyList<WallAttachedOxyflowmeter\>\)

```csharp
public void SetOxyflowmeters(IReadOnlyList<WallAttachedOxyflowmeter> sources)
```

#### Parameters

`sources` IReadOnlyList<[WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)\>

### <a id="TriageTrainer_Entity_PatientSupportExternalRefs_SetSuctionWalls_System_Collections_Generic_IReadOnlyList_TriageTrainer_Entity_WallAttachedWallSuction__"></a> SetSuctionWalls\(IReadOnlyList<WallAttachedWallSuction\>\)

```csharp
public void SetSuctionWalls(IReadOnlyList<WallAttachedWallSuction> sources)
```

#### Parameters

`sources` IReadOnlyList<[WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)\>

