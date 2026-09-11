using System;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using TriageTrainer.Entity.Patient;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자 "트리아지(Triage) 분류" 인터랙션 부분 구현.
  ///
  /// <para>
  /// 플레이어는 환자 상태에 따라 트리아지를 분류(KTAS 등급 부여)할 수 있다. 이 동작은 기존 사정(Assess)과
  /// 별개의 전용 인터랙션(<see cref="PatientTriageInteract"/>)으로 노출되며, 상호작용 시 트리아지 평가
  /// UI(<see cref="TriageTrainer.UI.TriageAssessmentUIController"/>)를 열어 등급을 선택받고 그 결과를
  /// 환자에게 적용한다.
  /// </para>
  ///
  /// <para>
  /// 노출 여부는 <see cref="TriageAssessmentConfig.Assessable"/> 플래그로 제어되며, 시나리오는
  /// <see cref="SetTriageAssessable"/> 로 이 플래그를 켜고 끌 수 있다. 평가 완료 후의 재상호작용 정책은
  /// <see cref="ChangeAssessableOnAssessDone"/> 로 지정한다.
  /// </para>
  /// </summary>
  public partial class PatientController
  {
    private const float TriageInteractionDistance = 3f;
    /// <summary>
    /// 트리아지 평가 완료 후 인터랙션 재노출 정책.
    /// </summary>
    public enum ChangeAssessableOnAssessDone
    {
      /// <summary>한 번 평가하면 이후 항상 트리아지 인터랙션을 비활성화한다.</summary>
      DisableAssessable,

      /// <summary>평가 후에도 항상 트리아지 인터랙션을 유지(재평가 허용)한다.</summary>
      RemainAssessable,

      /// <summary>의도된 정답(intendedTriage)을 맞췄을 때만 비활성화하고, 오답이면 계속 노출한다.</summary>
      DisableOnIntendedOnly,
    }

    [Serializable]
    public struct TriageAssessmentConfig
    {
      [Tooltip("트리아지 인터랙션 활성화 플래그. true 여야 상호작용이 노출/가능하다(시나리오가 SetTriageAssessable 로 제어).")]
      [SerializeField] private bool _assessable;

      [Tooltip("평가 후 재상호작용 정책.")]
      [SerializeField] private ChangeAssessableOnAssessDone _changeAssessableOnAssessDone;

      public bool Assessable
      {
        get => _assessable;
        set => _assessable = value;
      }

      public ChangeAssessableOnAssessDone ChangeAssessableOnAssessDone => _changeAssessableOnAssessDone;

      /// <summary>기본 문구. 시나리오별 문구는 interactions 정의의 display.text 가 덮어쓴다.</summary>
      public string DisplayText => "트리아지 분류";

      public static TriageAssessmentConfig Default()
      {
        var cfg = new TriageAssessmentConfig();
        cfg._assessable = false;
        cfg._changeAssessableOnAssessDone = ChangeAssessableOnAssessDone.DisableAssessable;
        return cfg;
      }
    }

    private sealed class PatientTriageInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;

      public PatientTriageInteract(PatientController owner) { _owner = owner; }

      public string DisplayText
      {
        get
        {
          var name = _owner.GetPatientDisplayName(null);
          return !string.IsNullOrWhiteSpace(name)
            ? $"{name}{Josa.ObjectParticle(name)} 트리아지 분류"
            : _owner._triageConfig.DisplayText;
        }
      }
      // 환자 상호작용 힌트는 아이콘을 표시하지 않는다(투명 처리).
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => InteractIdTriage;

      public bool CanInteract(Transform interactor)
      {
        if (!_owner.EffectiveAssessable || !_owner.CanPerformTriageOrAssessment)
          return false;

        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return player != null;
      }

      public void Interact(Transform interactor)
      {
        string briefing = null;
        if (InteractionRegistry.TryGetDefinition(this, out var definition))
          briefing = definition.GetExtra("triageBriefing");
        _owner.BeginTriageAssessment(interactor, briefing);
      }
    }

    public const string InteractIdTriage = "triage_assess";

    [Header("Triage (트리아지 분류)")]
    [SerializeField] private TriageAssessmentConfig _triageConfig;

    // 플레이어가 평가한 트리아지 등급(서버 권위 + 전 피어 복제).
    private readonly SyncVar<TriageLevel> _assessedTriage = new SyncVar<TriageLevel>(TriageLevel.Unassessed);

    // 트리아지 인터랙션 활성화 여부(서버 권위 + 전 피어 복제). 인스펙터의 _triageConfig.Assessable 은
    // 초깃값으로만 쓰이고, 런타임 활성/비활성 상태는 이 SyncVar 가 단일 진실 공급원이 되어 모든 피어가
    // 동일한 게이팅을 보게 한다. _assessableInitialized 는 서버가 초깃값을 아직 반영하지 않았을 때
    // 인스펙터 값을 폴백으로 쓰기 위한 플래그다.
    private readonly SyncVar<bool> _assessable = new SyncVar<bool>(false);
    private bool _assessableInitialized;

    /// <summary>현재 이 환자에 부여된(평가된) 트리아지 등급. 미평가면 <see cref="TriageLevel.Unassessed"/>.</summary>
    public TriageLevel AssessedTriage => _assessedTriage.Value;

    /// <summary>이 환자의 의도된 트리아지 등급(정답).</summary>
    public TriageLevel IntendedTriage => _patientDescriptor != null ? _patientDescriptor.intendedTriage : TriageLevel.Unassessed;

    /// <summary>
    /// 트리아지 인터랙션이 현재 활성 상태인지(전 피어 일관, 외부 조회용).
    /// 서버가 초깃값을 반영하기 전에는 인스펙터 값으로 폴백한다.
    /// </summary>
    public bool EffectiveAssessable => _assessableInitialized ? _assessable.Value : _triageConfig.Assessable;

    /// <summary>트리아지 인터랙션 엔트리를 추가한다(BuildInteractEntries 에서 호출).</summary>
    private void AddTriageInteract()
    {
      _interacts.Add(new PatientTriageInteract(this));
    }

    private void InitializeTriageSync()
    {
      // 서술된 descriptor 초깃값을 SyncVar 에 반영(서버 컨텍스트에서만 권위 기록).
      _assessedTriage.OnChange += OnAssessedTriageChanged;
      _assessable.OnChange += OnTriageAssessableChanged;

      if (IsFishNetServerStarted)
      {
        // 서버가 인스펙터 초깃값을 권위 상태로 승격한다(전 피어 복제).
        _assessable.Value = _triageConfig.Assessable;
        _assessableInitialized = true;

        if (_patientDescriptor != null && _patientDescriptor.assessedTriage != TriageLevel.Unassessed)
          _assessedTriage.Value = _patientDescriptor.assessedTriage;
      }
      else
      {
        // 클라이언트는 SyncVar 로 복제된 서버 권위 값을 사용한다.
        _assessableInitialized = true;
      }
    }

    private void TeardownTriageSync()
    {
      _assessedTriage.OnChange -= OnAssessedTriageChanged;
      _assessable.OnChange -= OnTriageAssessableChanged;
    }

    private void OnAssessedTriageChanged(TriageLevel previous, TriageLevel next, bool asServer)
    {
      // descriptor 상태값을 동기화된 값과 일치시켜, 사후 평가/조회가 일관되게 한다.
      if (_patientDescriptor != null)
        _patientDescriptor.assessedTriage = next;

      UpdateTriageOverheadLabel(next);
    }

    private void OnTriageAssessableChanged(bool previous, bool next, bool asServer)
    {
      // 재시도 이벤트는 서버에서 발생한다. SyncVar 복제 후 각 클라이언트의 근처 상호작용
      // 캐시도 갱신해야 다시 열린 트리아지 항목이 즉시 표시된다.
      RefreshTriageInteractableHints();
    }

    private static void RefreshTriageInteractableHints()
    {
      var players = UnityEngine.Object.FindObjectsByType<PlayerController>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);
      foreach (var player in players)
      {
        if (player != null && player.IsOwner)
          player.RefreshInteractableHintsNow();
      }
    }

    /// <summary>
    /// 시나리오 진행에 따라 트리아지 인터랙션 노출을 켜고 끈다.
    ///
    /// <para>
    /// 호환 실행 경로에서 ByRole 브랜치(TriageAssessControl 노드)와 브랜치 이벤트는 배정된 클라이언트
    /// 한 곳에서만 실행된다. 담당자가 호스트가 아니면 이 피어는 서버 권위 SyncVar 를 쓸 수 없으므로
    /// 서버에 위임한다. 서버가 값을 갱신하면 요청자를 포함한 전 피어로 복제되고, 각 피어는 SyncVar
    /// OnChange 로 상호작용 힌트를 다시 그린다. 위임하지 않으면 담당자가 서버 호스트일 때만
    /// 인터랙션이 열리는 문제가 생긴다.
    /// </para>
    /// </summary>
    public void SetTriageAssessable(bool assessable)
    {
      // 인스펙터 초깃값도 갱신해 두어(서버가 아직 SyncVar 를 승격하기 전 폴백 일관성) 초기 상태가 어긋나지 않게 한다.
      _triageConfig.Assessable = assessable;

      if (IsFishNetServerStarted)
      {
        _assessable.Value = assessable;
        _assessableInitialized = true;
      }
      else if (IsFishNetClientInitialized)
      {
        CmdSetTriageAssessable(assessable);
      }

      RefreshTriageInteractableHints();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdSetTriageAssessable(bool assessable, NetworkConnection sender = null)
    {
      // 시나리오 노드가 보내는 요청이므로 거리 검사는 하지 않지만, 등록된 플레이어의 요청만 받는다.
      if (!TryResolveTriageRequester(sender, out _))
        return;

      SetAssessableAuthoritative(assessable);
      RefreshTriageInteractableHints();
    }

    /// <summary>
    /// 시나리오가 오답인 환자만 다시 분류시킬 때 사용한다. 이전 등급과 정답 제출로 닫힌
    /// 상호작용 상태를 함께 초기화해, 재시도 인터랙션이 확실히 다시 노출되게 한다.
    ///
    /// <para>
    /// SyncVar 쓰기는 서버(또는 오프라인) 컨텍스트에서만 수행한다. 이 메서드는 시나리오
    /// 이벤트에서 호출되고 시나리오 이벤트는 표시 전용 클라이언트에서도 실행되므로, 컨텍스트를
    /// 검사하지 않으면 서버 권위 값에 비권위 쓰기가 발생한다. 그러면 해당 피어만 미분류로
    /// 보이거나 오류가 기록되어, 재시도 인터랙션 노출 상태가 피어마다 갈라진다.
    /// 호환 실행 경로에서는 재시도 이벤트가 담당 클라이언트 한 곳에서만 실행되므로, 클라이언트는
    /// <see cref="SetTriageAssessable"/> 과 같은 이유로 서버에 초기화를 위임한다.
    /// </para>
    /// </summary>
    public void ResetTriageAssessmentForRetry()
    {
      bool hasAuthority = IsFishNetServerStarted || InstanceFinder.IsOffline;

      _triageConfig.Assessable = true;

      if (hasAuthority)
        ApplyTriageRetryResetAuthoritative();
      else if (IsFishNetClientInitialized)
        CmdResetTriageAssessmentForRetry();

      // 표시 갱신은 모든 피어에서 수행한다. 권위 값은 서버가 복제하며, 클라이언트는
      // 복제된 값이 도착하면 그에 맞춰 다시 갱신된다.
      UpdateTriageOverheadLabel(hasAuthority ? TriageLevel.Unassessed : AssessedTriage);
      RefreshTriageInteractableHints();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdResetTriageAssessmentForRetry(NetworkConnection sender = null)
    {
      if (!TryResolveTriageRequester(sender, out _))
        return;

      _triageConfig.Assessable = true;
      ApplyTriageRetryResetAuthoritative();
      UpdateTriageOverheadLabel(TriageLevel.Unassessed);
      RefreshTriageInteractableHints();
    }

    // 서버(또는 오프라인) 컨텍스트: 이전 등급과 활성화 상태를 권위 값으로 초기화한다.
    private void ApplyTriageRetryResetAuthoritative()
    {
      _assessedTriage.Value = TriageLevel.Unassessed;
      _assessable.Value = true;
      _assessableInitialized = true;

      if (_patientDescriptor != null)
        _patientDescriptor.assessedTriage = TriageLevel.Unassessed;
    }

    /// <summary>
    /// 트리아지 평가를 시작한다(상호작용한 플레이어에게 트리아지 UI 를 연다).
    /// UI 는 로컬 플레이어 클라이언트에서만 열리며, 선택 결과는 <see cref="SubmitTriageAssessment"/> 로 반영된다.
    /// </summary>
    private void BeginTriageAssessment(Transform interactor, string briefing = null)
    {
      if (!EffectiveAssessable || !CanPerformTriageOrAssessment)
        return;

      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null || !player.IsOwner)
        return;

      void OpenAssessmentUI()
      {
        var ui = TriageTrainer.UI.TriageAssessmentUIController.ActiveInstance;
        if (ui == null)
        {
          Debug.LogWarning("[PatientController] TriageAssessmentUIController 를 찾을 수 없어 트리아지 UI 를 열 수 없습니다.", this);
          return;
        }

        ui.Open(AssessedTriage, selected => SubmitTriageAssessment(selected));
      }

      if (string.IsNullOrWhiteSpace(briefing))
      {
        OpenAssessmentUI();
        return;
      }

      var dialogue = MultiplayerInfrastructure.Registry.Registry.Get<
        MultiplayerInfrastructure.UI.DialoguePanelUIController>(
        MultiplayerInfrastructure.Registry.RegistryType.UI,
        MultiplayerInfrastructure.Registry.Registry.TypeKey<MultiplayerInfrastructure.UI.DialoguePanelUIController>());
      if (dialogue != null && dialogue.TryPresentTransientDialogue("시스템", briefing, 4f, onFinished: OpenAssessmentUI))
        return;

      OpenAssessmentUI();
    }

    /// <summary>
    /// 트리아지 UI 에서 선택된 등급을 환자에 적용한다(네트워크 전파). 로컬 플레이어 클라이언트에서 호출된다.
    /// </summary>
    public void SubmitTriageAssessment(TriageLevel level)
    {
      if (!CanPerformTriageOrAssessment || !IsValidAssessedTriage(level))
        return;

      SetAssessedTriageNetworked(level);
    }

    // 유효한 평가 등급인지(정의된 enum 값이며 미분류가 아님). 서버/클라이언트 양쪽에서 방어한다.
    private static bool IsValidAssessedTriage(TriageLevel level)
    {
      return level != TriageLevel.Unassessed && Enum.IsDefined(typeof(TriageLevel), level);
    }

    private void SetAssessedTriageNetworked(TriageLevel level)
    {
      if (IsFishNetServerStarted)
      {
        ApplyAssessedTriage(level);
      }
      else if (IsFishNetClientInitialized)
      {
        CmdSetAssessedTriage(level);
      }
      else
      {
        // 오프라인/단독 실행 → 로컬만 적용.
        ApplyAssessedTriageLocalOnly(level);
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdSetAssessedTriage(TriageLevel level, NetworkConnection sender = null)
    {
      if (!TryResolveTriageRequester(sender, out var requester)
          || (requester.transform.position - transform.position).sqrMagnitude
          > TriageInteractionDistance * TriageInteractionDistance)
        return;
      ApplyAssessedTriage(level);
    }

    private static bool TryResolveTriageRequester(
      NetworkConnection sender,
      out PlayerController requester)
    {
      requester = null;
      if (sender == null || !sender.IsValid
          || !MultiplayerInfrastructure.Registry.Registry.TryGetEntityByClientId(
            sender.ClientId, out var descriptor)
          || descriptor?.GameObject == null)
        return false;

      requester = descriptor.GameObject.GetComponent<PlayerController>()
                  ?? descriptor.GameObject.GetComponentInChildren<PlayerController>(true);
      return requester != null
             && requester.Owner != null
             && requester.Owner.IsValid
             && requester.Owner.ClientId == sender.ClientId;
    }

    // 서버 컨텍스트: 상태값을 갱신(SyncVar 로 전 피어 복제)하고, 완료 후 재노출 정책을 적용한다.
    // 클라이언트 신뢰 없이 서버에서 권위 검증(활성화 게이트 + enum 값)을 재수행한다.
    private void ApplyAssessedTriage(TriageLevel level)
    {
      // 서버 권위 게이트: 현재 평가 가능 상태가 아니면 거부(클라이언트 게이트 우회 방지).
      if (!EffectiveAssessable || !CanPerformTriageOrAssessment)
        return;

      // 값 검증: 정의되지 않은 enum/미분류 값 거부(조작된 RPC 방지).
      if (!IsValidAssessedTriage(level))
        return;

      _assessedTriage.Value = level;
      if (_patientDescriptor != null)
        _patientDescriptor.assessedTriage = level;

      // SyncVar OnChange 는 값이 실제로 변경된 경우에만 발화한다(같은 값이면 무시).
      // 서버 측 라벨 업데이트를 OnChange 경유 없이 직접 호출해 누락을 방지한다.
      UpdateTriageOverheadLabel(level);

      ApplyAssessableChangePolicy(level);

      // 세분화 상태 이벤트(OnTriageSubmitted) + 시나리오 바인딩 디스패치.
      // 서버 권위 컨텍스트에서 발생하므로 시나리오 신호 Raise 가 서버 권위로 동작한다.
      RaiseTriageSubmittedEvent(level);
    }

    // 재노출 정책은 서버 권위 SyncVar(_assessable)를 통해 전 피어에 일관되게 반영한다.
    private void ApplyAssessableChangePolicy(TriageLevel level)
    {
      switch (_triageConfig.ChangeAssessableOnAssessDone)
      {
        case ChangeAssessableOnAssessDone.DisableAssessable:
          SetAssessableAuthoritative(false);
          break;

        case ChangeAssessableOnAssessDone.RemainAssessable:
          // 유지: 아무 것도 하지 않는다.
          break;

        case ChangeAssessableOnAssessDone.DisableOnIntendedOnly:
          if (level == IntendedTriage)
            SetAssessableAuthoritative(false);
          break;
      }
    }

    // 활성/비활성 상태를 컨텍스트에 맞게 반영한다. 서버면 SyncVar 로 복제, 오프라인이면 로컬 폴백만 갱신.
    private void SetAssessableAuthoritative(bool assessable)
    {
      _triageConfig.Assessable = assessable;
      if (IsFishNetServerStarted)
      {
        _assessable.Value = assessable;
        _assessableInitialized = true;
      }
    }

    // 오프라인 전용 로컬 적용(SyncVar 를 직접 못 쓰는 컨텍스트 대비).
    private void ApplyAssessedTriageLocalOnly(TriageLevel level)
    {
      if (!IsValidAssessedTriage(level))
        return;

      if (_patientDescriptor != null)
        _patientDescriptor.assessedTriage = level;

      ApplyAssessableChangePolicy(level);
      UpdateTriageOverheadLabel(level);

      // 오프라인 컨텍스트에서도 세분화 상태 이벤트를 발생시킨다.
      RaiseTriageSubmittedEvent(level);
    }

    // ── 인게임 오버헤드 태그(환자 위 트리아지 표기) ──

    [Tooltip("트리아지 오버헤드 태그를 띄울 앵커(미지정 시 환자 루트 Transform 사용). 예: 머리 위 빈 오브젝트.")]
    [SerializeField] private Transform _triageLabelAnchor;

    private Transform TriageLabelAnchor => _triageLabelAnchor != null ? _triageLabelAnchor : transform;

    /// <summary>
    /// 평가된 트리아지 등급을 환자 위 오버헤드 라벨(색상 사각형 + 명칭)로 표기한다.
    /// 미평가(Unassessed)면 라벨을 제거한다.
    /// </summary>
    private void UpdateTriageOverheadLabel(TriageLevel level)
    {
      var ui = ResolveOverheadLabelUI();
      if (ui == null)
        return;

      if (level == TriageLevel.Unassessed)
      {
        ui.RemoveLabel(TriageLabelAnchor);
        return;
      }

      var content = new MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.LabelContent(
        TriageLevelInfo.GetColor(level),
        TriageLevelInfo.GetDisplayName(level),
        TriageLevelInfo.GetColor(level));  // 어두운 배경 위이므로 swatch 색을 텍스트에도 입힌다.

      ui.SetLabel(TriageLabelAnchor, content);
    }

    private void DestroyTriageOverheadLabel()
    {
      var ui = MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.ActiveInstance;
      ui?.RemoveLabel(TriageLabelAnchor);
    }

    // ActiveInstance 가 없으면 씬 내 컴포넌트를 직접 탐색해 폴백으로 사용한다.
    // (씬에 EntityOverheadLabelUIController 가 배치되지 않은 경우 경고를 출력한다.)
    private static MultiplayerInfrastructure.UI.EntityOverheadLabelUIController _cachedOverheadLabelUI;

    private static MultiplayerInfrastructure.UI.EntityOverheadLabelUIController ResolveOverheadLabelUI()
    {
      var instance = MultiplayerInfrastructure.UI.EntityOverheadLabelUIController.ActiveInstance;
      if (instance != null)
        return instance;

      // ActiveInstance 가 없으면 씬 탐색으로 폴백(캐시하여 매 호출마다 탐색하지 않는다).
      if (_cachedOverheadLabelUI != null)
        return _cachedOverheadLabelUI;

      _cachedOverheadLabelUI = UnityEngine.Object.FindFirstObjectByType<MultiplayerInfrastructure.UI.EntityOverheadLabelUIController>();
      if (_cachedOverheadLabelUI == null)
      {
        Debug.LogWarning("[PatientController] EntityOverheadLabelUIController 를 씬에서 찾을 수 없습니다. " +
                         "씬에 EntityOverheadLabelUIController + UIDocument 컴포넌트를 배치하세요.");
      }
      return _cachedOverheadLabelUI;
    }
  }
}
