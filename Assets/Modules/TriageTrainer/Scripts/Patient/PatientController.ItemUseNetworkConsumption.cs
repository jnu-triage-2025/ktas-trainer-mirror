using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Player;
using UnityEngine;

using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    private const float PendingItemUseLifetimeSeconds = 10f;

    private sealed class PendingItemUse
    {
      public int ClientId;
      public string ItemIdentifier;
      public string ActorIdentifier;
      public string ActorDisplayName;
      public string TreatmentIdentifier;
      public float CreatedAt;
    }

    private readonly Dictionary<string, PendingItemUse> _pendingItemUses = new(StringComparer.Ordinal);

    private void RequestApprovedRemoteItemConsumption(
      PlayerController player,
      string itemIdentifier,
      string actorIdentifier,
      string actorDisplayName)
    {
      if (player?.Owner == null || !player.Owner.IsValid)
        return;

      PruneExpiredPendingItemUses();
      if (!TryResolveItemUse(
            itemIdentifier, out _, out string treatmentIdentifier, out _))
        return;
      foreach (var pendingUse in _pendingItemUses.Values)
      {
        if (pendingUse != null
            && string.Equals(pendingUse.TreatmentIdentifier, treatmentIdentifier, StringComparison.Ordinal))
          return;
      }

      string token = Guid.NewGuid().ToString("N");
      _pendingItemUses[token] = new PendingItemUse
      {
        ClientId = player.Owner.ClientId,
        ItemIdentifier = itemIdentifier,
        ActorIdentifier = actorIdentifier,
        ActorDisplayName = actorDisplayName,
        TreatmentIdentifier = treatmentIdentifier,
        CreatedAt = Time.unscaledTime
      };
      TargetConsumeApprovedPatientItem(player.Owner, token, itemIdentifier);
    }

    [TargetRpc]
    private void TargetConsumeApprovedPatientItem(
      NetworkConnection connection,
      string token,
      string itemIdentifier)
    {
      PlayerController ownerPlayer = null;
      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        if (players[i] != null && players[i].IsOwner)
        {
          ownerPlayer = players[i];
          break;
        }
      }

      bool consumed = ownerPlayer != null && ownerPlayer.TryConsumeItemUse(itemIdentifier);
      CmdConfirmApprovedPatientItemConsumption(token, consumed);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdConfirmApprovedPatientItemConsumption(
      string token,
      bool consumed,
      NetworkConnection sender = null)
    {
      if (string.IsNullOrWhiteSpace(token)
          || sender == null
          || !sender.IsValid
          || !_pendingItemUses.Remove(token, out var pending)
          || pending == null
          || pending.ClientId != sender.ClientId
          || !consumed)
        return;

      // 거리와 역할은 승인 토큰을 발급할 때 서버에서 검증한다. 확인 응답 사이의 짧은 시간에
      // 플레이어가 움직였다는 이유로 이미 소비한 아이템을 유실하지 않도록 여기서는 처치 상태만 재검증한다.
      if (!CanApplyItemUse(pending.ItemIdentifier))
        return;

      using (MI.Scenario.ScenarioSignalPlayerContext.Push(
               pending.ActorIdentifier, pending.ActorDisplayName))
        ApplyItemUse(pending.ItemIdentifier);
    }

    private void PruneExpiredPendingItemUses()
    {
      float cutoff = Time.unscaledTime - PendingItemUseLifetimeSeconds;
      if (_pendingItemUses.Count == 0)
        return;

      var expired = new List<string>();
      foreach (var pair in _pendingItemUses)
      {
        if (pair.Value == null || pair.Value.CreatedAt < cutoff)
          expired.Add(pair.Key);
      }
      for (int i = 0; i < expired.Count; i++)
        _pendingItemUses.Remove(expired[i]);
    }

    private bool HasPendingApprovedItemUse(string itemIdentifier)
    {
      PruneExpiredPendingItemUses();
      if (!TryResolveItemUse(
            itemIdentifier, out _, out string treatmentIdentifier, out _))
        return false;

      foreach (var pending in _pendingItemUses.Values)
      {
        if (pending != null
            && string.Equals(pending.TreatmentIdentifier, treatmentIdentifier, StringComparison.Ordinal))
          return true;
      }
      return false;
    }
  }
}
