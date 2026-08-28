using System.Collections.Generic;
using System.Text.Json;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.Entity.LineConnection;
using TriageTrainer.ItemDefinitions;
using TriageTrainer.Patient;
using UnityEngine;
using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 처치 시각 표현(show/hide) + 처치/사용/사정 신호의 컨트롤러측 적용 로직.
  ///
  /// <para>설계 원칙</para>
  /// <list type="bullet">
  /// <item><b>데이터</b>: <see cref="PatientDisplayState"/>(플래그 <c>DisplayState</c> + 자식 GameObject 참조
  ///   <c>ChildGameObjects</c>)는 데이터만 보유한다.</item>
  /// <item><b>적용</b>: 본 컨트롤러가 신호/아이템 사용을 받아 해당 플래그를 켜고
  ///   <c>ChildGameObjects</c> 의 대응 GameObject 를 <c>SetActive</c> 한다(부위 구분은 환자 프리팹
  ///   hierarchy 가 이미 반영하므로 컨트롤러는 플래그만 켠다).</item>
  /// </list>
  ///
  /// 매핑(아이템→처치플래그/신호)은 <b>코드 하드코딩</b>이며 컴포넌트 Reset 과 무관하다.
  /// </summary>
  public partial class PatientController
  {
    public const string TreatmentGauze = "gauze";
    public const string TreatmentPlasterOnGauze = "plaster_on_gauze";
    public const string TreatmentPlasterOnIntubation = "plaster_on_intubation";

    [Header("Treatment State")]
    [SerializeField] private PatientTreatmentState _treatmentState = new();

    public PatientTreatmentState TreatmentState => _treatmentState ??= new PatientTreatmentState();

    [Header("AED Connection")]
    [Tooltip("제세동 패드 쪽 AED 라인 연결 지점들입니다. 비워 두면 자식 오브젝트에서 AEDLineConnectionPoint 를 찾는 fallback 을 사용합니다.")]
    [SerializeField]
    private TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint[] _aedConnectionPoints =
      System.Array.Empty<TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint>();

    /// <summary>
    /// 제세동 패드와 카트를 잇는 AED 라인의 패드 측 연결 지점 목록.
    /// 참조가 비어 있으면(배선 누락) 자식 오브젝트에서 클래스 기준으로 찾는 fallback 을 수행한다.
    /// </summary>
    public IReadOnlyList<TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint> AedConnectionPoints
    {
      get
      {
        if (_aedConnectionPoints == null || _aedConnectionPoints.Length == 0)
          _aedConnectionPoints = GetComponentsInChildren<TriageTrainer.Entity.AEDLine.AEDLineConnectionPoint>(true);
        return _aedConnectionPoints;
      }
    }

    /// <summary>
    /// 처치 시각 표현 항목. <see cref="PatientTreatmentDisplayModel"/> / <see cref="PatientTreatmentDisplayingChildGameObjects"/>
    /// 의 필드와 1:1 대응한다.
    /// </summary>
    public enum TreatmentDisplay
    {
      None = 0,
      Syringe18GInsertedIntoLeftArm,
      Syringe18GInsertedIntoRightArm,
      Syringe20GInsertedIntoLeftArm,
      Syringe20GInsertedIntoRightArm,
      CentralVenousCatheterInsertedIntoSubclavian,
      LaryngoscopeInserted,
      EndotrachealTubeStyletInserted,
      EndotrachealTubeInsertDone,
      TPieceAttachedToNasalCannula,
      AmbuBagAttachedToEndotrachealTube,
      GauzePatchedOnThorax,
      GauzeDressingDoneOnThorax,
      GauzePatchedOnRightArm,
      GauzeDressingDoneOnRightArm,
      GauzePatchedOnLeftArm,
      GauzeDressingDoneOnLeftArm,
      GauzePatchedOnRightEyebrow,
      GauzeDressingDoneOnRightEyebrow,
      GauzePatchedOnLeftEyebrow,
      GauzeDressingDoneOnLeftEyebrow,
      NasalCannulaApplied,
      CervicalCollarOnNeck,
      IntravenousStandAttached,
      IntravenousHangerAttached,
      IntravenousFluidAttached
    }

    /// <summary>
    /// 아이템을 환자에게 사용했을 때의 효과(켤 처치 표현 + 올릴 신호)를 정의하는 코드 하드코딩 항목.
    /// </summary>
    private readonly struct ItemUseEffect
    {
      public readonly TreatmentDisplay Display;
      /// <summary>{id} 는 환자 Identifier 로 치환된다.</summary>
      public readonly string[] SignalTemplates;

      public ItemUseEffect(TreatmentDisplay display, params string[] signalTemplates)
      {
        Display = display;
        SignalTemplates = signalTemplates;
      }
    }

    // ── 공유 효과 정의(별칭 키가 동일 인스턴스를 참조하여 드리프트를 방지) ──
    private static readonly ItemUseEffect CervicalCollarEffect =
      new(TreatmentDisplay.CervicalCollarOnNeck, "apply_stabilizer_{id}");
    private static readonly ItemUseEffect NasalCannulaEffect =
      new(TreatmentDisplay.NasalCannulaApplied, "apply_nasal_cannula");

    // ── 아이템 식별자 → 효과(처치표현 + 신호) 기본 매핑(하드코딩, Reset 무관) ──
    //
    // 부위가 환자별로 고정되어 있고(프리팹 hierarchy 반영) 컨트롤러는 플래그만 켜면 되므로,
    // 거즈/플라스터는 흉부 기준 표현을 기본으로 둔다(다른 부위가 필요한 환자는 해당 플래그를
    // 추가 매핑하거나 향후 부위 조준으로 확장). 신호는 시나리오 게이트 조건명과 일치시킨다.
    //
    // 환자별 결과 신호(SIGNAL-BC-3): 다수 환자가 같은 처치를 받는 흐름(B/C)에서는 공용 sticky 신호
    // (예: apply_gauze) 하나로는 B가 올린 신호로 C 게이트가 무행동 통과하는 문제가 있다. 동적
    // EntityStateSignalBinding도 유지하지만, 처치 단계가 실제로 전환된 권위 경로에서 환자별 신호를
    // 직접 올려 바인딩 등록 시점에 따라 퀘스트 완료가 누락되지 않게 한다.
    private static readonly Dictionary<string, ItemUseEffect> ItemUseEffects = new()
    {
      // 부착형(시각 표현 동반)
      { "gauze",          new ItemUseEffect(TreatmentDisplay.GauzePatchedOnThorax, "apply_gauze") },
      // plaster는 거즈/기관삽관 순서 조건에 따라 아래 ApplyItemUse에서 한 효과만 선택한다.
      { "plaster",        new ItemUseEffect(TreatmentDisplay.GauzeDressingDoneOnThorax) },
      { "sterile_gloves", new ItemUseEffect(TreatmentDisplay.None, "wear_glove", "wear_glove_{id}") },
      { "contaminated_gloves", new ItemUseEffect(TreatmentDisplay.None, "wear_glove", "wear_glove_{id}") },
      // 실제 아이템 식별자(cervical_collar / nasalcannula)가 프로덕션 경로의 키.
      // 구 명칭(neckstabilizer / nasal)은 디버그 훅(Debug_ApplyItemUse) 호환용 별칭이며,
      // 반드시 동일 인스턴스를 공유해 신호/표현이 갈라지지 않게 한다.
      { "cervical_collar", CervicalCollarEffect },
      { "neckstabilizer",  CervicalCollarEffect },
      { "electrode",      new ItemUseEffect(TreatmentDisplay.None, "apply_electrode", "apply_electrode_{id}") },
      { "nasalcannula",   NasalCannulaEffect },
      { "nasal_cannula", NasalCannulaEffect },
      { "nasal",          NasalCannulaEffect },

      // 사용형(시각 표현 없음 또는 별도 이벤트가 표현 담당)
      { "yankauer",            new ItemUseEffect(TreatmentDisplay.None, "suction_{id}") },
      { "yankauer_suction_ready", new ItemUseEffect(TreatmentDisplay.None, "suction_{id}") },
      { "yankauer_ready",      new ItemUseEffect(TreatmentDisplay.None, "suction_{id}") },
      { "ambubag",             new ItemUseEffect(TreatmentDisplay.AmbuBagAttachedToEndotrachealTube, "start_ambu") },
      { "epinephrine_ampule",  new ItemUseEffect(TreatmentDisplay.None, "push_epi") },
      { "normal_saline_20ml",  new ItemUseEffect(TreatmentDisplay.None, "push_ns") },
    };

    // 환자에게 적용하는 처치가 아니므로 환자 대상 item_apply 후보에서 제외하는 아이템이다.
    // 장갑은 장비 슬롯 착용 경로가 wear_glove 신호를 담당한다. 앰플과 생리식염수 20ml, 조립 전
    // 양커 팁은 조합 재료여서, 환자에게 그대로 사용하면 조합 재료가 사라지거나 조립·연결 절차를
    // 건너뛰고 처치 신호가 발신된다. 조립된 양커 팁(yankauer_suction_ready)은 더 이상 손에 들고
    // 환자에게 적용하는 방식이 아니라, 흡인기와 라인 연결된 상태에서 전용 상호작용
    // (<see cref="InteractIdWallSuctionUse"/>)으로만 사용하므로 여기서도 제외한다. 우클릭 아이템
    // 사용 경로(CanApplyItemUse)의 기존 동작은 그대로 두고, 상호작용 후보에서만 제외한다.
    private static readonly HashSet<string> NonPatientApplicationItems = new(System.StringComparer.Ordinal)
    {
      "sterile_gloves",
      "contaminated_gloves",
      "epinephrine_ampule",
      "normal_saline_20ml",
      "yankauer",
      "yankauer_ready",
      "yankauer_suction_ready",
    };

    // 조합 완제품 주사기는 ItemUseEffects 가 아니라 ApplyItemUse 의 회차별 분기가 처리하므로,
    // 손에 들지 않고 인벤토리에만 있는 경우에도 찾을 수 있도록 후보를 따로 열거한다.
    private static readonly string[] AdditionalPatientApplicationItems =
    {
      Epinephrine5ccSyringe.Identifier,
      NormalSaline20ccSyringe.Identifier,
    };

    private int _resuscitationMedicationRound = 1;

    /// <summary>동일한 조합 주사기를 사용하는 소생술 투여 회차를 전환합니다.</summary>
    public void SetResuscitationMedicationRound(int round)
    {
      _resuscitationMedicationRound = Mathf.Max(1, round);
    }

    /// <summary>
    /// 아이템 사용을 처리한다: (1) 매핑된 처치 표현을 켜고, (2) 매핑된 신호를 올린다.
    /// 매핑이 없으면 아무 것도 하지 않는다.
    /// </summary>
    private bool CanApplyItemUse(string itemIdentifier)
    {
      if (string.Equals(itemIdentifier, Epinephrine5ccSyringe.Identifier, System.StringComparison.Ordinal)
          || string.Equals(itemIdentifier, NormalSaline20ccSyringe.Identifier, System.StringComparison.Ordinal))
        return IsPatientA;

      return TryResolveItemUse(itemIdentifier, out _, out _, out _);
    }

    private bool ApplyItemUse(string itemIdentifier)
    {
      // 조합 완료 주사기는 동일 아이템을 1·2차 투여에 재사용하므로 현재 소생술 회차에
      // 맞는 신호를 동적으로 발신한다. 두 회차 신호를 동시에 올리면 후속 게이트가
      // 실제 재투여 없이 통과하므로 반드시 한 회차만 발신한다.
      if (string.Equals(itemIdentifier, Epinephrine5ccSyringe.Identifier, System.StringComparison.Ordinal))
      {
        if (!IsPatientA)
          return false;
        MI.Scenario.ScenarioInteractionSignals.Raise($"push_epi_r{_resuscitationMedicationRound}");
        return true;
      }

      if (string.Equals(itemIdentifier, NormalSaline20ccSyringe.Identifier, System.StringComparison.Ordinal))
      {
        if (!IsPatientA)
          return false;
        MI.Scenario.ScenarioInteractionSignals.Raise($"push_ns_r{_resuscitationMedicationRound}");
        return true;
      }

      if (!TryResolveItemUse(itemIdentifier, out var effect, out string treatmentIdentifier,
            out TreatmentDisplay resolvedDisplay))
        return false;

      if (!SetTreatmentApplied(treatmentIdentifier, true, resolvedDisplay))
        return false;

      bool raised = false;
      if (effect.SignalTemplates != null)
      {
        for (int i = 0; i < effect.SignalTemplates.Length; i++)
        {
          var signal = ResolveSignalTemplate(effect.SignalTemplates[i]);
          if (string.IsNullOrWhiteSpace(signal))
            continue;

          MI.Scenario.ScenarioInteractionSignals.Raise(signal);
          raised = true;
        }
      }

      bool applied = raised || resolvedDisplay != TreatmentDisplay.None;
      if (applied)
      {
        RaiseGenericItemAppliedSignal(itemIdentifier, treatmentIdentifier);
        NotifyPatientBCItemApplied(itemIdentifier);
      }
      return applied;
    }

    private bool TryResolveItemUse(
      string itemIdentifier,
      out ItemUseEffect effect,
      out string treatmentIdentifier,
      out TreatmentDisplay resolvedDisplay)
    {
      effect = default;
      treatmentIdentifier = itemIdentifier;
      resolvedDisplay = TreatmentDisplay.None;
      if (string.IsNullOrWhiteSpace(itemIdentifier)
          || !CanApplyPatientBCItem(itemIdentifier)
          || !ItemUseEffects.TryGetValue(itemIdentifier, out effect))
        return false;

      if (string.Equals(itemIdentifier, "plaster", System.StringComparison.Ordinal))
      {
        // B/C의 단계 SyncVar는 모든 피어가 공유하는 권위 순서 상태다. 클라이언트의 로컬
        // TreatmentState 복제 시점에 의존하지 않고 AwaitingPlaster 단계에서 메뉴를 연다.
        if ((IsPatientBC && _patientBCNurseDStage.Value == PatientBCTreatmentStage.AwaitingPlaster)
            || (!IsPatientBC && TreatmentState.IsApplied(TreatmentGauze)
                && !TreatmentState.IsApplied(TreatmentPlasterOnGauze)))
        {
          treatmentIdentifier = TreatmentPlasterOnGauze;
          effect = new ItemUseEffect(
            TreatmentDisplay.GauzeDressingDoneOnThorax,
            IsPatientBC ? null : "apply_plaster_on_gauze");
        }
        else if (IsPatientA
                 && IsDisplayActive(TreatmentDisplay.EndotrachealTubeInsertDone)
                 && !TreatmentState.IsApplied(TreatmentPlasterOnIntubation))
        {
          treatmentIdentifier = TreatmentPlasterOnIntubation;
          effect = new ItemUseEffect(TreatmentDisplay.None, "apply_plaster_on_intu");
        }
        else
          return false;
      }

      if (TreatmentState.IsApplied(treatmentIdentifier))
        return false;

      resolvedDisplay = ResolveTreatmentDisplayForPatient(effect.Display);
      // 메뉴뿐 아니라 우클릭/공격 브리지 등 모든 진입점에서 지원 여부를 강제한다.
      return resolvedDisplay == TreatmentDisplay.None || IsTreatmentDisplaySupported(resolvedDisplay);
    }

    public bool IsTreatmentApplied(string treatmentIdentifier)
      => TreatmentState.IsApplied(treatmentIdentifier);

    /// <summary>처치 상태를 권위 데이터로 변경하고 대응 Display 상태를 연달아 반영한다.</summary>
    public bool SetTreatmentApplied(
      string treatmentIdentifier,
      bool applied,
      TreatmentDisplay display = TreatmentDisplay.None)
    {
      if (IsFishNetClientInitialized && !IsFishNetServerStarted)
        return false;

      if (!TreatmentState.SetApplied(treatmentIdentifier, applied))
        return false;

      GameLogService.WriteInteraction(
        $"Patient treatment state changed: patient={Identifier}, treatment={treatmentIdentifier}, applied={applied}",
        Identifier);

      if (IsFishNetServerStarted)
        RpcSyncTreatmentState(TreatmentState.CreateSnapshot());

      if (display != TreatmentDisplay.None)
      {
        if (IsPatientBC && IsFishNetServerStarted)
          SetTreatmentDisplayNetworked(display, applied);
        else
          SetTreatmentDisplay(display, applied);
      }
      return true;
    }

    /// <summary>
    /// 시나리오 수동 진입처럼 여러 처치 상태를 한 번에 복원해야 할 때 사용한다.
    /// 개별 상태 전환을 흉내 내지 않고 권위 상태와 클라이언트 복제본을 같은 스냅샷으로 맞춘다.
    /// </summary>
    public bool SetTreatmentStateSnapshot(IEnumerable<string> treatmentIdentifiers)
    {
      if (IsFishNetClientInitialized && !IsFishNetServerStarted)
        return false;

      TreatmentState.ApplySnapshot(treatmentIdentifiers);
      if (IsFishNetServerStarted)
        RpcSyncTreatmentState(TreatmentState.CreateSnapshot());
      return true;
    }

    /// <summary>
    /// 처치 시각 표현이 이미 켜져 있는지 조회한다.
    /// 시나리오 준비 경로가 같은 표현을 매 프레임 다시 켜서 불필요한 RPC 를 내보내지 않도록 공개한다.
    /// </summary>
    public bool IsTreatmentDisplayActive(TreatmentDisplay display) => IsDisplayActive(display);

    private bool IsDisplayActive(TreatmentDisplay display)
    {
      var state = GetPatientDisplayState();
      if (state != null)
        return GetDisplayStateFlag(state, display, fromSupports: false);
      var legacy = GetPatientState()?.TreatmentDisplayState;
      return legacy != null && GetDisplayStateFlag(legacy, display, fromSupports: false);
    }

    /// <summary>
    /// C-line(중심정맥관) 연결이 가능한지 판정한다.
    /// 중심정맥관 삽입 시각 표현이 활성화되어 있고, 연결 지점이 존재하며,
    /// 아직 연결되지 않았을 때 <c>true</c>를 반환한다.
    /// </summary>
    public bool IsClineConnectionAvailable()
    {
      if (!IsDisplayActive(TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian))
        return false;
      var point = CentralLineAttachmentPoint;
      return point != null && point.CanAcceptAdditionalConnection;
    }

    /// <summary>
    /// 환자 대상 <c>item_apply</c> 상호작용이 이 아이템을 적용 후보로 노출할지 판정한다.
    /// 노출 기준이 실제 적용 기준(<see cref="CanApplyItemUse"/>)보다 좁으면 시각 표현이 없는 처치
    /// (구강 흡인, 기관내관 플라스터 고정, 소생술 약물 투여)가 퀘스트 마크만 남고 수행할 수 없게 되므로,
    /// 두 기준을 일치시키고 환자에게 적용하는 물품이 아닌 항목만 별도로 제외한다.
    /// </summary>
    internal bool CanApplyHeldTreatmentItem(string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier)
          || NonPatientApplicationItems.Contains(itemIdentifier))
        return false;

      return CanApplyItemUse(itemIdentifier);
    }

    /// <summary>
    /// 지금 이 물품을 적용하면 실제로 어떤 처치가 되는지 돌려준다(적용할 수 없으면 <c>null</c>).
    /// 플라스터처럼 하나의 물품이 상태에 따라 거즈 고정·기관내관 고정으로 갈리는 경우, 물품별
    /// 사용 상호작용이 자기 문구와 일치하는 처치일 때만 노출되도록 판정 근거를 공유한다.
    /// </summary>
    internal string ResolveTreatmentIdentifierForItem(string itemIdentifier)
      => TryResolveItemUse(itemIdentifier, out _, out string treatmentIdentifier, out _)
        ? treatmentIdentifier
        : null;

    internal string FindApplicableTreatmentInventoryItem(PlayerController player)
    {
      if (player == null)
        return null;

      string heldIdentifier = player.HandlingItem?.CurrentIdentifier;
      if (player.CountItemInInventory(heldIdentifier) > 0 && CanApplyHeldTreatmentItem(heldIdentifier))
        return heldIdentifier;

      foreach (var pair in ItemUseEffects)
      {
        if (player.CountItemInInventory(pair.Key) > 0 && CanApplyHeldTreatmentItem(pair.Key))
          return pair.Key;
      }

      for (int i = 0; i < AdditionalPatientApplicationItems.Length; i++)
      {
        string identifier = AdditionalPatientApplicationItems[i];
        if (player.CountItemInInventory(identifier) > 0 && CanApplyHeldTreatmentItem(identifier))
          return identifier;
      }
      return null;
    }

    private void RaiseGenericItemAppliedSignal(string itemIdentifier, string treatmentIdentifier)
    {
      string json = JsonSerializer.Serialize(new
      {
        patientType = Identifier,
        itemIdentifier,
        treatmentIdentifier
      });
      MI.Scenario.ScenarioInteractionSignals.Raise("item_applied_to_patient", json);
    }

    /// <summary>
    /// 거즈/드레싱의 기본 표현은 환자 A의 흉부 손상 기준으로 작성돼 있다. 환자 B/C는
    /// 각 프리팹이 지원하는 팔 표현으로 전환한다. 현재 B는 좌측, C는 우측 표현을 지원하지만,
    /// 여기서는 환자 식별자에 방향을 중복 하드코딩하지 않고 프리팹 지원 상태를 권위로 사용한다.
    /// </summary>
    private TreatmentDisplay ResolveTreatmentDisplayForPatient(TreatmentDisplay display)
    {
      bool isPatientBOrC = string.Equals(Identifier, "patient_b", System.StringComparison.Ordinal)
                           || string.Equals(Identifier, "patient_c", System.StringComparison.Ordinal);
      if (!isPatientBOrC)
        return display;

      if (display == TreatmentDisplay.GauzePatchedOnThorax)
        return IsTreatmentDisplaySupported(TreatmentDisplay.GauzePatchedOnRightArm)
          ? TreatmentDisplay.GauzePatchedOnRightArm
          : TreatmentDisplay.GauzePatchedOnLeftArm;

      if (display == TreatmentDisplay.GauzeDressingDoneOnThorax)
        return IsTreatmentDisplaySupported(TreatmentDisplay.GauzeDressingDoneOnRightArm)
          ? TreatmentDisplay.GauzeDressingDoneOnRightArm
          : TreatmentDisplay.GauzeDressingDoneOnLeftArm;

      return display;
    }

    /// <summary>
    /// 신호 템플릿의 "{id}" 를 현재 환자 Identifier 로 치환한다.
    /// "{id}" 를 포함하는 템플릿인데 환자 Identifier 가 비어 있으면, 잘못된(예: "apply_gauze_")
    /// 환자별 신호가 발신되지 않도록 빈 문자열을 반환해 호출부가 건너뛰게 한다.
    /// </summary>
    private string ResolveSignalTemplate(string template)
    {
      if (string.IsNullOrWhiteSpace(template) || template.IndexOf("{id}", System.StringComparison.Ordinal) < 0)
        return template;

      string id = Identifier;
      if (string.IsNullOrWhiteSpace(id))
        return string.Empty;

      return template.Replace("{id}", id);
    }

    /// <summary>
    /// 처치 표현을 켠다: <see cref="PatientDisplayState"/> 의 <c>DisplayState</c> 플래그를 true 로 설정하고
    /// 대응 <c>ChildGameObjects</c> GameObject 를 <c>SetActive(true)</c> 한다. (데이터는 State, 적용은 Controller)
    /// </summary>
    public void ShowTreatmentDisplay(TreatmentDisplay display) => SetTreatmentDisplay(display, true);

    /// <summary>처치 표현을 끈다(반복 사이클 리셋 등).</summary>
    public void HideTreatmentDisplay(TreatmentDisplay display) => SetTreatmentDisplay(display, false);

    /// <summary>
    /// 스폰 시 모든 처치 시각 표현(부착물)을 비활성화합니다.
    /// 프리팹 편집 편의를 위해 자식이 활성화된 채 저장되어 있어도, 런타임에서는
    /// 시나리오 또는 아이템 사용에 의해 명시적으로 켜진 부착물만 보이도록 합니다.
    /// </summary>
    private void InitializeTreatmentDisplaysFromConfiguredState()
    {
      foreach (TreatmentDisplay display in System.Enum.GetValues(typeof(TreatmentDisplay)))
      {
        if (display == TreatmentDisplay.None)
          continue;

        var go = GetTreatmentDisplayChildObject(display);
        if (go != null)
          go.SetActive(false);
      }
    }

    /// <summary>
    /// 현재 환자 프리팹에서 지정된 처치 표현에 해당하는 자식 GameObject 를 찾습니다.
    /// <see cref="PatientDisplayState"/> 와 레거시 <see cref="PatientTreatmentDisplayStateABC"/> 경로 모두 검색합니다.
    /// </summary>
    private GameObject GetTreatmentDisplayChildObject(TreatmentDisplay display)
    {
      var state = GetPatientDisplayState();
      if (state != null)
      {
        var go = GetDisplayChildObject(state, display);
        if (go != null)
          return go;
      }

      var legacyState = GetPatientState()?.TreatmentDisplayState;
      return legacyState != null ? GetDisplayChildObject(legacyState, display) : null;
    }

    /// <summary>
    /// 시나리오 EntityInit 노드가 부르는 명명된 표시 상태 설정(<see cref="MI.Entity.IScenarioEntityInitTarget"/>).
    /// <paramref name="displayStateName"/> 을 <see cref="TreatmentDisplay"/> 로 해석하여 표시/비표시한다.
    /// 환자 부착물 초기 표시 상태 설정의 진입점.
    /// </summary>
    public bool ApplyScenarioDisplayState(string displayStateName, bool active)
    {
      if (string.IsNullOrWhiteSpace(displayStateName))
        return false;

      string trimmed = displayStateName.Trim();

      // 숫자 문자열을 먼저 거부한다. Enum.TryParse 는 숫자도 허용하므로
      // "5" 같은 입력이 무관한 enum 멤버로 해석되는 것을 방지한다.
      if (trimmed.Length > 0 && char.IsDigit(trimmed[0]))
      {
        Debug.LogWarning($"[PatientController] Numeric display state name '{displayStateName}' is not allowed on patient '{Identifier}'.");
        return false;
      }

      if (!System.Enum.TryParse(trimmed, ignoreCase: true, out TreatmentDisplay display)
          || display == TreatmentDisplay.None
          || !System.Enum.IsDefined(typeof(TreatmentDisplay), display))
      {
        Debug.LogWarning($"[PatientController] Unknown scenario display state '{displayStateName}' on patient '{Identifier}'.");
        return false;
      }

      // 네트워크 환경에서는 서버/클라이언트 구분 없이 SetTreatmentDisplayNetworked 를 통해
      // 모든 피어에 변경 사항을 전파한다. 오프라인이면 로컬만 적용.
      SetTreatmentDisplayNetworked(display, active);
      return true;
    }

    private void SetTreatmentDisplay(TreatmentDisplay display, bool active)
    {
      if (display == TreatmentDisplay.None)
        return;

      var state = GetPatientDisplayState();
      if (state != null)
      {
        // DisplaySupports 가 명시적으로 false 인 항목은 이 환자 모델이 표현 불가 → 무시(데이터 기준).
        if (!IsTreatmentDisplaySupported(state, display))
          return;

        // 실제 플래그 전이(false→true / true→false)일 때만 상태 이벤트를 발생시킨다.
        bool previous = GetDisplayStateFlag(state, display, fromSupports: false);
        SetDisplayStateFlag(state, display, active);

        var go = GetDisplayChildObject(state, display);
        if (go != null)
          go.SetActive(active);

        if (previous != active)
          RaiseTreatmentStateEvent(display, active);
        return;
      }

      // 기존 환자 프리팹은 PatientDisplayState 대신 PatientStateABC 내부에 처치 표시 상태를
      // 직렬화한다. 마이그레이션 전 프리팹도 같은 표시·상태 이벤트 규약을 유지한다.
      var legacyState = GetPatientState()?.TreatmentDisplayState;
      if (legacyState == null || !IsTreatmentDisplaySupported(legacyState, display))
        return;

      bool legacyPrevious = GetDisplayStateFlag(legacyState, display, fromSupports: false);
      SetDisplayStateFlag(legacyState, display, active);

      var legacyGo = GetDisplayChildObject(legacyState, display);
      if (legacyGo != null)
        legacyGo.SetActive(active);

      if (legacyPrevious != active)
        RaiseTreatmentStateEvent(display, active);
    }

    private bool IsTreatmentDisplaySupported(TreatmentDisplay display)
    {
      var state = GetPatientDisplayState();
      if (state != null)
        return IsTreatmentDisplaySupported(state, display);

      var legacyState = GetPatientState()?.TreatmentDisplayState;
      return legacyState != null && IsTreatmentDisplaySupported(legacyState, display);
    }

    private static bool IsTreatmentDisplaySupported(PatientDisplayState state, TreatmentDisplay display)
    {
      // 자식 GameObject 가 연결되어 있으면 표현 가능으로 본다(DisplaySupports 는 보조 정보).
      return GetDisplayChildObject(state, display) != null
             || GetDisplayStateFlag(state, display, fromSupports: true);
    }

    private static bool IsTreatmentDisplaySupported(PatientTreatmentDisplayStateABC state, TreatmentDisplay display)
    {
      return GetDisplayChildObject(state, display) != null
             || GetDisplayStateFlag(state, display, fromSupports: true);
    }

    private static GameObject GetDisplayChildObject(PatientDisplayState state, TreatmentDisplay d)
    {
      return GetDisplayChildObject(state?.ChildGameObjects, d);
    }

    private static GameObject GetDisplayChildObject(PatientTreatmentDisplayStateABC state, TreatmentDisplay d)
    {
      return GetDisplayChildObject(state?.ChildGameObjects, d);
    }

    private static GameObject GetDisplayChildObject(PatientTreatmentDisplayingChildGameObjects c, TreatmentDisplay d)
    {
      if (c == null)
        return null;

      switch (d)
      {
        case TreatmentDisplay.Syringe18GInsertedIntoLeftArm:
          return c.Syringe18GInsertedIntoLeftArm;
        case TreatmentDisplay.Syringe18GInsertedIntoRightArm:
          return c.Syringe18GInsertedIntoRightArm;
        case TreatmentDisplay.Syringe20GInsertedIntoLeftArm:
          return c.Syringe20GInsertedIntoLeftArm;
        case TreatmentDisplay.Syringe20GInsertedIntoRightArm:
          return c.Syringe20GInsertedIntoRightArm;
        case TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian:
          return c.CentralVenousCatheterInsertedIntoSubclavian;
        case TreatmentDisplay.LaryngoscopeInserted:
          return c.LaryngoscopeInserted;
        case TreatmentDisplay.EndotrachealTubeStyletInserted:
          return c.EndotrachealTubeStyletInserted;
        case TreatmentDisplay.EndotrachealTubeInsertDone:
          return c.EndotrachealTubeInsertDone;
        case TreatmentDisplay.TPieceAttachedToNasalCannula:
          return c.TPieceAttachedToNasalCannula;
        case TreatmentDisplay.AmbuBagAttachedToEndotrachealTube:
          return c.AmbuBagAttachedToEndotrachealTube;
        case TreatmentDisplay.GauzePatchedOnThorax:
          return c.GauzePatchedOnThorax;
        case TreatmentDisplay.GauzeDressingDoneOnThorax:
          return c.GauzeDressingDoneOnThorax;
        case TreatmentDisplay.GauzePatchedOnRightArm:
          return c.GauzePatchedOnRightArm;
        case TreatmentDisplay.GauzeDressingDoneOnRightArm:
          return c.GauzeDressingDoneOnRightArm;
        case TreatmentDisplay.GauzePatchedOnLeftArm:
          return c.GauzePatchedOnLeftArm;
        case TreatmentDisplay.GauzeDressingDoneOnLeftArm:
          return c.GauzeDressingDoneOnLeftArm;
        case TreatmentDisplay.GauzePatchedOnRightEyebrow:
          return c.GauzePatchedOnRightEyebrow;
        case TreatmentDisplay.GauzeDressingDoneOnRightEyebrow:
          return c.GauzeDressingDoneOnRightEyebrow;
        case TreatmentDisplay.GauzePatchedOnLeftEyebrow:
          return c.GauzePatchedOnLeftEyebrow;
        case TreatmentDisplay.GauzeDressingDoneOnLeftEyebrow:
          return c.GauzeDressingDoneOnLeftEyebrow;
        case TreatmentDisplay.NasalCannulaApplied:
          return c.NasalCannulaApplied;
        case TreatmentDisplay.CervicalCollarOnNeck:
          return c.CervicalCollarOnNeck;
        case TreatmentDisplay.IntravenousStandAttached:
          return c.IntravenousStandAttached;
        case TreatmentDisplay.IntravenousHangerAttached:
          return c.IntravenousHangerAttached;
        case TreatmentDisplay.IntravenousFluidAttached:
          return c.IntravenousFluidAttached;
        default:
          return null;
      }
    }

    private static void SetDisplayStateFlag(PatientDisplayState state, TreatmentDisplay d, bool v)
    {
      ref var m = ref state.DisplayState;
      SetDisplayStateFlag(ref m, d, v);
    }

    private static void SetDisplayStateFlag(PatientTreatmentDisplayStateABC state, TreatmentDisplay d, bool v)
    {
      var m = state.DisplayState;
      SetDisplayStateFlag(ref m, d, v);
      state.DisplayState = m;
    }

    private static void SetDisplayStateFlag(ref PatientTreatmentDisplayModel m, TreatmentDisplay d, bool v)
    {
      switch (d)
      {
        case TreatmentDisplay.Syringe18GInsertedIntoLeftArm:
          m.Syringe18GInsertedIntoLeftArm = v;
          break;
        case TreatmentDisplay.Syringe18GInsertedIntoRightArm:
          m.Syringe18GInsertedIntoRightArm = v;
          break;
        case TreatmentDisplay.Syringe20GInsertedIntoLeftArm:
          m.Syringe20GInsertedIntoLeftArm = v;
          break;
        case TreatmentDisplay.Syringe20GInsertedIntoRightArm:
          m.Syringe20GInsertedIntoRightArm = v;
          break;
        case TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian:
          m.CentralVenousCatheterInsertedIntoSubclavian = v;
          break;
        case TreatmentDisplay.LaryngoscopeInserted:
          m.LaryngoscopeInserted = v;
          break;
        case TreatmentDisplay.EndotrachealTubeStyletInserted:
          m.EndotrachealTubeStyletInserted = v;
          break;
        case TreatmentDisplay.EndotrachealTubeInsertDone:
          m.EndotrachealTubeInsertDone = v;
          break;
        case TreatmentDisplay.TPieceAttachedToNasalCannula:
          m.TPieceAttachedToNasalCannula = v;
          break;
        case TreatmentDisplay.AmbuBagAttachedToEndotrachealTube:
          m.AmbuBagAttachedToEndotrachealTube = v;
          break;
        case TreatmentDisplay.GauzePatchedOnThorax:
          m.GauzePatchedOnThorax = v;
          break;
        case TreatmentDisplay.GauzeDressingDoneOnThorax:
          m.GauzeDressingDoneOnThorax = v;
          break;
        case TreatmentDisplay.GauzePatchedOnRightArm:
          m.GauzePatchedOnRightArm = v;
          break;
        case TreatmentDisplay.GauzeDressingDoneOnRightArm:
          m.GauzeDressingDoneOnRightArm = v;
          break;
        case TreatmentDisplay.GauzePatchedOnLeftArm:
          m.GauzePatchedOnLeftArm = v;
          break;
        case TreatmentDisplay.GauzeDressingDoneOnLeftArm:
          m.GauzeDressingDoneOnLeftArm = v;
          break;
        case TreatmentDisplay.GauzePatchedOnRightEyebrow:
          m.GauzePatchedOnRightEyebrow = v;
          break;
        case TreatmentDisplay.GauzeDressingDoneOnRightEyebrow:
          m.GauzeDressingDoneOnRightEyebrow = v;
          break;
        case TreatmentDisplay.GauzePatchedOnLeftEyebrow:
          m.GauzePatchedOnLeftEyebrow = v;
          break;
        case TreatmentDisplay.GauzeDressingDoneOnLeftEyebrow:
          m.GauzeDressingDoneOnLeftEyebrow = v;
          break;
        case TreatmentDisplay.NasalCannulaApplied:
          m.NasalCannulaApplied = v;
          break;
        case TreatmentDisplay.CervicalCollarOnNeck:
          m.CervicalCollarOnNeck = v;
          break;
        case TreatmentDisplay.IntravenousStandAttached:
          m.IntravenousStandAttached = v;
          break;
        case TreatmentDisplay.IntravenousHangerAttached:
          m.IntravenousHangerAttached = v;
          break;
        case TreatmentDisplay.IntravenousFluidAttached:
          m.IntravenousFluidAttached = v;
          break;
      }
    }

    private static bool GetDisplayStateFlag(PatientDisplayState state, TreatmentDisplay d, bool fromSupports)
    {
      // 읽기 전용이므로 구조체 복사본을 사용한다(ref 불필요).
      var m = fromSupports ? state.DisplaySupports : state.DisplayState;
      return GetDisplayStateFlag(m, d);
    }

    private static bool GetDisplayStateFlag(PatientTreatmentDisplayStateABC state, TreatmentDisplay d, bool fromSupports)
    {
      var m = fromSupports ? state.DisplaySupports : state.DisplayState;
      return GetDisplayStateFlag(m, d);
    }

    private static bool GetDisplayStateFlag(PatientTreatmentDisplayModel m, TreatmentDisplay d)
    {
      switch (d)
      {
        case TreatmentDisplay.Syringe18GInsertedIntoLeftArm:
          return m.Syringe18GInsertedIntoLeftArm;
        case TreatmentDisplay.Syringe18GInsertedIntoRightArm:
          return m.Syringe18GInsertedIntoRightArm;
        case TreatmentDisplay.Syringe20GInsertedIntoLeftArm:
          return m.Syringe20GInsertedIntoLeftArm;
        case TreatmentDisplay.Syringe20GInsertedIntoRightArm:
          return m.Syringe20GInsertedIntoRightArm;
        case TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian:
          return m.CentralVenousCatheterInsertedIntoSubclavian;
        case TreatmentDisplay.LaryngoscopeInserted:
          return m.LaryngoscopeInserted;
        case TreatmentDisplay.EndotrachealTubeStyletInserted:
          return m.EndotrachealTubeStyletInserted;
        case TreatmentDisplay.EndotrachealTubeInsertDone:
          return m.EndotrachealTubeInsertDone;
        case TreatmentDisplay.TPieceAttachedToNasalCannula:
          return m.TPieceAttachedToNasalCannula;
        case TreatmentDisplay.AmbuBagAttachedToEndotrachealTube:
          return m.AmbuBagAttachedToEndotrachealTube;
        case TreatmentDisplay.GauzePatchedOnThorax:
          return m.GauzePatchedOnThorax;
        case TreatmentDisplay.GauzeDressingDoneOnThorax:
          return m.GauzeDressingDoneOnThorax;
        case TreatmentDisplay.GauzePatchedOnRightArm:
          return m.GauzePatchedOnRightArm;
        case TreatmentDisplay.GauzeDressingDoneOnRightArm:
          return m.GauzeDressingDoneOnRightArm;
        case TreatmentDisplay.GauzePatchedOnLeftArm:
          return m.GauzePatchedOnLeftArm;
        case TreatmentDisplay.GauzeDressingDoneOnLeftArm:
          return m.GauzeDressingDoneOnLeftArm;
        case TreatmentDisplay.GauzePatchedOnRightEyebrow:
          return m.GauzePatchedOnRightEyebrow;
        case TreatmentDisplay.GauzeDressingDoneOnRightEyebrow:
          return m.GauzeDressingDoneOnRightEyebrow;
        case TreatmentDisplay.GauzePatchedOnLeftEyebrow:
          return m.GauzePatchedOnLeftEyebrow;
        case TreatmentDisplay.GauzeDressingDoneOnLeftEyebrow:
          return m.GauzeDressingDoneOnLeftEyebrow;
        case TreatmentDisplay.NasalCannulaApplied:
          return m.NasalCannulaApplied;
        case TreatmentDisplay.CervicalCollarOnNeck:
          return m.CervicalCollarOnNeck;
        case TreatmentDisplay.IntravenousStandAttached:
          return m.IntravenousStandAttached;
        case TreatmentDisplay.IntravenousHangerAttached:
          return m.IntravenousHangerAttached;
        case TreatmentDisplay.IntravenousFluidAttached:
          return m.IntravenousFluidAttached;
        default:
          return false;
      }
    }

    private enum PatientBCTreatmentStage
    {
      Inactive,
      AwaitingPupil,
      AwaitingIv,
      AwaitingNormalSaline,
      AwaitingNasalCannula,
      AwaitingOxygen,
      AwaitingGauze,
      AwaitingPlaster,
      Complete,
    }

    private readonly SyncVar<PatientBCTreatmentStage> _patientBCNurseCStage =
      new(PatientBCTreatmentStage.Inactive);
    private readonly SyncVar<PatientBCTreatmentStage> _patientBCNurseDStage =
      new(PatientBCTreatmentStage.Inactive);
    private bool _patientBCRequiresOxygenDetach;
    private bool _patientBCObservedOxygenDetach;
    private bool _patientBCFreshOxygenInstalled;
    private bool _patientBCPendingNormalSalineConnection;
    private string _patientBCPendingNormalSalineActorIdentifier;
    private string _patientBCPendingNormalSalineActorDisplayName;
    private IntravenousLineConnectionPoint _patientBCPhysicalNormalSalinePoint;
    private bool _patientBCIvAttachmentPointWarned;
    private const string NurseCRoleTag = "nurse_c";
    private const string NurseDRoleTag = "nurse_d";
    private const float PatientBCTreatmentInteractionDistance = 3f;

    private bool IsPatientA =>
      string.Equals(Identifier, "patient_a", System.StringComparison.Ordinal);

    private bool IsPatientBC =>
      string.Equals(Identifier, "patient_b", System.StringComparison.Ordinal)
      || string.Equals(Identifier, "patient_c", System.StringComparison.Ordinal);

    public void ActivatePatientBCNurseCStage()
    {
      if (!IsPatientBC)
        return;
      if (!IsFishNetServerStarted && !InstanceFinder.IsOffline)
      {
        Debug.LogWarning("[PatientController] B/C nurse C stage activation is server-authoritative.", this);
        return;
      }

      _patientBCNurseCStage.Value = PatientBCTreatmentStage.AwaitingPupil;
      _patientBCPendingNormalSalineConnection = false;
      _patientBCPhysicalNormalSalinePoint = null;
      _cannulaLeftArmInserted = false;
      _cannulaRightArmInserted = false;
      // 남성(B)과 여성(C) 환자 모두 정맥로 확보 목표를 받는다. 삽입할 팔은 처치 표시 프리셋이
      // 지원하는 쪽으로 자동 배정된다(남성 우측, 여성 좌측). 여기서는 상호작용만 열어둔다.
      _intravenousLineCannulaInteractable = true;
      SetTreatmentDisplayNetworked(TreatmentDisplay.Syringe20GInsertedIntoLeftArm, false);
      SetTreatmentDisplayNetworked(TreatmentDisplay.Syringe20GInsertedIntoRightArm, false);
      ClearPatientBCSignals(
        $"{Identifier}_pupil_checked",
        $"insert_iv_{Identifier}_left",
        $"insert_iv_{Identifier}_right",
        $"connect_cannula_and_ns1_{Identifier}");
    }

    public bool ActivatePatientBCNurseDStage()
    {
      if (!IsPatientBC)
        return false;
      if (!IsFishNetServerStarted && !InstanceFinder.IsOffline)
      {
        Debug.LogWarning("[PatientController] B/C nurse D stage activation is server-authoritative.", this);
        return false;
      }

      _patientBCRequiresOxygenDetach = ConnectedOxyflowmeter != null
                                       && ConnectedOxyflowmeter.IsAttached;
      _patientBCObservedOxygenDetach = false;
      _patientBCFreshOxygenInstalled = false;
      _patientBCNurseDStage.Value = PatientBCTreatmentStage.AwaitingNasalCannula;
      SetTreatmentDisplayNetworked(TreatmentDisplay.NasalCannulaApplied, false);
      SetTreatmentDisplayNetworked(
        ResolveTreatmentDisplayForPatient(TreatmentDisplay.GauzePatchedOnThorax), false);
      SetTreatmentDisplayNetworked(
        ResolveTreatmentDisplayForPatient(TreatmentDisplay.GauzeDressingDoneOnThorax), false);
      ClearPatientBCSignals(
        $"apply_nasal_cannula_{Identifier}",
        $"equipment_connected_oxyflowmeter_{Identifier}",
        $"oxyflowmeter_attached_{Identifier}",
        $"apply_gauze_{Identifier}",
        $"apply_plaster_on_gauze_{Identifier}");
      return _patientBCRequiresOxygenDetach;
    }

    private void NotifyPatientBCPupilCompleted()
    {
      if (IsPatientBC && (IsFishNetServerStarted || InstanceFinder.IsOffline))
        TryAdvancePatientBCNurseCStage(PatientBCTreatmentStage.AwaitingPupil, PatientBCTreatmentStage.AwaitingIv);
    }

    private bool CanPerformPatientBCIv() =>
      !IsPatientBC || _patientBCNurseCStage.Value == PatientBCTreatmentStage.AwaitingIv;

    private bool CanConnectPatientBCNormalSaline()
    {
      if (!IsPatientBC || _patientBCNurseCStage.Value != PatientBCTreatmentStage.AwaitingNormalSaline)
        return false;
      return CurrentBed != null && CurrentBed.TryGetNormalSalineConnectionPoint(out _);
    }

    private bool TryConnectPatientBCNormalSaline(Transform interactor)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null || player.CountItemInInventory(IntravenousSet.Identifier) < 1)
      {
        ShowRequiredItemDialogue("수액세트를 갖고 있지 않다.", "수액세트를 찾자.");
        return false;
      }

      if (IsFishNetClientInitialized && !IsFishNetServerStarted)
      {
        CmdConnectPatientBCNormalSaline();
        return true;
      }

      if (IsPatientBC && player != null && !TryValidatePatientBCTreatmentActor(player, NurseCRoleTag))
        return false;
      return TryConnectPatientBCNormalSalineAuthoritative(player);
    }

    private bool TryConnectPatientBCNormalSalineAuthoritative(PlayerController player = null)
    {
      if (!CanConnectPatientBCNormalSaline()
          || (player != null && player.CountItemInInventory(IntravenousSet.Identifier) < 1)
          || !CurrentBed.TryGetNormalSalineConnectionPoint(out var salinePoint))
        return false;

      var patientPoint = PatientBCIvAttachmentPoint;
      if (patientPoint == null)
      {
        WarnMissingPatientBCIvAttachmentPointOnce();
        return false;
      }
      if (!patientPoint.IsPhysicallyConnectedTo(salinePoint))
      {
        var service = LineConnectionService.TopologyService
                      ?? FindFirstObjectByType<LineConnectionService>(FindObjectsInactive.Include);
        if (service == null || !service.TryCreateAutomaticConnection(salinePoint, patientPoint))
          return false;
        if (player != null && player.RemoveItemFromInventory(IntravenousSet.Identifier, 1) != 1)
        {
          service.DisconnectAutomaticConnection(salinePoint, patientPoint);
          return false;
        }
      }
      return TryCompletePatientBCNormalSalineConnection(salinePoint);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdConnectPatientBCNormalSaline(NetworkConnection sender = null)
    {
      if (!TryValidatePatientBCTreatmentActor(sender, NurseCRoleTag, out var player,
            out var actorIdentifier, out var actorDisplayName)
          || !CanConnectPatientBCNormalSaline())
        return;

      using (MI.Scenario.ScenarioSignalPlayerContext.Push(actorIdentifier, actorDisplayName))
        TryConnectPatientBCNormalSalineAuthoritative(player);
    }

    public bool TryCompletePatientBCNormalSalineConnection(
      IntravenousLineConnectionPoint salinePoint = null)
    {
      if (!IsPatientBC)
        return true;
      if (IsFishNetServerStarted || InstanceFinder.IsOffline)
      {
        if (!HasPhysicalPatientBCNormalSalineConnection(salinePoint))
          return false;
        if (salinePoint != null)
          _patientBCPhysicalNormalSalinePoint = salinePoint;
        if (TryCompletePatientBCNormalSalineConnectionAuthoritative())
          return true;

        if (MI.Scenario.ScenarioSignalPlayerContext.TryGetCurrent(
              out var actorIdentifier, out var actorDisplayName))
          RememberPatientBCNormalSalineConnection(actorIdentifier, actorDisplayName);
        return false;
      }
      if (IsFishNetClientInitialized)
        CmdCompletePatientBCNormalSalineConnection(salinePoint.ConnectionIdentifier);
      return false;
    }

    private bool TryCompletePatientBCNormalSalineConnectionAuthoritative()
    {
      if (!HasPhysicalPatientBCNormalSalineConnection())
        return false;
      if (!TryAdvancePatientBCNurseCStage(PatientBCTreatmentStage.AwaitingNormalSaline,
            PatientBCTreatmentStage.Complete))
        return false;

      const string signal = "connect_cannula_and_ns1";
      MI.Scenario.ScenarioInteractionSignals.Raise($"{signal}_{Identifier}");
      MI.Scenario.ScenarioInteractionSignals.Raise(signal,
        JsonSerializer.Serialize(new { patientIdentifier = Identifier }));
      return true;
    }

    private bool HasPhysicalPatientBCNormalSalineConnection(
      IntravenousLineConnectionPoint salinePoint = null)
    {
      var patientPoint = PatientBCIvAttachmentPoint;
      if (patientPoint == null)
        return false;

      if (salinePoint != null)
        return (CurrentBed != null
                 ? CurrentBed.IsNormalSalineConnectionPoint(salinePoint)
                 : InstanceFinder.IsOffline)
               && patientPoint.IsPhysicallyConnectedTo(salinePoint);

      return CurrentBed != null
             && CurrentBed.TryGetNormalSalineConnectionPoint(out var bedPoint)
             && patientPoint.IsPhysicallyConnectedTo(bedPoint);
    }

    /// <summary>
    /// 정맥로 IV 연결 지점이 배선되지 않아 생리식염수 연결이 성립할 수 없음을 한 번만 알린다.
    /// 이 참조는 환자 유형별 State 컴포넌트(PatientTypeBMaleState 등)에서 사람이 직접 배선한다.
    /// </summary>
    private void WarnMissingPatientBCIvAttachmentPointOnce()
    {
      if (_patientBCIvAttachmentPointWarned)
        return;
      _patientBCIvAttachmentPointWarned = true;
      Debug.LogError(
        $"[PatientController] {Identifier}: 정맥로 IV 연결 지점이 배선되지 않아 생리식염수를 연결할 수 없습니다. " +
        "환자 프리팹의 PatientType...State 컴포넌트에 Intravenous Line Connection Point 참조를 지정하십시오.",
        this);
    }

    private void RememberPatientBCNormalSalineConnection(string actorIdentifier, string actorDisplayName)
    {
      _patientBCPendingNormalSalineConnection = true;
      _patientBCPendingNormalSalineActorIdentifier = actorIdentifier;
      _patientBCPendingNormalSalineActorDisplayName = actorDisplayName;
    }

    public void NotifyPatientBCNormalSalineDisconnected(IntravenousLineConnectionPoint salinePoint)
    {
      if (!IsPatientBC || salinePoint == null)
        return;
      if (IsFishNetServerStarted || InstanceFinder.IsOffline)
      {
        ClearPatientBCNormalSalineConnection(salinePoint);
        return;
      }
      if (IsFishNetClientInitialized)
        CmdClearPatientBCNormalSalineConnection(salinePoint.ConnectionIdentifier);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdClearPatientBCNormalSalineConnection(
      string salinePointIdentifier,
      NetworkConnection sender = null)
    {
      var salinePoint = FindIntravenousLineConnectionPoint(salinePointIdentifier);
      if (!TryValidatePatientBCTreatmentActor(sender, NurseCRoleTag, out var player, out _, out _)
          || salinePoint == null
          || !IsWithinPatientBCTreatmentDistance(player, salinePoint.transform.position))
        return;
      ClearPatientBCNormalSalineConnection(salinePoint);
    }

    private void ClearPatientBCNormalSalineConnection(IntravenousLineConnectionPoint salinePoint)
    {
      if (!ReferenceEquals(_patientBCPhysicalNormalSalinePoint, salinePoint))
        return;
      _patientBCPhysicalNormalSalinePoint = null;
      _patientBCPendingNormalSalineConnection = false;
    }

    private void TryCreditPendingPatientBCNormalSalineConnection()
    {
      if (!_patientBCPendingNormalSalineConnection || !HasPhysicalPatientBCNormalSalineConnection())
        return;

      using (MI.Scenario.ScenarioSignalPlayerContext.Push(
               _patientBCPendingNormalSalineActorIdentifier,
               _patientBCPendingNormalSalineActorDisplayName))
      {
        if (TryCompletePatientBCNormalSalineConnectionAuthoritative())
          _patientBCPendingNormalSalineConnection = false;
      }
    }

    private bool CanApplyPatientBCItem(string itemIdentifier)
    {
      if (!IsPatientBC)
        return true;

      return itemIdentifier switch
      {
        "nasalcannula" or "nasal_cannula" or "nasal" =>
          _patientBCNurseDStage.Value == PatientBCTreatmentStage.AwaitingNasalCannula,
        "gauze" => _patientBCNurseDStage.Value == PatientBCTreatmentStage.AwaitingGauze,
        "plaster" => _patientBCNurseDStage.Value == PatientBCTreatmentStage.AwaitingPlaster,
        _ => false,
      };
    }

    private void NotifyPatientBCItemApplied(string itemIdentifier)
    {
      if (!IsPatientBC)
        return;

      TryAdvancePatientBCItemStageAuthoritative(itemIdentifier);
    }

    private void TryAdvancePatientBCItemStageAuthoritative(string itemIdentifier)
    {
      if ((itemIdentifier == "nasalcannula" || itemIdentifier == "nasal_cannula" || itemIdentifier == "nasal")
          && TryAdvancePatientBCNurseDStage(PatientBCTreatmentStage.AwaitingNasalCannula,
            PatientBCTreatmentStage.AwaitingOxygen))
      {
        // 거즈/플라스터/산소 연결과 같은 이유(SIGNAL-BC-3)로 환자별 신호를 직접 올린다.
        // 이 신호는 nurse D 순서 게이트를 여는 유일한 조건인데, EntityStateSignalBinding 은
        // consumeOnce 라 세션 중 한 번 소비되면 다시 발신되지 않는다. 게다가 단계 활성화가
        // 신호를 먼저 지우므로, 바인딩에만 의존하면 게이트가 영구히 열리지 않을 수 있다.
        RaisePatientBCTreatmentSignal("apply_nasal_cannula");

        // 유량계를 먼저 조작한 경우에는 비강 캐뉼라 포트가 아직 비활성이라 라인 생성이
        // 보류된다. 캐뉼라를 적용한 직후 같은 유량계를 다시 판정해야 순서와 무관하게
        // 실제 산소 라인 연결 및 처치 완료 신호가 발생한다.
        ReconcilePatientBCOxygenLine();
        if (_patientBCFreshOxygenInstalled
            && ShouldCreditPatientBCEquipmentConnection(EquipmentTypeOxyflowmeter))
          RaisePatientBCOxygenSuppliedSignals();
      }
      else if (itemIdentifier == "gauze"
               && TryAdvancePatientBCNurseDStage(PatientBCTreatmentStage.AwaitingGauze,
                 PatientBCTreatmentStage.AwaitingPlaster))
        RaisePatientBCTreatmentSignal("apply_gauze");
      else if (itemIdentifier == "plaster"
               && TryAdvancePatientBCNurseDStage(PatientBCTreatmentStage.AwaitingPlaster,
                 PatientBCTreatmentStage.Complete))
        RaisePatientBCTreatmentSignal("apply_plaster_on_gauze");

      // SyncVar.OnChange 만으로는 부족한 사례가 있어(트리아지 갱신과 동일한 이유),
      // 권위 측에서 단계를 바꾼 직후 이 자리에서도 명시적으로 힌트를 갱신한다.
      RefreshPatientBCInteractableHints();
    }

    private void ReconcilePatientBCOxygenLine()
    {
      var flowmeter = ConnectedOxyflowmeter;
      if (flowmeter == null)
        return;

      var zones = FindObjectsByType<PatientCareDescriptionZone>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < zones.Length; i++)
        zones[i].TryReconcileOxygenLineFor(flowmeter);
    }

    /// <summary>
    /// B/C 환자의 산소 처치 완료 조건을 디버그 요약에 표시한다.
    /// CareZone 장비 참조와 실제 산소 라인 연결을 혼동하지 않기 위한 진단 정보다.
    /// </summary>
    public string GetPatientBCOxygenTreatmentSummary()
    {
      if (!IsPatientBC)
        return "  B/C oxygen treatment: (not applicable)\n";

      var flowmeter = ConnectedOxyflowmeter;
      var flowmeterPort = flowmeter != null ? flowmeter.OxyLineConnectionPoint : null;
      var patientPort = ConfiguredOxygenMaskAttachmentPoint;
      bool lineConnected = flowmeterPort != null
                           && patientPort != null
                           && flowmeterPort.IsPhysicallyConnectedTo(patientPort);
      bool flowmeterOperated = flowmeter != null && flowmeter.IsAttachedInteractCompleted;
      bool completionSignalRaised = MI.Scenario.ScenarioInteractionSignals.IsRaised(
        $"equipment_connected_oxyflowmeter_{Identifier}");
      return $"  B/C oxygen treatment: stage={_patientBCNurseDStage.Value}, "
             + $"requiresDetach={_patientBCRequiresOxygenDetach}, "
             + $"observedDetach={_patientBCObservedOxygenDetach}, "
             + $"flowmeterAttached={flowmeter != null && flowmeter.IsAttached}, "
             + $"flowmeterOperated={flowmeterOperated}, "
             + $"patientPortActive={OxygenMaskAttachmentPoint != null}, "
             + $"physicalLineConnected={lineConnected}, "
             + $"completionSignalRaised={completionSignalRaised}\n";
    }

    private bool ShouldCreditPatientBCEquipmentConnection(string equipmentType)
    {
      if (!IsPatientBC || !string.Equals(equipmentType, EquipmentTypeOxyflowmeter, System.StringComparison.Ordinal))
        return true;
      if (!IsFishNetServerStarted && !InstanceFinder.IsOffline)
      {
        if (IsFishNetClientInitialized)
          CmdCompletePatientBCOxygenConnection();
        return false;
      }

      if (_patientBCNurseDStage.Value == PatientBCTreatmentStage.Inactive)
        return false;
      if (_patientBCRequiresOxygenDetach && !_patientBCObservedOxygenDetach)
        return false;

      _patientBCFreshOxygenInstalled = true;
      bool advanced = TryAdvancePatientBCNurseDStage(PatientBCTreatmentStage.AwaitingOxygen,
        PatientBCTreatmentStage.AwaitingGauze);
      if (advanced)
        RefreshPatientBCInteractableHints();
      return advanced;
    }

    /// <summary>
    /// 유량계 참조가 이미 환자에게 설정된 뒤 산소 라인이 완성된 경우에도
    /// 산소 공급 처치의 연결 신호를 한 번 평가한다.
    /// </summary>
    public void NotifyOxygenLineConnected()
    {
      if (!IsPatientBC)
        return;

      if (!ShouldCreditPatientBCEquipmentConnection(EquipmentTypeOxyflowmeter))
        return;

      RaisePatientBCOxygenSuppliedSignals();
    }

    /// <summary>
    /// 실제 산소 라인 연결로 B/C 환자 산소 처치가 완료된 뒤, 상태 이벤트와 환자별 완료
    /// 신호를 함께 올린다. 동적 상태 이벤트 바인딩이 없는 시점에도 퀘스트 완료가 누락되지
    /// 않도록 신호를 직접 발신한다.
    /// </summary>
    private void RaisePatientBCOxygenSuppliedSignals()
    {
      RaiseEquipmentStateEvent(EquipmentTypeOxyflowmeter, connected: true);
      MI.Scenario.ScenarioInteractionSignals.Raise($"equipment_connected_oxyflowmeter_{Identifier}");
    }

    private void RaisePatientBCTreatmentSignal(string signalPrefix)
    {
      if (!IsPatientBC || string.IsNullOrWhiteSpace(signalPrefix))
        return;

      MI.Scenario.ScenarioInteractionSignals.Raise($"{signalPrefix}_{Identifier}");
    }

    private void NotifyPatientBCEquipmentDisconnected(string equipmentType, MonoBehaviour equipment)
    {
      if (!IsPatientBC
          || _patientBCNurseDStage.Value == PatientBCTreatmentStage.Inactive
          || !string.Equals(equipmentType, EquipmentTypeOxyflowmeter, System.StringComparison.Ordinal))
        return;

      _patientBCFreshOxygenInstalled = false;
      if (_patientBCRequiresOxygenDetach)
        _patientBCObservedOxygenDetach = true;
    }

    private bool TryAdvancePatientBCNurseCStage(PatientBCTreatmentStage expected, PatientBCTreatmentStage next)
    {
      if (_patientBCNurseCStage.Value != expected)
        return false;
      _patientBCNurseCStage.Value = next;
      return true;
    }

    private void RevertPatientBCIvStageAuthoritative()
    {
      if (_patientBCNurseCStage.Value == PatientBCTreatmentStage.AwaitingNormalSaline)
        _patientBCNurseCStage.Value = PatientBCTreatmentStage.AwaitingIv;
    }

    private bool TryAdvancePatientBCNurseDStage(PatientBCTreatmentStage expected, PatientBCTreatmentStage next)
    {
      if (_patientBCNurseDStage.Value != expected)
        return false;
      _patientBCNurseDStage.Value = next;
      return true;
    }

    private void InitializeNurseDStageSync()
    {
      _patientBCNurseDStage.OnChange += OnPatientBCNurseDStageChanged;
    }

    private void TeardownNurseDStageSync()
    {
      _patientBCNurseDStage.OnChange -= OnPatientBCNurseDStageChanged;
    }

    // 비강 캐뉼라 적용 등으로 단계가 전환된 직후, 스테일해진 인터랙션 힌트(예: "비강 캐뉼라 적용")가
    // 재상호작용 없이도 즉시 사라지도록 SyncVar 복제 시점에 근처 상호작용 캐시를 갱신한다.
    private void OnPatientBCNurseDStageChanged(
      PatientBCTreatmentStage previous, PatientBCTreatmentStage next, bool asServer)
    {
      RefreshPatientBCInteractableHints();
    }

    private static void RefreshPatientBCInteractableHints()
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

    [ServerRpc(RequireOwnership = false)]
    private void CmdApplyPatientItemUse(string itemIdentifier, NetworkConnection sender = null)
    {
      if (IsPatientBC)
      {
        ApplyPatientBCItemUseAuthoritative(itemIdentifier, sender);
        return;
      }

      if (!TryResolvePlayerForTreatmentSender(sender, out var player,
            out var actorIdentifier, out var actorDisplayName)
          || !IsWithinPatientBCTreatmentDistance(player)
          || !CanApplyItemUse(itemIdentifier))
        return;

      RequestApprovedRemoteItemConsumption(
        player, itemIdentifier, actorIdentifier, actorDisplayName);
    }

    private bool TryResolvePlayerForTreatmentSender(
      NetworkConnection sender,
      out PlayerController player,
      out string actorIdentifier,
      out string actorDisplayName)
    {
      player = null;
      actorIdentifier = null;
      actorDisplayName = null;
      if (sender == null || !sender.IsValid)
        return false;

      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        var candidate = players[i];
        if (candidate?.Owner == null || !candidate.Owner.IsValid
            || candidate.Owner.ClientId != sender.ClientId)
          continue;
        player = candidate;
        actorIdentifier = candidate.UserIdentifier;
        actorDisplayName = candidate.UserIdentifier;
        return true;
      }
      return false;
    }

    private void ApplyPatientBCItemUseAuthoritative(string itemIdentifier, NetworkConnection sender)
    {
      if (!IsPatientBC
          || !CanApplyItemUse(itemIdentifier)
          || !TryValidatePatientBCTreatmentActor(sender, NurseDRoleTag, out var player,
            out var actorIdentifier, out var actorDisplayName))
        return;

      RequestApprovedRemoteItemConsumption(
        player, itemIdentifier, actorIdentifier, actorDisplayName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdCompletePatientBCNormalSalineConnection(
      string salinePointIdentifier,
      NetworkConnection sender = null)
    {
      var salinePoint = FindIntravenousLineConnectionPoint(salinePointIdentifier);
      if (!IsPatientBC
          || !TryValidatePatientBCTreatmentActor(sender, NurseCRoleTag, out var player, out var actorIdentifier,
            out var actorDisplayName)
          || salinePoint == null
          || CurrentBed == null
          || !CurrentBed.IsNormalSalineConnectionPoint(salinePoint)
          || !HasPhysicalPatientBCNormalSalineConnection(salinePoint)
          || !IsWithinPatientBCTreatmentDistance(player, salinePoint.transform.position))
        return;

      _patientBCPhysicalNormalSalinePoint = salinePoint;
      using (MI.Scenario.ScenarioSignalPlayerContext.Push(actorIdentifier, actorDisplayName))
      {
        if (!TryCompletePatientBCNormalSalineConnectionAuthoritative())
          RememberPatientBCNormalSalineConnection(actorIdentifier, actorDisplayName);
      }
    }

    private static IntravenousLineConnectionPoint FindIntravenousLineConnectionPoint(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return null;

      var points = FindObjectsByType<IntravenousLineConnectionPoint>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (var i = 0; i < points.Length; i++)
      {
        if (points[i] != null && points[i].ConnectionIdentifier == identifier)
          return points[i];
      }
      return null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdCompletePatientBCOxygenConnection(NetworkConnection sender = null)
    {
      if (!IsPatientBC
          || !TryValidatePatientBCTreatmentActor(sender, NurseDRoleTag, out var player, out var actorIdentifier,
            out var actorDisplayName)
          || ConnectedOxyflowmeter == null
          || !ConnectedOxyflowmeter.IsAttached
          || !IsWithinPatientBCTreatmentDistance(
            player, ConnectedOxyflowmeter.transform.position))
        return;

      using (MI.Scenario.ScenarioSignalPlayerContext.Push(actorIdentifier, actorDisplayName))
      {
        if (ShouldCreditPatientBCEquipmentConnection(EquipmentTypeOxyflowmeter))
          RaisePatientBCOxygenSuppliedSignals();
      }
    }

    private bool TryValidatePatientBCTreatmentActor(PlayerController player, string requiredRoleTag)
    {
      if (InstanceFinder.IsOffline)
        return player != null && IsWithinPatientBCTreatmentDistance(player);
      return player != null
             && player.Owner != null
             && player.Owner.IsValid
             && !string.IsNullOrWhiteSpace(player.UserIdentifier)
             && PlayerTagService.HasTag(player.UserIdentifier, requiredRoleTag)
             && IsWithinPatientBCTreatmentDistance(player);
    }

    private bool TryValidatePatientBCTreatmentActor(
      NetworkConnection sender,
      string requiredRoleTag,
      out PlayerController player,
      out string actorIdentifier,
      out string actorDisplayName) =>
      TryResolveTreatmentActor(sender, requiredRoleTag, out player, out actorIdentifier, out actorDisplayName);

    /// <summary>
    /// 처치 요청을 보낸 접속자를 서버에서 확인한다. <paramref name="requiredRoleTag"/> 가 비어 있으면
    /// 역할 태그를 요구하지 않고, 접속자 정보와 환자와의 거리만 확인한다.
    /// </summary>
    private bool TryResolveTreatmentActor(
      NetworkConnection sender,
      string requiredRoleTag,
      out PlayerController player,
      out string actorIdentifier,
      out string actorDisplayName)
    {
      player = null;
      actorIdentifier = null;
      actorDisplayName = null;
      bool requiresRole = !string.IsNullOrWhiteSpace(requiredRoleTag);
      if (sender == null
          || !sender.IsValid
          || !UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor)
          || descriptor == null
          || string.IsNullOrWhiteSpace(descriptor.Identifier)
          || (requiresRole && !PlayerTagService.HasTag(descriptor.Identifier, requiredRoleTag)))
        return false;

      var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        var candidate = players[i];
        if (candidate?.Owner == null
            || !candidate.Owner.IsValid
            || candidate.Owner.ClientId != sender.ClientId
            || !string.Equals(candidate.UserIdentifier, descriptor.Identifier, System.StringComparison.Ordinal))
          continue;
        player = candidate;
        break;
      }

      if (!IsPatientBCTreatmentActorValid(
            player,
            actorKnown: player != null,
            hasRequiredRole: true))
        return false;
      actorIdentifier = descriptor.Identifier;
      actorDisplayName = descriptor.DisplayName;
      return true;
    }

    private bool IsWithinPatientBCTreatmentDistance(PlayerController player) =>
      IsWithinPatientBCTreatmentDistance(player, transform.position);

    private static bool IsWithinPatientBCTreatmentDistance(PlayerController player, Vector3 targetPosition) =>
      player != null
      && (player.transform.position - targetPosition).sqrMagnitude
      <= PatientBCTreatmentInteractionDistance * PatientBCTreatmentInteractionDistance;

    private bool IsPatientBCTreatmentActorValid(
      PlayerController player,
      bool actorKnown,
      bool hasRequiredRole) =>
      actorKnown && hasRequiredRole && IsWithinPatientBCTreatmentDistance(player);

    public bool CanPlayerCompletePatientBCNormalSalineConnection(PlayerController player) =>
      !IsPatientBC || TryValidatePatientBCTreatmentActor(player, NurseCRoleTag);

    public bool CanAuthoritativelyConnectPatientBCNormalSaline() =>
      IsPatientBC
      && (_patientBCNurseCStage.Value == PatientBCTreatmentStage.AwaitingIv
          || _patientBCNurseCStage.Value == PatientBCTreatmentStage.AwaitingNormalSaline);

    private static void ClearPatientBCSignals(params string[] signals)
    {
      for (int i = 0; i < signals.Length; i++)
        MI.Scenario.ScenarioInteractionSignals.Clear(signals[i]);
    }
  }
}
