using System;

namespace MultiplayerInfrastructure.Commons
{
  [Serializable]
  public enum IconSpriteDefinitions
  {
    // 0 은 직렬화/기본값이 채워지는 값이므로 "지정되지 않음"을 의미하도록 둡니다.
    // (이 경우 사용처에서 컨텍스트 기본 아이콘 - 예: 시나리오 실행 아이콘 - 이 적용됩니다.)
    Undefined = 0,

    // 명시적으로 "아이콘 없음"을 지정할 때 사용합니다. 컨텍스트 기본값도 적용하지 않습니다.
    None = 1,

    NPCMessage = 2,
    NPCMessageQuest = 3,
  }

  /// <summary>
  /// 코드 전반에서 참조하는 잘 알려진 아이콘 레지스트리 식별자 상수 모음입니다.
  /// (식별자는 Registry.IconSprite 레지스트리에 등록되어 있어야 합니다.)
  /// </summary>
  public static class IconSpriteIdentifiers
  {
    /// <summary>
    /// 시나리오 실행(대화 등)을 fire하는 모든 interactable의 기본 아이콘 식별자입니다.
    /// 명시적으로 아이콘을 지정하지 않은 시나리오 interactable에 적용됩니다.
    /// </summary>
    public const string ScenarioDefault = "message-circle";

    /// <summary>퀘스트 대상 상호작용의 기본 아이콘을 대체할 때 사용하는 퀘스트 마크 식별자입니다.</summary>
    public const string QuestInteractionMark = "quest-interaction";

    /// <summary>퀘스트 대상 NPC의 머리 위에 표시하는 퀘스트 마크 식별자입니다.</summary>
    public const string QuestNpcMark = "quest-marker";
  }
}
