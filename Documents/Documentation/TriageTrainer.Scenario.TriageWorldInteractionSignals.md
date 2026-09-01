# <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals"></a> Class TriageWorldInteractionSignals

Namespace: [TriageTrainer.Scenario](TriageTrainer.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

트리아지 월드 오브젝트가 자동으로 발생시키는 시나리오 신호의 규칙.

```csharp
public static class TriageWorldInteractionSignals
```

#### Inheritance

object ← 
[TriageWorldInteractionSignals](TriageTrainer.Scenario.TriageWorldInteractionSignals.md)

## Methods

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseCareZoneBedEntered_System_String_System_String_"></a> RaiseCareZoneBedEntered\(string, string\)

```csharp
public static void RaiseCareZoneBedEntered(string zoneIdentifier, string bedIdentifier)
```

#### Parameters

`zoneIdentifier` string

`bedIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseCareZoneBedExited_System_String_System_String_"></a> RaiseCareZoneBedExited\(string, string\)

```csharp
public static void RaiseCareZoneBedExited(string zoneIdentifier, string bedIdentifier)
```

#### Parameters

`zoneIdentifier` string

`bedIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseCareZoneBedSnapped_System_String_System_String_System_String_"></a> RaiseCareZoneBedSnapped\(string, string, string\)

```csharp
public static void RaiseCareZoneBedSnapped(string zoneIdentifier, string bedIdentifier, string pointIdentifier)
```

#### Parameters

`zoneIdentifier` string

`bedIdentifier` string

`pointIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseCareZoneDisabled_System_String_"></a> RaiseCareZoneDisabled\(string\)

```csharp
public static void RaiseCareZoneDisabled(string zoneIdentifier)
```

#### Parameters

`zoneIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseCareZoneEnabled_System_String_"></a> RaiseCareZoneEnabled\(string\)

```csharp
public static void RaiseCareZoneEnabled(string zoneIdentifier)
```

#### Parameters

`zoneIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseCareZonePatientEntered_System_String_System_String_"></a> RaiseCareZonePatientEntered\(string, string\)

```csharp
public static void RaiseCareZonePatientEntered(string zoneIdentifier, string patientIdentifier)
```

#### Parameters

`zoneIdentifier` string

`patientIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseCareZonePatientEquipmentConnected_System_String_System_String_System_String_UnityEngine_MonoBehaviour_"></a> RaiseCareZonePatientEquipmentConnected\(string, string, string, MonoBehaviour\)

```csharp
public static void RaiseCareZonePatientEquipmentConnected(string zoneIdentifier, string patientIdentifier, string equipmentType, MonoBehaviour equipment)
```

#### Parameters

`zoneIdentifier` string

`patientIdentifier` string

`equipmentType` string

`equipment` MonoBehaviour

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseCareZonePatientEquipmentDisconnected_System_String_System_String_System_String_UnityEngine_MonoBehaviour_"></a> RaiseCareZonePatientEquipmentDisconnected\(string, string, string, MonoBehaviour\)

```csharp
public static void RaiseCareZonePatientEquipmentDisconnected(string zoneIdentifier, string patientIdentifier, string equipmentType, MonoBehaviour equipment)
```

#### Parameters

`zoneIdentifier` string

`patientIdentifier` string

`equipmentType` string

`equipment` MonoBehaviour

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseCareZonePatientExited_System_String_System_String_"></a> RaiseCareZonePatientExited\(string, string\)

```csharp
public static void RaiseCareZonePatientExited(string zoneIdentifier, string patientIdentifier)
```

#### Parameters

`zoneIdentifier` string

`patientIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseDefibrillatorCartSnapPointDisabled_System_String_"></a> RaiseDefibrillatorCartSnapPointDisabled\(string\)

```csharp
public static void RaiseDefibrillatorCartSnapPointDisabled(string pointIdentifier)
```

#### Parameters

`pointIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseDefibrillatorCartSnapPointEnabled_System_String_"></a> RaiseDefibrillatorCartSnapPointEnabled\(string\)

```csharp
public static void RaiseDefibrillatorCartSnapPointEnabled(string pointIdentifier)
```

#### Parameters

`pointIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseDefibrillatorCartSnapPointLatched_System_String_System_String_"></a> RaiseDefibrillatorCartSnapPointLatched\(string, string\)

```csharp
public static void RaiseDefibrillatorCartSnapPointLatched(string cartIdentifier, string pointIdentifier)
```

#### Parameters

`cartIdentifier` string

`pointIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseDefibrillatorCartSnapPointUnlatched_System_String_System_String_"></a> RaiseDefibrillatorCartSnapPointUnlatched\(string, string\)

```csharp
public static void RaiseDefibrillatorCartSnapPointUnlatched(string cartIdentifier, string pointIdentifier)
```

#### Parameters

`cartIdentifier` string

`pointIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseOxyflowmeterDisabled_System_String_"></a> RaiseOxyflowmeterDisabled\(string\)

```csharp
public static void RaiseOxyflowmeterDisabled(string equipmentIdentifier)
```

#### Parameters

`equipmentIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseOxyflowmeterEnabled_System_String_"></a> RaiseOxyflowmeterEnabled\(string\)

```csharp
public static void RaiseOxyflowmeterEnabled(string equipmentIdentifier)
```

#### Parameters

`equipmentIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseOxyflowmeterInstalled_System_String_"></a> RaiseOxyflowmeterInstalled\(string\)

```csharp
public static void RaiseOxyflowmeterInstalled(string equipmentIdentifier)
```

#### Parameters

`equipmentIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseOxyflowmeterRemoved_System_String_"></a> RaiseOxyflowmeterRemoved\(string\)

```csharp
public static void RaiseOxyflowmeterRemoved(string equipmentIdentifier)
```

#### Parameters

`equipmentIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaisePatientBedPositioningPointDisabled_System_String_"></a> RaisePatientBedPositioningPointDisabled\(string\)

```csharp
public static void RaisePatientBedPositioningPointDisabled(string pointIdentifier)
```

#### Parameters

`pointIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaisePatientBedPositioningPointEnabled_System_String_"></a> RaisePatientBedPositioningPointEnabled\(string\)

```csharp
public static void RaisePatientBedPositioningPointEnabled(string pointIdentifier)
```

#### Parameters

`pointIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaisePatientBedPositioningPointLatched_System_String_System_String_"></a> RaisePatientBedPositioningPointLatched\(string, string\)

```csharp
public static void RaisePatientBedPositioningPointLatched(string bedIdentifier, string pointIdentifier)
```

#### Parameters

`bedIdentifier` string

`pointIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaisePatientBedPositioningPointUnlatched_System_String_System_String_"></a> RaisePatientBedPositioningPointUnlatched\(string, string\)

```csharp
public static void RaisePatientBedPositioningPointUnlatched(string bedIdentifier, string pointIdentifier)
```

#### Parameters

`bedIdentifier` string

`pointIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaisePatientEquipmentConnected_System_String_System_String_UnityEngine_MonoBehaviour_"></a> RaisePatientEquipmentConnected\(string, string, MonoBehaviour\)

```csharp
public static void RaisePatientEquipmentConnected(string patientIdentifier, string equipmentType, MonoBehaviour equipment)
```

#### Parameters

`patientIdentifier` string

`equipmentType` string

`equipment` MonoBehaviour

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaisePatientEquipmentDisconnected_System_String_System_String_UnityEngine_MonoBehaviour_"></a> RaisePatientEquipmentDisconnected\(string, string, MonoBehaviour\)

```csharp
public static void RaisePatientEquipmentDisconnected(string patientIdentifier, string equipmentType, MonoBehaviour equipment)
```

#### Parameters

`patientIdentifier` string

`equipmentType` string

`equipment` MonoBehaviour

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseWallSuctionDisabled_System_String_"></a> RaiseWallSuctionDisabled\(string\)

```csharp
public static void RaiseWallSuctionDisabled(string equipmentIdentifier)
```

#### Parameters

`equipmentIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseWallSuctionEnabled_System_String_"></a> RaiseWallSuctionEnabled\(string\)

```csharp
public static void RaiseWallSuctionEnabled(string equipmentIdentifier)
```

#### Parameters

`equipmentIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseWallSuctionInstalled_System_String_"></a> RaiseWallSuctionInstalled\(string\)

```csharp
public static void RaiseWallSuctionInstalled(string equipmentIdentifier)
```

#### Parameters

`equipmentIdentifier` string

### <a id="TriageTrainer_Scenario_TriageWorldInteractionSignals_RaiseWallSuctionRemoved_System_String_"></a> RaiseWallSuctionRemoved\(string\)

```csharp
public static void RaiseWallSuctionRemoved(string equipmentIdentifier)
```

#### Parameters

`equipmentIdentifier` string

