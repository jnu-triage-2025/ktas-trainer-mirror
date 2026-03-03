namespace MultiplayerInfrastructure.UI.Models
{
  /// <summary>
  /// 텍스처 성능 품질 단계를 나타내는 열거형입니다.
  /// <see cref="UnityEngine.QualitySettings.masterTextureLimit"/>에 대응합니다.
  /// </summary>
  public enum TextureQuality
  {
    /// <summary>원본 해상도 (masterTextureLimit = 0)</summary>
    Ultra = 0,

    /// <summary>1/2 해상도 (masterTextureLimit = 1)</summary>
    High = 1,

    /// <summary>1/4 해상도 (masterTextureLimit = 2)</summary>
    Medium = 2,

    /// <summary>1/8 해상도 (masterTextureLimit = 3)</summary>
    Low = 3,
  }
}
