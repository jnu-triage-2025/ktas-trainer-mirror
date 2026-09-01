# <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags"></a> Class PatientACriticalQuestStateFlags

Namespace: [TriageTrainer.Scenario](TriageTrainer.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

`patient_a_critical` 시나리오가 사용하는 퀘스트 상태 플래그와, 그 플래그가 여닫는 상호작용 표를 담는다.

<p>
이 시나리오는 상호작용 노출을 엔티티에 저장된 활성 플래그가 아니라
<xref href="MultiplayerInfrastructure.Quest.PlayerQuestStateFlagService" data-throw-if-not-resolved="false"></xref>의 플레이어별 플래그 풀로 판정한다. 엔티티 활성 플래그는
환자 인스턴스 하나에 공유되어 있어서, 어떤 플레이어의 퀘스트 단계가 다른 플레이어의 상호작용
목록까지 바꿔 버린다. 퀘스트가 플레이어별로 발행되므로 노출 판정도 플레이어별이어야 한다.
</p>

<p>
게이트는 이 시나리오가 실행 중일 때만(<xref href="TriageTrainer.Scenario.PatientACriticalQuestStateFlags.IsArmed" data-throw-if-not-resolved="false"></xref>) 동작한다. 표에 없는 상호작용과
다른 시나리오는 기존 판정 경로를 그대로 쓴다.
</p>

```csharp
public static class PatientACriticalQuestStateFlags
```

#### Inheritance

object ← 
[PatientACriticalQuestStateFlags](TriageTrainer.Scenario.PatientACriticalQuestStateFlags.md)

## Fields

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_ArrestPulseAssess"></a> ArrestPulseAssess

심정지 직후 맥박 확인. ROSC 시점에 내려간다.

```csharp
public const string ArrestPulseAssess = "scen_a.assess_pulse_r1"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_AvpuGcsAssess"></a> AvpuGcsAssess

남성 환자 의식상태 사정(`nurse_c`).

```csharp
public const string AvpuGcsAssess = "scen_a.assess_avpu_gcs"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_ClothingRemoval"></a> ClothingRemoval

의복 제거.

```csharp
public const string ClothingRemoval = "scen_a.clothing_removal"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_Cpr1Actions"></a> Cpr1Actions

CPR 1주기 처치 동작(가슴압박·앰부·제세동 패드·T-piece 해제).

```csharp
public const string Cpr1Actions = "scen_a.cpr1_actions"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_Cpr2Actions"></a> Cpr2Actions

CPR 2주기 처치 동작.

```csharp
public const string Cpr2Actions = "scen_a.cpr2_actions"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_RoscGcsAssess"></a> RoscGcsAssess

ROSC 이후 의식상태 재사정.

```csharp
public const string RoscGcsAssess = "scen_a.rosc_gcs_assess"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_RoscPulseAssess"></a> RoscPulseAssess

ROSC 이후 맥박 재사정.

```csharp
public const string RoscPulseAssess = "scen_a.rosc_pulse_assess"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_ScenarioIdentifier"></a> ScenarioIdentifier

```csharp
public const string ScenarioIdentifier = "patient_a_critical"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_StyletRemoval"></a> StyletRemoval

삽관 보조의 스타일렛 제거.

```csharp
public const string StyletRemoval = "scen_a.stylet_removal"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_TpieceAttach"></a> TpieceAttach

T-piece 산소 연결.

```csharp
public const string TpieceAttach = "scen_a.tpiece_attach"
```

#### Field Value

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_VitalAssess"></a> VitalAssess

남성 환자 활력징후 사정(`nurse_b`).

```csharp
public const string VitalAssess = "scen_a.assess_vital"
```

#### Field Value

 string

## Properties

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_GatedInteractionAddresses"></a> GatedInteractionAddresses

테스트와 진단용. 게이트 대상 상호작용 주소를 모두 반환한다.

```csharp
public static IReadOnlyCollection<string> GatedInteractionAddresses { get; }
```

#### Property Value

 IReadOnlyCollection<string\>

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_IsArmed"></a> IsArmed

이 시나리오가 실행 중이라 플래그 판정이 켜져 있는지 여부.

```csharp
public static bool IsArmed { get; }
```

#### Property Value

 bool

## Methods

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_ArmFor_System_String_"></a> ArmFor\(string\)

시나리오가 시작될 때 게이트를 켠다. 다른 시나리오 식별자는 무시한다.

```csharp
public static void ArmFor(string scenarioIdentifier)
```

#### Parameters

`scenarioIdentifier` string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_Disarm"></a> Disarm\(\)

시나리오가 끝날 때 게이트를 끄고 이 시나리오가 올린 플래그를 모두 내린다.

```csharp
public static void Disarm()
```

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_FindFlag_System_String_System_String_"></a> FindFlag\(string, string\)

테스트와 진단용. 지정한 상호작용을 여는 플래그를 반환한다(없으면 null).

```csharp
public static string FindFlag(string entityIdentifier, string interactionIdentifier)
```

#### Parameters

`entityIdentifier` string

`interactionIdentifier` string

#### Returns

 string

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_GetInteractionDisplayPriority_System_String_System_String_"></a> GetInteractionDisplayPriority\(string, string\)

환자 A의 단계별 상호작용 가운데 목록 상단에 고정해야 하는 항목의 표시 우선순위입니다.
게이트가 켜진 동안에만 적용되므로 다른 시나리오의 같은 식별자에는 영향을 주지 않습니다.

```csharp
public static int GetInteractionDisplayPriority(string entityIdentifier, string interactionIdentifier)
```

#### Parameters

`entityIdentifier` string

`interactionIdentifier` string

#### Returns

 int

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_RequiresActiveQuestBinding_System_String_System_String_"></a> RequiresActiveQuestBinding\(string, string\)

단계 플래그뿐 아니라 현재 퀘스트의 표시 바인딩까지 활성화되어야 하는 상호작용인지 반환한다.
제세동 패드는 CPR 1주기 공용 플래그가 열린 동안에도 담당자의 제세동기 퀘스트에서만 보여야 한다.

```csharp
public static bool RequiresActiveQuestBinding(string entityIdentifier, string interactionIdentifier)
```

#### Parameters

`entityIdentifier` string

`interactionIdentifier` string

#### Returns

 bool

### <a id="TriageTrainer_Scenario_PatientACriticalQuestStateFlags_TryEvaluate_System_String_System_String_MultiplayerInfrastructure_Player_PlayerController_System_Boolean__"></a> TryEvaluate\(string, string, PlayerController, out bool\)

상호작용이 이 시나리오의 플래그 게이트 대상인지 판정하고, 대상이면 노출 여부를 돌려준다.

```csharp
public static bool TryEvaluate(string entityIdentifier, string interactionIdentifier, PlayerController player, out bool allowed)
```

#### Parameters

`entityIdentifier` string

`interactionIdentifier` string

`player` [PlayerController](MultiplayerInfrastructure.Player.PlayerController.md)

`allowed` bool

#### Returns

 bool

플래그가 노출을 결정하는 상호작용이면 true. false면 호출자가 기존 판정을 그대로 쓴다.

