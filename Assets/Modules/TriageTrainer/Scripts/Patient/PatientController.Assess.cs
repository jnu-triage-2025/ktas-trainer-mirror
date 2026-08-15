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

      [Tooltip("이 사정을 수행했을 때 올릴 시나리오 신호 조건명(sig.* 게이팅용). 예: check_avpu_gcs_patient_a, check_pulse_patient_a.")]
      [SerializeField] private string _assessSignal;

      [SerializeField] private bool _enabled = true;

      public string Identifier => _identifier;
      public string DisplayText => _displayText;
      public string AssessSignal => _assessSignal;
      public bool Enabled { get => _enabled; set => _enabled = value; }

      /// <summary>
      /// 코드 기본값(런타임 전용) config 를 구성한다. AssessSignal 은 비워 두어 식별자 규칙 기반
      /// 기본 신호(<see cref="ResolveDefaultAssessSignal"/>)가 적용되게 한다.
      /// </summary>
      internal void InitializeRuntimeDefault(string identifier, string displayText)
      {
        _identifier = identifier;
        _displayText = displayText;
        _assessSignal = null;
        _enabled = true;
      }
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
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      public bool CanInteract(Transform interactor)
      {
        var cfg = Config;
        if (cfg == null || !cfg.Enabled || !_owner.CanPerformTriageOrAssessment)
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

    /// <summary>표준 사정 동작의 코드 기본값(식별자 → 표시문구). Reset/미설정 시에도 사정 인터랙션이 노출되도록 한다.</summary>
    private static readonly (string Id, string DisplayText)[] DefaultAssessActions =
    {
      ("assess_avpu_gcs", "의식상태 사정(AVPU/GCS)"),
      ("assess_pulse", "맥박 확인"),
      ("assess_gcs", "GCS 재사정"),
      ("assess_vital", "활력징후 사정"),
    };

    /// <summary>
    /// B Male/Female 프리팹에 저장되어 있던 사정 항목의 직렬화 기본값을 복원한다.
    /// 사정은 시나리오가 활성화하기 전까지 모두 비활성으로 시작한다.
    /// </summary>
    private void ApplySerializedDefaultAssessActions()
    {
      _assessActions = new List<AssessActionConfig>(DefaultAssessActions.Length);
      for (int i = 0; i < DefaultAssessActions.Length; i++)
      {
        var definition = DefaultAssessActions[i];
        var config = new AssessActionConfig();
        config.InitializeRuntimeDefault(definition.Id, definition.DisplayText);
        config.Enabled = false;
        _assessActions.Add(config);
      }
    }

    /// <summary>
    /// 등록된 사정 동작들을 IInteract 엔트리로 추가한다(BuildInteractEntries 에서 호출).
    /// 인스펙터에 없는 표준 사정 동작은 코드 기본값으로 보충한다(직렬화 필드를 건드리지 않으므로 Reset 무관).
    /// </summary>
    private void AddAssessInteracts()
    {
      RebuildAssessActionMap();

      // 인스펙터에 명시된 사정 동작.
      var added = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < _assessActions.Count; i++)
      {
        var each = _assessActions[i];
        if (each == null || string.IsNullOrWhiteSpace(each.Identifier))
          continue;

        _interacts.Add(new PatientAssessInteract(this, each.Identifier));
        added.Add(each.Identifier);
      }

      // 미설정 표준 사정 동작을 코드 기본값으로 보충(런타임 전용 맵, 직렬화 안 함).
      for (int i = 0; i < DefaultAssessActions.Length; i++)
      {
        var def = DefaultAssessActions[i];
        if (added.Contains(def.Id))
          continue;

        // 런타임 기본 config 를 맵에 등록(PerformAssess 의 신호 폴백이 동작하도록).
        if (!_assessActionMap.ContainsKey(def.Id))
          _assessActionMap[def.Id] = MakeDefaultAssessConfig(def.Id, def.DisplayText);

        _interacts.Add(new PatientAssessInteract(this, def.Id));
      }
    }

    private static AssessActionConfig MakeDefaultAssessConfig(string id, string displayText)
    {
      var cfg = new AssessActionConfig();
      cfg.InitializeRuntimeDefault(id, displayText);
      return cfg;
    }

    private void PerformAssess(string actionIdentifier)
    {
      var cfg = GetAssessAction(actionIdentifier);
      if (cfg == null || !cfg.Enabled || !CanPerformTriageOrAssessment)
        return;

      // 인스펙터 AssessSignal 우선, 없으면 식별자 규칙 기반 코드 기본값(Reset 무관)으로 폴백.
      string signal = !string.IsNullOrWhiteSpace(cfg.AssessSignal)
          ? cfg.AssessSignal
          : ResolveDefaultAssessSignal(actionIdentifier);

      if (!string.IsNullOrWhiteSpace(signal))
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(signal);
    }

    /// <summary>시나리오 진행에 따라 특정 사정 동작의 노출을 켜고 끈다.</summary>
    public void SetAssessActionEnabled(string identifier, bool enabled)
    {
      var cfg = GetAssessAction(identifier);
      if (cfg != null)
        cfg.Enabled = enabled;
    }

    // ── 사정 기본 신호(코드 하드코딩): 사정 동작 식별자 → 신호 템플릿 ──
    // AssessSignal 이 비어 있을 때 적용된다. {id} 는 환자 Identifier 로 치환(ResolveSignalTemplate, TreatmentDisplay.cs).
    private static readonly Dictionary<string, string> DefaultAssessSignals = new(StringComparer.Ordinal)
    {
      { "assess_avpu_gcs", "check_avpu_gcs_{id}" },
      { "assess_pulse", "check_pulse_{id}" },
      { "assess_gcs", "check_gcs_{id}" },
      { "assess_vital", "check_vital_{id}" },
    };

    private string ResolveDefaultAssessSignal(string actionIdentifier)
    {
      if (string.IsNullOrWhiteSpace(actionIdentifier))
        return null;

      return DefaultAssessSignals.TryGetValue(actionIdentifier, out var template)
          ? ResolveSignalTemplate(template)
          : null;
    }
  }
}
