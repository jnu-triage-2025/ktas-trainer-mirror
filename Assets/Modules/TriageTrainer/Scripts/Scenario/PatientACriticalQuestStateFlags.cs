using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Quest;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// `patient_a_critical` 시나리오가 사용하는 퀘스트 상태 플래그와, 그 플래그가 여닫는 상호작용 표를 담는다.
  ///
  /// <para>
  /// 이 시나리오는 상호작용 노출을 엔티티에 저장된 활성 플래그가 아니라
  /// <see cref="PlayerQuestStateFlagService"/>의 플레이어별 플래그 풀로 판정한다. 엔티티 활성 플래그는
  /// 환자 인스턴스 하나에 공유되어 있어서, 어떤 플레이어의 퀘스트 단계가 다른 플레이어의 상호작용
  /// 목록까지 바꿔 버린다. 퀘스트가 플레이어별로 발행되므로 노출 판정도 플레이어별이어야 한다.
  /// </para>
  ///
  /// <para>
  /// 게이트는 이 시나리오가 실행 중일 때만(<see cref="IsArmed"/>) 동작한다. 표에 없는 상호작용과
  /// 다른 시나리오는 기존 판정 경로를 그대로 쓴다.
  /// </para>
  /// </summary>
  public static class PatientACriticalQuestStateFlags
  {
    public const string ScenarioIdentifier = "patient_a_critical";

    /// <summary>남성 환자 활력징후 사정(`nurse_b`).</summary>
    public const string VitalAssess = "scen_a.assess_vital";

    /// <summary>남성 환자 의식상태 사정(`nurse_c`).</summary>
    public const string AvpuGcsAssess = "scen_a.assess_avpu_gcs";

    /// <summary>심정지 직후 맥박 확인. ROSC 시점에 내려간다.</summary>
    public const string ArrestPulseAssess = "scen_a.assess_pulse_r1";

    /// <summary>CPR 1주기 처치 동작(가슴압박·앰부·제세동 패드·T-piece 해제).</summary>
    public const string Cpr1Actions = "scen_a.cpr1_actions";

    /// <summary>삽관 보조의 스타일렛 제거.</summary>
    public const string StyletRemoval = "scen_a.stylet_removal";

    /// <summary>T-piece 산소 연결.</summary>
    public const string TpieceAttach = "scen_a.tpiece_attach";

    /// <summary>CPR 2주기 처치 동작.</summary>
    public const string Cpr2Actions = "scen_a.cpr2_actions";

    /// <summary>의복 제거.</summary>
    public const string ClothingRemoval = "scen_a.clothing_removal";

    /// <summary>ROSC 이후 맥박 재사정.</summary>
    public const string RoscPulseAssess = "scen_a.rosc_pulse_assess";

    /// <summary>ROSC 이후 의식상태 재사정.</summary>
    public const string RoscGcsAssess = "scen_a.rosc_gcs_assess";

    /// <summary>이 시나리오에서 플래그가 여닫는 상호작용. 키는 `엔티티/상호작용` 주소다.</summary>
    private static readonly Dictionary<string, string> FlagByInteraction = new(StringComparer.Ordinal)
    {
      { Address("patient_a", "assess_vital"), VitalAssess },
      { Address("patient_a", "assess_avpu_gcs"), AvpuGcsAssess },
      { Address("patient_a", "assess_pulse_r1"), ArrestPulseAssess },
      { Address("patient_a", "click_to_start_comp"), Cpr1Actions },
      { Address("patient_a", "start_ambu_r1"), Cpr1Actions },
      { Address("patient_a", "interact_patient_chest"), Cpr1Actions },
      { Address("patient_a", "remove_tpiece"), Cpr1Actions },
      { Address("patient_a", "connect_ambubag"), Cpr1Actions },
      { Address("patient_a", "connect_o2_to_ambu"), Cpr1Actions },
      { Address("patient_a", "remove_intu_stylet"), StyletRemoval },
      { Address("patient_a", "interact_tpiece"), TpieceAttach },
      { Address("patient_a", "interact_chest"), Cpr2Actions },
      { Address("patient_a", "start_ambu_r2"), Cpr2Actions },
      { Address("patient_a", "remove_patient_clothing"), ClothingRemoval },
      { Address("patient_a", "assess_pulse_r2"), RoscPulseAssess },
      { Address("patient_a", "assess_gcs_rosc"), RoscGcsAssess },
    };

    /// <summary>이 시나리오가 실행 중이라 플래그 판정이 켜져 있는지 여부.</summary>
    public static bool IsArmed { get; private set; }

    private static string Address(string entityIdentifier, string interactionIdentifier)
      => entityIdentifier + "/" + interactionIdentifier;

    /// <summary>시나리오가 시작될 때 게이트를 켠다. 다른 시나리오 식별자는 무시한다.</summary>
    public static void ArmFor(string scenarioIdentifier)
    {
      if (!string.Equals(scenarioIdentifier, ScenarioIdentifier, StringComparison.Ordinal))
        return;

      IsArmed = true;
      // 인스펙터 같은 도구가 이 시나리오의 플래그를 목록에서 고를 수 있게 어휘를 알린다.
      PlayerQuestStateFlagService.RegisterKnownFlags(FlagByInteraction.Values);
      // 이전 회차의 플래그가 남아 있으면 첫 단계부터 상호작용이 열려 있게 된다.
      PlayerQuestStateFlagService.ClearAll();
    }

    /// <summary>시나리오가 끝날 때 게이트를 끄고 이 시나리오가 올린 플래그를 모두 내린다.</summary>
    public static void Disarm()
    {
      if (!IsArmed)
        return;

      IsArmed = false;
      PlayerQuestStateFlagService.ClearKnownFlags();
      PlayerQuestStateFlagService.ClearAll();
    }

    /// <summary>
    /// 상호작용이 이 시나리오의 플래그 게이트 대상인지 판정하고, 대상이면 노출 여부를 돌려준다.
    /// </summary>
    /// <returns>플래그가 노출을 결정하는 상호작용이면 true. false면 호출자가 기존 판정을 그대로 쓴다.</returns>
    public static bool TryEvaluate(
      string entityIdentifier, string interactionIdentifier, PlayerController player, out bool allowed)
    {
      allowed = false;
      if (!IsArmed
          || string.IsNullOrWhiteSpace(entityIdentifier)
          || string.IsNullOrWhiteSpace(interactionIdentifier))
        return false;

      if (!FlagByInteraction.TryGetValue(
            Address(entityIdentifier.Trim(), interactionIdentifier.Trim()), out string flag))
        return false;

      allowed = player != null && PlayerQuestStateFlagService.Has(player.UserIdentifier, flag);
      return true;
    }

    /// <summary>테스트와 진단용. 지정한 상호작용을 여는 플래그를 반환한다(없으면 null).</summary>
    public static string FindFlag(string entityIdentifier, string interactionIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || string.IsNullOrWhiteSpace(interactionIdentifier))
        return null;

      return FlagByInteraction.TryGetValue(
        Address(entityIdentifier.Trim(), interactionIdentifier.Trim()), out string flag) ? flag : null;
    }

    /// <summary>
    /// 단계 플래그뿐 아니라 현재 퀘스트의 표시 바인딩까지 활성화되어야 하는 상호작용인지 반환한다.
    /// 제세동 패드는 CPR 1주기 공용 플래그가 열린 동안에도 담당자의 제세동기 퀘스트에서만 보여야 한다.
    /// </summary>
    public static bool RequiresActiveQuestBinding(
      string entityIdentifier, string interactionIdentifier)
      => IsArmed
         && string.Equals(entityIdentifier?.Trim(), "patient_a", StringComparison.Ordinal)
         && string.Equals(interactionIdentifier?.Trim(), "interact_patient_chest", StringComparison.Ordinal);

    /// <summary>테스트와 진단용. 게이트 대상 상호작용 주소를 모두 반환한다.</summary>
    public static IReadOnlyCollection<string> GatedInteractionAddresses => FlagByInteraction.Keys;
  }
}
