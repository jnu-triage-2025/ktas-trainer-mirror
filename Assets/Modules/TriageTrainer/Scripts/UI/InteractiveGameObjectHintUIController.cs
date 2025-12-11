using System.Collections.Generic;
using FishNet.Object;
using Modules.TriageTrainer.Scripts.Camera;
using Modules.TriageTrainer.Scripts.Connection;
using Modules.TriageTrainer.Scripts.PlayerInteractiveGameObject;
using TriageTrainer.Definitions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Modules.TriageTrainer.Scripts.UI
{
  /// <summary>
  /// InteractiveGameObjectHintUIController는 플레이어가 상호작용할 수 있는 오브젝트가 근처에 있을 때,
  /// 해당 오브젝트들의 정보를 UI로 표시하고, 플레이어의 입력을 받아 상호작용을 처리합니다.
  /// 
  /// 이 컴포넌트는 UIDocument를 필요로 하며, UI 요소들은 USS 스타일시트를 통해 꾸며집니다.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class InteractiveGameObjectHintUIController : NetworkBehaviour
  {
    private bool _isPlayerStarted = false;
    
    [SerializeField] private PlayerInteractiveDetector _detector;
    [SerializeField] private PlayerInteractionResolver _interactionResolver;
    [SerializeField] private float _hideDelay = .5f;
    [SerializeField] private KeyCode _interactKey = DefaultsKeyConfiguration.InteractInteractiveGameObject;
    [SerializeField] private Transform _interactorTransform;
    
    private UIDocument _uiDocument;
    private VisualElement _documentRoot;
    private VisualElement _interactablesContainer;
    private Label _selectedLabel;

    private readonly List<VisualElement> _interactableViews = new();
    private int selectedIndex;
    private float _lastVisibleTime;
    
    void Awake()
    {
      _uiDocument = GetComponent<UIDocument>();
      _documentRoot = _uiDocument.rootVisualElement;
      _interactablesContainer = _documentRoot.Q<VisualElement>(DefaultsInteractInteractiveGameObject.VisualElementIdentifierInteractivesContainer);
      _selectedLabel = _documentRoot.Q<Label>(DefaultsInteractInteractiveGameObject.VisualElementIdentifierSelectedInteractableLabel);
      _documentRoot.style.display = DisplayStyle.None;
    }

    void Start()
    {
      CurrentSessionPlayInfoRegistry.Instance.OnLocalCameraHolderRegistered += (t) => OnLocalPlayerStarted();
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      if (!IsOwner) return;
    }

    void Update()
    {
      if (!_isPlayerStarted) return;
      
      var nearby = _detector.Nearby;

      if (nearby.Count == 0)
      {
        if (Time.time - _lastVisibleTime > _hideDelay)
          _documentRoot.style.display = DisplayStyle.None;
        return;
      }
      
      _documentRoot.style.display = DisplayStyle.Flex;
      _lastVisibleTime = Time.time;

      HandleScrollInput(nearby.Count);
      HandleInteractionInput(nearby);

      if (_interactableViews.Count != nearby.Count)
        RebuildInteractableViews(nearby.Count);

      UpdateInteractableContent(nearby);
    }

    public void OnLocalPlayerStarted()
    {
      _detector = CurrentSessionPlayInfoRegistry.Instance.PlayerController.InteractiveDetector;
      _interactorTransform = CurrentSessionPlayInfoRegistry.Instance.PlayerCameraHolderTransform;
      _interactionResolver = CurrentSessionPlayInfoRegistry.Instance.PlayerController.InteractionResolver;
      _isPlayerStarted = true;
    }

    void HandleScrollInput(int nearbyCount)
    {
      if (nearbyCount == 0) return;
      
      // 현재는 키보드-마우스의 PC 환경만 지원
      if (Mouse.current == null) return;

      float scroll = Mouse.current.scroll.ReadValue().y;
      if (Mathf.Abs(scroll) > DefaultsInteractInteractiveGameObject.ScrollSpeedUnderBoundOnHintUI)
      {
        selectedIndex = (selectedIndex - Mathf.RoundToInt(Mathf.Sign(scroll)) + nearbyCount) % nearbyCount;
        RefreshSelection();
      }
    }

    void HandleInteractionInput(IReadOnlyList<PlayerInteractiveModel> nearby)
    {
      if (nearby.Count == 0) return;

      if (!IsInteractorTransform()) return;

      // 현재는 키보드-마우스의 PC 환경만 지원
      if (Keyboard.current == null) return;
      
      if (Input.GetKeyDown(_interactKey))
      {
        Debug.Log($"[InteractiveGameObjectHintUIController] Interact key pressed: {_interactKey}");
        var model = nearby[selectedIndex];
        _interactionResolver.Resolve(model, _interactorTransform);
        return;
      }
    }

    bool IsInteractorTransform()
    {
      if (_interactorTransform != null) return true;
      var currInteractorTransform = CurrentSessionPlayInfoRegistry.Instance.PlayerCameraHolderTransform;
      if (currInteractorTransform != null)
      {
        _interactorTransform = currInteractorTransform;
        return true;
      }
      return false;
    }

    void RebuildInteractableViews(int count)
    {
      _interactablesContainer.Clear();
      _interactableViews.Clear();

      for (int i = 0; i < count; i++)
      {
        var row = new VisualElement { name = $"Interactive-{i}" };
        row.AddToClassList(DefaultsInteractInteractiveGameObject.VisualElementIdentifierInteractive);

        var icon = new VisualElement();
        icon.AddToClassList(DefaultsInteractInteractiveGameObject.VisualElementIdentifierInteractableIcon);
        row.Add(icon);

        var label = new Label();
        label.AddToClassList(DefaultsInteractInteractiveGameObject.VisualElementIdentifierInteractableLabel);
        row.Add(label);

        _interactablesContainer.Add(row);
        _interactableViews.Add(row);
      }
      
      selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, count - 1));
      RefreshSelection();
    }

    void UpdateInteractableContent(IReadOnlyList<PlayerInteractiveModel> nearby)
    {
      for (int i = 0; i < nearby.Count; i++)
      {
        var eachInteractable = nearby[i];
        var eachRow = _interactableViews[i];
        
        var icon = eachRow.Q<VisualElement>(className: DefaultsInteractInteractiveGameObject.VisualElementIdentifierInteractableIcon);
        icon.style.backgroundImage = new StyleBackground(eachInteractable.Icon);
        icon.style.unityBackgroundImageTintColor = eachInteractable.Color;
        
        var text = eachRow.Q<Label>(className: DefaultsInteractInteractiveGameObject.VisualElementIdentifierInteractableLabel);
        text.text = eachInteractable.DisplayText;
      }
      
      if (nearby.Count > 0)
        _selectedLabel.text = nearby[selectedIndex].DisplayText;
    }

    void RefreshSelection()
    {
      for (int i = 0; i < _interactableViews.Count; i++)
      {
        if (i == selectedIndex)
          _interactableViews[i].AddToClassList(DefaultsInteractInteractiveGameObject.VisualElementClassNameSelectedInteractable);
        else
          _interactableViews[i].RemoveFromClassList(DefaultsInteractInteractiveGameObject.VisualElementClassNameSelectedInteractable);
      }
    }
  }
}
