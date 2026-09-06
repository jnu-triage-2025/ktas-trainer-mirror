using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace TriageTrainer.Interactions
{
  /// <summary>
  /// 시나리오 데이터의 interactions 정의가 display.iconIdentifiers 로 참조하는 트리아지 전용 아이콘 식별자.
  /// 식별자는 IconSprite 레지스트리에 Resources 경로로 등록되어 필요할 때 로드된다.
  /// </summary>
  public static class TriageInteractionIcons
  {
    public const string Cpr = "interaction-cpr";
    public const string Unlink = "interaction-unlink";
    public const string CutCloth = "interaction-cut-cloth";

    private static readonly (string Identifier, string Path)[] Literals =
    {
      (Cpr, "Textures/Interactions/cpr"),
      (Unlink, "Textures/Interactions/unlink"),
      (CutCloth, "Textures/Interactions/cut-cloth"),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterLiterals()
    {
      for (int i = 0; i < Literals.Length; i++)
        Registry.Register(RegistryType.IconSprite, Literals[i].Identifier, Literals[i].Path);
    }
  }
}
