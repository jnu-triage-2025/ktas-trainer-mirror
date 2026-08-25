using UnityEngine;

namespace MultiplayerInfrastructure.UI
{
  public interface IOverheadPresentationAnchorProvider
  {
    public Transform OverheadPresentationAnchor { get; }
  }
}
