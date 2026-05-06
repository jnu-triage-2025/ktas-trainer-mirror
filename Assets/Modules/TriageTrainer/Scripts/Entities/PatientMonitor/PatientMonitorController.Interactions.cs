using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public partial class PatientMonitorController : IInteractable, PatientController.IMonitorSelectionRequester, PatientController.IMedicalStateListener
  {
    private sealed class MonitorSelectModeInteract : IInteract
    {
      private readonly PatientMonitorController _owner;
      public MonitorSelectModeInteract(PatientMonitorController owner) { _owner = owner; }
      public string DisplayText => "모니터링할 환자 선택";
      public Sprite DisplayIcon => _owner._interactIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public void Interact(Transform interactor)
      {
        _owner.EnterSelectionMode(interactor);
      }
    }

    [Serializable]
    public class InteractEntry
    {
      [SerializeField] private string _identifier;
      [SerializeField] private bool _enabled = true;

      public string Identifier => _identifier;
      public bool Enabled
      {
        get => _enabled;
        set => _enabled = value;
      }

      public InteractEntry(string identifier, bool enabled = true)
      {
        _identifier = identifier;
        _enabled = enabled;
      }
    }

    private const string InteractIdSelectPatient = "select_patient_mode";

    [Header("Interact")]
    [SerializeField] private Sprite _interactIcon;
    [SerializeField] private List<InteractEntry> _interactEntries = new();

    private readonly List<IInteract> _interacts = new();
    private readonly Dictionary<string, InteractEntry> _interactEntryMap = new(StringComparer.Ordinal);
    private readonly Dictionary<int, PlayerController> _selectionModePlayers = new();

    public IInteract[] Interacts => _interacts.ToArray();

    private void Awake()
    {
      BuildInteracts();
    }

    private void OnDisable()
    {
      ExitSelectionModeForAll();
      UnregisterMedicalStateSubscription();
    }

    private void OnDestroy()
    {
      ExitSelectionModeForAll();
      UnregisterMedicalStateSubscription();
    }

    private void BuildInteracts()
    {
      EnsureInteractEntry(InteractIdSelectPatient, true);
      RebuildInteractEntryMap();

      _interacts.Clear();
      _interacts.Add(new MonitorSelectModeInteract(this));
    }

    private void EnsureInteractEntry(string identifier, bool enabled)
    {
      for (int i = 0; i < _interactEntries.Count; i++)
      {
        var each = _interactEntries[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        return;
      }

      _interactEntries.Add(new InteractEntry(identifier, enabled));
    }

    private void RebuildInteractEntryMap()
    {
      _interactEntryMap.Clear();
      for (int i = 0; i < _interactEntries.Count; i++)
      {
        var each = _interactEntries[i];
        if (each == null || string.IsNullOrWhiteSpace(each.Identifier))
          continue;

        _interactEntryMap[each.Identifier] = each;
      }
    }

    public void SetInteractEnabled(string identifier, bool enabled)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      for (int i = 0; i < _interactEntries.Count; i++)
      {
        var each = _interactEntries[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        each.Enabled = enabled;
        RebuildInteractEntryMap();
        return;
      }

      _interactEntries.Add(new InteractEntry(identifier, enabled));
      RebuildInteractEntryMap();
    }

    public void AddInteract(string identifier, bool enabled = true)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      EnsureInteractEntry(identifier, enabled);
      RebuildInteractEntryMap();
    }

    public void RemoveInteract(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      for (int i = _interactEntries.Count - 1; i >= 0; i--)
      {
        var each = _interactEntries[i];
        if (each == null || !string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          continue;

        _interactEntries.RemoveAt(i);
      }

      RebuildInteractEntryMap();
    }

    public bool IsInteractEnabled(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return false;

      return _interactEntryMap.TryGetValue(identifier, out var entry) && entry.Enabled;
    }

    private void EnterSelectionMode(Transform interactor)
    {
      if (!IsInteractEnabled(InteractIdSelectPatient))
        return;

      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return;

      _selectionModePlayers[player.GetInstanceID()] = player;
      player.SetPatientSelectionMode(true);

      var patients = FindObjectsByType<PatientController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < patients.Length; i++)
      {
        var patient = patients[i];
        if (patient == null)
          continue;

        patient.SetMonitorSelectionRequester(this);
      }

      player.RefreshInteractableHintsNow();
    }

    private void ExitSelectionModeFor(PlayerController player)
    {
      if (player == null)
        return;

      _selectionModePlayers.Remove(player.GetInstanceID());
      player.SetPatientSelectionMode(false);
      if (_selectionModePlayers.Count <= 0)
      {
        var patients = FindObjectsByType<PatientController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < patients.Length; i++)
        {
          var patient = patients[i];
          if (patient == null)
            continue;

          patient.ClearMonitorSelectionRequester(this);
        }
      }

      player.RefreshInteractableHintsNow();
    }

    private void ExitSelectionModeForAll()
    {
      var values = new List<PlayerController>(_selectionModePlayers.Values);
      _selectionModePlayers.Clear();

      var patients = FindObjectsByType<PatientController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < patients.Length; i++)
      {
        var patient = patients[i];
        if (patient == null)
          continue;

        patient.ClearMonitorSelectionRequester(this);
      }

      for (int i = 0; i < values.Count; i++)
      {
        values[i]?.SetPatientSelectionMode(false);
        values[i]?.RefreshInteractableHintsNow();
      }
    }

    public void HandlePatientSelected(PatientController patient, Transform interactor)
    {
      if (patient == null)
        return;

      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return;

      if (!_selectionModePlayers.ContainsKey(player.GetInstanceID()))
        return;

      SetMonitoringPatient(patient);
      ExitSelectionModeFor(player);
    }
  }
}
