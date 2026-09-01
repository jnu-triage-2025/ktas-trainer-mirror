# <a id="MultiplayerInfrastructure_Camera"></a> Namespace MultiplayerInfrastructure.Camera

### Classes

 [CameraAttachPoint](MultiplayerInfrastructure.Camera.CameraAttachPoint.md)

플레이어(또는 관전 대상) 측에 위치하는 카메라 부착 지점입니다.

이전에는 <code>PlayerController</code>가 "CameraHolder"라는 이름의 GameObject를 직접 생성하여
카메라 피벗 겸 뷰모델 부착점으로 사용했습니다. 그러나 "카메라를 실제로 들고 다니는 래퍼"(<xref href="MultiplayerInfrastructure.Camera.CameraHolder" data-throw-if-not-resolved="false"></xref>)와
"카메라가 와서 붙는 대상"의 책임이 하나의 Transform에 뒤섞여 있어, 관전자 위치 표시 등
부착점 고유의 시각 표현을 확장하기 어려웠습니다.

<xref href="MultiplayerInfrastructure.Camera.CameraAttachPoint" data-throw-if-not-resolved="false"></xref>는 그 "부착 대상" 책임만을 담당합니다.
- 상하 시야각(pitch) 피벗 회전을 보관/적용합니다.
- 뷰모델·아이템 등 카메라 기준으로 배치되어야 하는 오브젝트의 부모(부착점)를 제공합니다.
- 카메라 래퍼(<xref href="MultiplayerInfrastructure.Camera.CameraHolder" data-throw-if-not-resolved="false"></xref>)는 이 지점에 <xref href="MultiplayerInfrastructure.Camera.CameraHolder.AttachTo(MultiplayerInfrastructure.Camera.CameraAttachPoint)" data-throw-if-not-resolved="false"></xref>로 부착됩니다.

향후 관전자 위치 마커 등 부착점 고유의 표현은 이 컴포넌트(또는 그 자식)에 추가합니다.

 [CameraHolder](MultiplayerInfrastructure.Camera.CameraHolder.md)

실제 <xref href="UnityEngine.Camera" data-throw-if-not-resolved="false"></xref>를 감싸고 특정 <xref href="MultiplayerInfrastructure.Camera.CameraAttachPoint" data-throw-if-not-resolved="false"></xref>에 부착되어
그 지점을 추종하는 "카메라 래퍼"입니다.

이전 구조에서는 <code>MainCameraController</code>가 플레이어 자식으로 생성된 "CameraHolder" Transform을
직접 <code>_followingCameraHolder</code>로 참조하여, "카메라가 붙는 대상"과 "카메라를 들고 다니는 주체"의
책임이 분리되어 있지 않았습니다.

이 클래스는 "카메라를 들고 다니는 주체" 책임만을 담당합니다.
- <xref href="MultiplayerInfrastructure.Camera.CameraHolder.AttachTo(MultiplayerInfrastructure.Camera.CameraAttachPoint)" data-throw-if-not-resolved="false"></xref> / <xref href="MultiplayerInfrastructure.Camera.CameraHolder.Detach" data-throw-if-not-resolved="false"></xref>로 어느 부착점을 따라갈지 결정합니다.
- 매 프레임 <xref href="MultiplayerInfrastructure.Camera.CameraHolder.Follow" data-throw-if-not-resolved="false"></xref>에서 부착점의 위치/회전을 카메라에 복사하고,
  시점 모드에 따른 거리 오프셋과 벽 충돌 보정을 적용합니다.

<xref href="UnityEngine.MonoBehaviour" data-throw-if-not-resolved="false"></xref>가 아닌 직렬화 가능한 순수 클래스로 두어,
소유자(<code>MainCameraController</code>)의 인스펙터에서 설정값을 그대로 편집할 수 있게 합니다.

 [MainCameraController](MultiplayerInfrastructure.Camera.MainCameraController.md)

메인 카메라 컨트롤러는 메인 카메라 제어를 위해 작성되었습니다.
이 컨트롤러는 게임 시작 시 자동으로 인스턴스화하여 싱글톤 오브젝트로 동작합니다.

카메라를 실제로 들고 다니며 부착점을 추종하는 로직은 <xref href="MultiplayerInfrastructure.Camera.CameraHolder" data-throw-if-not-resolved="false"></xref>가 담당합니다.
이 컨트롤러는 네트워크/싱글톤 수명주기와 부착 대상(플레이어) 바인딩을 담당하고,
카메라 제어 API는 내부 <xref href="MultiplayerInfrastructure.Camera.MainCameraController._holder" data-throw-if-not-resolved="false"></xref>에 위임합니다.

 [MainGameplayCameraObject](MultiplayerInfrastructure.Camera.MainGameplayCameraObject.md)

 [NearbyInteractablesDetector](MultiplayerInfrastructure.Camera.NearbyInteractablesDetector.md)

PlayerInteractiveDetector는 플레이어가 상호작용할 수 있는 물체를 감지해 사용자의 로컬 UI에 표시하도록 지시합니다.

상호작용 가능 물체에 대한 툴팁은 플레이어 위치가 아닌 카메라의 위치를 기준으로 표시되므로,
이 컴포넌트는 카메라에 추가되도록 의도되었습니다.

카메라의 위치를 기준으로 하는 이유는 관전자 모드에서도 상호작용 툴팁을 표시하기 위함입니다.
(카메라 홀더를 기준으로 하면, 관전자 모드 상황에서는 카메라만 다른 카메라 홀더에 붙으므로 적절히 표시되지 않음)
(+ 이와 관련한 개선 구현 방안이 있으나 후순위로 변경: TODO.md 참고)

### Enums

 [CameraViewMode](MultiplayerInfrastructure.Camera.CameraViewMode.md)

