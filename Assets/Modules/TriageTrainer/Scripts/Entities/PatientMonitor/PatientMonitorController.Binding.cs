using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public partial class PatientMonitorController
  {
    [Header("Monitoring")]
    [SerializeField] private PatientController _monitoringPatient;

    // PatientController 참조는 로컬 Unity 오브젝트다. 대신 안정적인 런타임 식별자를
    // 복제하고, 환자가 스폰된 뒤 각 피어에서 해석한다.
    private readonly SyncVar<string> _monitoringPatientIdentifier = new(string.Empty);

    private PatientController _subscribedPatient;
    private string _pendingMonitoringPatientIdentifier;
    private readonly HashSet<int> _serverSelectionClientIds = new();

    public PatientController MonitoringPatient => _monitoringPatient;

    public void SetMonitoringPatient(PatientController patient)
    {
      if (IsServerStarted)
      {
        SetMonitoringPatientOnServer(patient);
        return;
      }

      if (IsClientStarted)
      {
        CmdRequestMonitoringPatient(patient != null ? patient.Identifier : string.Empty);
        return;
      }

      ApplyMonitoringPatient(patient);
    }

    public override void OnStartServer()
    {
      base.OnStartServer();
      SetMonitoringPatientOnServer(_monitoringPatient);
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      _monitoringPatientIdentifier.OnChange += OnMonitoringPatientIdentifierChanged;
      ApplyMonitoringPatientIdentifier(_monitoringPatientIdentifier.Value);
    }

    public override void OnStopClient()
    {
      _monitoringPatientIdentifier.OnChange -= OnMonitoringPatientIdentifierChanged;
      _pendingMonitoringPatientIdentifier = null;
      base.OnStopClient();
    }

    private void SetMonitoringPatientOnServer(PatientController patient)
    {
      ApplyMonitoringPatient(patient);
      _monitoringPatientIdentifier.Value = patient != null ? patient.Identifier : string.Empty;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestMonitoringPatient(string identifier, NetworkConnection sender = null)
    {
      var player = sender != null && sender.IsValid ? FindPlayer(sender.ClientId) : null;
      if (player == null)
        return;

      if (!_serverSelectionClientIds.Contains(sender.ClientId) ||
          (player.transform.position - transform.position).sqrMagnitude > 9f)
      {
        TargetConfirmMonitoringPatient(sender, false);
        return;
      }

      if (string.IsNullOrWhiteSpace(identifier))
      {
        SetMonitoringPatientOnServer(null);
        _serverSelectionClientIds.Remove(sender.ClientId);
        TargetConfirmMonitoringPatient(sender, true);
        return;
      }

      var patient = FindPatient(identifier);
      if (patient == null || (patient.transform.position - transform.position).sqrMagnitude > 25f)
      {
        TargetConfirmMonitoringPatient(sender, false);
        return;
      }

      SetMonitoringPatientOnServer(patient);
      _serverSelectionClientIds.Remove(sender.ClientId);
      TargetConfirmMonitoringPatient(sender, true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdBeginMonitoringPatientSelection(NetworkConnection sender = null)
    {
      var player = sender != null && sender.IsValid ? FindPlayer(sender.ClientId) : null;
      if (player == null || (player.transform.position - transform.position).sqrMagnitude > 9f)
        return;

      _serverSelectionClientIds.Add(sender.ClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdEndMonitoringPatientSelection(NetworkConnection sender = null)
    {
      if (sender != null && sender.IsValid)
        _serverSelectionClientIds.Remove(sender.ClientId);
    }

    [TargetRpc]
    private void TargetConfirmMonitoringPatient(NetworkConnection connection, bool accepted)
    {
      var player = FindLocalOwnerPlayer();
      if (player == null)
        return;

      if (accepted)
        ExitSelectionModeFor(player);
      else
        player.RefreshInteractableHintsNow();
    }

    private void BeginNetworkSelection(PlayerController player)
    {
      if (player == null || player.Owner == null)
        return;

      if (IsServerStarted)
        _serverSelectionClientIds.Add(player.Owner.ClientId);
      else if (IsClientStarted)
        CmdBeginMonitoringPatientSelection();
    }

    private void EndNetworkSelection(PlayerController player)
    {
      if (player == null || player.Owner == null)
        return;

      if (IsServerStarted)
        _serverSelectionClientIds.Remove(player.Owner.ClientId);
      else if (IsClientStarted)
        CmdEndMonitoringPatientSelection();
    }

    private void OnMonitoringPatientIdentifierChanged(string previous, string next, bool asServer)
    {
      if (!asServer)
        ApplyMonitoringPatientIdentifier(next);
    }

    private void ApplyMonitoringPatientIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
      {
        _pendingMonitoringPatientIdentifier = null;
        ApplyMonitoringPatient(null);
        return;
      }

      var patient = FindPatient(identifier);
      if (patient == null)
      {
        _pendingMonitoringPatientIdentifier = identifier;
        return;
      }

      _pendingMonitoringPatientIdentifier = null;
      ApplyMonitoringPatient(patient);
    }

    private void TryResolvePendingMonitoringPatient()
    {
      if (!string.IsNullOrWhiteSpace(_pendingMonitoringPatientIdentifier))
        ApplyMonitoringPatientIdentifier(_pendingMonitoringPatientIdentifier);
    }

    private static PatientController FindPatient(string identifier)
    {
      if (Registry.TryGetEntity(identifier, out var descriptor) &&
          descriptor?.GameObject != null &&
          descriptor.GameObject.TryGetComponent(out PatientController registeredPatient))
        return registeredPatient;

      return FindObjectsByType<PatientController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
        .FirstOrDefault(patient => patient != null && string.Equals(patient.Identifier, identifier, StringComparison.Ordinal));
    }

    private static PlayerController FindPlayer(int clientId)
    {
      return FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
        .FirstOrDefault(player => player != null && player.Owner != null && player.Owner.ClientId == clientId);
    }

    private static PlayerController FindLocalOwnerPlayer()
    {
      return FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
        .FirstOrDefault(player => player != null && player.IsOwner);
    }

    private void ApplyMonitoringPatient(PatientController patient)
    {
      if (ReferenceEquals(_monitoringPatient, patient))
        return;

      var existingMonitor = patient != null ? patient.MonitoringPatientMonitor : null;
      if (existingMonitor != null && !ReferenceEquals(existingMonitor, this))
      {
        if (IsServerStarted)
          existingMonitor.SetMonitoringPatientOnServer(null);
        else
          existingMonitor.ApplyMonitoringPatient(null);
      }

      var previousPatient = _monitoringPatient;

      // 이전 환자에게 이 모니터가 더 이상 감시하지 않음을 알림
      if (previousPatient != null)
      {
        previousPatient.ClearMonitoringPatientMonitor(this);
      }

      _monitoringPatient = patient;

      // 새 환자에게 이 모니터가 감시 중임을 알림
      if (_monitoringPatient != null)
      {
        _monitoringPatient.SetMonitoringPatientMonitor(this);
      }

      ResolvePatientStateIfNeeded();
      PullParametersFromPatientState();
      UpdateTrackingLine();
    }

    private void ResolvePatientStateIfNeeded()
    {
      var previousPatient = patientState;

      if (_monitoringPatient != null)
        patientState = _monitoringPatient;
      else
        patientState = GetComponentInParent<PatientController>();

      UpdateMedicalStateSubscription(patientState);

      if (!ReferenceEquals(previousPatient, patientState))
        PullParametersFromPatientState();
    }

    private void UpdateMedicalStateSubscription(PatientController nextPatient)
    {
      if (ReferenceEquals(_subscribedPatient, nextPatient))
        return;

      if (_subscribedPatient != null)
      {
        _subscribedPatient.UnregisterMedicalStateListener(this);
      }

      _subscribedPatient = nextPatient;

      if (_subscribedPatient != null)
      {
        _subscribedPatient.RegisterMedicalStateListener(this);
      }
    }

    private void UnregisterMedicalStateSubscription()
    {
      if (_subscribedPatient == null)
        return;

      _subscribedPatient.UnregisterMedicalStateListener(this);
      _subscribedPatient = null;
    }
  }
}
