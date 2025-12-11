using UnityEngine;

namespace Modules.TriageTrainer.Scripts.PlayerInteractiveGameObject
{
  public class PlayerInteractiveModel : MonoBehaviour
  {
    [SerializeField] private string displayText;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color color;
    
    public string DisplayText => displayText;
    public Sprite Icon => icon;
    public Color Color => color;
    
    public PlayerInteractiveModel(string displayText, Sprite icon, Color? color = null)
    {
      this.displayText = displayText;
      this.icon = icon;
      this.color = color ?? Color.white;
    }
  }
}
