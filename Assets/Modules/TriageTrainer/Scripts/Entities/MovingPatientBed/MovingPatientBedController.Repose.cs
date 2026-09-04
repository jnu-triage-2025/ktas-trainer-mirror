using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자침대의 "누운 대상(repose) 결합" 을 <b>네트워크 권위적(서버) + 식별자 기반</b>으로 다루는 부분.
  ///
  /// 설계(정공법):
  ///  - 결합 관계의 단일 진실원본(authority)은 서버다. 서버가 <see cref="_reposedTargetIdentifier"/>(SyncVar)에
  ///    "누운 환자의 엔티티 식별자" 를 기록하면, 모든 피어(서버+클라)가 OnChange 에서 그 식별자로 환자를 레지스트리에서
  ///    찾아 로컬 결합(_reposedTargetComponent / patient.SetCurrentBed)을 재구성한다.
  ///  - 결합 상태가 "식별자(string)" 로 표현되므로 직렬화 가능하다. 프리팹에 <see cref="_initialReposedTargetIdentifier"/>
  ///    를 사전 지정해 두면(=결합 사전 설정), 스폰 시 서버가 OnStartServer 에서 이를 SyncVar 에 적용 → 즉시 결합되고
  ///    모든 피어로 복제된다. 별도의 attach 이벤트 없이도 "사전 결합된 채로 스폰" 이 성립한다.
  ///  - 스폰 시점에 환자가 아직 레지스트리에 등록되지 않았을 수 있으므로(비동기 OnStartClient 등록), 식별자 해석은
  ///    지연 재시도한다(<see cref="ResolveDesiredReposeLinkIfPending"/> 를 매 프레임 호출).
  /// </summary>
  public partial class MovingPatientBedController
  {
    private const float ReposeInteractionDistance = 3f;
    [Header("Repose (Network)")]
    [Tooltip("이 침대에 사전 결합(누워있게)할 환자의 엔티티 식별자. 비우면 결합 없이 스폰된다. " +
             "예) patient_a. 스폰 시 서버가 이 값을 적용하고 모든 피어로 복제한다.")]
    [SerializeField] private string _initialReposedTargetIdentifier;

    /// <summary>현재 이 침대에 누운 대상의 엔티티 식별자(권위값). 비어 있으면 결합 없음. 서버만 기록한다.</summary>
    private readonly SyncVar<string> _reposedTargetIdentifier = new SyncVar<string>(string.Empty);

    /// <summary>
    /// 런타임 엔티티 식별자(서버 권위 + 전 피어 복제). 프리셋 스폰 시 서버가 SetIdentifier 로 설정하면
    /// 스폰 페이로드로 전 피어에 동기화된다. 비어 있으면 프리팹 기본 타입 식별자(_entityTypeIdentifier)를 사용한다.
    /// </summary>
    private readonly SyncVar<string> _runtimeIdentifierSync = new SyncVar<string>(string.Empty);

    // OnChange 로 전달받았으나 아직 환자를 레지스트리에서 찾지 못해 적용을 보류 중인 식별자.
    private string _pendingReposeIdentifier;
    private bool _hasPendingReposeResolution;

    public string ReposedTargetIdentifier => _reposedTargetIdentifier.Value;

    /// <summary>
    /// FishNet 이 이 인스턴스를 네트워크로 초기화했는지(= 스폰이 끝났는지). 스폰 전에는 false 다.
    /// </summary>
    private bool IsFishNetNetworkInitialized =>
      NetworkObject != null
      && (NetworkObject.IsServerInitialized || NetworkObject.IsClientInitialized);

    /// <summary>
    /// 서버 권위 SyncVar 에 값을 기록해도 되는 컨텍스트인지.
    ///
    /// <para>
    /// 프리셋 스폰은 <c>ServerManager.Spawn</c> <b>이전</b>에 식별자를 주입한다. 그 시점의 인스턴스는
    /// 아직 NetworkManager 를 갖지 않아 <c>IsServerStarted</c> 가 false 이므로, 서버 여부만으로
    /// 게이트하면 값이 SyncVar 에 기록되지 않는다. FishNet 은 네트워크 초기화 전 쓰기를 초깃값으로
    /// 받아 스폰 페이로드에 실어 보내므로 이 구간의 쓰기를 허용해야 한다.
    /// </para>
    /// </summary>
    private bool CanWriteAuthoritativeSyncVar =>
      !IsFishNetNetworkInitialized
      || (NetworkObject != null && NetworkObject.NetworkManager != null && IsServerStarted);

    private string EffectiveBedIdentifier =>
      !string.IsNullOrWhiteSpace(_runtimeIdentifierSync.Value) ? _runtimeIdentifierSync.Value
      : !string.IsNullOrWhiteSpace(_entityRuntimeIdentifier) ? _entityRuntimeIdentifier
      : _entityTypeIdentifier;

    public override void OnStartServer()
    {
      base.OnStartServer();
      OnIntravenousAttachmentStartServer();

      // 사전 결합 지정이 있으면 권위값으로 적용한다(스폰 즉시 결합 → 전 피어 복제).
      if (!string.IsNullOrWhiteSpace(_initialReposedTargetIdentifier))
      {
        _reposedTargetIdentifier.Value = _initialReposedTargetIdentifier.Trim();
      }
    }

    public override void OnStartClient()
    {
      base.OnStartClient();

      _runtimeIdentifierSync.OnChange += OnBedRuntimeIdentifierChanged;
      _reposedTargetIdentifier.OnChange += OnReposedTargetIdentifierChanged;
      _positioningPointIdentifierSync.OnChange += OnPositioningPointIdentifierChanged;
      OnIntravenousAttachmentStartClient();

      // 스폰 페이로드로 동기화된 식별자로 모든 피어에서 등록한다.
      RegisterBedEntity();

      // 스폰 패킷에 이미 담겨온 초기 SyncVar 값(또는 호스트의 서버측 값)을 즉시 반영 시도한다.
      RequestReposeResolution(_reposedTargetIdentifier.Value);
      RequestPositioningPointResolution(_positioningPointIdentifierSync.Value);
    }

    public override void OnStopClient()
    {
      _runtimeIdentifierSync.OnChange -= OnBedRuntimeIdentifierChanged;
      _reposedTargetIdentifier.OnChange -= OnReposedTargetIdentifierChanged;
      _positioningPointIdentifierSync.OnChange -= OnPositioningPointIdentifierChanged;
      OnIntravenousAttachmentStopClient();

      // 침대가 디스폰될 때 결합되어 있던 환자의 로컬 참조(_currentBed)를 정리해 댕글링을 방지한다.
      ClearReposeLinkLocal();
      _pendingReposeIdentifier = null;
      _hasPendingReposeResolution = false;
      _pendingPositioningPointIdentifier = null;
      _latchedPositioningPoint = null;

      UnregisterBedEntity();

      base.OnStopClient();
    }

    private void OnBedRuntimeIdentifierChanged(string previous, string next, bool asServer)
    {
      if (string.IsNullOrWhiteSpace(next))
        return;

      _entityRuntimeIdentifier = next;
      RegisterBedEntity();
    }

    /// <summary>동기화된 식별자로 침대를 레지스트리에 등록한다(모든 피어). 중복/갱신 안전.</summary>
    private void RegisterBedEntity()
    {
      string id = EffectiveBedIdentifier;
      if (string.IsNullOrWhiteSpace(id))
        return;

      _entityRuntimeIdentifier = id;

      try
      {
        Registry.RegisterEntity(
          id,
          EntityType.MovingPatientBed,
          gameObject,
          displayName: GetDisplayText(),
          ownerUserIdentifier: null,
          clientId: null,
          isNetworked: true);
      }
      catch (System.Exception ex)
      {
        Debug.LogWarning($"[MovingPatientBed] Failed to register entity '{id}': {ex.Message}");
      }
    }

    private void UnregisterBedEntity()
    {
      if (!string.IsNullOrWhiteSpace(_entityRuntimeIdentifier))
        Registry.UnregisterEntity(_entityRuntimeIdentifier);
    }

    private void OnReposedTargetIdentifierChanged(string previous, string next, bool asServer)
    {
      RequestReposeResolution(next);
    }

    /// <summary>
    /// 권위 식별자 변경 요청을 받아, 즉시 로컬 결합을 해석·적용한다. 환자를 아직 못 찾으면 지연 재시도 큐에 둔다.
    /// </summary>
    private void RequestReposeResolution(string desiredIdentifier)
    {
      if (string.IsNullOrWhiteSpace(desiredIdentifier))
      {
        // 결합 해제.
        _pendingReposeIdentifier = null;
        _hasPendingReposeResolution = false;
        ClearReposeLinkLocal();
        return;
      }

      _pendingReposeIdentifier = desiredIdentifier.Trim();
      _hasPendingReposeResolution = true;
      ResolveDesiredReposeLinkIfPending();
    }

    /// <summary>
    /// 보류 중인 결합 식별자가 있으면 레지스트리에서 환자를 찾아 로컬 결합을 적용한다.
    /// 매 프레임(Update → SyncReposedTargetTransform) 에서 호출되어, 환자 등록이 늦어도 결국 결합된다.
    /// </summary>
    private void ResolveDesiredReposeLinkIfPending()
    {
      if (!_hasPendingReposeResolution || string.IsNullOrWhiteSpace(_pendingReposeIdentifier))
      {
        return;
      }

      // 이미 동일 대상에 결합되어 있으면 완료 처리.
      if (_reposedTargetComponent is PatientController current
          && string.Equals(current.Identifier, _pendingReposeIdentifier, System.StringComparison.Ordinal))
      {
        _hasPendingReposeResolution = false;
        return;
      }

      if (!Registry.TryGetEntity(_pendingReposeIdentifier, out var descriptor) || descriptor?.GameObject == null)
      {
        // 아직 환자가 등록되지 않음 → 다음 프레임에 재시도(보류 유지).
        return;
      }

      if (!descriptor.GameObject.TryGetComponent(out PatientController patient) || patient == null)
      {
        // 식별자는 있으나 환자 컴포넌트가 아님 → 잘못된 지정. 보류 해제(스팸 방지).
        Debug.LogWarning($"[MovingPatientBed] Repose target '{_pendingReposeIdentifier}' is not a PatientController.");
        _hasPendingReposeResolution = false;
        return;
      }

      ApplyReposeLinkLocal(patient);
      _hasPendingReposeResolution = false;
    }

    /// <summary>로컬(이 피어)에서 환자↔침대 결합 상태를 적용한다. 권위값(SyncVar)에 의해 구동된다.</summary>
    private void ApplyReposeLinkLocal(PatientController patient)
    {
      if (patient == null)
      {
        return;
      }

      // 기존에 다른 대상이 누워 있었다면 먼저 해제.
      if (_reposedTargetComponent != null && !ReferenceEquals(_reposedTargetComponent, patient))
      {
        ClearReposeLinkLocal();
      }

      if (PatientAttachPoints.Count > 0)
      {
        TryOccupyNextPatientAttachPoint(patient, out _);
      }

      _reposedTargetComponent = patient;
      SnapReposedTargetToAnchor(patient);
      ((IReposable)patient).OnMovingPatientBedAttachedEnter();
      patient.SetCurrentBed(this);
      RefreshDisplayName();
    }

    /// <summary>로컬(이 피어)에서 환자↔침대 결합 상태를 해제한다.</summary>
    private void ClearReposeLinkLocal()
    {
      var previous = _reposedTargetComponent;
      _reposedTargetComponent = null;

      if (previous == null)
      {
        return;
      }

      ReleasePatientAttachPoint(previous);

      if (previous is IReposable reposable)
      {
        reposable.OnMovingPatientBedAttachedExit();
      }

      if (previous is PatientController patient)
      {
        patient.SetCurrentBed(null);
      }

      RefreshDisplayName();
    }

    // ── 서버 권위 진입점 ───────────────────────────────────────────────────────

    /// <summary>
    /// 서버에서 결합 권위값을 설정한다. 식별자가 비어 있으면 결합 해제. 모든 피어로 복제된다.
    /// 비서버 컨텍스트에서 호출되면 ServerRpc 로 위임한다.
    /// </summary>
    public void SetReposedTargetByIdentifier(string patientIdentifier)
    {
      string normalized = string.IsNullOrWhiteSpace(patientIdentifier)
        ? string.Empty
        : patientIdentifier.Trim();

      // 스폰 전에는 ServerRpc 를 보낼 수 없다. 이 구간의 쓰기는 초깃값이 되어 스폰 페이로드로
      // 복제되므로, 사전 결합(프리셋의 부모 링크 등)도 원격 피어에서 그대로 재구성된다.
      if (CanWriteAuthoritativeSyncVar)
      {
        _reposedTargetIdentifier.Value = normalized;
        return;
      }

      CmdSetReposedTarget(normalized);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdSetReposedTarget(
      string patientIdentifier,
      NetworkConnection sender = null)
    {
      if (!TryValidateReposeRequest(sender, patientIdentifier, out string normalizedIdentifier))
        return;
      _reposedTargetIdentifier.Value = normalizedIdentifier;
    }

    private bool TryValidateReposeRequest(
      NetworkConnection sender,
      string patientIdentifier,
      out string normalizedIdentifier)
    {
      normalizedIdentifier = string.IsNullOrWhiteSpace(patientIdentifier)
        ? string.Empty
        : patientIdentifier.Trim();
      if (sender == null || !sender.IsValid
          || !Registry.TryGetEntityByClientId(sender.ClientId, out var playerDescriptor)
          || playerDescriptor?.GameObject == null)
        return false;

      var player = playerDescriptor.GameObject.GetComponent<MultiplayerInfrastructure.Player.PlayerController>()
                   ?? playerDescriptor.GameObject.GetComponentInChildren<MultiplayerInfrastructure.Player.PlayerController>(true);
      if (player == null
          || player.Owner == null
          || !player.Owner.IsValid
          || player.Owner.ClientId != sender.ClientId
          || (player.transform.position - transform.position).sqrMagnitude
          > ReposeInteractionDistance * ReposeInteractionDistance)
        return false;

      if (string.IsNullOrEmpty(normalizedIdentifier))
        return !string.IsNullOrWhiteSpace(_reposedTargetIdentifier.Value);

      if (!_enablePatientRepose
          || (!string.IsNullOrWhiteSpace(_reposedTargetIdentifier.Value)
              && !string.Equals(_reposedTargetIdentifier.Value, normalizedIdentifier,
                System.StringComparison.Ordinal))
          || !Registry.TryGetEntity(normalizedIdentifier, out var patientDescriptor)
          || patientDescriptor?.GameObject == null)
        return false;

      var patient = patientDescriptor.GameObject.GetComponent<PatientController>()
                    ?? patientDescriptor.GameObject.GetComponentInChildren<PatientController>(true);
      return patient != null
             && (patient.CurrentBed == null || ReferenceEquals(patient.CurrentBed, this))
             && (player.transform.position - patient.transform.position).sqrMagnitude
             <= ReposeInteractionDistance * ReposeInteractionDistance;
    }

    /// <summary>
    /// 엔티티 프리셋 스폰 시 부모(루트) 식별자를 전달받는다(IEntityPresetParentLinkReceiver).
    /// 환자(부모) 위에 이 침대(하위)가 결합되는 의미이므로, 부모 환자 식별자를 결합 권위값으로 설정한다.
    /// 프리셋 스폰은 서버에서 수행되므로 보통 즉시 권위값이 설정되고 전 피어로 복제된다.
    /// </summary>
    public void ApplyParentEntityIdentifier(string parentEntityIdentifier)
    {
      SetReposedTargetByIdentifier(parentEntityIdentifier);
    }
  }
}
