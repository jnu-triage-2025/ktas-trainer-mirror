# <a id="MultiplayerInfrastructure_Performance_UIScalePreferenceService"></a> Class UIScalePreferenceService

Namespace: [MultiplayerInfrastructure.Performance](MultiplayerInfrastructure.Performance.md)  
Assembly: Assembly\-CSharp.dll  

UI 배율(스케일) 사용자 설정을 관리하는 서비스입니다.

역할:
  - <xref href="MultiplayerInfrastructure.UI.Models.UIScale" data-throw-if-not-resolved="false"></xref> 열거형 단계에 따라 <xref href="UnityEngine.UIElements.PanelSettings.scale" data-throw-if-not-resolved="false"></xref> 배율을 적용합니다.
  - <xref href="UnityEngine.PlayerPrefs" data-throw-if-not-resolved="false"></xref>를 통해 설정을 저장하고 불러옵니다.
  - 설정 변경 시 <xref href="MultiplayerInfrastructure.Performance.UIScalePreferenceService.OnScaleChanged" data-throw-if-not-resolved="false"></xref> 이벤트를 발행합니다.
  - <xref href="MultiplayerInfrastructure.Registry.RegistryType.Service" data-throw-if-not-resolved="false"></xref>에 등록되어 설정 UI 등 외부에서 조회 가능합니다.

게임 내 모든 UIDocument가 하나의 공유 <xref href="UnityEngine.UIElements.PanelSettings" data-throw-if-not-resolved="false"></xref>를 사용하므로,
이 에셋의 scale 계수를 조정하면 전체 UI 배율이 함께 변경됩니다.
scaleMode를 바꾸지 않고 scale 계수만 곱하므로 기존 레이아웃 설계가 유지됩니다.

씬에 하나만 배치하세요(TexturePerformanceService와 동일한 시스템 오브젝트에 두는 것을 권장).
<xref href="MultiplayerInfrastructure.Performance.UIScalePreferenceService._panelSettings" data-throw-if-not-resolved="false"></xref>를 인스펙터에서 지정하면 해당 에셋을 사용하고,
비어 있으면 활성 UIDocument에서 자동으로 찾습니다.

```csharp
public class UIScalePreferenceService : MonoBehaviour
```

#### Inheritance

object ← 
Object ← 
Component ← 
Behaviour ← 
MonoBehaviour ← 
[UIScalePreferenceService](MultiplayerInfrastructure.Performance.UIScalePreferenceService.md)

## Properties

### <a id="MultiplayerInfrastructure_Performance_UIScalePreferenceService_CurrentScale"></a> CurrentScale

현재 적용된 UI 배율 단계입니다.

```csharp
public UIScale CurrentScale { get; }
```

#### Property Value

 [UIScale](MultiplayerInfrastructure.UI.Models.UIScale.md)

## Methods

### <a id="MultiplayerInfrastructure_Performance_UIScalePreferenceService_LoadAndApply"></a> LoadAndApply\(\)

저장된 설정을 불러와 적용합니다.
Awake에서 자동으로 호출됩니다.

```csharp
public void LoadAndApply()
```

### <a id="MultiplayerInfrastructure_Performance_UIScalePreferenceService_SetScale_MultiplayerInfrastructure_UI_Models_UIScale_"></a> SetScale\(UIScale\)

UI 배율을 설정하고 즉시 적용한 뒤 PlayerPrefs에 저장합니다.

```csharp
public void SetScale(UIScale scale)
```

#### Parameters

`scale` [UIScale](MultiplayerInfrastructure.UI.Models.UIScale.md)

적용할 배율 단계

### <a id="MultiplayerInfrastructure_Performance_UIScalePreferenceService_ToScaleFactor_MultiplayerInfrastructure_UI_Models_UIScale_"></a> ToScaleFactor\(UIScale\)

배율 단계에 대응하는 실제 <xref href="UnityEngine.UIElements.PanelSettings.scale" data-throw-if-not-resolved="false"></xref> 계수를 반환합니다.

```csharp
public static float ToScaleFactor(UIScale scale)
```

#### Parameters

`scale` [UIScale](MultiplayerInfrastructure.UI.Models.UIScale.md)

#### Returns

 float

### <a id="MultiplayerInfrastructure_Performance_UIScalePreferenceService_OnScaleChanged"></a> OnScaleChanged

UI 배율이 변경되었을 때 새 배율 단계가 전달됩니다.

```csharp
public event Action<UIScale> OnScaleChanged
```

#### Event Type

 Action<[UIScale](MultiplayerInfrastructure.UI.Models.UIScale.md)\>

