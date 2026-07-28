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
  }
}
