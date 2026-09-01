using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 트리거 존 진입 시 자동으로 시나리오를 시작합니다.
  /// 신호 계열 존(그래프 미지정)은 재생 중인 시나리오가 있을 때만 감지/발신합니다
  /// (<see cref="ScenarioController.HasActiveScenario"/> 기준).
  /// </summary>
  public class ScenarioTriggerZone : MonoBehaviour
  {
    #region Serialized Fields

    [Header("Identity")]
    [SerializeField] private string _identifier;

    [Header("Scenario Settings")]
    [SerializeField] private TextAsset _scenarioJson;
    [SerializeField] private string _startNodeIdentifier;

    [Header("Trigger Settings")]
    [SerializeField] private bool _triggerOnce = true;
    [SerializeField] private float _triggerCooldown = 1f;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _disableAfterTrigger = false;

    [Header("Interaction Signals (옵션)")]
    [Tooltip("플레이어가 존에 진입할 때 올릴 시나리오 인터랙션 신호(sig.* 게이팅용). 예: enter_triage_zone, arrive_triagearea. 비워 두면 신호를 올리지 않는다(기존 동작). 재생 중인 시나리오가 없으면 발신되지 않는다.")]
    [SerializeField] private string[] _raiseSignalsOnEnter = Array.Empty<string>();

    [Header("Per-Entity Signal (옵션)")]
    [Tooltip("진입한 '식별된 엔티티'(IScenarioIdentifiedEntity 구현, 예: 환자)마다 대상별 신호를 올릴 때 사용하는 템플릿. " +
             "'{id}' 는 진입 엔티티의 식별자로 치환된다. 예: 'enter_triage_zone_{id}' → enter_triage_zone_patient_b. " +
             "비워 두면 대상별 신호를 올리지 않는다. 이 경로는 _playerTag 필터와 무관하게 동작한다. " +
             "재생 중인 시나리오가 없으면 발신되지 않는다.")]
    [SerializeField] private string _perEntitySignalTemplate = string.Empty;

    [Tooltip("대상별 신호가 이미 발신된 엔티티에는 재발신하지 않는다(distinct 계측용, 기본 true). " +
             "SignalCounter 로 도착 인원 수를 셀 때 중복 발신을 방지한다.")]
    [SerializeField] private bool _perEntityRaiseOncePerEntity = true;

    [Tooltip("Only PlayerController entities may raise the per-entity signal.")]
    [SerializeField] private bool _perEntityPlayersOnly;

    [Header("Debug")]
    [SerializeField] private bool _debugTriggerLogs = false;

    #endregion

    #region Private Fields

    /// <summary>대상별 신호 점유 질의 주기(초).</summary>
    private const float PerEntityPollIntervalSeconds = 0.3f;

    /// <summary>점유 질의 결과 버퍼의 상한. 이 수를 넘는 콜라이더는 한 번의 질의에서 잘린다.</summary>
    private const int PerEntityOverlapBufferLimit = 256;

    /// <summary>신호 대상이 될 수 없다고 판정한 콜라이더를 기억해 두는 개수의 상한.</summary>
    private const int PerEntityIrrelevantColliderLimit = 512;

    private ScenarioGraph _cachedGraph;
    private bool _hasTriggered;
    private float _lastTriggerTime = float.NegativeInfinity;
    private string _registeredIdentifier;

    // 대상별 신호를 이미 발신한 엔티티 식별자(중복 발신 방지).
    private readonly HashSet<string> _perEntityRaised = new(StringComparer.Ordinal);

    // 점유 질의 기준으로 현재 이 존 안에 있다고 판정된 엔티티 식별자.
    // 물리 트리거 경로와 폴링 경로가 같은 진입을 두 번 발신하지 않도록 공유한다.
    private readonly HashSet<string> _perEntityInside = new(StringComparer.Ordinal);
    private readonly HashSet<string> _perEntityInsideScratch = new(StringComparer.Ordinal);

    // 이 존과 겹치지만 어떤 엔티티로도 해석될 수 없는 콜라이더(벽, 바닥, 소품 등).
    // 질의 결과의 대부분을 차지하므로, 계층 탐색을 매 주기 되풀이하지 않도록 기억해 둔다.
    private readonly HashSet<Collider> _perEntityIrrelevantColliders = new();

    private Collider _zoneCollider;
    private Collider[] _perEntityOverlapBuffer = new Collider[32];
    private float _nextPerEntityPollTime;

    /// <summary>
    /// 새 시나리오 실행마다 중복 방지 상태를 되돌리기 위해 존을 추적한다.
    ///
    /// <para>
    /// 추적은 GameObject 의 활성 상태와 무관해야 한다. <see cref="_disableAfterTrigger"/> 로
    /// 스스로를 비활성화한 존을 목록에서 제거하면 <see cref="ResetAllForNewScenarioRun"/> 이
    /// 그 존에 도달할 수 없고, 재활성화하는 코드도 없다. 그 결과 재시작 시 그 존이 담당한
    /// 신호가 다시 올라가지 않아, 그 신호를 기다리는 게이트가 영구히 막힌다.
    /// 따라서 Awake 에서 등록하고 OnDestroy 에서만 제거한다.
    /// </para>
    /// </summary>
    private static readonly List<ScenarioTriggerZone> LiveZones = new();

    /// <summary>
    /// 정적 상태를 초기화한다. 도메인 리로드가 비활성인 환경에서는 정적 목록이 플레이 세션
    /// 사이에 유지되어 파괴된 존 참조가 누적되므로, 세션 시작 시 비운다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
      LiveZones.Clear();
      OnScenarioRequested = null;
    }

    #endregion

    #region Events

    public static event Action<ScenarioGraph, string, int?> OnScenarioRequested;

    #endregion

    #region Properties

    public bool HasTriggered => _hasTriggered;
    public string Identifier => _identifier;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
      CacheScenarioGraph();
      RegisterToRegistry();
      _zoneCollider = GetComponent<Collider>();
      // 활성 상태와 무관하게 추적한다. LiveZones 주석의 설명을 참고한다.
      if (!LiveZones.Contains(this))
        LiveZones.Add(this);
    }

    private void OnEnable()
    {
      RegisterToRegistry();
      if (!LiveZones.Contains(this))
        LiveZones.Add(this);
    }

    private void OnDisable()
    {
      UnregisterFromRegistry();
    }

    private void OnDestroy()
    {
      UnregisterFromRegistry();
      LiveZones.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
      TryRaisePerEntitySignal(other.gameObject);
      TryTrigger(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
      TryRaisePerEntitySignal(other.gameObject);
      TryTrigger(other.gameObject);
    }

    private void Update()
    {
      PollPerEntityOccupancy();
    }

    #endregion

    #region Trigger Logic

    /// <summary>
    /// 현재 활성화된(재생 중인) 시나리오 그래프가 존재하는지 여부.
    /// ScenarioController 가 없는 씬에서는 false 로 간주한다.
    /// 재생 여부 판정은 노드 실행 상태 기반(IsActive)이 아니라 그래프 존재 기반이어야
    /// 클라이언트 표시 모드/즉시 진행 노드 체인 사이에서도 재생 중으로 올바르게 판정된다.
    /// </summary>
    private static bool IsScenarioPlaying =>
      ScenarioController.Instance != null && ScenarioController.Instance.HasActiveScenario;

    /// <summary>
    /// 대상별 점유 질의를 이 피어에서 수행해야 하는지 여부.
    ///
    /// <para>
    /// 질의로 감지하는 대상(환자, 침대)은 서버 권위로 이동하므로 서버의 위치가 기준이다. 또한
    /// 클라이언트가 올린 신호는 그래프가 client-origin 으로 인가한 것만 서버에서 수락되기 때문에,
    /// 모든 피어에서 질의하면 인가받지 못한 도착 신호가 거부되면서 경고와 시스템 메시지만 쌓인다.
    /// 그래서 이 경로는 서버 또는 오프라인 단독 실행에서만 수행한다. 플레이어 도착을 감지하는
    /// 물리 트리거 경로는 종전대로 모든 피어에서 그대로 동작한다.
    /// </para>
    /// </summary>
    private static bool IsPerEntityPollingActive =>
      InstanceFinder.IsServerStarted || InstanceFinder.IsOffline;

    private void TryTrigger(GameObject other)
    {
      if (!other.CompareTag(_playerTag))
      {
        if (_debugTriggerLogs)
          Debug.Log($"[ScenarioTriggerZone] Ignored '{other.name}': tag '{other.tag}' != required '{_playerTag}'.", this);
        return;
      }

      bool hasGraph = _cachedGraph != null;

      // 신호 계열 존(그래프 미지정)은 시나리오가 재생 중일 때만 감지한다.
      // 재생 중이 아니면 올린 신호가 소비되지 않고(다음 시나리오 시작 시 모두 초기화되므로)
      // 진입을 무시한다. 쿨다운/1회 트리거 상태도 소비하지 않는다.
      // 시나리오 시작용 존(그래프 지정)은 재생 중이 아닐 때 유일하게 동작해야 하므로 게이팅 예외다.
      if (!hasGraph && !IsScenarioPlaying)
      {
        if (_debugTriggerLogs)
          Debug.Log("[ScenarioTriggerZone] Ignored trigger: no scenario is playing.", this);
        return;
      }

      if (_triggerOnce && _hasTriggered)
      {
        if (_debugTriggerLogs)
          Debug.Log("[ScenarioTriggerZone] Ignored trigger: triggerOnce is enabled and zone already fired.", this);
        return;
      }

      if (Time.time - _lastTriggerTime < _triggerCooldown)
      {
        if (_debugTriggerLogs)
          Debug.Log($"[ScenarioTriggerZone] Ignored trigger: cooldown active ({Time.time - _lastTriggerTime:F2}s < {_triggerCooldown:F2}s).", this);
        return;
      }

      // 신호 전용 존(시나리오 그래프 미지정)도 허용한다: 그래프가 있으면 시나리오를 시작하고,
      // 없더라도 진입 신호(_raiseSignalsOnEnter)만 올리는 게이트 트리거로 동작할 수 있다.
      bool hasSignals = _raiseSignalsOnEnter != null && _raiseSignalsOnEnter.Length > 0;

      if (!hasGraph && !hasSignals)
      {
        // 대상별 신호 전용 존(_perEntitySignalTemplate 만 설정)은 정상 구성이다.
        // 이 경로(플레이어 태그 필터 통과)에서는 할 일이 없으므로 조용히 반환한다.
        if (!string.IsNullOrWhiteSpace(_perEntitySignalTemplate))
          return;

        Debug.LogError("[ScenarioTriggerZone] Scenario graph is null and no enter-signals configured; nothing to trigger.", this);
        return;
      }

      ExecuteTrigger(other, hasGraph);
    }

    private void ExecuteTrigger(GameObject triggeringObject = null, bool startScenario = true)
    {
      _hasTriggered = true;
      _lastTriggerTime = Time.time;

      // 진입 신호를 먼저 올린다(게이트 통과용). 시나리오 시작 여부와 독립적으로 동작한다.
      RaiseEnterSignals();

      if (startScenario && _cachedGraph != null)
      {
        Debug.Log("[ScenarioTriggerZone] Triggering scenario");

        int? clientId = null;
        if (triggeringObject != null)
        {
          var netObj = triggeringObject.GetComponentInParent<NetworkObject>();
          // 서버 소유(Owner 미지정) 오브젝트는 ClientId 가 -1 이므로 소유자로 취급하지 않는다.
          if (netObj != null && netObj.Owner != null && netObj.Owner.IsValid)
          {
            clientId = (int)netObj.Owner.ClientId;
          }
        }

        OnScenarioRequested?.Invoke(_cachedGraph, _startNodeIdentifier, clientId);
      }

      if (_disableAfterTrigger)
      {
        gameObject.SetActive(false);
      }
    }

    private void RaiseEnterSignals()
    {
      if (_raiseSignalsOnEnter == null)
        return;

      foreach (var signal in _raiseSignalsOnEnter)
      {
        if (string.IsNullOrWhiteSpace(signal))
          continue;

        if (_debugTriggerLogs)
          Debug.Log($"[ScenarioTriggerZone] Raising enter signal '{signal}'.", this);

        ScenarioInteractionSignals.Raise(signal);
      }
    }

    /// <summary>
    /// 진입한 오브젝트가 <see cref="IScenarioIdentifiedEntity"/> 를 구현하면, 대상별 신호 템플릿의
    /// "{id}" 를 그 식별자로 치환해 신호를 올린다(예: 환자별 트리아지 구역 도착).
    /// _playerTag 필터와 무관하게 동작하며, 기본적으로 엔티티당 1회만 발신한다.
    /// 대상별 신호는 재생 중인 시나리오의 게이트/카운터에서만 소비되므로, 재생 중이 아니면 발신하지 않는다.
    /// </summary>
    private void TryRaisePerEntitySignal(GameObject other)
    {
      if (string.IsNullOrWhiteSpace(_perEntitySignalTemplate) || other == null)
        return;

      // 재생 중이 아니면 신호가 소비되지 않으므로 발신하지 않는다.
      // (_perEntityRaised 가 재생 전 진입으로 오염되는 것도 함께 방지된다.)
      if (!IsScenarioPlaying)
        return;

      string id = ResolvePerEntitySignalIdentifier(other);
      if (string.IsNullOrWhiteSpace(id))
      {
        if (_debugTriggerLogs)
          Debug.Log($"[ScenarioTriggerZone] Per-entity signal skipped for '{other.name}': no IScenarioIdentifiedEntity identifier.", this);
        return;
      }

      // 점유 질의가 도는 피어에서는 같은 진입을 두 경로가 중복 발신하지 않도록 진입 사실을
      // 먼저 기록한다. 질의가 돌지 않는 피어에서는 이탈을 판정할 방법이 없으므로, 진입마다
      // 발신하는 기존 동작을 그대로 둔다.
      if (IsPerEntityPollingActive && !_perEntityInside.Add(id))
        return;

      RaisePerEntitySignal(id);
    }

    /// <summary>
    /// 콜라이더가 걸린 오브젝트로부터 대상별 신호에 사용할 엔티티 식별자를 해석한다.
    /// 해석에 실패하거나 필터에 걸리면 null 을 반환한다.
    /// </summary>
    private string ResolvePerEntitySignalIdentifier(GameObject other)
      => ResolvePerEntitySignalIdentifier(other, out _);

    /// <summary>
    /// <inheritdoc cref="ResolvePerEntitySignalIdentifier(GameObject)"/>
    /// </summary>
    /// <param name="hasResolvableSource">
    /// 이 오브젝트의 상위 계층에 신호 대상이 될 수 있는 구성 요소가 하나라도 있으면 true.
    /// 지금 식별자를 얻지 못했더라도, 나중에 얻게 될 수 있는 대상인지를 구분한다.
    /// 환자를 아직 태우지 않은 침대가 대표적인 예다. 침대는 <see cref="IScenarioIdentifiedEntity"/> 를
    /// 구현하지 않으므로 빈 상태에서는 식별자가 없지만, 환자를 태우면 그 환자로 해석된다.
    /// </param>
    private string ResolvePerEntitySignalIdentifier(GameObject other, out bool hasResolvableSource)
    {
      var identified = other.GetComponentInParent<IScenarioIdentifiedEntity>();
      // 환자가 침대에 누운 채 이동하면 보통 침대 collider가 먼저 존에 들어온다.
      // 이때 운반체 ID(bed_b)가 아니라 실제 도착 대상(patient_b)으로 신호를 발행한다.
      var arrivalResolver = other.GetComponentInParent<IScenarioArrivalSignalEntityResolver>();
      hasResolvableSource = identified != null || arrivalResolver != null;

      identified = arrivalResolver?.ResolveArrivalSignalEntity() ?? identified;
      if (_perEntityPlayersOnly && identified is not Player.PlayerController)
        return null;

      return identified?.ScenarioEntityIdentifier;
    }

    /// <summary>해석된 엔티티 식별자로 대상별 신호를 발신한다(중복 발신 정책 포함).</summary>
    private void RaisePerEntitySignal(string id)
    {
      if (_perEntityRaiseOncePerEntity && !_perEntityRaised.Add(id))
        return; // 이미 이 엔티티에 발신함

      string signal = _perEntitySignalTemplate.IndexOf("{id}", StringComparison.Ordinal) >= 0
        ? _perEntitySignalTemplate.Replace("{id}", id)
        : _perEntitySignalTemplate;

      if (_debugTriggerLogs)
        Debug.Log($"[ScenarioTriggerZone] Raising per-entity signal '{signal}' for entity '{id}'.", this);

      ScenarioInteractionSignals.Raise(signal);
    }

    /// <summary>
    /// 물리 트리거 콜백 없이도 대상별 진입을 감지하는 폴링 경로.
    ///
    /// <para>
    /// 이 프로젝트의 환자와 침대, 그리고 이 존은 모두 Rigidbody 없이 transform 을 직접 갱신하여
    /// 이동한다. Unity 는 두 콜라이더 중 어느 쪽에도 Rigidbody 가 없으면 트리거 이벤트를 보내지
    /// 않으므로, 침대에 실려 이동하는 환자는 <see cref="OnTriggerEnter"/> 로 절대 감지되지 않는다.
    /// CharacterController 를 가진 플레이어만 트리거 경로로 감지되기 때문에, 그 경로에만 의존하면
    /// 환자 도착 신호가 영영 발신되지 않는다.
    /// </para>
    ///
    /// <para>
    /// 그래서 존 콜라이더와 같은 영역을 주기적으로 질의하여 안에 있는 엔티티 집합을 직접 구한다.
    /// 트리거 경로와 동일한 식별자 해석을 사용하고 <see cref="_perEntityInside"/> 를 공유하므로,
    /// 두 경로가 같은 진입을 중복해서 발신하지 않는다.
    /// </para>
    /// </summary>
    private void PollPerEntityOccupancy()
    {
      if (string.IsNullOrWhiteSpace(_perEntitySignalTemplate) || !IsPerEntityPollingActive)
        return;

      if (Time.time < _nextPerEntityPollTime)
        return;
      _nextPerEntityPollTime = Time.time + PerEntityPollIntervalSeconds;

      // 재생 중이 아닐 때의 점유 상태는 남겨 두지 않는다. 다음 실행에서 같은 엔티티가 이미
      // 들어와 있는 것으로 오인되면 도착 신호가 발신되지 않는다.
      if (!IsScenarioPlaying)
      {
        _perEntityInside.Clear();
        return;
      }

      _perEntityInsideScratch.Clear();
      int hitCount = OverlapZone();
      for (int i = 0; i < hitCount; i++)
      {
        var hit = _perEntityOverlapBuffer[i];
        if (hit == null || hit.gameObject == gameObject)
          continue;

        // 질의 결과의 대부분은 벽과 바닥, 소품이다. 이들은 신호 대상이 될 수 없다는 판정이
        // 바뀌지 않으므로, 한 번 확인한 뒤에는 계층 탐색을 되풀이하지 않는다.
        if (_perEntityIrrelevantColliders.Contains(hit))
          continue;

        string id = ResolvePerEntitySignalIdentifier(hit.gameObject, out bool hasResolvableSource);
        if (string.IsNullOrWhiteSpace(id))
        {
          // 지금 식별자가 없더라도 나중에 생길 수 있는 대상(예: 환자를 아직 태우지 않은 침대)은
          // 제외 목록에 넣지 않는다. 넣으면 그 침대의 도착을 영영 감지하지 못한다.
          if (!hasResolvableSource && _perEntityIrrelevantColliders.Count < PerEntityIrrelevantColliderLimit)
            _perEntityIrrelevantColliders.Add(hit);
          continue;
        }

        // 한 엔티티가 콜라이더를 여러 개 가질 수 있으므로 식별자 기준으로 한 번만 처리한다.
        if (!_perEntityInsideScratch.Add(id))
          continue;

        // 이전 주기에도 안에 있었다면 새 진입이 아니다.
        if (!_perEntityInside.Contains(id))
          RaisePerEntitySignal(id);
      }

      _perEntityInside.Clear();
      foreach (var id in _perEntityInsideScratch)
        _perEntityInside.Add(id);
    }

    /// <summary>
    /// 존 콜라이더와 같은 영역을 질의하여 겹친 콜라이더를 <see cref="_perEntityOverlapBuffer"/> 에 채운다.
    /// 버퍼가 가득 차면 결과가 잘렸을 수 있으므로 상한까지 버퍼를 늘려 다시 질의한다.
    /// </summary>
    private int OverlapZone()
    {
      if (_zoneCollider == null)
        _zoneCollider = GetComponent<Collider>();

      if (_zoneCollider == null || !_zoneCollider.enabled)
        return 0;

      while (true)
      {
        int count = OverlapZoneOnce();
        if (count < _perEntityOverlapBuffer.Length || _perEntityOverlapBuffer.Length >= PerEntityOverlapBufferLimit)
          return count;

        _perEntityOverlapBuffer = new Collider[Mathf.Min(_perEntityOverlapBuffer.Length * 2, PerEntityOverlapBufferLimit)];
      }
    }

    private int OverlapZoneOnce()
    {
      var zoneTransform = _zoneCollider.transform;
      var scale = zoneTransform.lossyScale;
      var absoluteScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

      switch (_zoneCollider)
      {
        case BoxCollider box:
          return Physics.OverlapBoxNonAlloc(
            zoneTransform.TransformPoint(box.center),
            Vector3.Scale(box.size, absoluteScale) * 0.5f,
            _perEntityOverlapBuffer,
            zoneTransform.rotation,
            ~0,
            QueryTriggerInteraction.Collide);

        case SphereCollider sphere:
          return Physics.OverlapSphereNonAlloc(
            zoneTransform.TransformPoint(sphere.center),
            sphere.radius * Mathf.Max(absoluteScale.x, Mathf.Max(absoluteScale.y, absoluteScale.z)),
            _perEntityOverlapBuffer,
            ~0,
            QueryTriggerInteraction.Collide);

        default:
          // 그 밖의 콜라이더 모양은 월드 AABB 로 근사한다.
          var bounds = _zoneCollider.bounds;
          return Physics.OverlapBoxNonAlloc(
            bounds.center,
            bounds.extents,
            _perEntityOverlapBuffer,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Collide);
      }
    }

    #endregion

    #region Scenario Graph

    private void CacheScenarioGraph()
    {
      if (_scenarioJson == null)
        return;

      try
      {
        _cachedGraph = ScenarioGraphLoader.LoadFromJson(_scenarioJson.text);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[ScenarioTriggerZone] Failed to load scenario: {ex.Message}");
      }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      CacheScenarioGraph();
      if (string.IsNullOrWhiteSpace(_identifier))
        _identifier = global::MultiplayerInfrastructure.Registry.EntityId.Ensure(_identifier, gameObject, "scenario-trigger-zone");
    }
#endif

    private void RegisterToRegistry()
    {
      if (string.IsNullOrWhiteSpace(_identifier))
        return;

      _registeredIdentifier = _identifier;
      Registry.Registry.RegisterEntity(_registeredIdentifier, EntityType.ScenarioTriggerZone, gameObject, displayName: gameObject.name);
    }

    private void UnregisterFromRegistry()
    {
      if (string.IsNullOrWhiteSpace(_registeredIdentifier))
        return;

      Registry.Registry.UnregisterEntity(_registeredIdentifier);
      _registeredIdentifier = null;
    }

    public void SetScenario(TextAsset scenarioJson, string startNodeIdentifier = null)
    {
      _scenarioJson = scenarioJson;
      _startNodeIdentifier = startNodeIdentifier;
      CacheScenarioGraph();
    }

    public void SetScenario(ScenarioGraph graph, string startNodeIdentifier = null)
    {
      _cachedGraph = graph;
      _startNodeIdentifier = startNodeIdentifier;
    }

    #endregion

    #region Public API

    /// <summary>
    /// 새 시나리오 실행을 위해 모든 활성 존의 중복 방지 상태를 되돌린다.
    ///
    /// <para>
    /// 존의 발신 억제 상태는 한 번의 시나리오 실행 안에서만 의미가 있다. 시나리오가 다시
    /// 시작되면 신호 레지스트리는 비워지지만 존의 상태는 그대로 남아, 같은 엔티티나 플레이어가
    /// 다시 진입해도 신호를 올리지 않는다. 그 신호를 기다리는 게이트와 카운터는 영구히 막힌다.
    /// </para>
    /// </summary>
    public static void ResetAllForNewScenarioRun()
    {
      // Awake 는 처음부터 비활성인 GameObject 에서는 호출되지 않으므로, 그런 존은 LiveZones 에
      // 등록되지 않는다. 실행 시작 시 씬을 한 번 훑어 누락된 존을 보충한다. 비활성 오브젝트도
      // 포함해 조회한다.
      var sceneZones = FindObjectsByType<ScenarioTriggerZone>(
        FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int index = 0; index < sceneZones.Length; index++)
      {
        var zone = sceneZones[index];
        if (zone != null && !LiveZones.Contains(zone))
          LiveZones.Add(zone);
      }

      for (int index = LiveZones.Count - 1; index >= 0; index--)
      {
        var zone = LiveZones[index];
        // 파괴된 존은 목록에서 정리한다(UnityEngine.Object 의 == 오버로드를 사용한다).
        if (zone == null)
        {
          LiveZones.RemoveAt(index);
          continue;
        }

        zone.ResetForNewScenarioRun();
      }
    }

    private void ResetForNewScenarioRun()
    {
      _perEntityRaised.Clear();
      // 이전 실행의 점유 상태가 남아 있으면 같은 엔티티가 계속 안에 있는 동안 새 실행에서
      // 진입으로 판정되지 않는다. 실행마다 도착을 다시 발신해야 하므로 함께 비운다.
      _perEntityInside.Clear();
      // 실행 사이에 파괴된 콜라이더가 목록에 남지 않도록 함께 비운다.
      _perEntityIrrelevantColliders.Clear();

      // 시나리오를 시작시키는 존(그래프 지정)은 방금 자신이 띄운 시나리오를 다시 시작시키면
      // 안 되므로 1회 트리거 상태를 유지한다. 신호 전용 존은 실행마다 다시 발신해야 한다.
      if (_cachedGraph != null)
        return;

      _hasTriggered = false;
      _lastTriggerTime = float.NegativeInfinity;

      // _disableAfterTrigger 로 스스로를 끈 신호 전용 존은 다시 켜 주어야 다음 실행에서
      // 진입을 감지할 수 있다. 상태만 초기화하고 GameObject 를 켜지 않으면 존은 영구히
      // 침묵하며, 그 신호를 기다리는 게이트가 막힌다.
      if (_disableAfterTrigger && !gameObject.activeSelf)
        gameObject.SetActive(true);
    }

    [ContextMenu("Scenario Trigger/Reset Trigger")]
    public void ResetTrigger()
    {
      _hasTriggered = false;
      _lastTriggerTime = float.NegativeInfinity;
      _perEntityRaised.Clear();
      _perEntityInside.Clear();
      gameObject.SetActive(true);
    }

    [ContextMenu("Scenario Trigger/Force Trigger")]
    public void ForceTrigger()
    {
      ExecuteTrigger();
    }

    #endregion
  }
}
