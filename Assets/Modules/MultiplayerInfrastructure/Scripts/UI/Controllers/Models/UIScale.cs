namespace MultiplayerInfrastructure.UI.Models
{
  /// <summary>
  /// UI 배율(스케일) 단계를 나타내는 열거형입니다.
  /// <see cref="UnityEngine.UIElements.PanelSettings.scale"/>에 곱해질 배율 계수에 대응합니다.
  ///
  /// 단계가 높을수록 UI가 크게 표시됩니다.
  /// 정수 값(1~4)이 PlayerPrefs에 그대로 저장됩니다.
  /// </summary>
  public enum UIScale
  {
    /// <summary>1단계 - 가장 작게 (배율 0.75)</summary>
    Level1 = 1,

    /// <summary>2단계 - 기본값 (배율 1.0)</summary>
    Level2 = 2,

    /// <summary>3단계 - 크게 (배율 1.25)</summary>
    Level3 = 3,

    /// <summary>4단계 - 가장 크게 (배율 1.5)</summary>
    Level4 = 4,
  }
}
