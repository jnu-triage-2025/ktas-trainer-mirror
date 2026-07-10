using System;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.UI.Models;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 그래픽 설정 UI에서 UI 배율 단계 하나를 나타내는 VisualElement입니다.
  ///
  /// 각 단계(1~4)마다 하나씩 생성되며,
  /// 클릭 시 <see cref="OnOptionSelected"/> 이벤트로 선택된 배율을 상위에 알립니다.
  /// <see cref="SetActive"/>로 현재 선택된 항목임을 강조합니다.
  /// </summary>
  [UxmlElement]
  public partial class UIScaleOptionElement : VisualElement
  {
    // ──────────────────────────────────────────────────────────────────────────
    // USS 클래스 이름
    // ──────────────────────────────────────────────────────────────────────────
    private const string BaseClass     = "ui-scale-option";
    private const string ActiveClass   = "ui-scale-option--active";
    private const string LabelClass    = "ui-scale-option__label";
    private const string SubtitleClass = "ui-scale-option__subtitle";

    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>이 옵션을 클릭했을 때 해당 배율 단계가 전달됩니다.</summary>
    public event Action<UIScale> OnOptionSelected;

    // ──────────────────────────────────────────────────────────────────────────
    // 상태
    // ──────────────────────────────────────────────────────────────────────────
    private UIScale _scale;
    private Label _label;
    private Label _subtitle;

    /// <summary>현재 바인딩된 배율 단계입니다.</summary>
    public UIScale BoundScale => _scale;

    // ──────────────────────────────────────────────────────────────────────────
    // 생성자
    // ──────────────────────────────────────────────────────────────────────────
    public UIScaleOptionElement()
    {
      AddToClassList(BaseClass);
      BuildLayout();
      RegisterCallback<ClickEvent>(OnClick);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 표시할 배율 데이터를 바인딩합니다.
    /// </summary>
    /// <param name="scale">이 옵션이 나타내는 배율 단계</param>
    public void Bind(UIScale scale)
    {
      _scale         = scale;
      _label.text    = ScaleToLabel(scale);
      _subtitle.text = ScaleToSubtitle(scale);
    }

    /// <summary>
    /// 이 옵션이 현재 선택된 옵션인지 여부를 설정합니다.
    /// </summary>
    /// <param name="active"><c>true</c>이면 활성(선택됨) 상태로 강조합니다.</param>
    public void SetActive(bool active)
    {
      if (active)
        AddToClassList(ActiveClass);
      else
        RemoveFromClassList(ActiveClass);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 레이아웃 구성
    // ──────────────────────────────────────────────────────────────────────────
    private void BuildLayout()
    {
      _label = new Label();
      _label.AddToClassList(LabelClass);

      _subtitle = new Label();
      _subtitle.AddToClassList(SubtitleClass);

      Add(_label);
      Add(_subtitle);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트 핸들러
    // ──────────────────────────────────────────────────────────────────────────
    private void OnClick(ClickEvent _) => OnOptionSelected?.Invoke(_scale);

    // ──────────────────────────────────────────────────────────────────────────
    // 정적 유틸
    // ──────────────────────────────────────────────────────────────────────────
    private static string ScaleToLabel(UIScale scale) => scale switch
    {
      UIScale.Level1 => "1단계",
      UIScale.Level2 => "2단계",
      UIScale.Level3 => "3단계",
      UIScale.Level4 => "4단계",
      _              => scale.ToString(),
    };

    private static string ScaleToSubtitle(UIScale scale)
    {
      var factor = UIScalePreferenceService.ToScaleFactor(scale);
      var suffix = scale == UIScale.Level2 ? " (기본)" : string.Empty;
      return $"{factor * 100f:0}%{suffix}";
    }
  }
}
