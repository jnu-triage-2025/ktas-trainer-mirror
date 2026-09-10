using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Player;
using TriageTrainer.ItemDefinitions;
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
    // Keep terminal outcomes for this patient's lifetime so late/duplicate confirmations cannot
    // undo a successful use or apply an expired approval a second time.
    private readonly Dictionary<string, (int ClientId, bool Accepted)> _completedItemUses = new(StringComparer.Ordinal);
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
      if (!TryResolveApprovedItemUseKey(itemIdentifier, out string treatmentIdentifier))
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

    // Patient A's CPR syringes are handled by ApplyItemUse's round-aware branch,
    // rather than the static ItemUseEffects table.  They still need a stable key
    // while the server waits for the owning client to confirm inventory
    // consumption; otherwise remote players can see and execute the interaction
    // but the approval request is silently discarded.
    private bool TryResolveApprovedItemUseKey(string itemIdentifier, out string treatmentIdentifier)
    {
      if (TryResolveItemUse(itemIdentifier, out _, out treatmentIdentifier, out _))
        return true;

      if (IsPatientA
          && (IsEpinephrineSyringeIdentifier(itemIdentifier)
              || string.Equals(itemIdentifier, NormalSaline20ccSyringe.Identifier,
                StringComparison.Ordinal)))
      {
        treatmentIdentifier = itemIdentifier;
        return true;
      }

      treatmentIdentifier = null;
      return false;
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
      if (string.IsNullOrWhiteSpace(token) || sender == null || !sender.IsValid)
        return;

      bool accepted = ResolveApprovedPatientItemConsumption(token, sender.ClientId, consumed);
      TargetCompleteApprovedPatientItemConsumption(sender, token, accepted);
    }

    private bool ResolveApprovedPatientItemConsumption(string token, int clientId, bool consumed)
    {
      PruneExpiredPendingItemUses();
      if (_completedItemUses.TryGetValue(token, out var completed))
        return completed.ClientId == clientId && completed.Accepted;

      if (!_pendingItemUses.TryGetValue(token, out var pending)
          || pending == null || pending.ClientId != clientId)
        return false;

      _pendingItemUses.Remove(token);
      bool accepted = false;
      try
      {
        // Movement between approval and confirmation does not invalidate consumed inventory.
        if (consumed && CanApplyItemUse(pending.ItemIdentifier))
        {
          using (MI.Scenario.ScenarioSignalPlayerContext.Push(
                   pending.ActorIdentifier, pending.ActorDisplayName))
            accepted = ApplyItemUse(pending.ItemIdentifier);
        }
      }
      catch (Exception ex)
      {
        Debug.LogException(ex, this);
      }
      finally
      {
        _completedItemUses[token] = (clientId, accepted);
      }
      return accepted;
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
      {
        string token = expired[i];
        var pending = _pendingItemUses[token];
        _pendingItemUses.Remove(token);
        if (pending == null)
          continue;

        _completedItemUses[token] = (pending.ClientId, false);
        // Notify even when no confirmation arrives. A later confirmation replays this rejection.
        var clients = FishNet.InstanceFinder.ServerManager?.Clients;
        if (clients != null && clients.TryGetValue(pending.ClientId, out var connection)
            && connection != null && connection.IsValid)
          TargetCompleteApprovedPatientItemConsumption(connection, token, accepted: false);
      }
    }

    private bool HasPendingApprovedItemUse(string itemIdentifier)
    {
      PruneExpiredPendingItemUses();
      if (!TryResolveApprovedItemUseKey(itemIdentifier, out string treatmentIdentifier))
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
