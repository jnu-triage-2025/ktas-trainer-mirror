using System.Collections.Generic;
using UnityEngine;
using TriageTrainer.Patient;

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
      new(TreatmentDisplay.NasalCannulaApplied, "apply_nasal_cannula", "click_nasal");

    // ── 아이템 식별자 → 효과(처치표현 + 신호) 기본 매핑(하드코딩, Reset 무관) ──
    //
    // 부위가 환자별로 고정되어 있고(프리팹 hierarchy 반영) 컨트롤러는 플래그만 켜면 되므로,
    // 거즈/플라스터는 흉부 기준 표현을 기본으로 둔다(다른 부위가 필요한 환자는 해당 플래그를
    // 추가 매핑하거나 향후 부위 조준으로 확장). 신호는 시나리오 게이트 조건명과 일치시킨다.
    private static readonly Dictionary<string, ItemUseEffect> ItemUseEffects = new()
    {
      // 부착형(시각 표현 동반)
      { "gauze",          new ItemUseEffect(TreatmentDisplay.GauzePatchedOnThorax, "apply_gauze") },
      { "plaster",        new ItemUseEffect(TreatmentDisplay.GauzeDressingDoneOnThorax, "apply_plaster_on_gauze", "apply_plaster_on_intu") },
      { "gloves",         new ItemUseEffect(TreatmentDisplay.None, "wear_glove") },
      // 실제 아이템 식별자(cervical_collar / nasalcannula)가 프로덕션 경로의 키.
      // 구 명칭(neckstabilizer / nasal)은 디버그 훅(Debug_ApplyItemUse) 호환용 별칭이며,
      // 반드시 동일 인스턴스를 공유해 신호/표현이 갈라지지 않게 한다.
      { "cervical_collar", CervicalCollarEffect },
      { "neckstabilizer",  CervicalCollarEffect },
      { "electrode",      new ItemUseEffect(TreatmentDisplay.None, "apply_electrode") },
      { "nasalcannula",   NasalCannulaEffect },
      { "nasal",          NasalCannulaEffect },

      // 사용형(시각 표현 없음 또는 별도 이벤트가 표현 담당)
      { "yankauer",            new ItemUseEffect(TreatmentDisplay.None, "suction_{id}") },
      { "yankauer_ready",      new ItemUseEffect(TreatmentDisplay.None, "suction_{id}") },
      { "ambubag",             new ItemUseEffect(TreatmentDisplay.AmbuBagAttachedToEndotrachealTube, "start_ambu") },
      { "epinephrine_ampule",  new ItemUseEffect(TreatmentDisplay.None, "push_epi") },
      { "normal_saline_20ml",  new ItemUseEffect(TreatmentDisplay.None, "push_ns") },
    };

    /// <summary>
    /// 아이템 사용을 처리한다: (1) 매핑된 처치 표현을 켜고, (2) 매핑된 신호를 올린다.
    /// 매핑이 없으면 아무 것도 하지 않는다.
    /// </summary>
    private bool ApplyItemUse(string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier)
          || !ItemUseEffects.TryGetValue(itemIdentifier, out var effect))
        return false;

      if (effect.Display != TreatmentDisplay.None)
        ShowTreatmentDisplay(effect.Display);

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

      return raised || effect.Display != TreatmentDisplay.None;
    }

    /// <summary>
    /// 신호 템플릿의 "{id}" 를 현재 환자 Identifier 로 치환한다(없으면 "{id}" 제거).
    /// </summary>
    private string ResolveSignalTemplate(string template)
    {
      if (string.IsNullOrWhiteSpace(template) || template.IndexOf("{id}", System.StringComparison.Ordinal) < 0)
        return template;

      string id = Identifier;
      return template.Replace("{id}", string.IsNullOrWhiteSpace(id) ? string.Empty : id);
    }

    /// <summary>
    /// 처치 표현을 켠다: <see cref="PatientDisplayState"/> 의 <c>DisplayState</c> 플래그를 true 로 설정하고
    /// 대응 <c>ChildGameObjects</c> GameObject 를 <c>SetActive(true)</c> 한다. (데이터는 State, 적용은 Controller)
    /// </summary>
    public void ShowTreatmentDisplay(TreatmentDisplay display) => SetTreatmentDisplay(display, true);

    /// <summary>처치 표현을 끈다(반복 사이클 리셋 등).</summary>
    public void HideTreatmentDisplay(TreatmentDisplay display) => SetTreatmentDisplay(display, false);

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
      if (state == null)
        return;

      // DisplaySupports 가 명시적으로 false 인 항목은 이 환자 모델이 표현 불가 → 무시(데이터 기준).
      if (!IsTreatmentDisplaySupported(state, display))
        return;

      SetDisplayStateFlag(state, display, active);

      var go = GetDisplayChildObject(state, display);
      if (go != null)
        go.SetActive(active);
    }

    private static bool IsTreatmentDisplaySupported(PatientDisplayState state, TreatmentDisplay display)
    {
      // 자식 GameObject 가 연결되어 있으면 표현 가능으로 본다(DisplaySupports 는 보조 정보).
      return GetDisplayChildObject(state, display) != null
             || GetDisplayStateFlag(state, display, fromSupports: true);
    }

    private static GameObject GetDisplayChildObject(PatientDisplayState state, TreatmentDisplay d)
    {
      var c = state.ChildGameObjects;
      if (c == null)
        return null;

      switch (d)
      {
        case TreatmentDisplay.Syringe18GInsertedIntoLeftArm: return c.Syringe18GInsertedIntoLeftArm;
        case TreatmentDisplay.Syringe18GInsertedIntoRightArm: return c.Syringe18GInsertedIntoRightArm;
        case TreatmentDisplay.Syringe20GInsertedIntoLeftArm: return c.Syringe20GInsertedIntoLeftArm;
        case TreatmentDisplay.Syringe20GInsertedIntoRightArm: return c.Syringe20GInsertedIntoRightArm;
        case TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian: return c.CentralVenousCatheterInsertedIntoSubclavian;
        case TreatmentDisplay.LaryngoscopeInserted: return c.LaryngoscopeInserted;
        case TreatmentDisplay.EndotrachealTubeStyletInserted: return c.EndotrachealTubeStyletInserted;
        case TreatmentDisplay.EndotrachealTubeInsertDone: return c.EndotrachealTubeInsertDone;
        case TreatmentDisplay.TPieceAttachedToNasalCannula: return c.TPieceAttachedToNasalCannula;
        case TreatmentDisplay.AmbuBagAttachedToEndotrachealTube: return c.AmbuBagAttachedToEndotrachealTube;
        case TreatmentDisplay.GauzePatchedOnThorax: return c.GauzePatchedOnThorax;
        case TreatmentDisplay.GauzeDressingDoneOnThorax: return c.GauzeDressingDoneOnThorax;
        case TreatmentDisplay.GauzePatchedOnRightArm: return c.GauzePatchedOnRightArm;
        case TreatmentDisplay.GauzeDressingDoneOnRightArm: return c.GauzeDressingDoneOnRightArm;
        case TreatmentDisplay.GauzePatchedOnLeftArm: return c.GauzePatchedOnLeftArm;
        case TreatmentDisplay.GauzeDressingDoneOnLeftArm: return c.GauzeDressingDoneOnLeftArm;
        case TreatmentDisplay.GauzePatchedOnRightEyebrow: return c.GauzePatchedOnRightEyebrow;
        case TreatmentDisplay.GauzeDressingDoneOnRightEyebrow: return c.GauzeDressingDoneOnRightEyebrow;
        case TreatmentDisplay.GauzePatchedOnLeftEyebrow: return c.GauzePatchedOnLeftEyebrow;
        case TreatmentDisplay.GauzeDressingDoneOnLeftEyebrow: return c.GauzeDressingDoneOnLeftEyebrow;
        case TreatmentDisplay.NasalCannulaApplied: return c.NasalCannulaApplied;
        case TreatmentDisplay.CervicalCollarOnNeck: return c.CervicalCollarOnNeck;
        case TreatmentDisplay.IntravenousStandAttached: return c.IntravenousStandAttached;
        case TreatmentDisplay.IntravenousHangerAttached: return c.IntravenousHangerAttached;
        case TreatmentDisplay.IntravenousFluidAttached: return c.IntravenousFluidAttached;
        default: return null;
      }
    }

    private static void SetDisplayStateFlag(PatientDisplayState state, TreatmentDisplay d, bool v)
    {
      ref var m = ref state.DisplayState;
      switch (d)
      {
        case TreatmentDisplay.Syringe18GInsertedIntoLeftArm: m.Syringe18GInsertedIntoLeftArm = v; break;
        case TreatmentDisplay.Syringe18GInsertedIntoRightArm: m.Syringe18GInsertedIntoRightArm = v; break;
        case TreatmentDisplay.Syringe20GInsertedIntoLeftArm: m.Syringe20GInsertedIntoLeftArm = v; break;
        case TreatmentDisplay.Syringe20GInsertedIntoRightArm: m.Syringe20GInsertedIntoRightArm = v; break;
        case TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian: m.CentralVenousCatheterInsertedIntoSubclavian = v; break;
        case TreatmentDisplay.LaryngoscopeInserted: m.LaryngoscopeInserted = v; break;
        case TreatmentDisplay.EndotrachealTubeStyletInserted: m.EndotrachealTubeStyletInserted = v; break;
        case TreatmentDisplay.EndotrachealTubeInsertDone: m.EndotrachealTubeInsertDone = v; break;
        case TreatmentDisplay.TPieceAttachedToNasalCannula: m.TPieceAttachedToNasalCannula = v; break;
        case TreatmentDisplay.AmbuBagAttachedToEndotrachealTube: m.AmbuBagAttachedToEndotrachealTube = v; break;
        case TreatmentDisplay.GauzePatchedOnThorax: m.GauzePatchedOnThorax = v; break;
        case TreatmentDisplay.GauzeDressingDoneOnThorax: m.GauzeDressingDoneOnThorax = v; break;
        case TreatmentDisplay.GauzePatchedOnRightArm: m.GauzePatchedOnRightArm = v; break;
        case TreatmentDisplay.GauzeDressingDoneOnRightArm: m.GauzeDressingDoneOnRightArm = v; break;
        case TreatmentDisplay.GauzePatchedOnLeftArm: m.GauzePatchedOnLeftArm = v; break;
        case TreatmentDisplay.GauzeDressingDoneOnLeftArm: m.GauzeDressingDoneOnLeftArm = v; break;
        case TreatmentDisplay.GauzePatchedOnRightEyebrow: m.GauzePatchedOnRightEyebrow = v; break;
        case TreatmentDisplay.GauzeDressingDoneOnRightEyebrow: m.GauzeDressingDoneOnRightEyebrow = v; break;
        case TreatmentDisplay.GauzePatchedOnLeftEyebrow: m.GauzePatchedOnLeftEyebrow = v; break;
        case TreatmentDisplay.GauzeDressingDoneOnLeftEyebrow: m.GauzeDressingDoneOnLeftEyebrow = v; break;
        case TreatmentDisplay.NasalCannulaApplied: m.NasalCannulaApplied = v; break;
        case TreatmentDisplay.CervicalCollarOnNeck: m.CervicalCollarOnNeck = v; break;
        case TreatmentDisplay.IntravenousStandAttached: m.IntravenousStandAttached = v; break;
        case TreatmentDisplay.IntravenousHangerAttached: m.IntravenousHangerAttached = v; break;
        case TreatmentDisplay.IntravenousFluidAttached: m.IntravenousFluidAttached = v; break;
      }
    }

    private static bool GetDisplayStateFlag(PatientDisplayState state, TreatmentDisplay d, bool fromSupports)
    {
      // 읽기 전용이므로 구조체 복사본을 사용한다(ref 불필요).
      var m = fromSupports ? state.DisplaySupports : state.DisplayState;
      switch (d)
      {
        case TreatmentDisplay.Syringe18GInsertedIntoLeftArm: return m.Syringe18GInsertedIntoLeftArm;
        case TreatmentDisplay.Syringe18GInsertedIntoRightArm: return m.Syringe18GInsertedIntoRightArm;
        case TreatmentDisplay.Syringe20GInsertedIntoLeftArm: return m.Syringe20GInsertedIntoLeftArm;
        case TreatmentDisplay.Syringe20GInsertedIntoRightArm: return m.Syringe20GInsertedIntoRightArm;
        case TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian: return m.CentralVenousCatheterInsertedIntoSubclavian;
        case TreatmentDisplay.LaryngoscopeInserted: return m.LaryngoscopeInserted;
        case TreatmentDisplay.EndotrachealTubeStyletInserted: return m.EndotrachealTubeStyletInserted;
        case TreatmentDisplay.EndotrachealTubeInsertDone: return m.EndotrachealTubeInsertDone;
        case TreatmentDisplay.TPieceAttachedToNasalCannula: return m.TPieceAttachedToNasalCannula;
        case TreatmentDisplay.AmbuBagAttachedToEndotrachealTube: return m.AmbuBagAttachedToEndotrachealTube;
        case TreatmentDisplay.GauzePatchedOnThorax: return m.GauzePatchedOnThorax;
        case TreatmentDisplay.GauzeDressingDoneOnThorax: return m.GauzeDressingDoneOnThorax;
        case TreatmentDisplay.GauzePatchedOnRightArm: return m.GauzePatchedOnRightArm;
        case TreatmentDisplay.GauzeDressingDoneOnRightArm: return m.GauzeDressingDoneOnRightArm;
        case TreatmentDisplay.GauzePatchedOnLeftArm: return m.GauzePatchedOnLeftArm;
        case TreatmentDisplay.GauzeDressingDoneOnLeftArm: return m.GauzeDressingDoneOnLeftArm;
        case TreatmentDisplay.GauzePatchedOnRightEyebrow: return m.GauzePatchedOnRightEyebrow;
        case TreatmentDisplay.GauzeDressingDoneOnRightEyebrow: return m.GauzeDressingDoneOnRightEyebrow;
        case TreatmentDisplay.GauzePatchedOnLeftEyebrow: return m.GauzePatchedOnLeftEyebrow;
        case TreatmentDisplay.GauzeDressingDoneOnLeftEyebrow: return m.GauzeDressingDoneOnLeftEyebrow;
        case TreatmentDisplay.NasalCannulaApplied: return m.NasalCannulaApplied;
        case TreatmentDisplay.CervicalCollarOnNeck: return m.CervicalCollarOnNeck;
        case TreatmentDisplay.IntravenousStandAttached: return m.IntravenousStandAttached;
        case TreatmentDisplay.IntravenousHangerAttached: return m.IntravenousHangerAttached;
        case TreatmentDisplay.IntravenousFluidAttached: return m.IntravenousFluidAttached;
        default: return false;
      }
    }
  }
}
