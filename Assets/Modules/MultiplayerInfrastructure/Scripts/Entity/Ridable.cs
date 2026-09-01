using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace MultiplayerInfrastructure.Entity
{
  public abstract class Ridable : NetworkBehaviour
  {
    [SerializeField] private List<RidableAttachPointObject> attachPoints = new List<RidableAttachPointObject>();

    // 길이는 attachPoints 와 같아야 한다
    [SerializeField] private List<PlayerController> nowRidingPlayers = new List<PlayerController>();

    protected void Awake_Ridable()
    {
      attachPoints = new List<RidableAttachPointObject>(GetComponentsInChildren<RidableAttachPointObject>());
      nowRidingPlayers = new List<PlayerController>(attachPoints.Count);
      for (int i = 0; i < attachPoints.Count; i++)
        nowRidingPlayers.Add(null);

      if (attachPoints.Count == 0)
        Debug.LogWarning($"Ridable object {gameObject.name} has no attach points defined.");
    }

    protected IReadOnlyList<RidableAttachPointObject> AttachPoints => attachPoints;

    protected bool TryOccupyNextAttachPoint(PlayerController player, out int attachPointIndex, out Transform attachTransform)
    {
      attachPointIndex = -1;
      attachTransform = null;

      if (player == null)
        return false;

      for (int i = 0; i < nowRidingPlayers.Count; i++)
      {
        if (nowRidingPlayers[i] != null)
          continue;

        nowRidingPlayers[i] = player;
        attachPointIndex = i;
        attachTransform = attachPoints[i] != null ? attachPoints[i].transform : transform;
        return true;
      }

      return false;
    }

    protected bool ReleaseAttachPoint(PlayerController player, out int attachPointIndex)
    {
      attachPointIndex = -1;
      if (player == null)
        return false;

      for (int i = 0; i < nowRidingPlayers.Count; i++)
      {
        if (nowRidingPlayers[i] != player)
          continue;

        nowRidingPlayers[i] = null;
        attachPointIndex = i;
        return true;
      }

      return false;
    }
  }
}
