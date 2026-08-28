namespace MultiplayerInfrastructure.Registry
{
  public static class RegistryGlobalKeys
  {
    public const string SessionInformation = "SessionInformation";
    public const string IsOpeningServer = "IsOpeningServer";
    public const string UseLanDiscovery = "UseLanDiscovery";
    public const string LoadedFromIntroScene = "LoadedFromIntroScene";
    public const string DefaultCommonSpawnPoint = "DefaultCommonSpawnPoint";

    /// <summary>
    /// IntroScene에서 플레이어가 입력한 DisplayName.
    /// PlayerController 스폰 시 서버에 전달(CmdSetDisplayName)하는 데 사용됩니다.
    /// 개발용 씬에서 직접 실행 시 이 키는 존재하지 않으며, UUID 앞 8자리가 대신 사용됩니다.
    /// </summary>
    public const string UserDisplayName = "UserDisplayName";
    public const string SelectedDatapackIds = "SelectedDatapackIds";

    /// <summary>
    /// 데디케이티드 서버(헤드리스) 모드로 실행 중인지 여부.
    /// DedicatedServerRuntime이 등록하며, 부트스트랩 흐름에서 클라이언트 전용 처리를 건너뛰는 데 사용됩니다.
    /// </summary>
    public const string IsDedicatedServer = "IsDedicatedServer";
  }
}
