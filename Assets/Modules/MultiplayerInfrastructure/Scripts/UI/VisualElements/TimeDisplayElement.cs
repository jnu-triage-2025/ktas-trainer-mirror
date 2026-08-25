using MultiplayerInfrastructure.Scenario;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 시간 표시(스톱워치/카운트다운) HUD 의 시각 요소.
  ///
  /// 화면 가로 중앙, 세로 top 정렬로 hh:mm:ss 를 표기한다.
  /// 정방향(스톱워치)이면 증가, 역방향(카운트다운)이면 감소하는 값을
  /// <see cref="TimeDisplayUIController"/> 가 매 프레임 주입한다.
  ///
  /// 비차단 HUD 이므로 pickingMode 는 Ignore 로 두어 하위 UI 클릭을 가로채지 않는다.
  /// (<see cref="UIOverlayStack"/>/<see cref="IUIOverlay"/> 는 모달 전용이므로 사용하지 않는다.)
  /// </summary>
  [UxmlElement]
  public partial class TimeDisplayElement : VisualElement
  {
    public const string RootName = "time-display-root";

    private static readonly Color CardColor = new Color(0f, 0f, 0f, 0.62f);
    private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color StopwatchColor = new Color(0.92f, 0.96f, 1f, 1f);
    private static readonly Color CountdownColor = new Color(1f, 0.86f, 0.55f, 1f);
    private static readonly Color CountdownFinishedColor = new Color(1f, 0.5f, 0.45f, 1f);
    private static readonly Color LabelColor = new Color(0.78f, 0.86f, 0.92f, 0.85f);

    private VisualElement _card;
    private Label _modeLabel;
    private Label _timeLabel;

    // 프레임당 중복 쓰기(텍스트/색상 재설정으로 인한 불필요한 리페인트)를 피하기 위한 캐시.
    private long _lastWholeSeconds = long.MinValue;
    private bool _hasRenderState;
    private ScenarioTimeDirection _lastDirection;
    private bool _lastCountdownFinished;

    public TimeDisplayElement()
    {
      name = RootName;
      AddToClassList("time-display-root");
      pickingMode = PickingMode.Ignore;
      ApplyInlineStyles();
      Build();
      SetVisibleState(false);
    }

    private void ApplyInlineStyles()
    {
      // 가로 중앙 정렬, 세로 top 정렬을 위한 전체폭 컨테이너.
      style.position = Position.Absolute;
      style.top = 12;
      style.left = 0;
      style.right = 0;
      style.flexDirection = FlexDirection.Row;
      style.alignItems = Align.FlexStart;
      style.justifyContent = Justify.Center;
    }

    private void Build()
    {
      _card = new VisualElement { pickingMode = PickingMode.Ignore };
      _card.style.backgroundColor = CardColor;
      _card.style.borderTopWidth = 1;
      _card.style.borderBottomWidth = 1;
      _card.style.borderLeftWidth = 1;
      _card.style.borderRightWidth = 1;
      _card.style.borderTopColor = BorderColor;
      _card.style.borderBottomColor = BorderColor;
      _card.style.borderLeftColor = BorderColor;
      _card.style.borderRightColor = BorderColor;
      _card.style.borderTopLeftRadius = 10;
      _card.style.borderTopRightRadius = 10;
      _card.style.borderBottomLeftRadius = 10;
      _card.style.borderBottomRightRadius = 10;
      _card.style.paddingLeft = 18;
      _card.style.paddingRight = 18;
      _card.style.paddingTop = 6;
      _card.style.paddingBottom = 8;
      _card.style.flexDirection = FlexDirection.Column;
      _card.style.alignItems = Align.Center;

      _modeLabel = new Label(string.Empty) { pickingMode = PickingMode.Ignore };
      _modeLabel.style.color = LabelColor;
      _modeLabel.style.fontSize = 10;
      _modeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
      _modeLabel.style.letterSpacing = 1;
      _card.Add(_modeLabel);

      _timeLabel = new Label("00:00:00") { pickingMode = PickingMode.Ignore };
      _timeLabel.style.color = StopwatchColor;
      _timeLabel.style.fontSize = 30;
      _timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _timeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
      _card.Add(_timeLabel);

      Add(_card);
    }

    /// <summary>표시 여부를 토글한다.</summary>
    public void SetVisibleState(bool visible)
    {
      style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      // 다시 표시될 때 강제로 한 번 다시 그리도록 캐시를 무효화한다.
      if (!visible)
      {
        InvalidateRenderCache();
      }
    }

    /// <summary>
    /// 렌더 캐시를 무효화하여 다음 <see cref="Render"/> 에서 강제로 다시 그리게 한다.
    /// 표시 대상 타이머가 교체될 때(Show) 호출하여, 값/모드가 우연히 같아도 즉시 갱신되게 한다.
    /// </summary>
    public void InvalidateRenderCache()
    {
      _hasRenderState = false;
      _lastWholeSeconds = long.MinValue;
    }

    /// <summary>
    /// 현재 시각 스냅샷을 렌더한다. 값/모드/상태가 실제로 바뀐 경우에만 요소를 갱신하여
    /// 프레임당 불필요한 텍스트/색상 쓰기(리페인트 유발)를 피한다.
    /// </summary>
    public void Render(ScenarioTimeDirection direction, long wholeSeconds, bool countdownFinished)
    {
      if (_timeLabel == null)
      {
        return;
      }

      // 시각 값은 초 단위로만 표시되므로 정수 초가 바뀐 경우에만 재포맷/재할당한다.
      if (wholeSeconds != _lastWholeSeconds)
      {
        _lastWholeSeconds = wholeSeconds;
        _timeLabel.text = ScenarioTimeState.FormatHhMmSs(wholeSeconds);
      }

      // 모드/종료 상태 전이 시에만 라벨 텍스트와 색상을 갱신한다.
      if (!_hasRenderState || direction != _lastDirection || countdownFinished != _lastCountdownFinished)
      {
        _hasRenderState = true;
        _lastDirection = direction;
        _lastCountdownFinished = countdownFinished;

        if (direction == ScenarioTimeDirection.Countdown)
        {
          _modeLabel.text = "COUNTDOWN";
          _timeLabel.style.color = countdownFinished ? CountdownFinishedColor : CountdownColor;
        }
        else
        {
          _modeLabel.text = "STOPWATCH";
          _timeLabel.style.color = StopwatchColor;
        }
      }
    }
  }
}
