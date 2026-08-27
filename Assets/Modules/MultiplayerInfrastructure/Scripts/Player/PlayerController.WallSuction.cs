using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [Header("Wall Suction")]
    [SerializeField] private bool _isWallSuctionAvailable;

    /// <summary>
    /// 흡인기(WallSuction)와 양커 라인으로 연결되어 있어, 환자에게 흡인기 사용 상호작용을
    /// 수행할 수 있는지 여부입니다. 라인 연결/해제 시 <see cref="SetWallSuctionAvailable"/> 로 갱신됩니다.
    /// </summary>
    public bool IsWallSuctionAvailable => _isWallSuctionAvailable;

    public void SetWallSuctionAvailable(bool available)
    {
      if (_isWallSuctionAvailable == available)
        return;

      _isWallSuctionAvailable = available;
      RefreshInteractableHintsNow();
    }
  }
}
