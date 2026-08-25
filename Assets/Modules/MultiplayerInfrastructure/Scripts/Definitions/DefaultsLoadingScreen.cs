namespace MultiplayerInfrastructure.Definitions
{
  /// <summary>로딩 화면에 표시할 기본 안내 문구를 관리합니다.</summary>
  public static class DefaultsLoadingScreen
  {
    public const string GenericLoadingMessage = "로드 중..";

    public const string IntroSceneLoadingMessage = "로드 중..";
    public const string IngameSceneLoadingMessage = "게임 시작 중..";
    public const string TutorialSceneLoadingMessage = "튜토리얼 정보 로드 중..";
    public const string OverworldSceneLoadingMessage = "세계 리소스 로드 중..";
    public const string SystemOverlaySceneLoadingMessage = "시스템 로드 중..";
    public const string IndevSceneLoadingMessage = "IndevScene 로드 중..";

    /// <summary>
    /// 등록된 씬에는 전용 문구를, 그 외 씬에는 간략한 기본 문구를 반환합니다.
    /// </summary>
    public static string GetSceneLoadingMessage(string sceneName)
    {
      return sceneName switch
      {
        DefaultsSceneControl.IntroSceneName => IntroSceneLoadingMessage,
        DefaultsSceneControl.IngameSceneName => IngameSceneLoadingMessage,
        DefaultsSceneControl.TutorialSceneName => TutorialSceneLoadingMessage,
        "OverworldScene" => OverworldSceneLoadingMessage,
        "SystemOverlayScene" => SystemOverlaySceneLoadingMessage,
        "IndevScene" => IndevSceneLoadingMessage,
        _ => GenericLoadingMessage
      };
    }
  }
}
