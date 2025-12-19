using System.Collections.Generic;
using FishNet.Object;
using TriageTrainer.Camera;
using TriageTrainer.Registry;
using TriageTrainer.Definitions;
using TriageTrainer.InteractableEntity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.Events;
using Unity.VisualScripting;
using System;

namespace TriageTrainer.UI
{
  /// <summary>
  /// InteractableObjectHintUIController는 InteractableObjectHintUI를 사용하는 데 필요한
  /// 컨트롤을 제공합니다. PlayerController등에서 이 컨트롤을 제어하는 것이 의도됩니다.
  /// 
  /// InteractiveGameObjectHintUIController는 플레이어가 상호작용할 수 있는 오브젝트가 근처에 있을 때,
  /// 해당 오브젝트들의 정보를 UI로 표시하고, 플레이어의 입력을 받아 상호작용을 처리합니다.
  /// 
  /// 이 컴포넌트는 UIDocument를 필요로 하며, UI 요소들은 USS 스타일시트를 통해 꾸며집니다.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class InteractableObjectHintUIController : UIControllerABC
  {
    [SerializeField] private int _nowSelected = -1;
    [SerializeField] private List<IInteractable> _interactables = new();
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private ScrollView _scrollView;
    [Header("Visuals")]
    [SerializeField] private Sprite _fallbackIcon;

    public UnityEvent OnNewInteractableAdded = new();
    public UnityEvent OnNewInteractableRemoved = new();

    void OnEnable()
    {
      CacheVisualReferences();

      OnNewInteractableAdded.AddListener(RefreshUI);
      OnNewInteractableRemoved.AddListener(RefreshUI);

      RefreshUI();
    }

    void OnDisable()
    {
      OnNewInteractableAdded.RemoveListener(RefreshUI);
      OnNewInteractableRemoved.RemoveListener(RefreshUI);
    }

    private void CacheVisualReferences()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      _scrollView = _uiDocument?.rootVisualElement?.Q<ScrollView>("interactable-scroll");

      if (_scrollView == null)
        Debug.LogError("[InteractableGameObjectHintUI] ScrollView with name 'interactable-scroll' was not found.");
    }

    private VisualElement GetContentContainer()
    {
      if (_scrollView == null)
      {
        CacheVisualReferences();
        if (_scrollView == null) return null;
      }

      var content = _scrollView.contentContainer;
      if (content == null)
        Debug.LogError("[InteractableGameObjectHintUI] ScrollView content container is null (panel not ready yet?).");

      return content;
    }

    private void RefreshUI()
    {
      var content = GetContentContainer();
      if (content == null) return;

      content.Clear();

      if (_interactables == null || _interactables.Count == 0)
        return;

      for (var i = 0; i < _interactables.Count; i++)
      {
        var element = CreateInteractableHintElement(_interactables[i], i == _nowSelected);
        content.Add(element);
      }
    }
    
    protected override void Awake()
    {
      base.Awake();

      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      _scrollView = _uiDocument != null
        ? _uiDocument.rootVisualElement.Q<ScrollView>("interactable-scroll")
        : null;
      if (_scrollView == null && _uiDocument != null)
      {
        _scrollView = _uiDocument.rootVisualElement.Q<ScrollView>();
      }

      ValidateRequirementsAndWarn();
    }

    /// <summary>
    /// Clear InteractableObjects List
    /// </summary>
    public void Clear()
    {
      if (_interactables == null) _interactables = new List<IInteractable>();
      _interactables.Clear();
      _nowSelected = -1;
      OnNewInteractableRemoved?.Invoke();
    }

    public void Add(IInteractable interactable)
    {
      if (interactable == null) return;
      if (_interactables == null) _interactables = new List<IInteractable>();
      _interactables.Add(interactable);
      if (_nowSelected < 0) _nowSelected = 0;
      OnNewInteractableAdded?.Invoke();
    }

    /// <summary>
    /// 현재 선택된 요소를 interactables 목록에서 제거합니다.
    /// </summary>
    public void Remove()
    {
      if (_interactables == null || _interactables.Count == 0) return;
      if (_nowSelected < 0 || _nowSelected >= _interactables.Count) return;
      _interactables.RemoveAt(_nowSelected);
      if (_interactables.Count == 0) _nowSelected = -1;
      else _nowSelected = Mathf.Clamp(_nowSelected, 0, _interactables.Count - 1);
      OnNewInteractableRemoved?.Invoke();
    }

    /// <summary>
    /// idx 위치의 요소 제거를 시도합니다.
    /// </summary>
    /// <param name="idx"></param>
    public void Remove(int idx)
    {
      if (_interactables == null) return;
      if (idx < 0 || idx >= _interactables.Count) return;
      _interactables.RemoveAt(idx);
      if (_interactables.Count == 0) _nowSelected = -1;
      else _nowSelected = Mathf.Clamp(_nowSelected, 0, _interactables.Count - 1);
      OnNewInteractableRemoved?.Invoke();
    }

    /// <summary>
    /// 현재 선택된 요소를 interactables 목록에서 제거하고 반환합니다.
    /// </summary>
    /// <returns></returns>
    public IInteractable Pop()
    {
      if (_interactables == null || _interactables.Count == 0) return null;
      if (_nowSelected < 0 || _nowSelected >= _interactables.Count) return null;
      var item = _interactables[_nowSelected];
      _interactables.RemoveAt(_nowSelected);
      if (_interactables.Count == 0) _nowSelected = -1;
      else _nowSelected = Mathf.Clamp(_nowSelected, 0, _interactables.Count - 1);
      OnNewInteractableRemoved?.Invoke();
      return item;
    }

    /// <summary>
    /// idx 위치의 요소를 interactables 목록에서 제거하고 반홚바니다.
    /// </summary>
    /// <param name="idx"></param>
    /// <returns></returns>
    public IInteractable Pop(int idx)
    {
      if (_interactables == null) return null;
      if (idx < 0 || idx >= _interactables.Count) return null;
      var item = _interactables[idx];
      _interactables.RemoveAt(idx);
      if (_interactables.Count == 0) _nowSelected = -1;
      else _nowSelected = Mathf.Clamp(_nowSelected, 0, _interactables.Count - 1);
      OnNewInteractableRemoved?.Invoke();
      return item;
    }

    public IInteractable Get(int idx)
    {
      if (_interactables == null || _interactables.Count == 0) return null;
      if (idx < 0 || idx >= _interactables.Count) return null;
      return _interactables[idx];
    }

    public IInteractable GetSelected()
    {
      return Get(_nowSelected);
    }

    public void MoveSelected(int delta)
    {
      SetSelected(_nowSelected + delta);
    }

    public void SetSelected(int idx)
    {
      if (_interactables == null || _interactables.Count == 0)
      {
        _nowSelected = -1;
        return;
      }
      // wrap-around selection
      if (idx < 0) idx = (_interactables.Count + (idx % _interactables.Count)) % _interactables.Count;
      else idx = idx % _interactables.Count;
      _nowSelected = Mathf.Clamp(idx, 0, _interactables.Count - 1);

      RefreshUI();
    }

    private VisualElement CreateInteractableHintElement(IInteractable interactable, bool isSelected)
    {
      var root = new VisualElement();
      root.AddToClassList("interactable-hint");
      if (isSelected) root.AddToClassList("selected");

      var keyHint = new VisualElement();
      keyHint.AddToClassList("interact-key-hint");
      var keyLabel = new Label(GetInteractKeyText());
      keyLabel.AddToClassList("interact-key-text");
      keyHint.Add(keyLabel);

      var contentWrapper = new VisualElement();
      contentWrapper.AddToClassList("interactable-content-wrapper");
      contentWrapper.AddToClassList("interactable-content-row");

      var iconHolder = new VisualElement();
      iconHolder.AddToClassList("interactable-icon-holder");

      var displayColor = interactable != null ? interactable.DisplayColor : new Color(1f, 1f, 1f, 0f);
      var iconSprite = interactable != null ? interactable.DisplayIcon : null;
      if (iconSprite == null) iconSprite = _fallbackIcon;

      if (iconSprite != null)
      {
        iconHolder.style.backgroundImage = new StyleBackground(iconSprite);
        iconHolder.style.backgroundColor = Color.clear; // let sprite alpha show through
      }
      else
      {
        iconHolder.style.backgroundImage = StyleKeyword.None;
        iconHolder.style.backgroundColor = displayColor;
      }

      var textLabel = new Label(interactable != null ? interactable.DisplayText : string.Empty);
      textLabel.AddToClassList("interactable-content-text");

      contentWrapper.Add(iconHolder);
      contentWrapper.Add(textLabel);

      root.Add(keyHint);
      root.Add(contentWrapper);

      return root;
    }

    private static string GetInteractKeyText()
    {
      return KeyboardConfigurationRegistry.InteractInteractableObject.ToString();
    }

    private void ValidateRequirementsAndWarn()
    {
      if (_uiDocument.IsUnityNull()) Debug.LogError("[InteractableGameObjectHintUI] Controller cannot find UIDocument.");
      if (_scrollView.IsUnityNull()) Debug.LogError("[InteractableGameObjectHintUI] Controller cannot find ScrollView.");
    }
  }
}
