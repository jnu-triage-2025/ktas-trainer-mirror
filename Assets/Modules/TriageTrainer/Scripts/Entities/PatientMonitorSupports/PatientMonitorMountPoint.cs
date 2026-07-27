using System;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using TriageTrainer.ItemDefinitions;
using UnityEngine;
using UnityEngine.Rendering;
using PatientMonitorItem = TriageTrainer.ItemDefinitions.PatientMonitor;

namespace TriageTrainer.Entity.PatientMonitor
{
  /// <summary>
  /// 월드맵에 사전 배치된 환자 모니터 오브젝트에 붙이는 설치 슬롯입니다.
  /// 이 컴포넌트는 네트워크 오브젝트가 아니며, 서버 전역 상태는 StaticObjectDisplaymentService가 관리합니다.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public sealed class PatientMonitorMountPoint : StaticObjectDisplayment
  {
    private sealed class InstallInteract : IInteract, IInteractorConditional, ILocalInteractionFocus
    {
      private readonly PatientMonitorMountPoint _owner;
      public InstallInteract(PatientMonitorMountPoint owner) => _owner = owner;
      public string DisplayText => "환자 모니터 부착";
      public Sprite DisplayIcon => _owner._interactIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public bool CanInteract(Transform interactor) => _owner.CanInstall(interactor);
      public void Interact(Transform interactor) => _owner.Install(interactor);
      public void SetLocalInteractionFocused(bool focused) => _owner.SetPreviewVisible(focused);
    }

    private sealed class RetrieveInteract : IInteract, IInteractorConditional
    {
      private readonly PatientMonitorMountPoint _owner;
      public RetrieveInteract(PatientMonitorMountPoint owner) => _owner = owner;
      public string DisplayText => "환자 모니터 회수";
      public Sprite DisplayIcon => _owner._interactIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;
      public bool CanInteract(Transform interactor) => _owner.CanRetrieve(interactor);
      public void Interact(Transform interactor) => _owner.Retrieve(interactor);
    }

    [Header("Patient monitor mount")]
    [Tooltip("현재 월드맵에 배치된 이 오브젝트의 모델 루트입니다. MountPoint 자신이 아닌 자식 Transform을 지정하세요.")]
    [SerializeField] private GameObject _monitorVisualRoot;
    [Tooltip("선택 중인 로컬 플레이어에게만 보여 줄 고스트 모델입니다. 비우면 monitorVisualRoot를 복제합니다.")]
    [SerializeField] private GameObject _previewPrefab;
    [SerializeField] private Sprite _interactIcon;

    private readonly InstallInteract _installInteract;
    private readonly RetrieveInteract _retrieveInteract;
    private GameObject _preview;
    private Material _previewMaterial;

    protected override string EntityIdPrefix => "patient-monitor-mount";

    public PatientMonitorMountPoint()
    {
      _installInteract = new InstallInteract(this);
      _retrieveInteract = new RetrieveInteract(this);
    }

    public override string DisplayText => IsVisible ? "환자 모니터 회수" : "환자 모니터 부착";
    public override Sprite DisplayIcon => _interactIcon;
    public override IInteract[] Interacts => new IInteract[] { _installInteract, _retrieveInteract };

    protected override void ApplyInitialVisibility()
    {
      // 씬 배치 모델은 서버 상태가 내려오기 전까지 숨긴다. 신규 접속자는 PlayerController가 상태를 동기화한다.
      ShowMonitorVisual(false);
      Hide();
    }

    public override void ApplyShownFromNetwork()
    {
      ShowMonitorVisual(true);
      base.ApplyShownFromNetwork();
    }

    public override void ApplyHiddenFromNetwork()
    {
      SetPreviewVisible(false);
      ShowMonitorVisual(false);
      base.ApplyHiddenFromNetwork();
    }

    public override bool CanInteract(Transform interactor) => CanInstall(interactor) || CanRetrieve(interactor);

    public override void Interact(Transform interactor)
    {
      if (CanInstall(interactor)) Install(interactor);
      else if (CanRetrieve(interactor)) Retrieve(interactor);
    }

    private bool CanInstall(Transform interactor)
    {
      var player = ResolvePlayer(interactor);
      return !IsVisible && player != null &&
             string.Equals(player.HandlingItem?.CurrentIdentifier, PatientMonitorItem.Identifier, StringComparison.Ordinal) &&
             player.CountItemInInventory(PatientMonitorItem.Identifier) > 0;
    }

    private bool CanRetrieve(Transform interactor) => IsVisible && ResolvePlayer(interactor) != null;

    private void Install(Transform interactor)
    {
      var player = ResolvePlayer(interactor);
      if (player != null && CanInstall(interactor))
        RequestApplyShown(player, PatientMonitorItem.Identifier, 1);
    }

    private void Retrieve(Transform interactor)
    {
      var player = ResolvePlayer(interactor);
      if (player != null && CanRetrieve(interactor))
        player.TryClearStaticObjectDisplaymentAndGrantItem(EntityIdentifier, PatientMonitorItem.Identifier);
    }

    private void ShowMonitorVisual(bool visible)
    {
      if (_monitorVisualRoot != null && _monitorVisualRoot.activeSelf != visible)
        _monitorVisualRoot.SetActive(visible);
    }

    private void SetPreviewVisible(bool visible)
    {
      if (!visible || IsVisible)
      {
        if (_preview != null) _preview.SetActive(false);
        return;
      }
      if (_preview == null && !CreatePreview()) return;
      _preview.transform.SetPositionAndRotation(transform.position, transform.rotation);
      _preview.SetActive(true);
    }

    private bool CreatePreview()
    {
      var source = _previewPrefab != null ? _previewPrefab : _monitorVisualRoot;
      if (source == null) return false;
      _preview = Instantiate(source, transform.position, transform.rotation);
      _preview.name = "PatientMonitorMountPreview";
      foreach (var behaviour in _preview.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
      foreach (var collider in _preview.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
      foreach (var body in _preview.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
      var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
      if (shader == null) { Destroy(_preview); _preview = null; return false; }
      _previewMaterial = new Material(shader) { name = "PatientMonitorMountPreviewMaterial" };
      var tint = new Color(0.25f, 0.9f, 1f, 0.38f);
      if (_previewMaterial.HasProperty("_BaseColor")) _previewMaterial.SetColor("_BaseColor", tint);
      if (_previewMaterial.HasProperty("_Color")) _previewMaterial.SetColor("_Color", tint);
      _previewMaterial.SetOverrideTag("RenderType", "Transparent");
      _previewMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
      _previewMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
      _previewMaterial.SetInt("_ZWrite", 0);
      _previewMaterial.renderQueue = (int)RenderQueue.Transparent;
      foreach (var renderer in _preview.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = _previewMaterial;
      return true;
    }

    protected override void OnDestroy()
    {
      if (_preview != null) Destroy(_preview);
      if (_previewMaterial != null) Destroy(_previewMaterial);
      base.OnDestroy();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
      base.OnValidate();
      if (_monitorVisualRoot == gameObject)
        Debug.LogError("[PatientMonitorMountPoint] monitorVisualRoot에는 MountPoint 자신이 아닌 자식 모델을 지정해야 합니다.", this);
    }
#endif
  }
}
