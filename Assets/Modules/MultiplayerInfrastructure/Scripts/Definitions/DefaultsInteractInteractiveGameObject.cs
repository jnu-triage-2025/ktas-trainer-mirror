namespace MultiplayerInfrastructure.Definitions
{
  public class DefaultsInteractInteractiveGameObject
  {
    public const float ScrollSpeedUnderBoundOnHintUI = 0.01f;

    /**
     * Visual Element 식별자와 클래스 이름
     * 이 정의들은 UXML 과 USS 파일에서 쓰는 값과 일치해야 한다.
     */

    // 행(Row)의 식별자
    public const string VisualElementIdentifierInteractive = "interactive-item";
    // 상호작용 항목 컨테이너의 식별자
    public const string VisualElementIdentifierInteractivesContainer = "InteractivesContainer";
    // 선택된 행 라벨의 식별자
    public const string VisualElementIdentifierSelectedInteractableLabel = "SelectedLabel";
    // 행 아이콘의 식별자
    public const string VisualElementIdentifierInteractableIcon = "interactable-icon";
    // 행 텍스트의 식별자
    public const string VisualElementIdentifierInteractableLabel = "interactable-label";

    // 선택된 행의 클래스 이름
    public const string VisualElementClassNameSelectedInteractable = "selected";
  }
}
