namespace MultiplayerInfrastructure.Definitions
{
  public static class DefaultsUIDocument
  {
    public const float CrosshairUISortOrder = 1f;
    public const float QuestPanelUISortOrder = 4f;
    public const float QuestPreviewHudSortOrder = 2f;
    public const float HeldItemHudSortOrder = 2.5f;
    // 시간 표시(스톱워치/카운트다운) HUD. 상단 중앙에 표시되는 비차단 HUD.
    public const float TimeDisplayHudSortOrder = 2.6f;
    public const float TitleUISortOrder = 5f;
    public const float InventoryUISortOrder = 6f;
    public const float EscapeMenuUISortOrder = 7f;
    public const float GraphicsSettingsUISortOrder = 8f;
    // 통합 설정 UI. Escape 메뉴(7) 위에 표시되며, 기존 KeyConfig(8)/Graphics(8)를 대체한다.
    public const float SettingsUISortOrder = 8f;
    public const float ProblemSheetUISortOrder = 9f;
    public const float TriageAssessmentUISortOrder = 9.5f;
    // Chat is opened through UIOverlayStack and must receive pointer/wheel input
    // above every passive HUD and modal document. Its document root is made
    // non-interactable while closed by ChatUIController.
    public const float ChatPanelUISortOrder = 10f;
    public const float EntityOverheadLabelSortOrder = 2.2f;
  }
}
