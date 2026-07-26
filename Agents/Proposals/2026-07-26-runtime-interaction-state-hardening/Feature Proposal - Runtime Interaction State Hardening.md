### 개요

탑승형 장비, 환자 연결 장비, 지속 UI가 네트워크 및 Unity 오브젝트 수명주기에서도 일관된 상태를 유지하도록 런타임 상호작용 경계를 강화한다.

### 해결하려는 문제 상황

- 소유권이 없는 클라이언트도 거리 검증 없이 탑승 RPC를 호출할 수 있다.
- 교체된 IV 공급원의 늦은 해제 알림이 현재 공급원을 제거할 수 있다.
- 한 환자를 새 모니터에 연결해도 이전 모니터의 정방향 참조가 남는다.
- `UIDocument` 루트 재생성 시 지속 표시 메시지가 새 트리에 복원되지 않는다.
- 탑승 종료 키와 물체 내려놓기 키가 같은 프레임에 함께 실행된다.

### 사용자 경험 목표

사용자는 가까운 장비만 탑승할 수 있고, 장비 교체 뒤에도 최신 연결 상태가 유지되며, 조종 안내와 종료 입력이 예측 가능하게 동작한다.

### 제안

- 서버가 RPC 발신자의 실제 `PlayerController`와 장비 사이 거리를 검증한 뒤 탑승을 승인한다.
- IV 해제 API에 예상 공급원을 전달하여 현재 슬롯과 일치할 때만 해제한다.
- 새 환자 모니터가 연결되기 전에 기존 모니터를 정상 해제한다.
- Title UI 상태를 컨트롤러에 보존하고 새 Visual Tree에 재적용한다.
- 플레이어에 탑승형 조종 상태를 기록하여 조종 종료 입력을 물체 드롭 처리에서 제외한다.

### 자세한 달성 목표

- 이미 탑승한 사용자는 거리와 무관하게 정상적으로 하차할 수 있어야 한다.
- 새 IV 공급원이 이전 공급원의 지연 이벤트로 해제되지 않아야 한다.
- 환자 하나에는 한 개의 활성 환자 모니터 역참조만 존재해야 한다.
- UI 트리 재생성 전후로 제목, 부제목, Actionbar 표시 상태가 유지되어야 한다.
- 조종 중 `LeftShift`는 조종 종료만 수행해야 한다.

### 문서화

동작 계약과 구현 예시는 같은 제안 디렉터리의 `example-implementation.md`에 기록한다. 사용자용 설정 절차나 콘텐츠 데이터 형식은 변경하지 않는다.

### 가용성과 테스트

탑승 시작에 서버 거리 검증이 추가되므로 장비의 attach point 또는 루트가 플레이어로부터 3m 이내여야 한다. 기존 정상 상호작용 감지 거리보다 넉넉한 값이며, 하차에는 적용하지 않는다. IV 교체 회귀 테스트와 Unity EditMode/PlayMode 검증을 수행한다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 원거리 탑승 RPC가 서버에서 거부된다.
- 정상 근거리 탑승 및 탑승자의 하차가 유지된다.
- 오래된 IV 해제 요청 이후에도 교체된 공급원이 유지된다.
- 모니터 교체 시 이전 모니터의 `MonitoringPatient`가 null이 된다.
- UIDocument 루트 교체 후에도 활성 메시지가 표시된다.
- 조종 종료 시 운반 중 물체가 드롭되지 않는다.

### 링크, 참고사항

- `Assets/Modules/MultiplayerInfrastructure/Scripts/Entity/MinecraftBoadLikeControl.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/TitleUIController.cs`
- `Assets/Modules/TriageTrainer/Scripts/Patient/PatientController.EquipmentConnections.cs`
