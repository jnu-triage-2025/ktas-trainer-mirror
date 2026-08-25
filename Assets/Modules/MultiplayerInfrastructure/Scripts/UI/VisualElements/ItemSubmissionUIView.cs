using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 아이템 제출 패널의 순수 뷰.
  ///
  /// 레이아웃:
  ///  - 배경: 화면 전체를 덮는 반투명 딤(dim)으로 뒤 배경을 어둡게 처리(모달 느낌 강조)
  ///  - 헤더: 제목(좌) + × 닫기 버튼(우) + 하단 구분선
  ///  - 설명 문구: 무엇을 해야 하는지 짧게 안내
  ///  - "필요 아이템" 섹션 라벨
  ///  - 요구 아이템 그리드: 슬롯마다 아이콘(placeholder) + 아이템 이름 + 보유/요구 수량 세로 배치
  ///    · 미충족 → 아이콘 흐림/회색, 파란 수량 텍스트, navy 테두리
  ///    · 충족   → 아이콘 선명, 녹색 수량 텍스트, 녹색 테두리·배경, 살짝 확대(scale) 강조
  ///  - 제출(녹색) / 취소(빨간) 버튼 행: hover/active 시 색상·scale 트랜지션
  /// </summary>
  [UxmlElement]
  public partial class ItemSubmissionUIView : VisualElement
  {
    public event Action SubmitClicked;
    public event Action CloseClicked;

    private Label _titleLabel;
    private VisualElement _requirementGrid;
    private Button _submitButton;
    private Button _closeButton;

    private readonly List<RequirementSlot> _slots = new();
    private string _submitButtonText = "제출";

    public bool IsVisible => style.display != DisplayStyle.None;

    private sealed class RequirementSlot
    {
      public ItemRequirement Requirement;
      public VisualElement Root;
      public Image Icon;
      public Label NameLabel;
      public Label CountLabel;
    }

    public ItemSubmissionUIView()
    {
      AddToClassList("item-submission-root");
      BuildLayout();
      SetVisible(false);
    }

    private void BuildLayout()
    {
      Clear();

      var panel = new VisualElement { name = "ItemSubmissionPanel" };
      panel.AddToClassList("item-submission-panel");
      Add(panel);

      // ── 헤더 (제목 좌 + × 닫기 우) ──────────────────────────────────────
      var header = new VisualElement { name = "ItemSubmissionHeader" };
      header.AddToClassList("item-submission-header");
      panel.Add(header);

      _titleLabel = new Label { name = "ItemSubmissionTitle", text = "아이템 제출" };
      _titleLabel.AddToClassList("item-submission-title");
      header.Add(_titleLabel);

      // × 닫기 버튼: 헤더 우측
      var closeX = new Button(() => CloseClicked?.Invoke()) { name = "ItemSubmissionCloseX", text = "×" };
      closeX.AddToClassList("item-submission-close-x");
      header.Add(closeX);

      // ── 설명 문구 ────────────────────────────────────────────────────────
      var description = new Label
      {
        name = "ItemSubmissionDescription",
        text = "필요한 아이템을 모두 보유하면 제출 버튼을 클릭하세요."
      };
      description.AddToClassList("item-submission-description");
      panel.Add(description);

      // ── 섹션 라벨 ────────────────────────────────────────────────────────
      var sectionLabel = new Label { name = "ItemSubmissionSectionLabel", text = "필요 아이템" };
      sectionLabel.AddToClassList("item-submission-section-label");
      panel.Add(sectionLabel);

      // ── 요구 아이템 그리드 ───────────────────────────────────────────────
      _requirementGrid = new VisualElement { name = "ItemSubmissionRequirements" };
      _requirementGrid.AddToClassList("item-submission-requirements");
      panel.Add(_requirementGrid);

      // ── 버튼 행 ──────────────────────────────────────────────────────────
      var buttonRow = new VisualElement { name = "ItemSubmissionButtons" };
      buttonRow.AddToClassList("item-submission-buttons");
      panel.Add(buttonRow);

      _submitButton = new Button(() => SubmitClicked?.Invoke())
      {
        name = "ItemSubmissionSubmit",
        text = _submitButtonText
      };
      _submitButton.AddToClassList("item-submission-submit");
      buttonRow.Add(_submitButton);

      _closeButton = new Button(() => CloseClicked?.Invoke())
      {
        name = "ItemSubmissionClose",
        text = "취소"
      };
      _closeButton.AddToClassList("item-submission-close");
      buttonRow.Add(_closeButton);
    }

    public void SetVisible(bool visible)
    {
      style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
      style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>패널을 특정 요구 사항 세트로 구성한다.</summary>
    public void Configure(string title, string submitButtonText, IReadOnlyList<ItemRequirement> requirements)
    {
      _submitButtonText = string.IsNullOrWhiteSpace(submitButtonText) ? "제출" : submitButtonText;
      if (_titleLabel != null)
        _titleLabel.text = string.IsNullOrWhiteSpace(title) ? "아이템 제출" : title;
      if (_submitButton != null)
        _submitButton.text = _submitButtonText;

      _slots.Clear();
      _requirementGrid?.Clear();

      if (requirements != null)
      {
        for (int i = 0; i < requirements.Count; i++)
          AddRequirementSlot(requirements[i]);
      }
    }

    private void AddRequirementSlot(ItemRequirement requirement)
    {
      var slotRoot = new VisualElement();
      slotRoot.AddToClassList("item-submission-slot");

      // 아이콘: 슬롯 내부 중앙 상단 (세로 배치의 첫 요소)
      // 기본 상태 USS → 흐리고 회색 tint (placeholder). 충족 시 선명하게 전환.
      var icon = new Image { name = "RequirementIcon", pickingMode = PickingMode.Ignore };
      icon.AddToClassList("item-submission-slot__icon");
      var sprite = Registry.Registry.GetOrLoadIconSprite(requirement.identifier);
      if (sprite != null)
        icon.image = sprite.texture;
      slotRoot.Add(icon);

      // 아이템 이름: 아이콘 아래, 수량 위. identifier 로부터 표시 이름을 조회한다.
      var nameLabel = new Label
      {
        name = "RequirementName",
        pickingMode = PickingMode.Ignore,
        text = Registry.Registry.GetItemDisplayName(requirement.identifier)
      };
      nameLabel.AddToClassList("item-submission-slot__name");
      slotRoot.Add(nameLabel);

      // 수량 텍스트: 이름 바로 아래 (예제의 "보유/요구" 패턴)
      var countLabel = new Label
      {
        name = "RequirementCount",
        pickingMode = PickingMode.Ignore,
        text = $"0/{requirement.count}"
      };
      countLabel.AddToClassList("item-submission-slot__count");
      slotRoot.Add(countLabel);

      _requirementGrid.Add(slotRoot);
      _slots.Add(new RequirementSlot
      {
        Requirement = requirement,
        Root = slotRoot,
        Icon = icon,
        NameLabel = nameLabel,
        CountLabel = countLabel
      });
    }

    /// <summary>
    /// 플레이어 보유량을 반영하여 각 요구 칸의 표시를 갱신하고,
    /// 모든 요구가 충족되면 제출 버튼을 활성화한다.
    /// </summary>
    public void UpdateHeldCounts(Func<string, int> heldCountResolver)
    {
      bool allSatisfied = _slots.Count > 0;

      foreach (var slot in _slots)
      {
        int held = heldCountResolver != null ? heldCountResolver(slot.Requirement.identifier) : 0;
        int need = slot.Requirement.count;
        bool satisfied = held >= need;

        if (slot.CountLabel != null)
          slot.CountLabel.text = $"{held}/{need}";

        slot.Root.EnableInClassList("item-submission-slot--satisfied", satisfied);
        slot.Root.EnableInClassList("item-submission-slot--unsatisfied", !satisfied);

        if (!satisfied)
          allSatisfied = false;
      }

      if (_submitButton != null)
        _submitButton.SetEnabled(allSatisfied);
    }
  }
}
