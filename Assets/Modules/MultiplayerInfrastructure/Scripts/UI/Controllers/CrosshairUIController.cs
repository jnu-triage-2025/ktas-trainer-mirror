using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// CrosshairUIController는 화면 중앙 크로스헤어 UI를 표시합니다.
  /// 
  /// 레이캐스트는 PlayerController.Raycast에서 수행되며,
  /// 이 컨트롤러는 UI 요소기만 담당합니다.
  /// 
  /// 레이캐스트 결과는 PlayerController의 <see cref="PlayerController.RaycastHasHit"/>,
  /// <see cref="PlayerController.RaycastHit"/>, <see cref="PlayerController.RaycastHitObject"/>에서 접근합니다.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class CrosshairUIController : UIControllerABC
  {
    [Header("References")]
    [SerializeField] private UIDocument _uiDocument;

    private CrosshairElement _crosshairElement;

    // ── 생명 주기 ─────────────────────────────────────────────
    protected override void Awake()
    {
      base.Awake();

      if (_uiDocument.IsUnityNull())
        _uiDocument = GetComponent<UIDocument>();

      SetupCrosshairUI();
    }

    // ── 공개 API ──────────────────────────────────────────────
    /// <summary>크로스헤어 UI의 시각성을 제어합니다.</summary>
    public void SetCrosshairVisible(bool visible)
    {
      if (_crosshairElement == null) return;
      _crosshairElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ── 내부 구현 ─────────────────────────────────────────────
    private void SetupCrosshairUI()
    {
      if (_uiDocument.IsUnityNull())
      {
        Debug.LogError("[CrosshairUIController] UIDocument is null");
        return;
      }

      var root = _uiDocument.rootVisualElement;
      SetDocumentRootPickingEnabled(_uiDocument, false);
      _crosshairElement = root.Q<CrosshairElement>("crosshair-root");

      if (_crosshairElement == null)
        Debug.LogWarning("[CrosshairUIController] 'crosshair-root' CrosshairElement를 찾지 못했습니다.");
    }
  }
}
