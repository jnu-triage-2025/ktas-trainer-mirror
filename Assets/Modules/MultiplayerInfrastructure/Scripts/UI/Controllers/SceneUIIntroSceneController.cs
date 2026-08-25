using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// IntroScene UI 컨트롤러.
///
/// 세 개의 패널을 전환하며 동작합니다:
///   1. 메인 메뉴: 플레이어 이름 입력, 튜토리얼, 플레이, 설정, 종료
///   2. 플레이 패널: 호스트(이 컴퓨터를 서버로), LAN 세션 목록(검색), 직접 연결 진입
///   3. 직접 연결 패널: 주소/포트 입력, 접속
///
/// 설정 UI는 <see cref="SettingsUIController"/>를 동적으로 생성하여 <see cref="UIOverlayStack"/>으로 표시합니다.
///
/// Requirements:
///   - Scenes in Build Settings: IntroScene, IngameScene, TutorialScene
/// </summary>
namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class SceneUIIntroSceneController : UIDocumentControllerABC
  {
    private const string PlayerNamePreferenceKey = "IntroScene.PlayerName";

    [Header("Scene flow")]
    [SerializeField] private string ingameSceneName = DefaultsSceneControl.IngameSceneName;
    [SerializeField] private string tutorialSceneName = DefaultsSceneControl.TutorialSceneName;

    [Header("Defaults")]
    [SerializeField] private string defaultAddress = DefaultsSessionInformationModel.address;
    [SerializeField] private ushort defaultPort = DefaultsSessionInformationModel.port;
    [SerializeField] private string defaultSessionName = "MyFishSession";

    [Header("Settings UI")]
    [SerializeField] private VisualTreeAsset settingsUxml;
    [SerializeField] private VisualTreeAsset datapackSelectionUxml;

    // ─── Panels ────────────────────────────────────────────────────────────
    private VisualElement _mainPanel;
    private VisualElement _playPanel;
    private VisualElement _directPanel;

    // ─── Main menu elements ────────────────────────────────────────────────
    private TextField _nameField;
    private Button _btnTutorial;
    private Button _btnPlay;
    private Button _btnSettings;
    private Button _btnExit;

    // ─── Play panel elements ───────────────────────────────────────────────
    private Button _btnHost;
    private Button _btnDatapacks;
    private TextField _searchField;
    private ListView _listView;
    private Label _emptyState;
    private Button _btnJoinSelected;
    private Button _btnPlayBack;
    private Button _btnDirectConnect;

    // ─── Direct connect panel elements ─────────────────────────────────────
    private TextField _addrField;
    private TextField _portField;
    private Button _btnDirectBack;
    private Button _btnDirectJoin;

    // ─── Status ────────────────────────────────────────────────────────────
    private Label _status;

    // ─── Data ──────────────────────────────────────────────────────────────
    private readonly List<SessionInformationModel> _items = new List<SessionInformationModel>();
    private SessionInformationModel _selected;
    private LanDiscoveryService _discovery;
    private SettingsUIController _settingsController;
    private DatapackSelectionUIController _datapackController;

    // ════════════════════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
      var sessionConfiguration = SessionConfigurationService.Current;
      defaultAddress = sessionConfiguration.address;
      defaultPort = sessionConfiguration.port;
      defaultSessionName = sessionConfiguration.sessionName;

      ApplyIntroCursorPolicy();
      EnsureDiscoveryService();

      var doc = GetComponent<UIDocument>();
      SetDocumentVisible(doc, true);
      var root = doc.rootVisualElement;

      // ── Panels ──
      _mainPanel = root.Q<VisualElement>("mainPanel");
      _playPanel = root.Q<VisualElement>("playPanel");
      _directPanel = root.Q<VisualElement>("directPanel");

      // ── Main menu ──
      _nameField = root.Q<TextField>("nameField");
      _btnTutorial = root.Q<Button>("btnTutorial");
      _btnPlay = root.Q<Button>("btnPlay");
      _btnSettings = root.Q<Button>("btnSettings");
      _btnExit = root.Q<Button>("btnExit");

      // ── Play panel ──
      _btnHost = root.Q<Button>("btnHost");
      _btnDatapacks = root.Q<Button>("btnDatapacks");
      _searchField = root.Q<TextField>("searchField");
      _listView = root.Q<ListView>("sessionList");
      _emptyState = root.Q<Label>("emptyState");
      _btnJoinSelected = root.Q<Button>("btnJoinSelected");
      _btnPlayBack = root.Q<Button>("btnPlayBack");
      _btnDirectConnect = root.Q<Button>("btnDirectConnect");

      // ── Direct connect panel ──
      _addrField = root.Q<TextField>("addrField");
      _portField = root.Q<TextField>("portField");
      _btnDirectBack = root.Q<Button>("btnDirectBack");
      _btnDirectJoin = root.Q<Button>("btnDirectJoin");

      // ── Status ──
      _status = root.Q<Label>("statusLabel");

      // ── Button bindings ──
      _btnTutorial.clicked += OnTutorial;
      _btnPlay.clicked += () => ShowPanel(_playPanel);
      _btnSettings.clicked += OnSettings;
      _btnExit.clicked += OnExit;

      _btnHost.clicked += OnHostAndJoin;
      _btnDatapacks.clicked += OnDatapacks;
      _btnJoinSelected.clicked += OnJoinSelected;
      _btnJoinSelected.SetEnabled(false);
      _btnPlayBack.clicked += () => ShowPanel(_mainPanel);
      _btnDirectConnect.clicked += OnShowDirectConnect;

      _btnDirectBack.clicked += () => ShowPanel(_playPanel);
      _btnDirectJoin.clicked += OnDirectJoin;

      // ── ListView setup ──
      SetupListView();

      // ── Search filter ──
      _searchField.RegisterValueChangedCallback(_ => RefreshSessions());

      // ── Name validation (soft constraint) ──
      _nameField.RegisterValueChangedCallback(evt =>
      {
        PersistUserDisplayName(evt.newValue);
        UpdateNameValidation();
      });

      // ── Defaults ──
      _addrField.value = defaultAddress;
      _portField.value = defaultPort.ToString();

      RestoreUserDisplayName();

      // ── Show main menu ──
      ShowPanel(_mainPanel);
      UpdateNameValidation();
    }

    private void OnEnable()
    {
      ApplyIntroCursorPolicy();
      InvokeRepeating(nameof(RefreshSessions), 0.5f, 1.0f);
    }

    private void OnDisable()
    {
      CancelInvoke(nameof(RefreshSessions));
    }

    // ════════════════════════════════════════════════════════════════════════
    // Panel Switching
    // ════════════════════════════════════════════════════════════════════════

    private void ShowPanel(VisualElement panel)
    {
      _mainPanel.style.display = panel == _mainPanel ? DisplayStyle.Flex : DisplayStyle.None;
      _playPanel.style.display = panel == _playPanel ? DisplayStyle.Flex : DisplayStyle.None;
      _directPanel.style.display = panel == _directPanel ? DisplayStyle.Flex : DisplayStyle.None;

      if (panel == _playPanel)
      {
        _discovery.StartDiscovery();
        _discovery.ClearDiscovered();
        RefreshSessions();
        _listView.Rebuild();
      }
    }

    // ════════════════════════════════════════════════════════════════════════
    // ListView
    // ════════════════════════════════════════════════════════════════════════

    private void SetupListView()
    {
      _listView.makeItem = () =>
      {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Column;
        row.style.paddingLeft = 8;
        row.style.paddingRight = 8;

        var name = new Label { name = "name" };
        name.AddToClassList("session-row-name");

        var endpoint = new Label { name = "endpoint" };
        endpoint.AddToClassList("session-row-endpoint");

        row.Add(name);
        row.Add(endpoint);
        return row;
      };

      _listView.bindItem = (e, i) =>
      {
        if (i < 0 || i >= _items.Count)
          return;
        var si = _items[i];
        e.Q<Label>("name").text = si.Name;
        e.Q<Label>("endpoint").text = $"{si.Address}:{si.Port}";
      };

      _listView.itemsSource = _items;
      _listView.selectionType = SelectionType.Single;
      _listView.selectionChanged += OnSelectionChanged;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Session Discovery
    // ════════════════════════════════════════════════════════════════════════

    private void RefreshSessions()
    {
      if (!_discovery.HasPendingUpdate() && _items.Count > 0)
        return;

      var snapshot = _discovery.GetDiscoveredSessions();

      // Apply search filter
      var query = _searchField?.value?.Trim();
      IEnumerable<SessionInformationModel> filtered = snapshot;
      if (!string.IsNullOrEmpty(query))
      {
        filtered = snapshot.Where(s =>
          s.Name.Contains(query, System.StringComparison.OrdinalIgnoreCase) ||
          s.Address.Contains(query, System.StringComparison.OrdinalIgnoreCase));
      }

      _items.Clear();
      _items.AddRange(filtered);
      _listView.Rebuild();

      _emptyState.style.display = _items.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
      if (_items.Count == 0)
      {
        _selected = null;
      }
    }

    private void OnSelectionChanged(IEnumerable<object> objs)
    {
      _selected = null;
      foreach (var o in objs)
      {
        _selected = o as SessionInformationModel;
        break;
      }
      _btnJoinSelected.SetEnabled(_selected != null);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Actions — Main Menu
    // ════════════════════════════════════════════════════════════════════════

    private void OnTutorial()
    {
      StoreUserDisplayName();
      SetStatus("튜토리얼 씬으로 이동합니다...");
      LoadingScreen.LoadSceneAsync(tutorialSceneName);
    }

    private void OnSettings()
    {
      if (_settingsController == null)
        CreateSettingsUI();

      if (_settingsController != null)
        UIOverlayStack.Push(_settingsController);
    }

    private void OnDatapacks()
    {
      if (_datapackController != null)
        return;
      var go = new GameObject("DatapackSelectionUI");
      var mainDoc = GetComponent<UIDocument>();
      if (datapackSelectionUxml == null)
        datapackSelectionUxml = Resources.Load<VisualTreeAsset>("DatapackSelectionUI");
      if (datapackSelectionUxml == null)
      {
        Debug.LogError("[IntroUI] 데이터 팩 선택 UI용 UXML을 찾을 수 없습니다.");
        Destroy(go);
        return;
      }
      _datapackController = go.AddComponent<DatapackSelectionUIController>();
      _datapackController.Initialize(datapackSelectionUxml, mainDoc.panelSettings, () => _datapackController = null);
    }

    private void OnExit()
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    // ════════════════════════════════════════════════════════════════════════
    // Actions — Play Panel
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 이 컴퓨터를 서버로 사용하고 접속하기.
    /// 로컬에서 서버를 열고 클라이언트가 자동으로 접속합니다.
    /// </summary>
    private void OnHostAndJoin()
    {
      StoreUserDisplayName();
      StoreLaunchRequest(
        new SessionInformationModel(
          address: defaultAddress,
          port: defaultPort,
          sessionName: defaultSessionName
        ),
        isOpeningServer: true,
        useLanDiscovery: true);
      SetStatus("서버를 시작하고 접속합니다...");
      SwitchIngameScene();
    }

    private void OnShowDirectConnect()
    {
      if (_selected != null)
      {
        _addrField.value = _selected.Address;
        _portField.value = _selected.Port.ToString();
      }
      ShowPanel(_directPanel);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Actions — Play Panel: Join Selected
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 선택된 LAN 세션에 접속합니다.
    /// </summary>
    private void OnJoinSelected()
    {
      if (_selected == null)
      {
        SetStatus("서버를 먼저 선택하세요.");
        return;
      }

      StoreUserDisplayName();
      StoreLaunchRequest(
        new SessionInformationModel(
          address: _selected.Address,
          port: _selected.Port,
          sessionName: _selected.Name
        ),
        isOpeningServer: false,
        useLanDiscovery: true);
      SetStatus($"접속 중: {_selected.Address}:{_selected.Port}");
      SwitchIngameScene();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Actions — Direct Connect Panel
    // ════════════════════════════════════════════════════════════════════════

    private void OnDirectJoin()
    {
      var ip = _addrField.value.Trim();
      if (!ushort.TryParse(_portField.value, out ushort port))
      {
        SetStatus("유효하지 않은 포트입니다.");
        return;
      }

      StoreUserDisplayName();
      StoreLaunchRequest(
        new SessionInformationModel(
          address: ip,
          port: port,
          sessionName: "Direct"
        ),
        isOpeningServer: false,
        useLanDiscovery: false);
      SetStatus($"직접 접속 중: {ip}:{port}");
      SwitchIngameScene();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Settings UI (Dynamic Creation)
    // ════════════════════════════════════════════════════════════════════════

    private void CreateSettingsUI()
    {
      // 1. Registry에서 기존 SettingsUIController 탐색
      _settingsController = Registry.Registry.Get<SettingsUIController>(
        RegistryType.UI,
        Registry.Registry.TypeKey(typeof(SettingsUIController))
      ) ?? FindFirstObjectByType<SettingsUIController>();

      if (_settingsController != null)
        return;

      // 2. UXML 에셋 로드
      if (settingsUxml == null)
        settingsUxml = Resources.Load<VisualTreeAsset>("SettingsUI");

      if (settingsUxml == null)
      {
        Debug.LogWarning("[IntroUI] 설정 UI용 UXML을 찾을 수 없습니다. " +
          "인스펙터에서 settingsUxml을 할당하거나, Resources/SettingsUI 경로에 배치하세요.");
        return;
      }

      // 3. SettingsUI GameObject 동적 생성
      var go = new GameObject("SettingsUI");
      var doc = go.AddComponent<UIDocument>();

      // Intro UI 컨트롤러의 자식으로 두면 UIDocument가 부모 문서의 중첩 UI로 취급되어
      // 좌표계가 카드/컨트롤러 범위로 제한된다. 설정은 별도 화면 오버레이이므로 씬 루트를 유지한다.
      var mainDoc = GetComponent<UIDocument>();
      if (mainDoc?.panelSettings != null)
        doc.panelSettings = mainDoc.panelSettings;

      // PanelSettings를 먼저 지정해야 UXML이 처음부터 Intro 화면과 동일한 패널 좌표계에 붙는다.
      doc.visualTreeAsset = settingsUxml;
      doc.sortingOrder = DefaultsUIDocument.SettingsUISortOrder;

      _settingsController = go.AddComponent<SettingsUIController>();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════════
    // Name Validation (Soft Constraint)
    // ════════════════════════════════════════════════════════════════════════

    private bool IsNameValid()
    {
      return !string.IsNullOrWhiteSpace(_nameField?.value);
    }

    private void UpdateNameValidation()
    {
      bool valid = IsNameValid();
      _btnTutorial.SetEnabled(valid);
      _btnPlay.SetEnabled(valid);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Helpers (continued)
    // ════════════════════════════════════════════════════════════════════════

    private void EnsureDiscoveryService()
    {
      _discovery = LanDiscoveryService.Instance;
      if (_discovery == null)
      {
        var go = new GameObject("LanDiscoveryService");
        _discovery = go.AddComponent<LanDiscoveryService>();
      }
    }

    private void StoreUserDisplayName()
    {
      var name = _nameField?.value?.Trim();
      if (!string.IsNullOrWhiteSpace(name))
      {
        PersistUserDisplayName(name);
        Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.UserDisplayName, name);
      }
    }

    private void RestoreUserDisplayName()
    {
      var name = PlayerPrefs.GetString(PlayerNamePreferenceKey, string.Empty).Trim();
      if (string.IsNullOrWhiteSpace(name))
        name = Registry.Registry.Get<string>(RegistryType.RuntimeState, RegistryGlobalKeys.UserDisplayName)?.Trim();

      if (!string.IsNullOrWhiteSpace(name))
      {
        _nameField.value = name;
        Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.UserDisplayName, name);
      }
    }

    private static void PersistUserDisplayName(string value)
    {
      var name = value?.Trim();
      if (string.IsNullOrWhiteSpace(name))
      {
        PlayerPrefs.DeleteKey(PlayerNamePreferenceKey);
        PlayerPrefs.Save();
        return;
      }

      PlayerPrefs.SetString(PlayerNamePreferenceKey, name);
      PlayerPrefs.Save();
    }

    private void StoreLaunchRequest(
      SessionInformationModel sessionInformation,
      bool isOpeningServer,
      bool useLanDiscovery)
    {
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.SessionInformation, sessionInformation);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer, isOpeningServer);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.UseLanDiscovery, useLanDiscovery);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.LoadedFromIntroScene, true);
    }

    private void SetStatus(string msg)
    {
      if (_status != null)
        _status.text = msg;
      Debug.Log($"[IntroUI] {msg}");
    }

    private void SwitchIngameScene()
    {
      if (string.IsNullOrWhiteSpace(ingameSceneName))
      {
        Debug.LogWarning("[IntroUI] Ingame scene name is not set. Cannot switch scenes.");
        return;
      }

      LoadingScreen.LoadSceneAsync(ingameSceneName);
    }

    private static void ApplyIntroCursorPolicy()
    {
      UnityEngine.Cursor.lockState = CursorLockMode.None;
      UnityEngine.Cursor.visible = true;
    }
  }
}
