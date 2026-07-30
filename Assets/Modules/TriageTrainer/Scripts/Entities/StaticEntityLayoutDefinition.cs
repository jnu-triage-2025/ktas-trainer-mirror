using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Entity
{
  public enum StaticEntityLayoutType { MovingPatientBedPositioningPoint, WallSuction, Oxyflowmeter, PatientCareDescriptionZone }
  [Serializable] public struct StaticEntityTransformDefinition
  {
    public StaticEntityLayoutType type;
    public string identifier;
    public bool useDefaultPosition;
    public Vector3 position;
    public bool useDefaultRotation;
    public Vector3 rotationEuler;
    public bool useDefaultOccupiedSize;
    public Vector2 occupiedSize;
    public bool useDefaultDisplayHeight;
    public float displayHeight;
    public bool useDefaultZoneCenter;
    public Vector3 zoneCenter;
    public bool useDefaultZoneSize;
    public Vector3 zoneSize;
  }
  [Serializable] public struct StaticEntityLayoutGroup
  {
    public List<StaticEntityTransformDefinition> entities;
  }
  [CreateAssetMenu(menuName = "Triage Trainer/Overworld/Static Entity Layout", fileName = "StaticEntityLayout")]
  public sealed class StaticEntityLayoutDefinition : ScriptableObject
  {
    public string identifier;
    public GameObject wallSuctionPrefab;
    public GameObject oxyflowmeterPrefab;
    public List<StaticEntityLayoutGroup> groups = new();

    public bool Validate(out string error)
    {
      error = string.Empty;
      if (string.IsNullOrWhiteSpace(identifier)) { error = "Layout identifier is empty."; return false; }
      if (groups == null) { error = "Layout groups are null."; return false; }
      var identifiers = new HashSet<string>();
      foreach (var group in groups)
      {
        if (group.entities == null) { error = "A layout group has no entity list."; return false; }
        foreach (var entity in group.entities)
        {
          if (string.IsNullOrWhiteSpace(entity.identifier)) { error = "An entity identifier is empty."; return false; }
          if (!identifiers.Add(entity.identifier.Trim())) { error = "Duplicate entity identifier: " + entity.identifier; return false; }
          if (entity.type == StaticEntityLayoutType.WallSuction && wallSuctionPrefab == null) { error = "WallSuction prefab is missing for " + entity.identifier; return false; }
          if (entity.type == StaticEntityLayoutType.Oxyflowmeter && oxyflowmeterPrefab == null) { error = "Oxyflowmeter prefab is missing for " + entity.identifier; return false; }
        }
      }
      return true;
    }

    private void OnValidate()
    {
      if (!Validate(out var error))
        Debug.LogWarning($"[StaticEntityLayoutDefinition] {name}: {error}", this);
    }
  }
}
