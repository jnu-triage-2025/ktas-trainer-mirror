using Input = MultiplayerInfrastructure.Automation.PlayerInput;
using System.Collections.Generic;
using System.Text;
using FishNet;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 현재 세션에 접속한 플레이어 목록을 지정한 키(기본 Tab)를 누르고 있는 동안 보여 주는 HUD 컨트롤러.
  ///
  /// 표시할 목록은 <see cref="UserDescriptorService"/> 의 로컬 사전에서 읽는다. 이 사전은 서버와
  /// 모든 클라이언트가 PlayerController 스폰/디스폰 시점에 각자 갱신하므로 별도 RPC 가 필요 없다.
  /// 역할 표시에 쓰는 태그도 <see cref="PlayerTagService"/> 가 옵저버 RPC 로 동기화한 로컬 값이다.
  ///
  /// z-order 및 입력 규약:
  ///   - 문서 sortingOrder 를 다른 비차단 HUD 보다는 위, QuestPanel(4) 이상의 모달 문서보다는
  ///     아래로 두어 모달 UI 를 가리지 않는다.
  ///   - 문서 전체를 <see cref="UIDocumentControllerABC.SetDocumentRootPickingEnabled"/> 로
  ///     항상 픽킹 불가로 유지해 포인터/휠 입력을 가로채지 않는다.
  ///   - 채팅·인벤토리·설정 등 <see cref="UIOverlayStack"/> 오버레이가 열려 있는 동안에는 표시하지
  ///     않는다. 채팅 입력 중 Tab 은 명령어 자동완성이므로 목록이 떠서는 안 된다.
  ///
  /// UI 구성은 코드 전용이다: 빈 UIDocument(+ PanelSettings)에 <see cref="PlayerListOverlayElement"/> 를
  /// C# 으로 생성해 부착한다(uxml/uss/StyleSheet 불필요).
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class PlayerListOverlayUIController : UIControllerABC
  {
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.PlayerListOverlaySortOrder;

    [Tooltip("키를 누르고 있는 동안 목록을 다시 만드는 주기(초).")]
    [SerializeField] private float _refreshInterval = 0.25f;

    private UIDocument _uiDocument;
    private PlayerListOverlayElement _element;
    private bool _isVisible;
    private bool _wasKeyHeld;
    private float _nextEvaluationTime;

    private readonly List<UserDescriptor> _descriptorBuffer = new();
    private readonly List<PlayerListOverlayElement.Entry> _entryBuffer = new();
    private readonly StringBuilder _tagBuilder = new();

    private static readonly System.Comparison<UserDescriptor> DescriptorOrder = CompareDescriptors;

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      _uiDocument.sortingOrder = _sortingOrder;
      EnsureElement();
    }

    private void OnDisable()
    {
      _wasKeyHeld = false;
      ApplyVisible(false);
    }

    private void Update()
    {
      var key = KeyBindingRepository.GetBoundKey(
        DefaultsKeyConfiguration.ShowPlayerListActionId,
        DefaultsKeyConfiguration.ShowPlayerList);

      bool held = key != KeyCode.None && Input.GetKey(key);
      if (!held)
      {
        _wasKeyHeld = false;
        ApplyVisible(false);
        return;
      }

      bool justPressed = !_wasKeyHeld;
      _wasKeyHeld = true;

      // 키를 누르고 있는 동안에만, 그것도 일정 주기로만 평가한다.
      // (UIOverlayStack.IsEmpty 는 내부 정리 과정에서 리스트를 할당하므로
      //  매 프레임 호출하면 프레임당 GC 할당이 발생한다.)
      if (!justPressed && Time.unscaledTime < _nextEvaluationTime)
        return;

      _nextEvaluationTime = Time.unscaledTime + Mathf.Max(0.05f, _refreshInterval);

      // 채팅(Tab = 명령어 자동완성)·인벤토리·설정 등 모달 오버레이가 열려 있으면 표시하지 않는다.
      if (!UIOverlayStack.IsEmpty())
      {
        ApplyVisible(false);
        return;
      }

      if (!EnsureElement())
        return;

      RebuildEntries();
      _element.SetEntries(_entryBuffer);
      // 매 갱신마다 행이 새로 만들어지므로, 새 자식에도 픽킹 불가 정책을 다시 적용한다.
      UIDocumentInteractionPolicy.Refresh(_uiDocument);
      ApplyVisible(true);
    }

    /// <summary>UIDocument root 가 (재)생성된 뒤에도 요소와 픽킹 정책을 유효하게 유지한다.</summary>
    private bool EnsureElement()
    {
      if (_element != null && _element.panel != null)
        return true;

      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      var root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
      if (root == null)
        return false;

      // 이 문서는 어떤 상태에서도 포인터 입력을 받지 않는다.
      SetDocumentRootPickingEnabled(_uiDocument, false);

      _element = root.Q<PlayerListOverlayElement>(PlayerListOverlayElement.RootName);
      if (_element == null)
      {
        _element = new PlayerListOverlayElement();
        root.Add(_element);
        // 동적으로 추가한 자식에도 픽킹 정책을 다시 적용한다.
        UIDocumentInteractionPolicy.Refresh(_uiDocument);
      }

      _element.SetVisibleState(_isVisible);
      return true;
    }

    private void ApplyVisible(bool visible)
    {
      if (_isVisible == visible)
        return;

      _isVisible = visible;
      _element?.SetVisibleState(visible);
    }

    private void RebuildEntries()
    {
      _descriptorBuffer.Clear();
      _entryBuffer.Clear();

      foreach (var pair in UserDescriptorService.GetAll())
      {
        if (pair.Value != null)
          _descriptorBuffer.Add(pair.Value);
      }

      // 모든 피어에서 같은 순서로 보이도록 표시 이름 기준으로 정렬한다
      // (사전 순회 순서에 의존하지 않는다).
      _descriptorBuffer.Sort(DescriptorOrder);

      string localIdentifier = ResolveLocalUserIdentifier();
      for (int i = 0; i < _descriptorBuffer.Count; i++)
      {
        var descriptor = _descriptorBuffer[i];
        bool isLocal = !string.IsNullOrEmpty(localIdentifier)
          && string.Equals(descriptor.Identifier, localIdentifier, System.StringComparison.Ordinal);
        _entryBuffer.Add(new PlayerListOverlayElement.Entry(
          descriptor.DisplayName,
          FormatTags(descriptor.Identifier),
          isLocal));
      }
    }

    /// <summary>로컬 플레이어의 Identifier. 아직 접속하지 않았으면 null.</summary>
    private static string ResolveLocalUserIdentifier()
    {
      // ClientManager.Connection 은 미접속 시 null 이 아니라 EmptyConnection(ClientId -1) 이므로
      // IsValid 로 걸러야 한다.
      var connection = InstanceFinder.ClientManager?.Connection;
      if (connection == null || !connection.IsValid)
        return null;

      return UserDescriptorService.TryGetByClientId(connection.ClientId, out var descriptor)
        ? descriptor?.Identifier
        : null;
    }

    private string FormatTags(string identifier)
    {
      var tags = PlayerTagService.GetTagsByIdentifier(identifier);
      if (tags == null || tags.Count == 0)
        return string.Empty;

      _tagBuilder.Clear();
      for (int i = 0; i < tags.Count; i++)
      {
        if (string.IsNullOrWhiteSpace(tags[i]))
          continue;

        if (_tagBuilder.Length > 0)
          _tagBuilder.Append(' ');
        _tagBuilder.Append('[').Append(tags[i]).Append(']');
      }

      return _tagBuilder.ToString();
    }

    private static int CompareDescriptors(UserDescriptor left, UserDescriptor right)
    {
      int byName = string.Compare(
        left.DisplayName, right.DisplayName, System.StringComparison.OrdinalIgnoreCase);
      return byName != 0
        ? byName
        : string.CompareOrdinal(left.Identifier, right.Identifier);
    }
  }
}
