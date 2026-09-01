# <a id="TriageTrainer_Entity"></a> Namespace TriageTrainer.Entity

### Namespaces

 [TriageTrainer.Entity.AEDLine](TriageTrainer.Entity.AEDLine.md)

 [TriageTrainer.Entity.CentralLine](TriageTrainer.Entity.CentralLine.md)

 [TriageTrainer.Entity.ElectricalLine](TriageTrainer.Entity.ElectricalLine.md)

 [TriageTrainer.Entity.IntravenousLine](TriageTrainer.Entity.IntravenousLine.md)

 [TriageTrainer.Entity.LineConnection](TriageTrainer.Entity.LineConnection.md)

 [TriageTrainer.Entity.OxyLine](TriageTrainer.Entity.OxyLine.md)

 [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)

 [TriageTrainer.Entity.PatientMonitor](TriageTrainer.Entity.PatientMonitor.md)

 [TriageTrainer.Entity.SuctionLine](TriageTrainer.Entity.SuctionLine.md)

### Classes

 [PatientController.AssessActionConfig](TriageTrainer.Entity.PatientController.AssessActionConfig.md)

 [MovingPatientBedController.AttachableItemVisualPair](TriageTrainer.Entity.MovingPatientBedController.AttachableItemVisualPair.md)

 [DefibrillatorCartController](TriageTrainer.Entity.DefibrillatorCartController.md)

제세동 카트의 1인 조종을 전담하는 컨트롤러.
탑승/점유/이동의 네트워크 구현은 도메인 독립 공통 모듈
<xref href="MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl" data-throw-if-not-resolved="false"></xref> 에 위임한다(<xref href="TriageTrainer.Entity.Level1RapidInfuserController" data-throw-if-not-resolved="false"></xref> 와 동일한 상속 패턴).

카트가 허용된 snap point에 도달하면 서버 권위로 스냅하고 시나리오 신호
defibrillator_cart_snap_point_reached_{카트 식별자}_{포인트 식별자} 를 발생시킨다.

 [DefibrillatorCartSnapPoint](TriageTrainer.Entity.DefibrillatorCartSnapPoint.md)

제세동 카트가 도착했을 때 정렬될 월드상의 고정 위치와 방향을 정의한다.

 [HumanoidAnimationController](TriageTrainer.Entity.HumanoidAnimationController.md)

 [PatientController.InteractConfig](TriageTrainer.Entity.PatientController.InteractConfig.md)

 [Level1RapidInfuserBloodBagDisplay](TriageTrainer.Entity.Level1RapidInfuserBloodBagDisplay.md)

 [Level1RapidInfuserController](TriageTrainer.Entity.Level1RapidInfuserController.md)

급속 주입기의 상호작용과 상태를 소유한다. 이동은 도메인 독립 공통 모듈
<xref href="MultiplayerInfrastructure.Entity.MinecraftBoatLikeControl" data-throw-if-not-resolved="false"></xref> 에 위임한다.

 [Level1RapidInfuserNormalSalineDisplay](TriageTrainer.Entity.Level1RapidInfuserNormalSalineDisplay.md)

 [Level1RapidInfuserPlasmaSolutionDisplay](TriageTrainer.Entity.Level1RapidInfuserPlasmaSolutionDisplay.md)

 [MovingPatientBedController](TriageTrainer.Entity.MovingPatientBedController.md)

이동 조종 네트워크 구현은 MinecraftBoatLikeControl로 공통화되었다.
이 partial 파일은 기존 Unity 메타/GUID 호환을 위해 유지한다.

 [MovingPatientBedIntravenousStandDisplay](TriageTrainer.Entity.MovingPatientBedIntravenousStandDisplay.md)

 [MovingPatientBedNormalSalineDisplay](TriageTrainer.Entity.MovingPatientBedNormalSalineDisplay.md)

 [MovingPatientBedPatientAttachPointObject](TriageTrainer.Entity.MovingPatientBedPatientAttachPointObject.md)

 [MovingPatientBedPlasmaSolutionDisplay](TriageTrainer.Entity.MovingPatientBedPlasmaSolutionDisplay.md)

 [MovingPatientBedPositioningPoint](TriageTrainer.Entity.MovingPatientBedPositioningPoint.md)

이동식 환자 침대가 도착했을 때 정렬될 월드상의 고정 위치와 방향을 정의한다.

 [PatientAnimatorRootObject](TriageTrainer.Entity.PatientAnimatorRootObject.md)

 [PatientCareDescriptionZone](TriageTrainer.Entity.PatientCareDescriptionZone.md)

환자 케어에 사용할 벽면 장비의 인식 범위를 정의한다.

<p>구역에 들어온 환자에게 구역 안의 벽면 석션과 산소 유량계를
환자 측 장비 역참조로 연결한다. 장비는 의도상 하나지만, 배치 오류나
확장 시에도 누락되지 않도록 감지 결과는 목록으로 보관한다.</p>

 [PatientController](TriageTrainer.Entity.PatientController.md)

환자 A의 수액 연결 상호작용 부분 구현. 좌측 정맥로에는 생리식염수(N/S)를, 우측 정맥로에는
플라즈마 솔루션을 연결한다.

<p>
예전에는 플레이어가 <code>IntravenousLineConnectionPoint</code> 의 "수액 줄 연결 시작"과
"여기에 수액 줄 연결"을 차례로 사용해서 두 지점을 직접 이었다. 지금은 그 상호작용이 모든
연결 지점에서 잠겨 있으므로, 환자 B/C 의 "생리식염수 연결"과 같은 방식으로 환자 쪽 전용
상호작용 한 번에 연결을 완성한다. 줄 오브젝트는
<xref href="TriageTrainer.Entity.LineConnection.LineConnectionService.TryCreateAutomaticConnection(TriageTrainer.Entity.LineConnection.LineConnectionPoint%2cTriageTrainer.Entity.LineConnection.LineConnectionPoint)" data-throw-if-not-resolved="false"></xref> 이 생성하고, 시나리오
진행 신호는 이 파일에서 직접 올린다.
</p>

 [RecognitionCheckMicrophoneInput](TriageTrainer.Entity.RecognitionCheckMicrophoneInput.md)

마이크 음량이 임계치를 1초 이상 넘으면 활성 의식 확인을 완료한다.

 [StaticEntityLayoutDefinition](TriageTrainer.Entity.StaticEntityLayoutDefinition.md)

 [StretcherController](TriageTrainer.Entity.StretcherController.md)

들것 전용 운반 컨트롤러(서버 권위 네트워크 대응).

<p>
최대 6명의 플레이어가 손잡이를 점유하여 협력 이동한다. 모든 점유 상태는
<xref href="FishNet.Object.Synchronizing.SyncVar%601" data-throw-if-not-resolved="false"></xref> 로 서버 권위 복제되며, 이동은 서버에서 입력을 집계한 뒤
ObserversRpc 로 전 피어에 transform 을 브로드캐스트한다.
</p>

<p>
오프라인(네트워크 비활성) 환경에서는 기존과 동일하게 로컬에서만 동작한다.
</p>

 [WallAttachedOxyflowmeter](TriageTrainer.Entity.WallAttachedOxyflowmeter.md)

벽면에 장착하는 산소 유량계(Oxyflowmeter) 표현입니다.

<p>
처음에는 <b>보이지 않는 상태</b>로 시작합니다. 플레이어가 인벤토리의 산소 유량계
(<xref href="TriageTrainer.ItemDefinitions.Oxyflowmeter" data-throw-if-not-resolved="false"></xref>)를 <b>손에 든 채</b> 이 오브젝트 근처
(부착된 트리거 Collider 범위 안)에 있으면 "설치(장착)" 상호작용이 힌트로 노출됩니다. 상호작용하면
이 오브젝트가 표시(Show)되고 인벤토리의 산소 유량계 1개가 소비됩니다.
</p>

<p>
상호작용으로 표시된 상태를 "설치했다 / 적용했다" 로 이해하며, 이 상태는 이후 데이터로 사용할 수
있도록 <xref href="TriageTrainer.Entity.WallAttachedOxyflowmeter.IsAttached" data-throw-if-not-resolved="false"></xref> 불리언 플래그로 공개합니다.
</p>

<p>
설치 상태의 전파 방식은 베이스의 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.ShareMode" data-throw-if-not-resolved="false"></xref> 로 설정합니다
(기본값 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentShareMode.ServerShared" data-throw-if-not-resolved="false"></xref>). ServerShared 이면 상호작용이
<xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 의 서버 권위 프로토콜로 위임되어, 확정 시 서버가 모든 클라이언트에
표시(설치)를 브로드캐스트하고(신규 접속자 포함) 요청자 클라이언트에서 산소 유량계가 소비됩니다
(진실 원천: <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentService" data-throw-if-not-resolved="false"></xref>). LocalOnly 이면 상호작용한 클라이언트에서만
소비/표시되고 전파되지 않습니다.
</p>

 [WallAttachedWallSuction](TriageTrainer.Entity.WallAttachedWallSuction.md)

벽면에 장착하는 흡인기(WallSuction) 표현입니다.

<p>
처음에는 <b>보이지 않는 상태</b>로 시작합니다. 플레이어가 인벤토리의 흡인기
(<xref href="TriageTrainer.ItemDefinitions.WallSuction" data-throw-if-not-resolved="false"></xref>)을 <b>손에 든 채</b> 이 오브젝트 근처
(부착된 트리거 Collider 범위 안)에 있으면 "설치(장착)" 상호작용이 힌트로 노출됩니다. 상호작용하면
이 오브젝트가 표시(Show)되고 인벤토리의 흡인기 1개가 소비됩니다.
</p>

<p>
상호작용으로 표시된 상태를 "설치했다 / 적용했다" 로 이해하며, 이 상태는 이후 데이터로 사용할 수
있도록 <xref href="TriageTrainer.Entity.WallAttachedWallSuction.IsAttached" data-throw-if-not-resolved="false"></xref> 불리언 플래그로 공개합니다.
</p>

<p>
설치 상태의 전파 방식은 베이스의 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.ShareMode" data-throw-if-not-resolved="false"></xref> 로 설정합니다
(기본값 <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentShareMode.ServerShared" data-throw-if-not-resolved="false"></xref>). ServerShared 이면 상호작용이
<xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 의 서버 권위 프로토콜로 위임되어, 확정 시 서버가 모든 클라이언트에
표시(설치)를 브로드캐스트하고(신규 접속자 포함) 요청자 클라이언트에서 흡인기가 소비됩니다
(진실 원천: <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentService" data-throw-if-not-resolved="false"></xref>). LocalOnly 이면 상호작용한 클라이언트에서만
소비/표시되고 전파되지 않습니다.
</p>

### Structs

 [PatientController.IntravenousLineCannulaConfig](TriageTrainer.Entity.PatientController.IntravenousLineCannulaConfig.md)

 [Level1RapidInfuserState](TriageTrainer.Entity.Level1RapidInfuserState.md)

 [PatientSupportExternalRefs](TriageTrainer.Entity.PatientSupportExternalRefs.md)

환자에 외부 장비가 연결된 상태를 모아 보관하는 참조 묶음.

 [RapidInfuserFluidLifecycleEvent](TriageTrainer.Entity.RapidInfuserFluidLifecycleEvent.md)

급속 주입기 용액 적용 수명주기 이벤트의 상세 정보다.

 [StaticEntityLayoutGroup](TriageTrainer.Entity.StaticEntityLayoutGroup.md)

 [StaticEntityTransformDefinition](TriageTrainer.Entity.StaticEntityTransformDefinition.md)

 [PatientController.TriageAssessmentConfig](TriageTrainer.Entity.PatientController.TriageAssessmentConfig.md)

### Interfaces

 [IAttachCompletionSignalConfigurable](TriageTrainer.Entity.IAttachCompletionSignalConfigurable.md)

배치 데이터가 설치 완료 신호를 지정할 수 있는 벽면 설치 장비입니다.
이 신호를 프리팹 오버라이드로만 남기면 레이아웃을 다시 생성할 때 함께 지워지므로,
배치 데이터가 재생성 시점마다 이 인터페이스를 통해 값을 다시 주입한다.

 [ILevel1RapidInfuserStateSource](TriageTrainer.Entity.ILevel1RapidInfuserStateSource.md)

환자 저장 상태가 급속 주입기 상태를 보유할 경우 구현하는 선택적 복원 규약.
PatientController 자체에 저장 형식을 강제하지 않기 위해 인터페이스로 예비한다.

 [PatientController.IMedicalStateListener](TriageTrainer.Entity.PatientController.IMedicalStateListener.md)

 [PatientController.IMonitorSelectionRequester](TriageTrainer.Entity.PatientController.IMonitorSelectionRequester.md)

### Enums

 [RecognitionCheckMicrophoneInput.Availability](TriageTrainer.Entity.RecognitionCheckMicrophoneInput.Availability.md)

 [BloodBagCancellationBehaviour](TriageTrainer.Entity.BloodBagCancellationBehaviour.md)

 [BloodBagWithoutPlasmaPolicy](TriageTrainer.Entity.BloodBagWithoutPlasmaPolicy.md)

 [PatientController.ChangeAssessableOnAssessDone](TriageTrainer.Entity.PatientController.ChangeAssessableOnAssessDone.md)

트리아지 평가 완료 후 인터랙션 재노출 정책.

 [RapidInfuserFluidCancellationReason](TriageTrainer.Entity.RapidInfuserFluidCancellationReason.md)

 [StaticEntityLayoutType](TriageTrainer.Entity.StaticEntityLayoutType.md)

 [PatientController.TreatmentDisplay](TriageTrainer.Entity.PatientController.TreatmentDisplay.md)

처치 시각 표현 항목. <xref href="TriageTrainer.Patient.PatientTreatmentDisplayModel" data-throw-if-not-resolved="false"></xref> / <xref href="TriageTrainer.Patient.PatientTreatmentDisplayingChildGameObjects" data-throw-if-not-resolved="false"></xref>
의 필드와 1:1 대응한다.

