using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Entity
{
  [DisallowMultipleComponent]
  public class Npc : Interactable, ISpawnedEntityIdentifierReceiver, IOverheadPresentationAnchorProvider
  {
    [Header("Npc")]
    [SerializeField] private NPCBaseModelSO _npcBaseModel;
    [SerializeField] private string _identifier;
    [Tooltip("카메라와 이 거리(m)보다 멀어지면 머리 위 이름표를 숨긴다. 0 이하이면 거리 제한 없이 항상 표시한다.")]
    [SerializeField, Min(0f)] private float _overheadNameMaxVisibleDistance = 12f;

    // 시나리오 시작·아이템 제출·신호 인터렉션은 시나리오 데이터(interactions)와 상시 카탈로그가 정의하고
    // 인터렉션 레지스트리가 이 NPC 식별자로 핸들러를 만든다. 프리팹에는 인터렉션 정의를 두지 않는다.

    [Header("Custom Interacts")]

    public string Identifier => _identifier;
    public override string PresentationEntityIdentifier => _identifier;
    public Transform OverheadPresentationAnchor
    {
      get
      {
        // 프리팹이 이름표 부착점을 지정했다면 그 위치를 그대로 쓴다(작업자가 에디터에서 눈으로 맞춘 높이).
        var attachPoint = ResolveNameTagAttachPoint();
        if (attachPoint != null)
          return attachPoint.transform;

        EnsureScenarioOverheadNameAnchor();
        _scenarioOverheadNameAnchor.localPosition = new Vector3(0f, GetOverheadNameHeight(), 0f);
        return _scenarioOverheadNameAnchor;
      }
    }

    /// <summary>
    /// 지금 라벨이 붙어 있는 앵커를 돌려준다. 해제 경로가 폴백 앵커를 새로 만들지 않도록,
    /// 부착점도 폴백 앵커도 없으면 null 을 돌려준다.
    /// </summary>
    private Transform CurrentOverheadNameAnchor
    {
      get
      {
        var attachPoint = ResolveNameTagAttachPoint();
        return attachPoint != null ? attachPoint.transform : _scenarioOverheadNameAnchor;
      }
    }

    private readonly List<IInteract> _resolvedInteracts = new List<IInteract>();
    private bool _interactsDirty = true;

    private bool _baseModelApplied;
    private string _registeredIdentifier;
    private Transform _scenarioOverheadNameAnchor;
    private NameTagDisplayAttachPoint _nameTagAttachPoint;
    private bool _scenarioOverheadNameVisible;

    private void Awake()
    {
      EnsureBaseModelApplied();
      MarkInteractsDirty();

      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = gameObject.name;

      RegisterToRegistry();
    }

    private void OnEnable()
    {
      EnsureBaseModelApplied();
      MarkInteractsDirty();
      InteractionRegistry.Changed += MarkInteractsDirty;
      RegisterToRegistry();
      if (_scenarioOverheadNameVisible)
        ConfigureScenarioOverheadNameLabel(gameObject.name);
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
      DestroyScenarioOverheadNameLabel();
    }

    private void OnDisable()
    {
      InteractionRegistry.Changed -= MarkInteractsDirty;
      UnregisterFromRegistry();
      DestroyScenarioOverheadNameLabel();
    }

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      _registeredIdentifier = _identifier;
      Registry.Registry.Register(RegistryType.Npc, _registeredIdentifier, gameObject);
      Registry.Registry.RegisterEntity(_registeredIdentifier, EntityType.Npc, gameObject, displayName: gameObject.name);
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      if (Registry.Registry.Get<GameObject>(RegistryType.Npc, _registeredIdentifier) == gameObject)
        Registry.Registry.Unregister(RegistryType.Npc, _registeredIdentifier);

      if (Registry.Registry.TryGetEntity(_registeredIdentifier, out var descriptor)
          && descriptor?.GameObject == gameObject)
        Registry.Registry.UnregisterEntity(_registeredIdentifier);
      _registeredIdentifier = null;
    }

    public override void Interact(Transform interactor)
    {
      var interacts = Interacts;
      if (interacts == null || interacts.Length == 0)
      {
        Debug.LogWarning($"[Npc] '{name}' has no interact options.", this);
        return;
      }

      interacts[0].Interact(interactor);
    }

    public override IInteract[] Interacts
    {
      get
      {
        if (_interactsDirty)
          RebuildInteracts();

        return _resolvedInteracts.ToArray();
      }
    }

    private void RebuildInteracts()
    {
      _resolvedInteracts.Clear();

      // 레지스트리가 이 NPC 식별자로 가진 정의(시나리오 데이터의 interactions, 상시 카탈로그)의 핸들러.
      InteractionRegistry.CollectInteractsForEntity(_identifier, _resolvedInteracts);


      _interactsDirty = false;
    }

    /// <summary>
    /// EntityPresetSpawn이 요청한 런타임 식별자를 NPC/Entity 레지스트리에 동일하게 적용한다.
    /// Instantiate의 Awake에서 프리팹 식별자로 먼저 등록된 경우에도 안전하게 재등록한다.
    /// </summary>
    public void ApplySpawnedEntityIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      UnregisterFromRegistry();
      _identifier = identifier.Trim();
      RegisterToRegistry();
      MarkInteractsDirty();
    }

    /// <summary>시나리오 최상위 actingNpcs 정의를 이 NPC 인스턴스에 적용한다.</summary>
    public void ConfigureScenarioActingNpc(ScenarioActingNpcDefinition actingNpc)
    {
      if (actingNpc == null)
        return;

      if (!string.IsNullOrWhiteSpace(actingNpc.DisplayName))
        gameObject.name = actingNpc.DisplayName;

      SetScenarioDisplay(actingNpc.DisplayName, actingNpc.ShowOverheadName);

      if (!string.IsNullOrWhiteSpace(actingNpc.Identifier))
        ApplySpawnedEntityIdentifier(actingNpc.Identifier);

      // actingNpc 의 인터렉션 정의는 로더가 그래프 최상위 interactions 로 옮겨 레지스트리가 적용한다.
      MarkInteractsDirty();
    }

    // 트리아지 오버헤드 라벨(EntityOverheadLabelElement)과 같은 UI Toolkit 라벨을 이름표로 재사용한다.
    // 같은 엘리먼트를 쓰므로 텍스트 크기/외곽선/반투명 배경이 트리아지 표기와 동일하게 렌더링되고,
    // 색상 사각형(swatch)만 숨겨 텍스트만 표시한다.
    private void ConfigureScenarioOverheadNameLabel(string displayName)
    {
      if (string.IsNullOrWhiteSpace(displayName))
      {
        ResolveOverheadLabelUI()?.RemoveLabel(CurrentOverheadNameAnchor, "npc-name");
        return;
      }

      var overheadAnchor = OverheadPresentationAnchor;
      // 이름표는 멀리서 화면을 어지럽히지 않도록 거리 제한을 둔다.
      // 퀘스트 마크는 길 안내 용도이므로 같은 앵커에 있어도 이 제한을 공유하지 않는다.
      ResolveOverheadLabelUI()?.SetLabel(
        overheadAnchor,
        "npc-name",
        0,
        new EntityOverheadLabelUIController.LabelContent(displayName.Trim(), Color.white),
        _overheadNameMaxVisibleDistance);
    }

    // 프리팹에 배치된 이름표 부착점. NPC 모델이 런타임에 교체되면 함께 파괴될 수 있으므로 없을 때 다시 찾는다.
    private NameTagDisplayAttachPoint ResolveNameTagAttachPoint()
    {
      if (_nameTagAttachPoint == null)
        _nameTagAttachPoint = GetComponentInChildren<NameTagDisplayAttachPoint>(true);

      return _nameTagAttachPoint;
    }

    // 부착점이 없을 때만 쓰는 폴백 앵커. UI 컨트롤러가 이 위치를 화면에 투영해 라벨을 배치한다.
    private void EnsureScenarioOverheadNameAnchor()
    {
      if (_scenarioOverheadNameAnchor != null)
        return;

      var anchorObject = new GameObject("Scenario Overhead Name Anchor");
      anchorObject.transform.SetParent(transform, false);
      _scenarioOverheadNameAnchor = anchorObject.transform;
    }

    // ActiveInstance 가 없으면 씬 내 컴포넌트를 직접 탐색해 폴백으로 사용한다(PatientController 와 동일한 방식).
    // (씬에 EntityOverheadLabelUIController 가 배치되지 않은 경우 경고를 출력한다.)
    private static EntityOverheadLabelUIController ResolveOverheadLabelUI()
      => EntityOverheadLabelUIController.Resolve();

    /// <summary>시나리오 노드가 NPC의 표시명과 머리 위 이름표를 런타임에 갱신한다.</summary>
    public void SetScenarioDisplay(string displayName, bool? showOverheadName)
    {
      bool nameChanged = !string.IsNullOrWhiteSpace(displayName);
      if (nameChanged)
      {
        gameObject.name = displayName.Trim();
        Registry.Registry.UpdateEntityDisplayName(
          string.IsNullOrWhiteSpace(_registeredIdentifier) ? _identifier : _registeredIdentifier,
          gameObject.name);
      }

      if (showOverheadName.HasValue)
      {
        _scenarioOverheadNameVisible = showOverheadName.Value;
        ConfigureScenarioOverheadNameLabel(_scenarioOverheadNameVisible ? gameObject.name : null);
      }
      else if (nameChanged && _scenarioOverheadNameVisible)
        ConfigureScenarioOverheadNameLabel(gameObject.name);
    }

    private float GetOverheadNameHeight()
    {
      var renderers = GetComponentsInChildren<Renderer>(true);
      bool hasBounds = false;
      float maxY = 0f;
      for (int i = 0; i < renderers.Length; i++)
      {
        var renderer = renderers[i];
        if (renderer == null)
          continue;

        if (!hasBounds || renderer.bounds.max.y > maxY)
        {
          maxY = renderer.bounds.max.y;
          hasBounds = true;
        }
      }

      if (!hasBounds)
        return 2f;

      // 라벨과 머리 사이 여유 간격은 UI 컨트롤러의 worldHeightOffset 이 담당하므로 머리 상단 높이만 반환한다.
      return transform.InverseTransformPoint(new Vector3(transform.position.x, maxY, transform.position.z)).y;
    }

    private void DestroyScenarioOverheadNameLabel()
    {
      // 파괴된 앵커는 컨트롤러 LateUpdate 의 stale 정리가 제거하지만, 명시적으로 먼저 해제한다.
      EntityOverheadLabelUIController.ActiveInstance?.RemoveLabels(CurrentOverheadNameAnchor);

      // 프리팹에 배치된 부착점은 그대로 두고, 런타임에 만든 폴백 앵커만 정리한다.
      if (_scenarioOverheadNameAnchor != null)
      {
        Destroy(_scenarioOverheadNameAnchor.gameObject);
        _scenarioOverheadNameAnchor = null;
      }
    }

    private void MarkInteractsDirty()
    {
      _interactsDirty = true;
    }

    [ContextMenu("NPC/Apply Base Model Now")]
    private void ApplyBaseModelFromContextMenu()
    {
      _baseModelApplied = false;
      EnsureBaseModelApplied(logResult: true);
    }

    private void EnsureBaseModelApplied(bool logResult = false)
    {
      if (_baseModelApplied)
        return;

      bool applied = ApplyBaseModel();
      _baseModelApplied = true;

      if (!logResult)
        return;

      if (_npcBaseModel == null)
      {
        Debug.LogWarning($"[Npc] '{name}' has no NPCBaseModelSO assigned.", this);
        return;
      }

      Debug.Log($"[Npc] '{name}' base model apply {(applied ? "succeeded" : "completed with fallback values")}: identifier='{_identifier}'", this);
    }

    private bool ApplyBaseModel()
    {
      if (_npcBaseModel == null)
        return false;

      bool changed = false;

      if (!string.IsNullOrWhiteSpace(_npcBaseModel.identifier) && _identifier != _npcBaseModel.identifier)
      {
        _identifier = _npcBaseModel.identifier;
        changed = true;
      }

      if (changed)
        MarkInteractsDirty();

      return changed;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      _baseModelApplied = false;
      ApplyBaseModel();
      MarkInteractsDirty();

      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_identifier, gameObject, "npc");
    }
#endif
  }
}
