using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
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

      public LabelContent(Color swatchColor, string text, Color textColor)
      {
        SwatchColor = swatchColor;
        Text = text;
        TextColor = textColor;
      }
    }

    /// <summary>
    /// 활성 인스턴스(단일 로컬 클라이언트에 하나). 도메인 코드가 손쉽게 접근하기 위한 헬퍼.
    /// </summary>
    public static EntityOverheadLabelUIController ActiveInstance { get; private set; }

    [SerializeField] private float _sortingOrder = DefaultsUIDocument.EntityOverheadLabelSortOrder;
    [Tooltip("월드 앵커로부터 위로 띄울 추가 높이(월드 단위).")]
    [SerializeField] private float _worldHeightOffset = 0.4f;
    [SerializeField] private UnityEngine.Camera _camera;

    private UIDocument _uiDocument;
    private VisualElement _root;

    private sealed class Entry
    {
      public Transform Target;
      public EntityOverheadLabelElement Element;
    }

    private readonly Dictionary<Transform, Entry> _entries = new();

    protected override void Awake()
    {
      base.Awake();
      ActiveInstance = this;
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
      if (ReferenceEquals(ActiveInstance, this))
        ActiveInstance = null;

      foreach (var kvp in _entries)
      {
        if (kvp.Value?.Element != null)
          kvp.Value.Element.RemoveFromHierarchy();
      }
      _entries.Clear();

      base.OnDestroy();
    }

    /// <summary>
    /// 대상 엔티티 위에 표시할 라벨을 설정한다(없으면 생성, 있으면 내용 갱신).
    /// </summary>
    /// <param name="target">라벨을 띄울 월드 앵커(예: 엔티티 Transform).</param>
    /// <param name="content">표시 내용(색상 사각형 + 텍스트).</param>
    public void SetLabel(Transform target, LabelContent content)
    {
      if (target == null)
        return;

      EnsureRoot();
      if (_root == null)
        return;

      if (!_entries.TryGetValue(target, out var entry))
      {
        entry = new Entry
        {
          Target = target,
          Element = new EntityOverheadLabelElement(),
        };
        _root.Add(entry.Element);
        // 오버헤드 라벨은 표시 전용이다. 다른 모달 UI의 클릭을 막지 않도록 생성 직후 전체를 Ignore한다.
        SetSubtreePickingMode(entry.Element, PickingMode.Ignore);
        _entries[target] = entry;
      }

      entry.Element.SetContent(content.SwatchColor, content.Text, content.TextColor);
    }

    /// <summary>대상 엔티티의 라벨을 제거한다.</summary>
    public void RemoveLabel(Transform target)
    {
      if (target == null)
        return;

      if (_entries.TryGetValue(target, out var entry))
      {
        entry.Element?.RemoveFromHierarchy();
        _entries.Remove(target);
      }
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
        var entry = kvp.Value;
        if (entry?.Target == null || entry.Element == null)
        {
          (stale ??= new List<Transform>()).Add(kvp.Key);
          continue;
        }

        Vector3 worldPos = entry.Target.position + Vector3.up * _worldHeightOffset;

        // 카메라 뒤쪽이면 숨긴다.
        Vector3 viewport = cam.WorldToViewportPoint(worldPos);
        bool behind = viewport.z <= 0f;
        entry.Element.style.display = behind ? DisplayStyle.None : DisplayStyle.Flex;
        if (behind)
          continue;

        Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(panel, worldPos, cam);
        entry.Element.SetScreenPosition(panelPos);
      }

      if (stale != null)
      {
        foreach (var key in stale)
        {
          if (_entries.TryGetValue(key, out var e))
            e.Element?.RemoveFromHierarchy();
          _entries.Remove(key);
        }
      }
    }
  }
}
