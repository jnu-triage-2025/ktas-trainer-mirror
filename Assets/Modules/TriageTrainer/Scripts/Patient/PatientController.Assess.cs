using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자 "사정(Assess)" 인터랙션 부분 구현.
  ///
  /// <para>
  /// 의식상태(AVPU/GCS)·활력징후·맥박 확인처럼 "환자를 클릭해 사정을 수행"하는 동작을, 데이터 기반
  /// 인터랙션으로 노출하고 완료 시 시나리오 게이팅 신호(sig.check_*)를 올린다. 별도 사정 UI 메커닉 없이
  /// 기존 환자 인터랙션 패턴(<see cref="IInteract"/>)을 재사용한다.
  /// </para>
  ///
  /// 운영자는 인스펙터의 Assess Actions 에 (표시문구, 신호명)을 등록한다. 시나리오 진행에 따라
  /// <see cref="SetAssessActionEnabled"/> 로 특정 사정 동작만 노출되도록 제어할 수 있다.
  /// </summary>
  public partial class PatientController
  {
    [Serializable]
    public class AssessActionConfig
    {
      [Tooltip("사정 동작 식별자(중복 불가). 예: assess_avpu_gcs, assess_pulse, assess_vital.")]
      [SerializeField] private string _identifier;

      [Tooltip("상호작용 힌트에 표시할 문구. 예: 의식상태 사정, 맥박 확인.")]
      [SerializeField] private string _displayText = "사정";

      [SerializeField] private Sprite _displayIcon = null;

      [Tooltip("이 사정을 수행했을 때 올릴 시나리오 신호 조건명(sig.* 게이팅용). 예: check_avpu_gcs_patient_a, check_pulse_patient_a.")]
      [SerializeField] private string _assessSignal;

      [SerializeField] private bool _enabled = true;

      public string Identifier => _identifier;
      public string DisplayText => _displayText;
      public Sprite DisplayIcon => _displayIcon;
      public string AssessSignal => _assessSignal;
      public bool Enabled { get => _enabled; set => _enabled = value; }
    }

    private sealed class PatientAssessInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;
      private readonly string _actionIdentifier;

      public PatientAssessInteract(PatientController owner, string actionIdentifier)
      {
        _owner = owner;
        _actionIdentifier = actionIdentifier;
      }

      private AssessActionConfig Config => _owner.GetAssessAction(_actionIdentifier);

      public string DisplayText => Config?.DisplayText ?? "사정";
      public Sprite DisplayIcon => Config?.DisplayIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        var cfg = Config;
        if (cfg == null || !cfg.Enabled)
          return false;

        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return player != null;
      }

      public void Interact(Transform interactor)
      {
        _owner.PerformAssess(_actionIdentifier);
      }
    }

    [Header("Assess Actions (환자 사정)")]
    [SerializeField] private List<AssessActionConfig> _assessActions = new();

    private readonly Dictionary<string, AssessActionConfig> _assessActionMap = new(StringComparer.Ordinal);

    private void RebuildAssessActionMap()
    {
      _assessActionMap.Clear();
      for (int i = 0; i < _assessActions.Count; i++)
      {
        var each = _assessActions[i];
        if (each == null || string.IsNullOrWhiteSpace(each.Identifier))
          continue;

        _assessActionMap[each.Identifier] = each;
      }
    }

    private AssessActionConfig GetAssessAction(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return null;

      return _assessActionMap.TryGetValue(identifier, out var cfg) ? cfg : null;
    }

    /// <summary>등록된 사정 동작들을 IInteract 엔트리로 추가한다(BuildInteractEntries 에서 호출).</summary>
    private void AddAssessInteracts()
    {
      RebuildAssessActionMap();
      for (int i = 0; i < _assessActions.Count; i++)
      {
        var each = _assessActions[i];
        if (each == null || string.IsNullOrWhiteSpace(each.Identifier))
          continue;

        _interacts.Add(new PatientAssessInteract(this, each.Identifier));
      }
    }

    private void PerformAssess(string actionIdentifier)
    {
      var cfg = GetAssessAction(actionIdentifier);
      if (cfg == null || !cfg.Enabled)
        return;

      if (!string.IsNullOrWhiteSpace(cfg.AssessSignal))
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(cfg.AssessSignal);
    }

    /// <summary>시나리오 진행에 따라 특정 사정 동작의 노출을 켜고 끈다.</summary>
    public void SetAssessActionEnabled(string identifier, bool enabled)
    {
      var cfg = GetAssessAction(identifier);
      if (cfg != null)
        cfg.Enabled = enabled;
    }
  }
}
