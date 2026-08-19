using UnityEngine;

namespace MultiplayerInfrastructure.UI
{
  public interface IOverheadPresentationAnchorProvider
  {
    Transform OverheadPresentationAnchor { get; }
  }
}
