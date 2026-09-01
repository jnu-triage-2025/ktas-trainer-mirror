# <a id="MultiplayerInfrastructure_Session"></a> Namespace MultiplayerInfrastructure.Session

### Classes

 [ConnectionGateService](MultiplayerInfrastructure.Session.ConnectionGateService.md)

서버의 외부 접속 허용/불가(커넥션 게이트) 상태를 관리하는 정적 서비스.
직렬화하지 않으며, 런타임 중에만 유효하다.

- 게이트가 닫히면 새로운 외부 접속을 거부하고 LAN 브로드캐스트를 중단한다.
- 게이트가 열리면 "로컬 포트 OOO에 서버가 개방되었습니다." 메시지를
  인게임 채팅과 로그로 발송하고, LAN 브로드캐스트를 재개한다.
- 이미 접속한 클라이언트의 연결은 끊지 않는다.

 [LanDiscoveryService](MultiplayerInfrastructure.Session.LanDiscoveryService.md)

 [SessionConfiguration](MultiplayerInfrastructure.Session.SessionConfiguration.md)

 [SessionConfigurationService](MultiplayerInfrastructure.Session.SessionConfigurationService.md)

Loads the development/runtime defaults used when opening a session.

 [SessionInformationModel](MultiplayerInfrastructure.Session.SessionInformationModel.md)

 [UserDescriptor](MultiplayerInfrastructure.Session.UserDescriptor.md)

서버에 접속한 단일 플레이어의 설명자.

- Identifier : 서버가 발급한 UUID. 코드 내 엔티티 쿼리, PlayerTag 레지스트리 키 등에 사용.
               재접속 시 새로 발급됩니다.
- DisplayName: 사람이 읽을 수 있는 표시 이름. /tag, 시나리오, UI 플레이어 목록 등에 사용.
               중복이 가능하므로 명확한 식별이 필요하면 Identifier를 사용하세요.

개발용 씬에서 직접 실행할 경우, 랜덤 UUID가 발급되고 앞 8자리가 DisplayName으로 사용됩니다.
빌드 런타임에서는 IntroScene에서 사용자가 입력한 이름이 DisplayName이 됩니다.

 [UserDescriptorService](MultiplayerInfrastructure.Session.UserDescriptorService.md)

접속 중인 모든 플레이어의 UserDescriptor를 관리하는 정적 서비스.

서버와 모든 클라이언트에서 각자 로컬 사전을 유지합니다.
- 서버  : PlayerController.OnStartServer / OnStopServer 에서 자동 등록·해제됩니다.
- 클라이언트: PlayerController 스폰·디스폰 시 (IsOwner 여부 무관) 자동 등록·해제됩니다.
- SyncVar 변경 시 UpdateDisplayName()이 자동 호출되어 DisplayName이 최신으로 유지됩니다.

쿼리 방법
- 코드 내 엔티티 기준 : TryGetByIdentifier(uuid)
- FishNet 연결 기준   : TryGetByClientId(clientId)
- 플레이어 이름 기준  : TryGetByDisplayName(name)  ← 채팅 명령어 등 사람 입력용
- 전체 열거           : GetAll()

