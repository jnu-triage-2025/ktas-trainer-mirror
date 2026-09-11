using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.Entity.PatientMonitor
{
  /// <summary>
  /// 월드 RenderTexture 모니터의 콘텐츠를 게임 화면 위 전체화면 UI로 표시합니다.
  /// BackendSystem이 이미 생성한 화면용 PanelSettings를 공유하므로 별도 월드 표면에는
  /// 표시되지 않으며, UIOverlayStack을 통해 커서/이동 상태를 일관되게 제어합니다.
  /// </summary>
  internal sealed class PatientMonitorDetailOverlay : IUIOverlay
  {
    private static PatientMonitorDetailOverlay _instance;

    private readonly UIDocument _document;
    private object _owner;
    private Action _restoreContent;
    private Action _closed;

    private PatientMonitorDetailOverlay(PanelSettings panelSettings)
    {
      var overlayObject = new GameObject("PatientMonitorDetailOverlay");
      UnityEngine.Object.DontDestroyOnLoad(overlayObject);
      _document = overlayObject.AddComponent<UIDocument>();
      _document.panelSettings = panelSettings;
      _document.sortingOrder = DefaultsUIDocument.ChatPanelUISortOrder + 1f;
      SetDocumentVisible(false);
    }

    public static bool Open(object owner,
      IReadOnlyList<VisualElement> contents,
      Action restoreContent,
      Action closed = null)
    {
      if (owner == null || contents == null || contents.Count == 0)
        return false;

      var overlay = EnsureInstance();
      if (overlay == null)
      {
        Debug.LogError("[PatientMonitorDetailOverlay] 화면용 UIDocument PanelSettings를 찾을 수 없습니다.");
        return false;
      }

      return overlay.Show(owner, contents, restoreContent, closed);
    }

    public static void Close(object owner)
    {
      if (_instance == null || !ReferenceEquals(_instance._owner, owner))
        return;

      if (UIOverlayStack.IsTop(_instance))
        UIOverlayStack.Pop();
      // 대화창 등에 가려져 스택 중간에 있으면 그 자리에서 제거한다. 스택에 없을 때만 직접 닫는다.
      else if (!UIOverlayStack.Remove(_instance))
        _instance.OnOverlayPopped();
    }

    private static PatientMonitorDetailOverlay EnsureInstance()
    {
      if (_instance != null)
        return _instance;

      var panelSettings = FindScreenPanelSettings();
      if (panelSettings == null)
        return null;

      _instance = new PatientMonitorDetailOverlay(panelSettings);
      return _instance;
    }

    private static PanelSettings FindScreenPanelSettings()
    {
      var documents = UnityEngine.Object.FindObjectsByType<UIDocument>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < documents.Length; i++)
      {
        var document = documents[i];
        if (document == null || document.panelSettings == null ||
            document.panelSettings.targetTexture != null ||
            document.GetComponent<UIDocumentWorldSurfaceBinder>() != null)
          continue;

        return document.panelSettings;
      }

      return null;
    }

    private bool Show(object owner,
      IReadOnlyList<VisualElement> contents,
      Action restoreContent,
      Action closed)
    {
      if (_owner != null)
        Close(_owner);

      _owner = owner;
      _restoreContent = restoreContent;
      _closed = closed;
      var root = _document.rootVisualElement;
      if (root == null)
      {
        _owner = null;
        _restoreContent = null;
        _closed = null;
        Debug.LogError("[PatientMonitorDetailOverlay] UIDocument rootVisualElement를 준비하지 못했습니다.");
        return false;
      }
      root.Clear();

      var backdrop = new VisualElement { name = "PatientMonitorDetailBackdrop" };
      backdrop.style.flexGrow = 1f;
      backdrop.style.paddingLeft = 36f;
      backdrop.style.paddingRight = 36f;
      backdrop.style.paddingTop = 28f;
      backdrop.style.paddingBottom = 28f;
      backdrop.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.9f));

      var panel = new VisualElement { name = "PatientMonitorDetailPanel" };
      panel.style.flexGrow = 1f;
      panel.style.backgroundColor = new StyleColor(new Color(0.04f, 0.04f, 0.04f));
      panel.style.paddingLeft = 20f;
      panel.style.paddingRight = 20f;
      panel.style.paddingTop = 14f;
      panel.style.paddingBottom = 16f;

      var header = new VisualElement { name = "PatientMonitorDetailHeader" };
      header.style.flexDirection = FlexDirection.Row;
      header.style.justifyContent = Justify.SpaceBetween;
      header.style.alignItems = Align.Center;
      header.style.marginBottom = 12f;
      var title = new Label("자세히 보기");
      title.style.fontSize = 22f;
      title.style.color = Color.white;
      title.style.unityFontStyleAndWeight = FontStyle.Bold;
      var closeButton = new Button(() => Close(_owner))
      {
        name = "PatientMonitorDetailCloseButton",
        text = "✕"
      };
      ApplyCloseButtonStyle(closeButton);
      header.Add(title);
      header.Add(closeButton);

      var contentHost = new VisualElement { name = "PatientMonitorDetailContent" };
      contentHost.style.flexGrow = 1f;
      contentHost.style.flexDirection = contents.Count > 1 ? FlexDirection.Row : FlexDirection.Column;
      contentHost.style.overflow = Overflow.Hidden;
      for (int i = 0; i < contents.Count; i++)
      {
        var content = contents[i];
        if (content == null)
          continue;

        content.style.flexGrow = 1f;
        content.style.flexBasis = 0f;
        if (i > 0 && contents.Count > 1)
          content.style.marginLeft = 12f;
        contentHost.Add(content);
      }

      panel.Add(header);
      panel.Add(contentHost);
      backdrop.Add(panel);
      root.Add(backdrop);
      SetDocumentVisible(true);
      UIOverlayStack.Push(this);
      return true;
    }

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    public void OnOverlayPushed()
    {
      // 서버가 내려보낸 대화 노드 등에 잠시 가려졌다가 돌아온 경우 상세 화면을 다시 보인다.
      if (_owner != null)
        SetDocumentVisible(true);

      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      // 다른 오버레이가 위에 올라와 가려진 것뿐이면 소유자와 콘텐츠를 유지한 채 숨기기만 한다.
      // 여기서 소유자를 비우면 덮개가 닫힌 뒤 보이지 않는 오버레이가 최상단에 남아
      // 닫기 버튼도 Close(owner) 도 통하지 않은 채 입력만 막는다.
      if (UIOverlayStack.Contains(this))
      {
        SetDocumentVisible(false);
        OverlayPopped?.Invoke();
        return;
      }

      var restore = _restoreContent;
      var closed = _closed;
      _owner = null;
      _restoreContent = null;
      _closed = null;
      SetDocumentVisible(false);
      restore?.Invoke();
      closed?.Invoke();
      OverlayPopped?.Invoke();
    }

    private void SetDocumentVisible(bool visible)
    {
      var root = _document != null ? _document.rootVisualElement : null;
      if (root == null)
        return;

      root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      SetPickingMode(root, visible ? PickingMode.Position : PickingMode.Ignore);
    }

    private static void SetPickingMode(VisualElement root, PickingMode mode)
    {
      root.pickingMode = mode;
      for (int i = 0; i < root.childCount; i++)
        SetPickingMode(root[i], mode);
    }

    private static void ApplyCloseButtonStyle(Button button)
    {
      if (button == null)
        return;

      var normalColor = new Color(0.55f, 0.11f, 0.14f);
      var hoverColor = new Color(0.82f, 0.18f, 0.22f);
      var pressedColor = new Color(0.38f, 0.06f, 0.08f);

      button.style.width = 42f;
      button.style.height = 34f;
      button.style.fontSize = 20f;
      button.style.color = Color.white;
      button.style.unityFontStyleAndWeight = FontStyle.Bold;
      button.style.unityTextAlign = TextAnchor.MiddleCenter;
      button.style.backgroundColor = new StyleColor(normalColor);
      button.style.borderTopWidth = 1f;
      button.style.borderBottomWidth = 1f;
      button.style.borderLeftWidth = 1f;
      button.style.borderRightWidth = 1f;
      button.style.borderTopColor = new StyleColor(new Color(1f, 1f, 1f, 0.45f));
      button.style.borderBottomColor = new StyleColor(new Color(0.18f, 0.01f, 0.02f));
      button.style.borderLeftColor = new StyleColor(new Color(1f, 1f, 1f, 0.3f));
      button.style.borderRightColor = new StyleColor(new Color(0.18f, 0.01f, 0.02f));
      button.style.borderTopLeftRadius = 5f;
      button.style.borderTopRightRadius = 5f;
      button.style.borderBottomLeftRadius = 5f;
      button.style.borderBottomRightRadius = 5f;

      button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = new StyleColor(hoverColor));
      button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = new StyleColor(normalColor));
      button.RegisterCallback<MouseDownEvent>(_ => button.style.backgroundColor = new StyleColor(pressedColor));
      button.RegisterCallback<MouseUpEvent>(_ => button.style.backgroundColor = new StyleColor(hoverColor));
    }
  }
}
