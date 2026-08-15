using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class InteractableObjectHintList : ScrollView
  {
    // 한 번에 상하로 표시할 목표 항목 수(선택 항목 포함, 홀수 권장).
    // 5 이면 선택 항목을 중앙에 두고 위/아래로 각각 2개씩 보이도록 페이드 범위를 잡는다.
    private const int VisibleRowCount = 5;

    // 중앙(선택 항목) 기준으로 완전 불투명하게 유지할 행 수.
    // 1 이면 선택 항목과 바로 위/아래 항목까지는 흐려지지 않는다.
    private const float FullOpacityRows = 2f;

    private int _selectedIndex = -1;

    public InteractableObjectHintList()
        : base(ScrollViewMode.Vertical)
    {
      name = "interactable-scroll";
      AddToClassList("interactable-scroll");

      // 스크롤바는 사용하지 않는다. 선택 항목이 항상 세로 중앙에 오도록
      // contentContainer 를 직접 위/아래로 이동시키기 때문이다.
      horizontalScrollerVisibility = ScrollerVisibility.Hidden;
      verticalScrollerVisibility = ScrollerVisibility.Hidden;

      style.flexGrow = 1f;
      style.alignSelf = Align.Center; // center horizontally within parent
      style.justifyContent = Justify.Center; // center vertically within parent flow
      style.alignItems = Align.Center;

      // left-align children inside the scroll content
      contentContainer.style.alignItems = Align.FlexStart;
      contentContainer.style.justifyContent = Justify.FlexStart;

      // 뷰포트/컨텐츠 지오메트리가 바뀔 때마다 중앙 정렬과 페이드를 다시 계산한다.
      RegisterCallback<GeometryChangedEvent>(_ => UpdateCenteringAndFade());
      contentContainer.RegisterCallback<GeometryChangedEvent>(_ => UpdateCenteringAndFade());
    }

    public void Rebuild(
        IReadOnlyList<IInteract> interacts,
        int selectedIndex,
        InteractableHintUIMode mode,
        string keyLabel,
        Sprite dialogueIcon,
        Action<int> onClicked = null)
    {
      contentContainer.Clear();

      _selectedIndex = selectedIndex;

      if (interacts == null || interacts.Count == 0)
        return;

      for (int i = 0; i < interacts.Count; i++)
      {
        int clickedIndex = i;
        var element = new InteractableObjectHintListElement();
        element.Bind(interacts[i], keyLabel, mode, dialogueIcon, i == selectedIndex);
        if (onClicked != null)
        {
          element.RegisterCallback<ClickEvent>(evt =>
          {
            evt.StopPropagation();
            onClicked(clickedIndex);
          });
        }
        contentContainer.Add(element);
      }

      ApplyStyles();
      ScrollToSelected(selectedIndex);
    }

    public void ScrollToSelected(int selectedIndex)
    {
      _selectedIndex = selectedIndex;

      // 레이아웃이 확정된 뒤 중앙 정렬/페이드를 계산해야 하므로 다음 프레임에 예약한다.
      schedule.Execute(UpdateCenteringAndFade);
      UpdateCenteringAndFade();
    }

    /// <summary>
    /// 선택된 항목의 세로 중심이 뷰포트의 세로 중앙에 오도록 contentContainer 전체를
    /// 위/아래로 이동시키고, 뷰포트 상/하단 경계로 갈수록 항목이 그라데이션으로
    /// 사라지도록 각 항목의 불투명도를 조정한다.
    /// </summary>
    private void UpdateCenteringAndFade()
    {
      var viewport = VisualElementContentViewport;
      if (viewport == null)
        return;

      int childCount = contentContainer.childCount;
      if (childCount == 0)
        return;

      float viewportHeight = viewport.resolvedStyle.height;
      if (viewportHeight <= 0f || float.IsNaN(viewportHeight))
        return;

      float viewportCenter = viewportHeight * 0.5f;

      // contentContainer 가 뷰포트 내부에서 flex 배치로 인해 세로로 이동해 있을 수 있으므로
      // 그 오프셋을 보정하여 항목 좌표를 뷰포트 좌표로 변환한다.
      float containerTop = contentContainer.layout.y;
      if (float.IsNaN(containerTop))
        containerTop = 0f;

      // 선택 항목 (범위를 벗어나면 첫 항목 기준)
      int selected = _selectedIndex;
      if (selected < 0 || selected >= childCount)
        selected = 0;

      var selectedElement = contentContainer[selected];
      float selectedTop = selectedElement.layout.y;
      float selectedHeight = selectedElement.layout.height;
      if (float.IsNaN(selectedTop) || float.IsNaN(selectedHeight))
        return;

      // 선택 항목 중심(뷰포트 좌표, translate 적용 전)
      float selectedCenter = containerTop + selectedTop + selectedHeight * 0.5f;

      // 선택 항목의 중심이 뷰포트 중앙에 오도록 컨텐츠 전체를 이동.
      // (스크롤 대신 transform translate 를 사용하여 첫/마지막 항목도 중앙에 올 수 있게 한다.)
      float offset = viewportCenter - selectedCenter;
      contentContainer.style.translate = new Translate(0f, offset);

      // 행 간격(pitch): 인접한 두 항목 중심 간 거리. 항목 크기가 바뀌어도
      // 항상 VisibleRowCount 개가 보이도록 실제 레이아웃에서 측정한다.
      float rowPitch = MeasureRowPitch(selectedHeight);

      // 중앙 기준으로 VisibleRowCount 개(위/아래 대칭)가 보이도록 페이드 범위를 계산한다.
      // 예: 5개면 중앙에서 위/아래로 각각 2.5 pitch 지점에서 완전히 사라지게 한다.
      float halfWindow = (VisibleRowCount * 0.5f) * rowPitch;
      float fullOpacityDistance = FullOpacityRows * rowPitch;
      float fadeDistance = Mathf.Max(1f, halfWindow - fullOpacityDistance);

      // 각 항목의 뷰포트 기준 중심 위치를 계산하여, 중앙에서 멀어질수록 페이드아웃.
      for (int i = 0; i < childCount; i++)
      {
        var child = contentContainer[i];
        float childTop = child.layout.y;
        float childHeight = child.layout.height;
        if (float.IsNaN(childTop) || float.IsNaN(childHeight))
          continue;

        float childCenterInViewport = containerTop + childTop + childHeight * 0.5f + offset;
        float distanceFromCenter = Mathf.Abs(childCenterInViewport - viewportCenter);

        float opacity;
        if (distanceFromCenter <= fullOpacityDistance)
        {
          opacity = 1f;
        }
        else
        {
          float t = (distanceFromCenter - fullOpacityDistance) / fadeDistance;
          opacity = Mathf.Clamp01(1f - t);
          // 부드러운 그라데이션 곡선 (선형보다 경계면을 더 흐릿하게)
          opacity = opacity * opacity;
        }

        child.style.opacity = opacity;
      }
    }

    /// <summary>
    /// 인접 항목 중심 간의 실제 간격(pitch)을 측정한다. 항목이 2개 이상이면
    /// 처음 두 항목의 중심 거리를 사용하고, 그렇지 않으면 선택 항목 높이에
    /// 기본 여백을 더한 근사값을 사용한다.
    /// </summary>
    private float MeasureRowPitch(float fallbackHeight)
    {
      if (contentContainer.childCount >= 2)
      {
        var a = contentContainer[0];
        var b = contentContainer[1];
        float ay = a.layout.y + a.layout.height * 0.5f;
        float by = b.layout.y + b.layout.height * 0.5f;
        float pitch = Mathf.Abs(by - ay);
        if (!float.IsNaN(pitch) && pitch > 1f)
          return pitch;
      }

      if (!float.IsNaN(fallbackHeight) && fallbackHeight > 1f)
        return fallbackHeight + 3f; // marginBottom 근사

      return 30f;
    }

    const string UssSelectorContentViewport = "unity-content-viewport";
    VisualElement _visualElementContentViewport;
    VisualElement VisualElementContentViewport
    {
      get
      {
        if (_visualElementContentViewport == null)
        {
          _visualElementContentViewport = this.Q(name: UssSelectorContentViewport);
        }
        return _visualElementContentViewport;
      }
    }

    public void ApplyStyles()
    {
      var viewport = VisualElementContentViewport;
      if (viewport == null)
        return;

      viewport.style.flexDirection = FlexDirection.Row;
      viewport.style.paddingLeft = 300;

      // 세로 중앙 정렬은 contentContainer 를 직접 translate 하여 처리하므로,
      // 뷰포트는 컨텐츠를 위쪽(y=0)에 맞춰 배치해야 한다. 이렇게 하면
      // contentContainer 로컬 좌표와 뷰포트 좌표의 세로 축이 일치하여
      // 중앙 정렬 계산이 정확해진다.
      viewport.style.alignItems = Align.FlexStart;

      // 중앙 정렬을 위해 컨텐츠가 위/아래로 잘려나가도(의도된 동작) 뷰포트 밖으로
      // 그려지지 않도록 클리핑한다. 잘린 경계면은 페이드로 흐릿하게 처리된다.
      viewport.style.overflow = Overflow.Hidden;
    }
  }
}
