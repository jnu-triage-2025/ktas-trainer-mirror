using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Definitions;

/// <summary>
/// Controller for the Intro Scene UI.
/// 
/// Requiremtns when getting started from empty scene:
/// Scenes must be added to Build Settings:
/// - IntroScene (this)
/// - IngameScene (gameplay)
/// </summary>
namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class SceneUIIntroSceneController : MonoBehaviour
  {
    [Header("Scene flow")]
    [SerializeField] private string ingameSceneName = DefaultsSceneControl.IngameSceneName;

    [Header("Defaults")]
    [SerializeField] private string defaultAddress = DefaultsSessionInformationModel.address;
    [SerializeField] private ushort defaultPort = DefaultsSessionInformationModel.port;
    [SerializeField] private string defaultSessionName = "MyFishSession";

    private Button _btnCreateAndJoin;
    private Button _btnSearchLAN;
    private Button _btnJoinSelected;
    private Button _btnDirectJoin;
    private TextField _addrField;
    private TextField _portField;
    private TextField _nameField;
    private ListView _listView;
    private Label _emptyState;
    private Label _status;

    private readonly List<SessionInformationModel> _items = new List<SessionInformationModel>();
    private SessionInformationModel _selected;

    private LanDiscoveryService _discovery;

    private void Awake()
    {
      EnsureDiscoveryService();

      var doc = GetComponent<UIDocument>();
      var root = doc.rootVisualElement;

      _btnCreateAndJoin = root.Q<Button>("btnCreateAndJoin");
      _btnSearchLAN = root.Q<Button>("btnSearchLAN");
      _btnJoinSelected = root.Q<Button>("btnJoinSelected");
      _btnDirectJoin = root.Q<Button>("btnDirectJoin");
      _addrField = root.Q<TextField>("addrField");
      _portField = root.Q<TextField>("portField");
      _nameField = root.Q<TextField>("nameField");
      _listView = root.Q<ListView>("sessionList");
      _emptyState = root.Q<Label>("emptyState");
      _status = root.Q<Label>("statusLabel");

      _listView.makeItem = () =>
      {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Column;
        row.style.paddingLeft = 8;
        row.style.paddingRight = 8;

        var name = new Label { name = "name" };
        name.style.unityFontStyleAndWeight = FontStyle.Bold;

        var endpoint = new Label { name = "endpoint" };
        endpoint.style.color = new StyleColor(new Color(0.7f, 0.75f, 0.82f));

        row.Add(name);
        row.Add(endpoint);
        return row;
      };

      _listView.bindItem = (e, i) =>
      {
        if (i < 0 || i >= _items.Count) return;
        var si = _items[i];
        e.Q<Label>("name").text = si.Name;
        e.Q<Label>("endpoint").text = $"{si.Address}:{si.Port}";
      };

      _listView.itemsSource = _items;
      _listView.selectionType = SelectionType.Single;
      _listView.onSelectionChange += OnSelectionChanged;

      _btnJoinSelected.SetEnabled(false);
      _btnCreateAndJoin.clicked += OnCreateAndJoin;
      _btnSearchLAN.clicked += OnSearchLan;
      _btnJoinSelected.clicked += OnJoinSelected;
      _btnDirectJoin.clicked += OnDirectJoin;

      _addrField.value = defaultAddress;
      _portField.value = defaultPort.ToString();
    }

    private void EnsureDiscoveryService()
    {
      _discovery = LanDiscoveryService.Instance;
      if (_discovery == null)
      {
        var go = new GameObject("LanDiscoveryService");
        _discovery = go.AddComponent<LanDiscoveryService>();
      }
    }

    private void OnEnable()
    {
      InvokeRepeating(nameof(RefreshSessions), 0.5f, 1.0f);
    }

    private void OnDisable()
    {
      CancelInvoke(nameof(RefreshSessions));
    }

    private void OnCreateAndJoin()
    {
      StoreUserDisplayName();
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.SessionInformation,
        new SessionInformationModel
        (
          address: DefaultsSessionInformationModel.address,
          port: DefaultsSessionInformationModel.port
        ));
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer, true);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.UseLanDiscovery, true);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.LoadedFromIntroScene, true);
      SetStatus("Hosting intent set. Load your gameplay scene to start FishNet server.");
      SwitchIngameScene();
    }

    private void OnSearchLan()
    {
      _discovery.StartDiscovery();
      _discovery.ClearDiscovered();
      RefreshSessions();
      SetStatus("Searching for LAN sessions…");
    }

    private void OnJoinSelected()
    {
      if (_selected == null)
      {
        SetStatus("Select a server first.");
        return;
      }

      StoreUserDisplayName();
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.SessionInformation,
        new SessionInformationModel
        (
          address: _selected.Address,
          port: _selected.Port,
          sessionName: _selected.Name
        ));
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer, false);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.UseLanDiscovery, true);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.LoadedFromIntroScene, true);
      SetStatus($"Join intent set: {_selected.Address}:{_selected.Port}");
      SwitchIngameScene();
    }

    private void OnDirectJoin()
    {
      var ip = _addrField.value.Trim();
      if (!ushort.TryParse(_portField.value, out ushort port))
      {
        SetStatus("Invalid port.");
        return;
      }

      StoreUserDisplayName();
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.SessionInformation,
        new SessionInformationModel
        (
          address: ip,
          port: port,
          sessionName: "Direct"
        ));
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer, false);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.UseLanDiscovery, false);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.LoadedFromIntroScene, true);
      SetStatus($"Direct join intent set: {ip}:{port}");
      SwitchIngameScene();
    }

    private void RefreshSessions()
    {
      if (!_discovery.HasPendingUpdate() && _items.Count > 0)
      {
        return;
      }

      var snapshot = _discovery.GetDiscoveredSessions();
      _items.Clear();
      _items.AddRange(snapshot);
      _listView.Rebuild();

      _emptyState.style.display = _items.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
      if (_items.Count == 0)
      {
        _selected = null;
        _btnJoinSelected.SetEnabled(false);
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

    private void StoreUserDisplayName()
    {
      var name = _nameField?.value?.Trim();
      if (!string.IsNullOrWhiteSpace(name))
        Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.UserDisplayName, name);
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
      
      SceneManager.LoadScene(ingameSceneName);
    }
  }
}
