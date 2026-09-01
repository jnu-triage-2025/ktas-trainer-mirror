using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Datapack;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// DatapackSelectionUI 의 데이터 전용 컨트롤러이다. 레이아웃과 시각 스타일은
  /// DatapackSelectionUI.uxml/.uss 에 있으며, IntroScene UIDocument 워크플로를 따른다.
  /// </summary>
  public sealed class DatapackSelectionUIController : UIDocumentControllerABC
  {
    private readonly List<DatapackFileInfo> _available = new();
    private readonly List<DatapackFileInfo> _active = new();
    private ListView _availableView;
    private ListView _activeView;
    private Label _status;
    private System.Action _onDone;

    public void Initialize(VisualTreeAsset visualTreeAsset, PanelSettings panelSettings, System.Action onDone)
    {
      _onDone = onDone;
      var document = gameObject.AddComponent<UIDocument>();
      document.panelSettings = panelSettings;
      document.visualTreeAsset = visualTreeAsset;
      document.sortingOrder = 100;
      SetDocumentVisible(document, true);

      var root = document.rootVisualElement;
      _availableView = root.Q<ListView>("availableList");
      _activeView = root.Q<ListView>("activeList");
      _status = root.Q<Label>("statusLabel");
      var openFolder = root.Q<Button>("openFolderButton");
      var done = root.Q<Button>("doneButton");

      if (_availableView == null || _activeView == null || openFolder == null || done == null)
      {
        Debug.LogError("[DatapackSelectionUI] DatapackSelectionUI.uxml is missing required elements.", this);
        return;
      }

      _availableView.makeItem = () => new VisualElement();
      _availableView.bindItem = (element, index) => BindRow(element, index, active: false);
      _availableView.itemsSource = _available;
      _availableView.selectionType = SelectionType.None;

      _activeView.makeItem = () => new VisualElement();
      _activeView.bindItem = (element, index) => BindRow(element, index, active: true);
      _activeView.itemsSource = _active;
      _activeView.selectionType = SelectionType.None;

      done.clicked += Finish;
      openFolder.clicked += MultiplayerInfrastructure.Logging.GameLogService.OpenDatapackFolder;
      Refresh();
    }

    private void BindRow(VisualElement element, int index, bool active)
    {
      element.Clear();
      element.AddToClassList("datapack-row");
      var source = active ? _active : _available;
      if (index < 0 || index >= source.Count)
        return;

      var pack = source[index];
      var display = pack.IsValid
        ? pack.DisplayName
        : $"{pack.DisplayName}\n올바르지 않은 파일: {pack.Error}";
      var text = new Label(display) { name = "rowText" };
      text.AddToClassList("datapack-row__text");
      if (!pack.IsValid)
        text.AddToClassList("datapack-row__text--invalid");
      element.Add(text);

      if (!active && pack.IsValid)
      {
        var activate = CreateButton("활성화", "datapack-row__button datapack-row__button--activate", () => Activate(pack));
        element.Add(activate);
        return;
      }

      if (!active)
        return;

      element.Add(CreateButton("▲", "datapack-row__button", () => Move(index, -1)));
      element.Add(CreateButton("▼", "datapack-row__button", () => Move(index, 1)));
      element.Add(CreateButton("비활성화", "datapack-row__button", () => Deactivate(pack)));
    }

    private static Button CreateButton(string text, string classes, System.Action callback)
    {
      var button = new Button(callback) { text = text };
      foreach (var className in classes.Split(' '))
        if (!string.IsNullOrWhiteSpace(className))
          button.AddToClassList(className);
      return button;
    }

    private void Refresh()
    {
      DatapackRuntimeService.EnsureRuntimeDatapackFolder();
      _available.Clear();
      _active.Clear();

      var packs = DatapackRuntimeService.ScanDatapacks();
      var selected = Registry.Registry.Get<List<string>>(RegistryType.RuntimeState, RegistryGlobalKeys.SelectedDatapackIds)
        ?? new List<string>(SessionConfigurationService.DatapackIds);

      foreach (var pack in packs)
      {
        if (pack.IsValid && selected.Contains(pack.PackId))
          _active.Add(pack);
        else
          _available.Add(pack);
      }

      _active.Sort((a, b) => selected.IndexOf(a.PackId).CompareTo(selected.IndexOf(b.PackId)));
      _availableView.Rebuild();
      _activeView.Rebuild();

      if (_status != null)
        _status.text = packs.Count == 0 ? "데이터 팩이 없습니다." : string.Empty;
    }

    private void Activate(DatapackFileInfo pack)
    {
      _active.Insert(0, pack);
      _available.Remove(pack);
      RebuildViews();
    }

    private void Deactivate(DatapackFileInfo pack)
    {
      _active.Remove(pack);
      _available.Add(pack);
      RebuildViews();
    }

    private void Move(int index, int delta)
    {
      int target = index + delta;
      if (target < 0 || target >= _active.Count)
        return;
      (_active[index], _active[target]) = (_active[target], _active[index]);
      RebuildViews();
    }

    private void RebuildViews()
    {
      _availableView.Rebuild();
      _activeView.Rebuild();
    }

    private void Finish()
    {
      Registry.Registry.Register(
        RegistryType.RuntimeState,
        RegistryGlobalKeys.SelectedDatapackIds,
        _active.Select(pack => pack.PackId).ToList());
      _onDone?.Invoke();
      Destroy(gameObject);
    }
  }
}
