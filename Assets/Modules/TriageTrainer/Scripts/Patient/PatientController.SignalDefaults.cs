using System.Collections.Generic;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 처치 적용/사용/사정 신호의 <b>코드 하드코딩 기본값</b> 부분 구현.
  ///
  /// <para>
  /// 운영자가 인스펙터에 매핑을 입력하지 않아도(또는 컴포넌트 Reset 으로 직렬화 필드가 비워져도)
  /// 기본 신호가 항상 동작하도록, 기본 매핑을 <b>직렬화 필드가 아닌 코드 상수</b>로 둔다.
  /// 인스펙터에 동일 아이템/사정 항목이 명시되어 있으면 그쪽이 우선(override)한다.
  /// </para>
  ///
  /// <para>
  /// 환자별로 달라지는 신호(예: suction_&lt;id&gt;, check_avpu_gcs_&lt;id&gt;)는 런타임에
  /// 환자 <see cref="Identifier"/> 로부터 계산한다. Identifier 가 변할 수 있으므로(스폰 주입),
  /// 기본값은 캐시하지 않고 신호 발생 시점에 해석한다.
  /// </para>
  /// </summary>
  public partial class PatientController
  {
    // ── 환자 무관(공통) 기본값: 아이템 식별자 → 신호 ────────────────────

    /// <summary>아이템 사용(부착 없음) 기본 신호. 예: 흡인기/앰부/약물.</summary>
    private static readonly Dictionary<string, string> DefaultItemUseSignals = new()
    {
      { "yankauer", "suction_{id}" },        // 흡인 (환자별)
      { "yankauer_ready", "suction_{id}" },
      { "ambubag", "start_ambu" },           // 앰부 백 (공통)
      { "epinephrine_ampule", "push_epi" },  // 에피네프린 투여 (공통)
      { "normal_saline_20ml", "push_ns" },   // 생리식염수 주입 (공통)
    };

    /// <summary>아이템 부착(시각 적용) 시 함께 올릴 기본 신호. 예: 거즈/장갑/전극/고정기.</summary>
    private static readonly Dictionary<string, string[]> DefaultApplySignals = new()
    {
      { "gauze", new[] { "apply_gauze" } },
      { "gloves", new[] { "wear_glove" } },
      { "electrode", new[] { "apply_electrode" } },
      { "neckstabilizer", new[] { "apply_stabilizer_{id}" } },
      // plaster 는 맥락에 따라 거즈 위/기관내관 고정 두 신호가 모두 쓰인다(아이템만으로 구분 불가).
      // 각 신호는 해당 게이트가 존재하는 구간에서만 의미가 있으므로 둘 다 올린다.
      { "plaster", new[] { "apply_plaster_on_gauze", "apply_plaster_on_intu" } },
    };

    // ── 사정 기본값: 사정 동작 식별자 → 신호 템플릿 ─────────────────────

    /// <summary>
    /// Assess Action 의 식별자(권장 표준)별 기본 신호 템플릿. AssessSignal 이 비어 있을 때 적용된다.
    /// </summary>
    private static readonly Dictionary<string, string> DefaultAssessSignals = new()
    {
      { "assess_avpu_gcs", "check_avpu_gcs_{id}" },
      { "assess_pulse", "check_pulse_{id}" },
      { "assess_gcs", "check_gcs_{id}" },
      { "assess_vital", "check_vital_{id}" },
    };

    /// <summary>
    /// 신호 템플릿의 "{id}" 를 현재 환자 Identifier 로 치환한다. Identifier 가 없으면 "{id}" 만 제거.
    /// </summary>
    private string ResolveSignalTemplate(string template)
    {
      if (string.IsNullOrWhiteSpace(template))
        return template;

      if (template.IndexOf("{id}", System.StringComparison.Ordinal) < 0)
        return template;

      string id = Identifier;
      return template.Replace("{id}", string.IsNullOrWhiteSpace(id) ? string.Empty : id);
    }

    /// <summary>아이템 사용 기본 신호(인스펙터 미지정 시)를 해석한다. 없으면 null.</summary>
    private string ResolveDefaultItemUseSignal(string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return null;

      return DefaultItemUseSignals.TryGetValue(itemIdentifier, out var template)
          ? ResolveSignalTemplate(template)
          : null;
    }

    /// <summary>아이템 부착 기본 신호 목록(인스펙터 미지정 시)을 해석한다. 없으면 빈 목록.</summary>
    private IEnumerable<string> ResolveDefaultApplySignals(string itemIdentifier)
    {
      if (!string.IsNullOrWhiteSpace(itemIdentifier)
          && DefaultApplySignals.TryGetValue(itemIdentifier, out var templates))
      {
        for (int i = 0; i < templates.Length; i++)
        {
          var resolved = ResolveSignalTemplate(templates[i]);
          if (!string.IsNullOrWhiteSpace(resolved))
            yield return resolved;
        }
      }
    }

    /// <summary>사정 기본 신호(AssessSignal 미지정 시)를 식별자 규칙으로 해석한다. 없으면 null.</summary>
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
