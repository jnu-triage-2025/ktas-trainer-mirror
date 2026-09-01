# <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules"></a> Class ScenarioGameRules

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

시나리오 실행에 적용되는 서버 게임 규칙.

```csharp
public static class ScenarioGameRules
```

#### Inheritance

object ← 
[ScenarioGameRules](MultiplayerInfrastructure.Scenario.ScenarioGameRules.md)

## Properties

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_AllowMultipleRoleBranchesForSinglePlayer"></a> AllowMultipleRoleBranchesForSinglePlayer

true이면 한 플레이어에게 여러 ByRole 브랜치가 배정된 경우 해당 브랜치들을 순차 실행하도록 허용한다.

```csharp
public static bool AllowMultipleRoleBranchesForSinglePlayer { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_DEBUG_INT_CPR_PLAYING_ESCAPE_KEY"></a> DEBUG\_INT\_CPR\_PLAYING\_ESCAPE\_KEY

true이면 로컬 플레이어가 CPR 수행 애니메이션과 위치 고정 상태를 Left Shift로 임시 해제할 수 있다.
이 값은 디버깅 편의를 위한 표현 규칙일 뿐이며, 시나리오의 CPR 진행·완료 상태는 변경하지 않는다.

```csharp
public static bool DEBUG_INT_CPR_PLAYING_ESCAPE_KEY { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_DisableInteractionInRecognitionCheck"></a> DisableInteractionInRecognitionCheck

의식 확인의 직접 상호작용 경로를 숨긴다. 마이크 경로가 켜진 경우에만 허용된다.

```csharp
public static bool DisableInteractionInRecognitionCheck { get; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_IgnoreTagAssignFullSatisfactionOnScenarioPlay"></a> IgnoreTagAssignFullSatisfactionOnScenarioPlay

true이면 Parallel/PlayerAssignedTag 게이트에서 요구 태그가 충족되지 않아도 흐름을 계속 진행한다.
기본값은 데모/단독 진행을 위해 true이며, false이면 그래프에 정의된 원래 태그 게이트를 엄격히 적용한다.

```csharp
public static bool IgnoreTagAssignFullSatisfactionOnScenarioPlay { get; set; }
```

#### Property Value

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_MissingCareZoneEquipmentFallback"></a> MissingCareZoneEquipmentFallback

```csharp
public static CareZoneMissingEquipmentFallback MissingCareZoneEquipmentFallback { get; }
```

#### Property Value

 [CareZoneMissingEquipmentFallback](MultiplayerInfrastructure.Scenario.CareZoneMissingEquipmentFallback.md)

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_UseMicInRecognitionCheck"></a> UseMicInRecognitionCheck

의식 확인 상호작용에서 로컬 마이크 음량 입력을 허용한다.

```csharp
public static bool UseMicInRecognitionCheck { get; }
```

#### Property Value

 bool

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_AllowsMissingCareZoneEquipmentFallback_MultiplayerInfrastructure_Scenario_CareZoneMissingEquipmentFallback_"></a> AllowsMissingCareZoneEquipmentFallback\(CareZoneMissingEquipmentFallback\)

```csharp
public static bool AllowsMissingCareZoneEquipmentFallback(CareZoneMissingEquipmentFallback equipment)
```

#### Parameters

`equipment` [CareZoneMissingEquipmentFallback](MultiplayerInfrastructure.Scenario.CareZoneMissingEquipmentFallback.md)

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_FormatMissingCareZoneEquipmentFallback"></a> FormatMissingCareZoneEquipmentFallback\(\)

```csharp
public static string FormatMissingCareZoneEquipmentFallback()
```

#### Returns

 string

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_TrySetDisableInteractionInRecognitionCheck_System_Boolean_System_String__"></a> TrySetDisableInteractionInRecognitionCheck\(bool, out string\)

```csharp
public static bool TrySetDisableInteractionInRecognitionCheck(bool value, out string error)
```

#### Parameters

`value` bool

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_TrySetMissingCareZoneEquipmentFallback_System_String_System_String__"></a> TrySetMissingCareZoneEquipmentFallback\(string, out string\)

```csharp
public static bool TrySetMissingCareZoneEquipmentFallback(string value, out string error)
```

#### Parameters

`value` string

`error` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioGameRules_TrySetUseMicInRecognitionCheck_System_Boolean_System_String__"></a> TrySetUseMicInRecognitionCheck\(bool, out string\)

```csharp
public static bool TrySetUseMicInRecognitionCheck(bool value, out string error)
```

#### Parameters

`value` bool

`error` string

#### Returns

 bool

