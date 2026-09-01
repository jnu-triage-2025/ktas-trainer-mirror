# <a id="MultiplayerInfrastructure_Performance"></a> Namespace MultiplayerInfrastructure.Performance

### Namespaces

 [MultiplayerInfrastructure.Performance.Tests](MultiplayerInfrastructure.Performance.Tests.md)

### Classes

 [CameraDistancePreferenceService](MultiplayerInfrastructure.Performance.CameraDistancePreferenceService.md)

3인칭 카메라 POV(거리) 사용자 설정을 관리하는 서비스입니다.

역할:
  - 사용자가 설정한 3인칭 카메라 거리를 <xref href="UnityEngine.PlayerPrefs" data-throw-if-not-resolved="false"></xref>에 저장/로드합니다.
  - 활성 <xref href="MultiplayerInfrastructure.Camera.MainCameraController" data-throw-if-not-resolved="false"></xref>에 값을 적용합니다.
  - 설정 변경 시 <xref href="MultiplayerInfrastructure.Performance.CameraDistancePreferenceService.OnDistanceChanged" data-throw-if-not-resolved="false"></xref> 이벤트를 발행합니다.
  - <xref href="MultiplayerInfrastructure.Registry.RegistryType.Service" data-throw-if-not-resolved="false"></xref>에 등록되어 설정 UI 등 외부에서 조회 가능합니다.

저장값이 없으면(HasStoredValue == false) 카메라의 인스펙터 기본 거리를 사용합니다.
씬에 하나만 배치하세요(TexturePerformanceService와 동일한 시스템 오브젝트에 두는 것을 권장).

 [GraphicsQualityPresets](MultiplayerInfrastructure.Performance.GraphicsQualityPresets.md)

프로젝트의 5단계 권장 그래픽 프리셋을 생성합니다.

 [GraphicsSettingsData](MultiplayerInfrastructure.Performance.GraphicsSettingsData.md)

저장·복제 가능한 그래픽 설정 스냅샷입니다. 디스플레이, 품질, URP 및
일반적인 렌더링 옵션을 한 곳에서 관리합니다.

 [MppmLiteMode](MultiplayerInfrastructure.Performance.MppmLiteMode.md)

MPPM 추가 Editor 인스턴스를 저사양 클라이언트로 전환합니다.
기본 세션은 조작할 수 있도록 화면을 유지하고, HeadlessLite 태그에서만 표현을 숨깁니다.
Main Editor와 일반 Player 빌드에는 절대로 적용하지 않습니다.

 [TexturePerformanceService](MultiplayerInfrastructure.Performance.TexturePerformanceService.md)

종합 그래픽 설정을 저장하고 즉시 적용하는 서비스입니다.
기존 텍스처 품질 API는 하위 호환을 위해 유지합니다.

 [UIScalePreferenceService](MultiplayerInfrastructure.Performance.UIScalePreferenceService.md)

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

### Enums

 [GraphicsAdditionalLights](MultiplayerInfrastructure.Performance.GraphicsAdditionalLights.md)

 [GraphicsAntiAliasing](MultiplayerInfrastructure.Performance.GraphicsAntiAliasing.md)

 [GraphicsQualityProfile](MultiplayerInfrastructure.Performance.GraphicsQualityProfile.md)

사용자에게 노출되는 종합 그래픽 품질 프리셋입니다.

 [GraphicsShadowResolution](MultiplayerInfrastructure.Performance.GraphicsShadowResolution.md)

