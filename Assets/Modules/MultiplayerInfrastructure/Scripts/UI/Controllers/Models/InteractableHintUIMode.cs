using System;

namespace MultiplayerInfrastructure.UI
{
  [Serializable]
  public enum InteractableHintUIMode
  {
    Normal,     // 일반 모드: 주변 상호작용 객체 표시
    Dialogue    // 다이얼로그 모드: 대화 선택지만 표시
  }
}
