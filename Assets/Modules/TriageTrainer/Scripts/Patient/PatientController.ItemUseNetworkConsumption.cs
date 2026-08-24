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

    private sealed class LocalPendingItemUse
    {
      public PlayerController Player;
      public PlayerController.ItemUseConsumptionReceipt Receipt;
    }

    private readonly Dictionary<string, PendingItemUse> _pendingItemUses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalPendingItemUse>
      _localPendingItemUseReceipts = new(StringComparer.Ordinal);

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

      PlayerController.ItemUseConsumptionReceipt receipt = null;
      bool consumed = ownerPlayer != null
                      && ownerPlayer.TryConsumeItemUse(itemIdentifier, out receipt);
      if (consumed && receipt != null)
      {
        _localPendingItemUseReceipts[token] = new LocalPendingItemUse
        {
          Player = ownerPlayer,
          Receipt = receipt
        };
      }
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
          || !_pendingItemUses.TryGetValue(token, out var pending)
          || pending == null
          || pending.ClientId != sender.ClientId)
        return;

      _pendingItemUses.Remove(token);
      bool accepted = false;

      // 거리와 역할은 승인 토큰을 발급할 때 서버에서 검증한다. 확인 응답 사이의 짧은 시간에
      // 플레이어가 움직였다는 이유로 이미 소비한 아이템을 유실하지 않도록 여기서는 처치 상태만 재검증한다.
      if (consumed && CanApplyItemUse(pending.ItemIdentifier))
      {
        using (MI.Scenario.ScenarioSignalPlayerContext.Push(
                 pending.ActorIdentifier, pending.ActorDisplayName))
          accepted = ApplyItemUse(pending.ItemIdentifier);
      }

      TargetCompleteApprovedPatientItemConsumption(sender, token, accepted);
    }

    [TargetRpc]
    private void TargetCompleteApprovedPatientItemConsumption(
      NetworkConnection connection,
      string token,
      bool accepted)
    {
      if (string.IsNullOrWhiteSpace(token)
          || !_localPendingItemUseReceipts.Remove(token, out var pending)
          || pending?.Receipt == null)
        return;

      var ownerPlayer = pending.Player != null
        ? pending.Player
        : FindLocalPatientItemUseOwner();
      ownerPlayer?.CompleteConsumedItemUse(pending.Receipt, accepted);
    }

    private static PlayerController FindLocalPatientItemUseOwner()
    {
      var players = FindObjectsByType<PlayerController>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        if (players[i] != null && players[i].IsOwner)
          return players[i];
      }
      return null;
    }

    private void RestoreLocalPendingPatientItemUses()
    {
      if (_localPendingItemUseReceipts.Count == 0)
        return;
      var ownerPlayer = FindLocalPatientItemUseOwner();
      if (ownerPlayer != null)
      {
        foreach (var pending in _localPendingItemUseReceipts.Values)
        {
          var player = pending?.Player != null ? pending.Player : ownerPlayer;
          player?.CompleteConsumedItemUse(pending?.Receipt, accepted: false);
        }
      }
      _localPendingItemUseReceipts.Clear();
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
