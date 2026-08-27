using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Entity
{
  public enum StaticEntityLayoutType { MovingPatientBedPositioningPoint, WallSuction, Oxyflowmeter, PatientCareDescriptionZone, DefibrillatorCartSnapPoint }

  /// <summary>
  /// 배치 데이터가 설치 완료 신호를 지정할 수 있는 벽면 설치 장비입니다.
  /// 이 신호를 프리팹 오버라이드로만 남기면 레이아웃을 다시 생성할 때 함께 지워지므로,
  /// 배치 데이터가 재생성 시점마다 이 인터페이스를 통해 값을 다시 주입한다.
  /// </summary>
  public interface IAttachCompletionSignalConfigurable
  {
    void SetAttachCompletionSignalForEditor(string signal);
  }

  [Serializable]
  public struct StaticEntityTransformDefinition
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
    /// <summary>
    /// WallSuction / Oxyflowmeter 를 설치 완료했을 때 올릴 시나리오 신호입니다.
    /// 비우면 신호를 올리지 않습니다. 그 밖의 타입에는 값을 넣을 수 없습니다.
    /// </summary>
    public string attachCompletionSignal;
  }
  [Serializable]
  public struct StaticEntityLayoutGroup
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
      if (string.IsNullOrWhiteSpace(identifier))
      { error = "Layout identifier is empty."; return false; }
      if (groups == null)
      { error = "Layout groups are null."; return false; }
      var identifiers = new HashSet<string>();
      var attachCompletionSignals = new HashSet<string>();
      foreach (var group in groups)
      {
        if (group.entities == null)
        { error = "A layout group has no entity list."; return false; }
        foreach (var entity in group.entities)
        {
          if (string.IsNullOrWhiteSpace(entity.identifier))
          { error = "An entity identifier is empty."; return false; }
          if (!identifiers.Add(entity.identifier.Trim()))
          { error = "Duplicate entity identifier: " + entity.identifier; return false; }
          if (entity.type == StaticEntityLayoutType.WallSuction && wallSuctionPrefab == null)
          { error = "WallSuction prefab is missing for " + entity.identifier; return false; }
          if (entity.type == StaticEntityLayoutType.Oxyflowmeter && oxyflowmeterPrefab == null)
          { error = "Oxyflowmeter prefab is missing for " + entity.identifier; return false; }
          if (string.IsNullOrWhiteSpace(entity.attachCompletionSignal))
            continue;
          if (!SupportsAttachCompletionSignal(entity.type))
          {
            error = "Attach completion signal is only valid for WallSuction/Oxyflowmeter: " + entity.identifier;
            return false;
          }
          // 신호 하나가 여러 장비에 걸리면 어느 쪽을 설치해도 같은 게이트가 열려,
          // 시나리오가 의도한 설치 지점을 구분할 수 없게 된다.
          if (!attachCompletionSignals.Add(entity.attachCompletionSignal.Trim()))
          {
            error = "Duplicate attach completion signal: " + entity.attachCompletionSignal;
            return false;
          }
        }
      }
      return true;
    }

    public static bool SupportsAttachCompletionSignal(StaticEntityLayoutType type)
      => type == StaticEntityLayoutType.WallSuction || type == StaticEntityLayoutType.Oxyflowmeter;

    private void OnValidate()
    {
      if (!Validate(out var error))
        Debug.LogWarning($"[StaticEntityLayoutDefinition] {name}: {error}", this);
    }
  }
}
