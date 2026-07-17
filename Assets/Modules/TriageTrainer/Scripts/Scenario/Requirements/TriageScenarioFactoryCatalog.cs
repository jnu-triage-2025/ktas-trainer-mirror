using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TriageTrainer.Scenario.Requirements
{
  [Serializable]
  public sealed class TriageScenarioFactoryEntry
  {
    [SerializeField] private string _identifier;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private bool _isPatient;
    public string Identifier => _identifier ?? string.Empty;
    public GameObject Prefab => _prefab;
    public bool IsPatient => _isPatient;
  }

  [CreateAssetMenu(fileName = "TriageScenarioFactoryCatalog", menuName = "Triage Trainer/Scenario Factory Catalog")]
  public sealed class TriageScenarioFactoryCatalog : ScriptableObject
  {
    [SerializeField] private List<TriageScenarioFactoryEntry> _entries = new List<TriageScenarioFactoryEntry>();
    public IReadOnlyList<TriageScenarioFactoryEntry> Entries => _entries ?? new List<TriageScenarioFactoryEntry>();
    public bool TryGet(string identifier, out TriageScenarioFactoryEntry entry)
    {
      entry = Entries.FirstOrDefault(value => value != null && string.Equals(value.Identifier, identifier, StringComparison.Ordinal));
      return entry != null && entry.Prefab != null;
    }
  }
}
