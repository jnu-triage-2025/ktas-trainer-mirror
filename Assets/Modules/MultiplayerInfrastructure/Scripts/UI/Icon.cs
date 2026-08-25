using UnityEngine;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>UI에서 공통으로 사용하는 아이콘 오버레이입니다.</summary>
  public static class Icon
  {
    private const string ClearRightBottomPath = "Textures/Icons/icon-overlay-clear__rb";
    private const string FailRightBottomPath = "Textures/Icons/icon-overlay-fail__rb";

    private static Sprite _clearRightBottom;
    private static Sprite _failRightBottom;

    public static Sprite ClearRightBottom => Load(ref _clearRightBottom, ClearRightBottomPath);
    public static Sprite FailRightBottom => Load(ref _failRightBottom, FailRightBottomPath);

    private static Sprite Load(ref Sprite cached, string resourcePath)
    {
      if (cached == null)
        cached = Resources.Load<Sprite>(resourcePath);
      return cached;
    }
  }
}
