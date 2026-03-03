using System;
using MultiplayerInfrastructure.UI.Models;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 그래픽 설정 UI에서 텍스처 품질 단계 하나를 나타내는 VisualElement입니다.
  ///
  /// 각 옵션(Ultra / High / Medium / Low)마다 하나씩 생성되며,
  /// 클릭 시 <see cref="OnOptionSelected"/> 이벤트로 선택된 품질을 상위에 알립니다.
  /// <see cref="SetActive"/>로 현재 선택된 항목임을 강조합니다.
  /// </summary>
  [UxmlElement]
  public partial class TextureQualityOptionElement : VisualElement
  {
    // ──────────────────────────────────────────────────────────────────────────
    // USS 클래스 이름
    // ──────────────────────────────────────────────────────────────────────────
    private const string BaseClass      = "texture-quality-option";
    private const string ActiveClass    = "texture-quality-option--active";
    private const string LabelClass     = "texture-quality-option__label";
    private const string SubtitleClass  = "texture-quality-option__subtitle";

    // ──────────────────────────────────────────────────────────────────────────
    // 이벤트
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>이 옵션을 클릭했을 때 해당 품질 단계가 전달됩니다.</summary>
    public event Action<TextureQuality> OnOptionSelected;

    // ──────────────────────────────────────────────────────────────────────────
    // 상태
    // ──────────────────────────────────────────────────────────────────────────
    private TextureQuality _quality;
    private Label _label;
    private Label _subtitle;

    /// <summary>현재 바인딩된 품질 단계입니다.</summary>
    public TextureQuality BoundQuality => _quality;

    // ──────────────────────────────────────────────────────────────────────────
    // 생성자
    // ──────────────────────────────────────────────────────────────────────────
    public TextureQualityOptionElement()
    {
      AddToClassList(BaseClass);
      BuildLayout();
      RegisterCallback<ClickEvent>(OnClick);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 표시할 품질 데이터를 바인딩합니다.
    /// </summary>
    /// <param name="quality">이 옵션이 나타내는 품질 단계</param>
    public void Bind(TextureQuality quality)
    {
      _quality  = quality;
      _label.text    = QualityToLabel(quality);
      _subtitle.text = QualityToSubtitle(quality);
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
    private void OnClick(ClickEvent _) => OnOptionSelected?.Invoke(_quality);

    // ──────────────────────────────────────────────────────────────────────────
    // 정적 유틸
    // ──────────────────────────────────────────────────────────────────────────
    private static string QualityToLabel(TextureQuality quality) => quality switch
    {
      TextureQuality.Ultra  => "매우 높음",
      TextureQuality.High   => "높음",
      TextureQuality.Medium => "보통",
      TextureQuality.Low    => "낮음",
      _                     => quality.ToString(),
    };

    private static string QualityToSubtitle(TextureQuality quality) => quality switch
    {
      TextureQuality.Ultra  => "원본 해상도",
      TextureQuality.High   => "1/2 해상도",
      TextureQuality.Medium => "1/4 해상도",
      TextureQuality.Low    => "1/8 해상도",
      _                     => string.Empty,
    };
  }
}
