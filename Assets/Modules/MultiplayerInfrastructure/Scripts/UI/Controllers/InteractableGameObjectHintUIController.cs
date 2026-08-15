using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.InteractableEntity;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Events;
using Unity.VisualScripting;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// InteractableObjectHintUIController는 InteractableObjectHintUI를 사용하는 데 필요한
  /// 컨트롤을 제공합니다. PlayerController등에서 이 컨트롤을 제어하는 것이 의도됩니다.
  /// 
  /// 다이얼로그 모드를 지원하여, 대화 중에는 선택지만 표시하고
  /// 대화 종료 후 원래 상호작용 객체 목록을 복원합니다.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class InteractableObjectHintUIController : UIControllerABC
  {
    #region Serialized Fields

    [SerializeField] private int _nowSelected = -1;
    [SerializeField] private List<IInteract> _interacts = new();
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private InteractableObjectHintList _hintList;

    [Header("Visuals")]
    [SerializeField] private Sprite _dialogueSelectionIcon;

    [Header("Mode")]
    [SerializeField] private InteractableHintUIMode _currentMode = InteractableHintUIMode.Normal;

    #endregion

    #region Private Fields

    // 다이얼로그 모드 진입 시 백업할 상호작용 객체 목록
    private List<IInteract> _cachedInteracts = new();
    private int _cachedSelectedIndex = -1;

    #endregion

    #region Events

    public UnityEvent OnNewInteractableAdded = new();
    public UnityEvent OnNewInteractableRemoved = new();
    public UnityEvent<InteractableHintUIMode> OnModeChanged = new();

    /// <summary>
    /// 다이얼로그 선택지가 변경되었을 때 발생
    /// </summary>
    public UnityEvent OnDialogueSelectionsChanged = new();

    /// <summary>
    /// 화면의 상호작용 메뉴 행을 마우스로 클릭했을 때 발생합니다.
    /// 인덱스는 클릭 시점의 현재 목록을 기준으로 합니다.
    /// </summary>
    public event Action<int> InteractionClicked;

    #endregion

    #region Properties

    /// <summary>
    /// 현재 UI 모드
    /// </summary>
    public InteractableHintUIMode CurrentMode => _currentMode;

    /// <summary>
    /// 다이얼로그 모드인지 여부
    /// </summary>
    public bool IsDialogueMode => _currentMode == InteractableHintUIMode.Dialogue;

    /// <summary>
    /// 현재 선택 가능한 항목이 있는지 여부
    /// </summary>
    public bool HasAnySelections => _interacts != null && _interacts.Count > 0;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
      base.Awake();

      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      _hintList = _uiDocument != null
          ? _uiDocument.rootVisualElement.Q<InteractableObjectHintList>("interactable-scroll")
          : null;

      if (_hintList == null && _uiDocument != null)
      {
        _hintList = _uiDocument.rootVisualElement.Q<InteractableObjectHintList>();
      }

      ValidateRequirementsAndWarn();
    }

    private void OnEnable()
    {
      CacheVisualReferences();

      OnNewInteractableAdded.AddListener(RefreshUI);
      OnNewInteractableRemoved.AddListener(RefreshUI);

      RefreshUI();
    }

    private void OnDisable()
    {
      OnNewInteractableAdded.RemoveListener(RefreshUI);
      OnNewInteractableRemoved.RemoveListener(RefreshUI);
    }

    #endregion

    #region Visual Reference Caching

    private void CacheVisualReferences()
    {
      if (_uiDocument.IsUnityNull())
        _uiDocument = GetComponent<UIDocument>();

      _hintList = _uiDocument?.rootVisualElement?.Q<InteractableObjectHintList>("interactable-scroll");

      if (_hintList == null)
        _hintList = _uiDocument?.rootVisualElement?.Q<InteractableObjectHintList>();

      if (_hintList == null)
        Debug.LogError("[InteractableHintUI] InteractableObjectHintList with name 'interactable-scroll' was not found.");
    }

    #endregion

    #region UI Refresh

    private void RefreshUI()
    {
      if (_hintList == null) return;

      _hintList.Rebuild(
        _interacts,
        _nowSelected,
        _currentMode,
        GetInteractKeyText(),
        _dialogueSelectionIcon,
        HandleInteractionClicked);
    }

    private void HandleInteractionClicked(int index)
    {
      if (_interacts == null || index < 0 || index >= _interacts.Count)
        return;

      _nowSelected = index;
      InteractionClicked?.Invoke(index);
      // 클릭 이벤트가 대화 모드를 끝내거나 목록을 변경할 수 있으므로, 현재 VisualElement를
      // 디스패치하는 도중 트리를 재구축하지 않고 상호작용 처리가 끝난 뒤 한 번만 갱신한다.
      if (this == null || !isActiveAndEnabled)
        return;
      RefreshUI();
    }

    private void ScrollToSelected()
    {
      if (_hintList == null || _nowSelected < 0) return;

      _hintList.ScrollToSelected(_nowSelected);
    }

    #endregion

    #region Dialogue Mode API

    /// <summary>
    /// 다이얼로그 모드 진입
    /// 현재 상호작용 객체 목록을 백업하고 UI를 비웁니다.
    /// </summary>
    public void EnterDialogueMode()
    {
      if (_currentMode == InteractableHintUIMode.Dialogue)
      {
        Debug.LogWarning("[InteractableHintUI] Already in dialogue mode");
        return;
      }

      // 현재 상태 백업
      _cachedInteracts.Clear();
      _cachedInteracts.AddRange(_interacts);
      _cachedSelectedIndex = _nowSelected;

      // UI 초기화
      _interacts.Clear();
      _nowSelected = -1;

      _currentMode = InteractableHintUIMode.Dialogue;

      RefreshUI();
      OnModeChanged?.Invoke(_currentMode);
    }

    /// <summary>
    /// 다이얼로그 모드 종료
    /// 백업된 상호작용 객체 목록을 복원합니다.
    /// </summary>
    public void ExitDialogueMode()
    {
      if (_currentMode != InteractableHintUIMode.Dialogue)
      {
        Debug.LogWarning("[InteractableHintUI] Not in dialogue mode");
        return;
      }

      // 다이얼로그 선택지 정리
      _interacts.Clear();

      // 백업된 상태 복원
      _interacts.AddRange(_cachedInteracts);
      _nowSelected = _cachedSelectedIndex;

      // 백업 초기화
      _cachedInteracts.Clear();
      _cachedSelectedIndex = -1;

      _currentMode = InteractableHintUIMode.Normal;

      RefreshUI();
      OnModeChanged?.Invoke(_currentMode);
    }

    /// <summary>
    /// 다이얼로그 선택지 설정 (다이얼로그 모드에서만 동작)
    /// </summary>
    /// <param name="selections">표시할 선택지 목록</param>
    public void SetDialogueSelections(List<IInteract> selections)
    {
      SetDialogueSelections((IReadOnlyList<IInteract>)selections);
    }

    /// <summary>
    /// 다이얼로그 선택지 설정 (IReadOnlyList 버전)
    /// </summary>
    public void SetDialogueSelections(IReadOnlyList<IInteract> selections)
    {
      if (_currentMode != InteractableHintUIMode.Dialogue)
      {
        Debug.LogWarning("[InteractableHintUI] SetDialogueSelections called but not in dialogue mode");
        return;
      }

      _interacts.Clear();
      _nowSelected = -1;

      if (selections != null && selections.Count > 0)
      {
        for (int i = 0; i < selections.Count; i++)
        {
          _interacts.Add(selections[i]);
        }
        _nowSelected = 0;
      }

      RefreshUI();
      OnDialogueSelectionsChanged?.Invoke();
    }

    /// <summary>
    /// 다이얼로그 선택지 클리어 (다이얼로그 모드에서만 동작)
    /// 선택지 없이 UI를 비웁니다.
    /// </summary>
    public void ClearDialogueSelections()
    {
      if (_currentMode != InteractableHintUIMode.Dialogue)
        return;

      _interacts.Clear();
      _nowSelected = -1;
      RefreshUI();
      OnDialogueSelectionsChanged?.Invoke();
    }

    /// <summary>
    /// 현재 선택된 다이얼로그 선택지가 있는지 확인
    /// </summary>
    public bool HasDialogueSelection()
    {
      return _currentMode == InteractableHintUIMode.Dialogue
          && _interacts != null
          && _interacts.Count > 0
          && _nowSelected >= 0
          && _nowSelected < _interacts.Count;
    }

    /// <summary>
    /// 현재 선택된 다이얼로그 선택지를 실행합니다.
    /// </summary>
    /// <param name="interactor">상호작용을 수행하는 Transform (플레이어)</param>
    /// <returns>선택지가 실행되었는지 여부</returns>
    public bool ExecuteSelectedDialogueSelection(Transform interactor)
    {
      if (!HasDialogueSelection())
        return false;

      var selected = _interacts[_nowSelected];
      if (selected != null)
      {
        selected.Interact(interactor);
        return true;
      }

      return false;
    }

    #endregion

    #region Normal Mode API

    /// <summary>
    /// Clear InteractableObjects List
    /// 다이얼로그 모드에서는 동작하지 않습니다.
    /// </summary>
    public void Clear()
    {
      if (_currentMode == InteractableHintUIMode.Dialogue)
      {
        Debug.LogWarning("[InteractableHintUI] Clear ignored in dialogue mode");
        return;
      }

      if (_interacts == null) _interacts = new List<IInteract>();
      _interacts.Clear();
      _nowSelected = -1;
      OnNewInteractableRemoved?.Invoke();
    }

    /// <summary>
    /// 상호작용 객체 추가
    /// 다이얼로그 모드에서는 캐시에 추가됩니다.
    /// </summary>
    public void Add(IInteract interact)
    {
      if (interact == null) return;

      if (_currentMode == InteractableHintUIMode.Dialogue)
      {
        // 다이얼로그 모드에서는 캐시에 추가
        if (_cachedInteracts == null) _cachedInteracts = new List<IInteract>();
        if (!_cachedInteracts.Contains(interact))
        {
          _cachedInteracts.Add(interact);
          if (_cachedSelectedIndex < 0) _cachedSelectedIndex = 0;
        }
        return;
      }

      if (_interacts == null) _interacts = new List<IInteract>();
      _interacts.Add(interact);
      if (_nowSelected < 0) _nowSelected = 0;
      OnNewInteractableAdded?.Invoke();
    }

    /// <summary>
    /// 상호작용 객체 제거
    /// 다이얼로그 모드에서는 캐시에서 제거됩니다.
    /// </summary>
    public void Remove(IInteract interact)
    {
      if (interact == null) return;

      if (_currentMode == InteractableHintUIMode.Dialogue)
      {
        // 다이얼로그 모드에서는 캐시에서 제거
        int cachedIdx = _cachedInteracts.IndexOf(interact);
        if (cachedIdx >= 0)
        {
          _cachedInteracts.RemoveAt(cachedIdx);
          if (_cachedInteracts.Count == 0) _cachedSelectedIndex = -1;
          else _cachedSelectedIndex = Mathf.Clamp(_cachedSelectedIndex, 0, _cachedInteracts.Count - 1);
        }
        return;
      }

      int idx = _interacts.IndexOf(interact);
      if (idx >= 0)
      {
        Remove(idx);
      }
    }

    /// <summary>
    /// 현재 선택된 요소를 interactables 목록에서 제거합니다.
    /// 다이얼로그 모드에서는 동작하지 않습니다.
    /// </summary>
    public void Remove()
    {
      if (_currentMode == InteractableHintUIMode.Dialogue) return;

      if (_interacts == null || _interacts.Count == 0) return;
      if (_nowSelected < 0 || _nowSelected >= _interacts.Count) return;
      _interacts.RemoveAt(_nowSelected);
      if (_interacts.Count == 0) _nowSelected = -1;
      else _nowSelected = Mathf.Clamp(_nowSelected, 0, _interacts.Count - 1);
      OnNewInteractableRemoved?.Invoke();
    }

    /// <summary>
    /// idx 위치의 요소 제거를 시도합니다.
    /// 다이얼로그 모드에서는 동작하지 않습니다.
    /// </summary>
    public void Remove(int idx)
    {
      if (_currentMode == InteractableHintUIMode.Dialogue) return;

      if (_interacts == null) return;
      if (idx < 0 || idx >= _interacts.Count) return;
      _interacts.RemoveAt(idx);
      if (_interacts.Count == 0) _nowSelected = -1;
      else _nowSelected = Mathf.Clamp(_nowSelected, 0, _interacts.Count - 1);
      OnNewInteractableRemoved?.Invoke();
    }

    /// <summary>
    /// 현재 선택된 요소를 interactables 목록에서 제거하고 반환합니다.
    /// 다이얼로그 모드에서는 null을 반환합니다.
    /// </summary>
    public IInteract Pop()
    {
      if (_currentMode == InteractableHintUIMode.Dialogue) return null;

      if (_interacts == null || _interacts.Count == 0) return null;
      if (_nowSelected < 0 || _nowSelected >= _interacts.Count) return null;
      var item = _interacts[_nowSelected];
      _interacts.RemoveAt(_nowSelected);
      if (_interacts.Count == 0) _nowSelected = -1;
      else _nowSelected = Mathf.Clamp(_nowSelected, 0, _interacts.Count - 1);
      OnNewInteractableRemoved?.Invoke();
      return item;
    }

    /// <summary>
    /// idx 위치의 요소를 interactables 목록에서 제거하고 반환합니다.
    /// 다이얼로그 모드에서는 null을 반환합니다.
    /// </summary>
    public IInteract Pop(int idx)
    {
      if (_currentMode == InteractableHintUIMode.Dialogue) return null;

      if (_interacts == null) return null;
      if (idx < 0 || idx >= _interacts.Count) return null;
      var item = _interacts[idx];
      _interacts.RemoveAt(idx);
      if (_interacts.Count == 0) _nowSelected = -1;
      else _nowSelected = Mathf.Clamp(_nowSelected, 0, _interacts.Count - 1);
      OnNewInteractableRemoved?.Invoke();
      return item;
    }

    /// <summary>
    /// 상호작용 객체 목록 일괄 업데이트
    /// 다이얼로그 모드에서는 캐시가 업데이트됩니다.
    /// </summary>
    public void UpdateInteractables(IReadOnlyList<IInteract> newInteracts)
    {
      if (_currentMode == InteractableHintUIMode.Dialogue)
      {
        // 다이얼로그 모드에서는 캐시 업데이트
        _cachedInteracts.Clear();
        if (newInteracts != null)
        {
          for (int i = 0; i < newInteracts.Count; i++)
            _cachedInteracts.Add(newInteracts[i]);
        }
        if (_cachedInteracts.Count == 0) _cachedSelectedIndex = -1;
        else if (_cachedSelectedIndex < 0) _cachedSelectedIndex = 0;
        else _cachedSelectedIndex = Mathf.Clamp(_cachedSelectedIndex, 0, _cachedInteracts.Count - 1);
        return;
      }

      _interacts.Clear();
      if (newInteracts != null)
      {
        for (int i = 0; i < newInteracts.Count; i++)
          _interacts.Add(newInteracts[i]);
      }

      if (_interacts.Count == 0) _nowSelected = -1;
      else if (_nowSelected < 0) _nowSelected = 0;
      else _nowSelected = Mathf.Clamp(_nowSelected, 0, _interacts.Count - 1);

      RefreshUI();
    }

    #endregion

    #region Common API

    /// <summary>
    /// 인덱스로 상호작용 객체 가져오기
    /// </summary>
    public IInteract Get(int idx)
    {
      if (_interacts == null || _interacts.Count == 0) return null;
      if (idx < 0 || idx >= _interacts.Count) return null;
      return _interacts[idx];
    }

    /// <summary>
    /// 현재 선택된 상호작용 객체 가져오기
    /// </summary>
    public IInteract GetSelected()
    {
      return Get(_nowSelected);
    }

    /// <summary>
    /// 선택 인덱스 이동 (delta만큼)
    /// </summary>
    public void MoveSelected(int delta)
    {
      SetSelected(_nowSelected + delta);
    }

    /// <summary>
    /// 선택 인덱스 직접 설정
    /// </summary>
    public void SetSelected(int idx)
    {
      if (_interacts == null || _interacts.Count == 0)
      {
        _nowSelected = -1;
        return;
      }

      // wrap-around selection
      if (idx < 0) idx = (_interacts.Count + (idx % _interacts.Count)) % _interacts.Count;
      else idx = idx % _interacts.Count;
      _nowSelected = Mathf.Clamp(idx, 0, _interacts.Count - 1);

      RefreshUI();
    }

    /// <summary>
    /// 다음 항목 선택
    /// </summary>
    public void SelectNext()
    {
      MoveSelected(1);
    }

    /// <summary>
    /// 이전 항목 선택
    /// </summary>
    public void SelectPrevious()
    {
      MoveSelected(-1);
    }

    /// <summary>
    /// 현재 선택된 인덱스
    /// </summary>
    public int GetSelectedIndex() => _nowSelected;

    /// <summary>
    /// 현재 목록의 항목 수
    /// </summary>
    public int GetCount() => _interacts?.Count ?? 0;

    /// <summary>
    /// 선택 가능한 항목이 있는지 여부
    /// </summary>
    public bool HasSelections() => _interacts != null && _interacts.Count > 0;

    /// <summary>
    /// 현재 선택된 상호작용 객체를 실행합니다.
    /// </summary>
    /// <param name="interactor">상호작용을 수행하는 Transform</param>
    /// <returns>상호작용이 실행되었는지 여부</returns>
    public bool ExecuteSelected(Transform interactor)
    {
      var selected = GetSelected();
      if (selected != null)
      {
        selected.Interact(interactor);
        return true;
      }
      return false;
    }

    #endregion

    #region UI Helpers

    private string GetInteractKeyText()
    {
      return DefaultsKeyConfiguration.InteractInteractableObject.ToString();
    }

    #endregion

    #region Validation

    private void ValidateRequirementsAndWarn()
    {
      if (_uiDocument.IsUnityNull())
        Debug.LogError("[InteractableHintUI] Controller cannot find UIDocument.");
      if (_hintList.IsUnityNull())
        Debug.LogError("[InteractableHintUI] Controller cannot find InteractableObjectHintList.");
    }

    #endregion

    #region Debug

#if UNITY_EDITOR
    [ContextMenu("Debug - Print State")]
    private void DebugPrintState()
    {
      Debug.Log($"[InteractableHintUI] Mode: {_currentMode}");
      Debug.Log($"[InteractableHintUI] Selected: {_nowSelected}/{_interacts?.Count ?? 0}");
      Debug.Log($"[InteractableHintUI] Cached: {_cachedSelectedIndex}/{_cachedInteracts?.Count ?? 0}");
    }

    [ContextMenu("Debug - Force Refresh")]
    private void DebugForceRefresh()
    {
      RefreshUI();
    }
#endif

    #endregion
  }
}
