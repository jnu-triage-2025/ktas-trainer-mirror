using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 아이템 제출 패널의 MonoBehaviour 컨트롤러.
  ///
  /// 흐름:
  ///  1) <see cref="ItemSubmissionInteractable"/> 가 <see cref="Open"/> 을 호출 → 요구 아이템으로 패널을 구성하고 오버레이로 push.
  ///  2) 매 프레임 플레이어 인벤토리 보유량으로 요구 칸 표시/제출 버튼 활성 상태를 갱신.
  ///  3) 제출 버튼 클릭 → 요구 아이템을 소모(제거)하고, Interactable 에 완료를 통지 → 서버 세션 전역 신호가 올라간다.
  ///
  /// 인벤토리는 소유자(owner) 클라이언트 로컬 권한이므로, 소모/검증은 소유자 클라이언트에서 수행하고
  /// 완료 신호만 서버 권한 경로(<see cref="Scenario.ScenarioInteractionSignals"/>)로 라우팅한다.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class ItemSubmissionUIController : UIControllerABC, IUIOverlay
  {
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.InventoryUISortOrder + 1f;

    private UIDocument _document;
    private ItemSubmissionUIView _view;

    private ItemSubmissionInteractable _activeInteractable;
    private PlayerController _activePlayer;
    private IReadOnlyList<ItemRequirement> _activeRequirements = Array.Empty<ItemRequirement>();

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    public bool IsOpened => _view != null && _view.IsVisible;

    protected override void Awake()
    {
      base.Awake();

      _document = GetComponent<UIDocument>();
      if (_document == null)
      {
        Debug.LogError("[ItemSubmissionUIController] UIDocument missing.");
        return;
      }
      _document.sortingOrder = _sortingOrder;
    }

    private void OnEnable()
    {
      if (_document == null)
        _document = GetComponent<UIDocument>();

      BindViewToCurrentDocumentRoot();

      // 여러 UIDocument가 하나의 PanelSettings를 공유하므로, 닫힌 상태의 이 오버레이 root가
      // 전체 화면을 덮은 채 pickingMode=Position 으로 남으면 sortingOrder가 낮은 인벤토리(6)의
      // 클릭/hover를 가로챈다. 이 컨트롤러는 sortingOrder = 인벤토리+1(=7) 이므로 반드시
      // 시작 시 닫힘 상태로 중립화(pickingMode 서브트리 Ignore)해야 한다.
      // (다른 오버레이 컨트롤러들과 동일한 규약 — overlay-uidocument-picking-guide.md 참고)
      if (!IsOpened)
        StartCoroutine(NeutralizeDocumentRootWhenReady(_document));
    }

    protected override void OnDestroy()
    {
      DetachViewEvents();
      base.OnDestroy();
    }

    private void Update()
    {
      if (!IsOpened || _view == null || _activePlayer == null)
        return;

      _view.UpdateHeldCounts(id => _activePlayer.CountItemInInventory(id));
    }

    private void BindViewToCurrentDocumentRoot()
    {
      if (_document == null)
        return;

      var root = _document.rootVisualElement;
      if (root == null)
        return;

      var currentView = root.Q<ItemSubmissionUIView>();

      if (_view != null && _view == currentView && _view.panel != null)
        return;

      DetachViewEvents();

      _view = currentView;
      if (_view == null)
      {
        _view = new ItemSubmissionUIView();
        root.Add(_view);
      }

      _view.SubmitClicked += HandleSubmitClicked;
      _view.CloseClicked += HandleCloseClicked;
      _view.SetVisible(false);
    }

    private void DetachViewEvents()
    {
      if (_view == null)
        return;

      _view.SubmitClicked -= HandleSubmitClicked;
      _view.CloseClicked -= HandleCloseClicked;
    }

    /// <summary>제출 패널을 특정 Interactable/플레이어 기준으로 연다.</summary>
    public void Open(ItemSubmissionInteractable interactable, PlayerController player)
    {
      if (interactable == null || player == null)
        return;

      if (_view == null || _view.panel == null)
        BindViewToCurrentDocumentRoot();

      if (_view == null)
        return;

      _activeInteractable = interactable;
      _activePlayer = player;

      var def = interactable.Definition;
      _activeRequirements = def != null ? def.GetValidRequirements() : Array.Empty<ItemRequirement>();

      _view.Configure(def?.title, def?.submitButtonText, _activeRequirements);
      _view.UpdateHeldCounts(id => player.CountItemInInventory(id));

      if (!UIOverlayStack.IsTop(this))
        UIOverlayStack.Push(this);

      _view.SetVisible(true);
    }

    public void Close()
    {
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
      else
        _view?.SetVisible(false);

      _activeInteractable = null;
      _activePlayer = null;
      _activeRequirements = Array.Empty<ItemRequirement>();
    }

    private void HandleCloseClicked() => Close();

    private void HandleSubmitClicked()
    {
      if (_activeInteractable == null || _activePlayer == null)
        return;

      // 1) 검증: 모든 요구 아이템을 충분히 보유했는지 재확인(제출 순간의 상태 기준).
      foreach (var req in _activeRequirements)
      {
        if (_activePlayer.CountItemInInventory(req.identifier) < req.count)
        {
          // 보유량 부족: UI 를 갱신하고 제출을 취소한다.
          _view?.UpdateHeldCounts(id => _activePlayer.CountItemInInventory(id));
          return;
        }
      }

      // 2) 소모: 요구 수량만큼 인벤토리에서 제거한다.
      foreach (var req in _activeRequirements)
      {
        _activePlayer.RemoveItemFromInventory(req.identifier, req.count);
      }

      // 3) 완료 통지: 서버 세션 전역 신호를 올린다(소유자 → 서버 권한 라우팅).
      _activeInteractable.NotifySubmissionCompleted();

      Close();
    }

    // IUIOverlay
    public void OnOverlayPushed()
    {
      if (_view == null || _view.panel == null)
        BindViewToCurrentDocumentRoot();

      _view?.SetVisible(true);
      SetDocumentRootInteractable(_document, true);
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      _view?.SetVisible(false);
      SetDocumentRootInteractable(_document, false);
      OverlayPopped?.Invoke();
    }
  }
}
