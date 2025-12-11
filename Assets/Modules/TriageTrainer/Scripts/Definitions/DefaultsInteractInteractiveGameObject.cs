namespace TriageTrainer.Definitions
{
  public class DefaultsInteractInteractiveGameObject
  {
    public const float ScrollSpeedUnderBoundOnHintUI = 0.01f;

    /**
     * Visual Element Identifiers and Class Names
     * These definitions have to equal the ones used in the UXML and USS files.
     */
    
    // Identifier of Row
    public const string VisualElementIdentifierInteractive = "interactive-item";
    // Identifier of Interactives' container
    public const string VisualElementIdentifierInteractivesContainer = "InteractivesContainer";
    // Identifier of Selected Row's Label
    public const string VisualElementIdentifierSelectedInteractableLabel = "SelectedLabel";
    // Identifier of Row's icon
    public const string VisualElementIdentifierInteractableIcon = "interactable-icon";
    // Identifier or Row's text
    public const string VisualElementIdentifierInteractableLabel = "interactable-label";
    
    // Classname of Selected Row
    public const string VisualElementClassNameSelectedInteractable = "selected";
  }
}
