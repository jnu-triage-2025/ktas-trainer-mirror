using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Quest;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 엔티티 위에 라벨(뱃지)을 띄우는 일반화된 오버헤드 라벨 컨트롤러.
  ///
  /// <para>
  /// 플레이어 이름을 머리 위에 표기하듯, 임의의 월드 엔티티(Transform) 위에 "색상 사각형 + 텍스트" 라벨을
  /// 스크린 스페이스(UI Toolkit)로 표시한다. 도메인 코드는 <see cref="SetLabel"/> / <see cref="RemoveLabel"/> 로
  /// 대상별 라벨을 등록/해제하기만 하면 되고, 월드→스크린 투영과 위치 갱신은 이 컨트롤러가 매 프레임 수행한다.
  /// </para>
  ///
  /// <para>
  /// 배치: 씬에 <see cref="UIDocument"/> 와 함께 배치한다. 카메라는 지정되지 않으면 <see cref="Camera.main"/> 을 사용한다.
  /// </para>
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public sealed class EntityOverheadLabelUIController : UIControllerABC
  {
    /// <summary>단일 라벨의 표시 내용.</summary>
    public readonly struct LabelContent
    {
      public readonly Color SwatchColor;
      public readonly string Text;
      public readonly Color TextColor;
      public readonly Sprite Icon;
      /// <summary>색상 사각형 표시 여부. false 면 텍스트만 표시한다(예: NPC 이름표).</summary>
      public readonly bool ShowSwatch;

      public LabelContent(Color swatchColor, string text, Color textColor)
      {
        SwatchColor = swatchColor;
        Text = text;
        TextColor = textColor;
        ShowSwatch = true;
        Icon = null;
      }

      /// <summary>색상 사각형 없이 텍스트만 표시하는 라벨(예: NPC 이름표).</summary>
      public LabelContent(string text, Color textColor)
      {
        SwatchColor = Color.clear;
        Text = text;
        TextColor = textColor;
        ShowSwatch = false;
        Icon = null;
      }

      public LabelContent(Sprite icon)
      {
        SwatchColor = Color.clear;
        Text = string.Empty;
        TextColor = Color.white;
        ShowSwatch = false;
        Icon = icon;
      }
    }

    /// <summary>
    /// 활성 인스턴스(단일 로컬 클라이언트에 하나). 도메인 코드가 손쉽게 접근하기 위한 헬퍼.
    /// </summary>
    public static EntityOverheadLabelUIController ActiveInstance { get; private set; }

    /// <summary>
    /// <see cref="ActiveInstance"/> 가 바뀔 때(준비/해제) 발생한다.
    /// 컨트롤러가 아직 없을 때 라벨을 요청한 표현 코드가 준비 시점에 다시 시도하기 위한 신호다.
    /// (오버레이 씬이 나중에 로드되는 구성에서 라벨이 영영 누락되는 것을 막는다.)
    /// </summary>
    public static event System.Action ActiveInstanceChanged;

    // ActiveInstance 가 아직 없을 때 Resolve() 가 1회 수행한 씬 탐색 결과. 파괴되면 다시 탐색한다.
    private static EntityOverheadLabelUIController _sceneLookupCache;

    [SerializeField] private float _sortingOrder = DefaultsUIDocument.EntityOverheadLabelSortOrder;
    [Tooltip("월드 앵커로부터 위로 띄울 추가 높이(월드 단위).")]
    [SerializeField] private float _worldHeightOffset = 0.4f;
    [SerializeField] private int _maxChannelsPerAnchor = 4;
    [Tooltip("표시 한계 거리 직전에 라벨을 서서히 흐리게 만들 구간의 길이(월드 단위). 0이면 즉시 사라진다.")]
    [SerializeField, Min(0f)] private float _distanceFadeBand = 2f;
    [Tooltip("같은 앵커에 쌓이는 채널 사이의 세로 간격(픽셀).")]
    [SerializeField] private float _channelStackSpacing = 26f;
    [SerializeField] private UnityEngine.Camera _camera;

    private UIDocument _uiDocument;
    private VisualElement _root;

    private sealed class Entry
    {
      public Transform Target;
      public string ChannelId;
      public int ChannelOrder;
      public EntityOverheadLabelElement Element;

      /// <summary>카메라와 이 거리(월드 단위)보다 멀어지면 숨긴다. 0 이하이면 거리 제한이 없다.</summary>
      public float MaxVisibleDistance;
    }

    private readonly Dictionary<Transform, Dictionary<string, Entry>> _entries = new();

    protected override void Awake()
    {
      base.Awake();
      ActiveInstance = this;
      QuestPresentationService.ActiveInstance?.RefreshPresentation();
      ActiveInstanceChanged?.Invoke();
    }

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      _uiDocument.sortingOrder = _sortingOrder;
      _root = _uiDocument.rootVisualElement;
      // 비차단 HUD는 루트만 Ignore하면 자식 Label이 여전히 포인터를 가로챌 수 있다.
      // 새 자식 VisualElement를 추가할 때도 반드시 전체 서브트리를 Ignore로 유지한다.
      if (_root != null)
        SetSubtreePickingMode(_root, PickingMode.Ignore);
    }

    protected override void OnDestroy()
    {
      bool wasActiveInstance = ReferenceEquals(ActiveInstance, this);
      if (wasActiveInstance)
        ActiveInstance = null;

      foreach (var targetEntries in _entries.Values)
      {
        foreach (var entry in targetEntries.Values)
        {
          if (entry?.Element != null)
            entry.Element.RemoveFromHierarchy();
        }
      }
      _entries.Clear();

      base.OnDestroy();

      if (wasActiveInstance)
        ActiveInstanceChanged?.Invoke();
    }

    /// <summary>
    /// 표현 코드가 쓸 컨트롤러를 조회한다. <see cref="ActiveInstance"/> 가 아직 없으면 씬을 1회 탐색해 캐시한다.
    /// (씬에 컨트롤러가 배치되지 않았다면 경고를 출력하고 null 을 돌려준다.)
    /// </summary>
    public static EntityOverheadLabelUIController Resolve()
    {
      var instance = ActiveInstance;
      if (instance != null)
        return instance;

      if (_sceneLookupCache != null)
        return _sceneLookupCache;

      _sceneLookupCache = FindFirstObjectByType<EntityOverheadLabelUIController>();
      if (_sceneLookupCache == null)
      {
        Debug.LogWarning(
          $"[{nameof(EntityOverheadLabelUIController)}] 씬에서 컨트롤러를 찾을 수 없어 머리 위 라벨을 표시하지 않습니다. " +
          "씬에 EntityOverheadLabelUIController + UIDocument 컴포넌트를 배치하세요.");
      }

      return _sceneLookupCache;
    }

    /// <summary>
    /// 대상 엔티티 위에 표시할 라벨을 설정한다(없으면 생성, 있으면 내용 갱신).
    /// </summary>
    /// <param name="target">라벨을 띄울 월드 앵커(예: 엔티티 Transform).</param>
    /// <param name="content">표시 내용(색상 사각형 + 텍스트).</param>
    public void SetLabel(Transform target, LabelContent content)
      => SetLabel(target, "default", 0, content);

    public void SetLabel(Transform target, string channelId, int channelOrder, LabelContent content)
      => SetLabel(target, channelId, channelOrder, content, maxVisibleDistance: 0f);

    /// <param name="maxVisibleDistance">
    /// 카메라와 이 거리보다 멀어지면 라벨을 숨긴다. 0 이하이면 거리와 무관하게 항상 표시한다.
    /// </param>
    public void SetLabel(
      Transform target,
      string channelId,
      int channelOrder,
      LabelContent content,
      float maxVisibleDistance)
    {
      if (target == null)
        return;

      EnsureRoot();
      if (_root == null)
        return;

      channelId = string.IsNullOrWhiteSpace(channelId) ? "default" : channelId.Trim();
      if (!_entries.TryGetValue(target, out var channels))
      {
        channels = new Dictionary<string, Entry>(System.StringComparer.Ordinal);
        _entries[target] = channels;
      }

      if (!channels.TryGetValue(channelId, out var entry))
      {
        entry = new Entry
        {
          Target = target,
          ChannelId = channelId,
          ChannelOrder = channelOrder,
          Element = new EntityOverheadLabelElement(),
        };
        _root.Add(entry.Element);
        // 오버헤드 라벨은 표시 전용이다. 다른 모달 UI의 클릭을 막지 않도록 생성 직후 전체를 Ignore한다.
        SetSubtreePickingMode(entry.Element, PickingMode.Ignore);
        channels[channelId] = entry;
      }

      entry.ChannelOrder = channelOrder;
      entry.MaxVisibleDistance = maxVisibleDistance;
      entry.Element.SetContent(content.SwatchColor, content.Text, content.TextColor, content.ShowSwatch, content.Icon);
    }

    /// <summary>대상 엔티티의 라벨을 제거한다.</summary>
    public void RemoveLabel(Transform target)
      => RemoveLabel(target, "default");

    public void RemoveLabel(Transform target, string channelId)
    {
      if (target == null)
        return;

      channelId = string.IsNullOrWhiteSpace(channelId) ? "default" : channelId.Trim();
      if (_entries.TryGetValue(target, out var channels) && channels.TryGetValue(channelId, out var entry))
      {
        entry.Element?.RemoveFromHierarchy();
        channels.Remove(channelId);
        if (channels.Count == 0)
          _entries.Remove(target);
      }
    }

    public void RemoveLabels(Transform target)
    {
      if (target == null || !_entries.TryGetValue(target, out var channels))
        return;

      foreach (var entry in channels.Values)
        entry?.Element?.RemoveFromHierarchy();
      _entries.Remove(target);
    }

    private void EnsureRoot()
    {
      if (_root != null)
        return;

      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      if (_uiDocument != null)
      {
        _root = _uiDocument.rootVisualElement;
        if (_root != null)
          SetSubtreePickingMode(_root, PickingMode.Ignore);
      }
    }

    // 인스펙터 지정 카메라를 우선 사용하고, 없으면 Camera.main 을 1회 조회해 캐시한다.
    // Camera.main 은 내부적으로 태그 검색을 수행하므로 매 프레임 호출을 피한다(캐시가 파괴되면 재조회).
    private UnityEngine.Camera ResolveCamera()
    {
      if (_camera != null)
        return _camera;

      _camera = UnityEngine.Camera.main;
      return _camera;
    }

    /// <summary>
    /// 표시 한계 거리에 따른 불투명도를 계산한다. 한계 안쪽이면 1, 바깥이면 0이며
    /// 한계 직전 <see cref="_distanceFadeBand"/> 구간에서는 선형으로 흐려진다.
    /// </summary>
    private float ResolveDistanceOpacity(float maxVisibleDistance, float cameraDistance)
    {
      if (maxVisibleDistance <= 0f)
        return 1f;

      if (cameraDistance >= maxVisibleDistance)
        return 0f;

      float fadeBand = Mathf.Min(_distanceFadeBand, maxVisibleDistance);
      if (fadeBand <= 0f)
        return 1f;

      float fadeStart = maxVisibleDistance - fadeBand;
      return cameraDistance <= fadeStart
        ? 1f
        : Mathf.InverseLerp(maxVisibleDistance, fadeStart, cameraDistance);
    }

    private void LateUpdate()
    {
      if (_entries.Count == 0)
        return;

      EnsureRoot();
      if (_root == null)
        return;

      var cam = ResolveCamera();
      if (cam == null)
        return;

      var panel = _root.panel;
      if (panel == null)
        return;

      // 대상이 파괴된 항목은 정리한다.
      List<Transform> stale = null;

      foreach (var kvp in _entries)
      {
        var target = kvp.Key;
        var channels = kvp.Value;
        if (target == null || channels == null)
        {
          (stale ??= new List<Transform>()).Add(kvp.Key);
          continue;
        }

        Vector3 worldPos = target.position + Vector3.up * _worldHeightOffset;

        // 카메라 뒤쪽이면 숨긴다.
        Vector3 viewport = cam.WorldToViewportPoint(worldPos);
        bool behind = viewport.z <= 0f;
        foreach (var entry in channels.Values)
          entry.Element.style.display = behind ? DisplayStyle.None : DisplayStyle.Flex;
        if (behind)
          continue;

        Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(panel, worldPos, cam);
        var ordered = new List<Entry>(channels.Values);
        ordered.Sort((left, right) =>
        {
          int order = left.ChannelOrder.CompareTo(right.ChannelOrder);
          return order != 0 ? order : string.Compare(left.ChannelId, right.ChannelId, System.StringComparison.Ordinal);
        });

        float cameraDistance = Vector3.Distance(cam.transform.position, worldPos);
        int maxChannels = Mathf.Max(1, _maxChannelsPerAnchor);
        // 거리로 숨겨진 채널은 스택에서 자리를 차지하지 않는다.
        // (이름표가 사라졌다고 퀘스트 마크가 빈 칸 위에 뜨면 안 된다.)
        int stackIndex = 0;
        for (int i = 0; i < ordered.Count; i++)
        {
          var entry = ordered[i];
          float opacity = ResolveDistanceOpacity(entry.MaxVisibleDistance, cameraDistance);
          bool visible = opacity > 0f && stackIndex < maxChannels;
          entry.Element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
          if (!visible)
            continue;

          entry.Element.style.opacity = opacity;
          // 패널 좌표는 y가 아래로 증가하므로 y를 빼면 화면 위쪽으로 쌓인다.
          // 즉 channelOrder 가 큰 채널일수록 위에 놓인다(NPC 이름표 0 < 퀘스트 마크 100).
          entry.Element.SetScreenPosition(
            new Vector2(panelPos.x, panelPos.y - stackIndex * _channelStackSpacing));
          stackIndex++;
        }
      }

      if (stale != null)
      {
        foreach (var key in stale)
        {
          if (_entries.TryGetValue(key, out var channels))
          {
            foreach (var entry in channels.Values)
              entry?.Element?.RemoveFromHierarchy();
          }
          _entries.Remove(key);
        }
      }
    }
  }
}
