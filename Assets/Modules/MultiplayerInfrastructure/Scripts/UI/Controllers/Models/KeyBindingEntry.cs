using System;
using UnityEngine;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 키 설정 항목 하나를 나타내는 데이터 클래스입니다.
  /// actionId와 actionDisplayName으로 어떤 기능인지 식별하고,
  /// boundKey로 현재 할당된 키코드를 저장합니다.
  /// </summary>
  [Serializable]
  public class KeyBindingEntry
  {
    /// <summary>기능의 고유 식별자 (예: "move_forward")</summary>
    public string actionId;

    /// <summary>UI에 표시될 기능 명칭 (예: "앞으로 이동")</summary>
    public string actionDisplayName;

    /// <summary>현재 할당된 키. KeyCode.None이면 미할당 상태입니다.</summary>
    public KeyCode boundKey;

    public KeyBindingEntry() { }

    public KeyBindingEntry(string actionId, string actionDisplayName, KeyCode boundKey = KeyCode.None)
    {
      this.actionId = actionId;
      this.actionDisplayName = actionDisplayName;
      this.boundKey = boundKey;
    }
  }
}
