using System;
using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using TriageTrainer.ItemDefinitions;
using UnityEngine;

namespace TriageTrainer.Entity.IntravenousLine
{
  public class IntravenousLineConnectionService : MonoBehaviour
  {
    [Serializable]
    private sealed class PendingConnectionContext
    {
      public PlayerController Player;
      public IntravenousLineConnectionPoint StartPoint;
    }

    [Header("Hierarchy")]
    [SerializeField] private Transform _linesRoot;

    [Header("Line Visual")]
    [SerializeField, Min(0.001f)] private float _lineWidth = 0.01f;
    [SerializeField] private Color _lineColor = new Color(0.94f, 0.98f, 1f, 0.18f);

    [Header("Line Shape")]
    [SerializeField, Min(2)] private int _lineSegments = 18;
    [SerializeField, Min(0f)] private float _lineSagAmount = 0.015f;

    [Header("Line Simulation")]
    [SerializeField] private bool _usePhysicsSimulation = true;
    [SerializeField, Min(1)] private int _simulationStepsPerFrame = 2;
    [SerializeField, Min(1)] private int _solverIterations = 8;
    [SerializeField, Range(0f, 1f)] private float _gravityScale = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _velocityDamping = 0.1f;
    [SerializeField, Min(0f)] private float _slackLength = 0.06f;

    [Header("Line Collision")]
    [SerializeField] private bool _collideWithWorld = true;
    [SerializeField, Min(0.0005f)] private float _collisionRadius = 0.006f;
    [SerializeField] private LayerMask _collisionMask = ~0;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Required Items")]
    [SerializeReference] private Item[] _requiredItems = Array.Empty<Item>();
    [SerializeField] private List<string> _requiredItemIdentifiers = new() { IntravenousSet.Identifier };

    private readonly Dictionary<int, PendingConnectionContext> _pendingConnections = new();

    private void Awake()
    {
      EnsureRequiredItemsDefaults();
      EnsureLinesRoot();
    }

    private void Update()
    {
      CleanupInvalidPendingConnections();
    }

    private void OnValidate()
    {
      EnsureRequiredItemsDefaults();
      EnsureLinesRoot();
      _lineSegments = Mathf.Max(2, _lineSegments);
      _lineSagAmount = Mathf.Max(0f, _lineSagAmount);
      _simulationStepsPerFrame = Mathf.Max(1, _simulationStepsPerFrame);
      _solverIterations = Mathf.Max(1, _solverIterations);
      _gravityScale = Mathf.Clamp01(_gravityScale);
      _velocityDamping = Mathf.Clamp01(_velocityDamping);
      _slackLength = Mathf.Max(0f, _slackLength);
      _collisionRadius = Mathf.Max(0.0005f, _collisionRadius);
    }

    public bool HasAnyRequiredItem(PlayerController player)
    {
      if (player == null)
        return false;

      foreach (var itemId in EnumerateRequiredItemIdentifiers())
      {
        if (player.CountItemInInventory(itemId) > 0)
          return true;
      }

      return false;
    }

    public bool TryConsumeOneRequiredItem(PlayerController player)
    {
      if (player == null)
        return false;

      foreach (var itemId in EnumerateRequiredItemIdentifiers())
      {
        if (player.CountItemInInventory(itemId) <= 0)
          continue;

        return player.RemoveItemFromInventory(itemId, 1) > 0;
      }

      return false;
    }

    public void BeginConnectionMode(PlayerController player, IntravenousLineConnectionPoint startPoint)
    {
      if (player == null || startPoint == null)
        return;

      int key = player.GetInstanceID();

      if (!HasAnyRequiredItem(player))
      {
        player.SetIntravenousLineConnectionMode(false, false);
        return;
      }

      _pendingConnections[key] = new PendingConnectionContext
      {
        Player = player,
        StartPoint = startPoint,
      };

      player.SetIntravenousLineConnectionMode(true);
      player.RefreshInteractableHintsNow();

      // 한 점 연결(연결 작업 시작)을 알린다: C# 이벤트 발화 + 인게임 서버 "연결 시도" 시그널.
      startPoint.NotifyConnectStart();
    }

    public bool HasPendingStartPoint(PlayerController player, out IntravenousLineConnectionPoint startPoint)
    {
      startPoint = null;

      if (player == null)
        return false;

      if (!_pendingConnections.TryGetValue(player.GetInstanceID(), out var context))
        return false;

      if (context == null || context.StartPoint == null)
        return false;

      startPoint = context.StartPoint;
      return true;
    }

    public bool TryCompleteConnection(PlayerController player, IntravenousLineConnectionPoint endPoint)
    {
      if (player == null || endPoint == null)
        return false;

      if (!HasPendingStartPoint(player, out var startPoint) || startPoint == null)
        return false;

      if (!player.IsIntravenousLineConnectionMode)
      {
        ClearPendingFor(player);
        player.RefreshInteractableHintsNow();
        return false;
      }

      if (ReferenceEquals(startPoint, endPoint))
        return false;

      if (!startPoint.CanAcceptAdditionalConnection || !endPoint.CanAcceptAdditionalConnection)
        return false;

      if (!CanConnectBetweenDifferentOwners(startPoint, endPoint, out _))
        return false;

      if (!TryConsumeOneRequiredItem(player))
      {
        player.SetIntravenousLineConnectionMode(false, false);
        ClearPendingFor(player);
        player.RefreshInteractableHintsNow();
        return false;
      }

      var lineObject = CreateLineObject(startPoint, endPoint);
      if (lineObject == null)
      {
        player.SetIntravenousLineConnectionMode(false, false);
        ClearPendingFor(player);
        player.RefreshInteractableHintsNow();
        return false;
      }

      startPoint.RegisterConnectedLineObject(lineObject);
      endPoint.RegisterConnectedLineObject(lineObject);

      // 시나리오 게이팅용 완료 신호(Identifier 그대로 / start__end 쌍).
      RaiseConnectionSignals(startPoint, endPoint);

      // 연결 완료를 각 지점에 알린다: C# 이벤트 발화 + 인게임 서버 "연결 완료" 시그널.
      startPoint.NotifyConnected(endPoint);
      endPoint.NotifyConnected(startPoint);

      ClearPendingFor(player);
      player.SetIntravenousLineConnectionMode(false, false);
      player.RefreshInteractableHintsNow();
      return true;
    }

    /// <summary>
    /// 수액/산소 줄 연결이 완료되면 시나리오 게이팅용 완료 신호(sig.*)를 올린다.
    /// 연결 지점의 Identifier 를 그대로 신호로 사용하므로, 시나리오가 기대하는 조건명
    /// (예: "connect_cannula_and_ns1")을 연결 지점 Identifier 로 지정하면 별도 코드 없이
    /// Validator(RegistryContains, RuntimeState, "sig.&lt;조건명&gt;") 게이트가 통과된다.
    /// 끝점 단독 / 시작점 단독 / 시작__끝 쌍 세 가지를 모두 올려 작성 유연성을 확보한다.
    /// </summary>
    private static void RaiseConnectionSignals(
      IntravenousLineConnectionPoint startPoint,
      IntravenousLineConnectionPoint endPoint)
    {
      string startId = startPoint != null ? startPoint.Identifier : null;
      string endId = endPoint != null ? endPoint.Identifier : null;

      if (!string.IsNullOrWhiteSpace(endId))
      {
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(endId);
      }

      if (!string.IsNullOrWhiteSpace(startId))
      {
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(startId);
      }

      if (!string.IsNullOrWhiteSpace(startId) && !string.IsNullOrWhiteSpace(endId))
      {
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise($"{startId}__{endId}");
      }
    }

    public void CancelConnectionMode(PlayerController player)
    {
      if (player == null)
        return;

      if (!_pendingConnections.Remove(player.GetInstanceID()))
        return;

      player.RefreshInteractableHintsNow();
    }

    public void DisconnectFromPoint(IntravenousLineConnectionPoint point)
    {
      if (point == null)
        return;

      bool destroyedAny = false;
      while (point.TryGetAnyConnectedLineObject(out var each) && each != null)
      {
        destroyedAny = true;
        DestroyLineObject(each);
      }

      if (destroyedAny)
      {
        var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
          players[i]?.RefreshInteractableHintsNow();
      }
    }

    private GameObject CreateLineObject(IntravenousLineConnectionPoint startPoint, IntravenousLineConnectionPoint endPoint)
    {
      EnsureLinesRoot();
      if (_linesRoot == null)
        return null;

      var lineObject = new GameObject($"IntravenousLine_{startPoint.name}_to_{endPoint.name}");
      lineObject.transform.SetParent(_linesRoot, false);

      var lineRenderer = lineObject.AddComponent<LineRenderer>();
      lineRenderer.positionCount = 0;
      lineRenderer.useWorldSpace = true;
      lineRenderer.alignment = LineAlignment.View;
      lineRenderer.textureMode = LineTextureMode.Stretch;
      lineRenderer.numCornerVertices = 6;
      lineRenderer.numCapVertices = 6;
      lineRenderer.startWidth = _lineWidth;
      lineRenderer.endWidth = _lineWidth;
      lineRenderer.startColor = _lineColor;
      lineRenderer.endColor = _lineColor;
      lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      lineRenderer.receiveShadows = false;
      lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

      var runtime = lineObject.AddComponent<IntravenousLineConnectionRuntime>();
      runtime.Bind(
        startPoint,
        endPoint,
        lineRenderer,
        _lineSegments,
        _lineSagAmount,
        _usePhysicsSimulation,
        _simulationStepsPerFrame,
        _solverIterations,
        _gravityScale,
        _velocityDamping,
        _slackLength,
        _collideWithWorld,
        _collisionRadius,
        _collisionMask,
        _triggerInteraction);

      return lineObject;
    }

    private void DestroyLineObject(GameObject lineObject)
    {
      if (lineObject == null)
        return;

      IntravenousLineConnectionPoint startPoint = null;
      IntravenousLineConnectionPoint endPoint = null;

      if (lineObject.TryGetComponent<IntravenousLineConnectionRuntime>(out var runtime) && runtime != null)
      {
        startPoint = runtime.StartPoint;
        endPoint = runtime.EndPoint;

        startPoint?.UnregisterConnectedLineObject(lineObject);
        endPoint?.UnregisterConnectedLineObject(lineObject);
      }

      Destroy(lineObject);

      // 줄 단위 연결 끊김을 각 지점에 알린다: C# 이벤트 발화 + 인게임 서버 "연결 끊김" 시그널.
      // 끊긴 줄마다 그 줄의 양 끝점에서 각각 발화한다(OnConnected 와 대칭). 지점에 다른
      // 연결이 남아 있는지 여부는 수신 측에서 HasAnyConnection 으로 판단한다.
      startPoint?.NotifyDisconnected(endPoint);
      endPoint?.NotifyDisconnected(startPoint);
    }

    private void EnsureLinesRoot()
    {
      if (_linesRoot != null)
        return;

      var existing = transform.Find("IntravenousLines");
      if (existing != null)
      {
        _linesRoot = existing;
        return;
      }

      var rootObject = new GameObject("IntravenousLines");
      _linesRoot = rootObject.transform;
      _linesRoot.SetParent(transform, false);
    }

    private void ClearPendingFor(PlayerController player)
    {
      if (player == null)
        return;

      _pendingConnections.Remove(player.GetInstanceID());
    }

    private void CleanupInvalidPendingConnections()
    {
      if (_pendingConnections.Count == 0)
        return;

      var keys = new List<int>();
      foreach (var each in _pendingConnections)
      {
        var context = each.Value;
        var player = context?.Player;
        var startPoint = context?.StartPoint;

        if (player == null || startPoint == null)
        {
          keys.Add(each.Key);
          continue;
        }

        if (!player.IsIntravenousLineConnectionMode)
          keys.Add(each.Key);
      }

      for (int i = 0; i < keys.Count; i++)
      {
        if (!_pendingConnections.TryGetValue(keys[i], out var context))
          continue;

        _pendingConnections.Remove(keys[i]);
        context?.Player?.RefreshInteractableHintsNow();
      }
    }

    private static bool CanConnectBetweenDifferentOwners(
      IntravenousLineConnectionPoint startPoint,
      IntravenousLineConnectionPoint endPoint,
      out string reason)
    {
      reason = string.Empty;

      if (startPoint == null || endPoint == null)
      {
        reason = "start/end point is null";
        return false;
      }

      var startNetworkObject = startPoint.OwningNetworkObject;
      var endNetworkObject = endPoint.OwningNetworkObject;

      if (startNetworkObject != null && endNetworkObject != null)
      {
        bool ok = !ReferenceEquals(startNetworkObject, endNetworkObject);
        if (!ok)
          reason = "same network object";
        return ok;
      }

      return true;
    }

    private void EnsureRequiredItemsDefaults()
    {
      _requiredItems ??= Array.Empty<Item>();
      _requiredItemIdentifiers ??= new List<string>();

      EnsureContainsRequiredItemIdentifier(IntravenousSet.Identifier);
    }

    private void EnsureContainsRequiredItemIdentifier(string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return;

      for (int i = 0; i < _requiredItemIdentifiers.Count; i++)
      {
        if (string.Equals(_requiredItemIdentifiers[i], itemIdentifier, StringComparison.Ordinal))
          return;
      }

      for (int i = 0; i < _requiredItems.Length; i++)
      {
        var each = _requiredItems[i];
        if (each == null)
          continue;

        if (string.Equals(each.CurrentIdentifier, itemIdentifier, StringComparison.Ordinal))
          return;
      }

      _requiredItemIdentifiers.Add(itemIdentifier);
    }

    private IEnumerable<string> EnumerateRequiredItemIdentifiers()
    {
      var yielded = new HashSet<string>(StringComparer.Ordinal);

      if (_requiredItemIdentifiers != null)
      {
        for (int i = 0; i < _requiredItemIdentifiers.Count; i++)
        {
          var each = _requiredItemIdentifiers[i];
          if (string.IsNullOrWhiteSpace(each))
            continue;

          if (yielded.Add(each))
            yield return each;
        }
      }

      if (_requiredItems != null)
      {
        for (int i = 0; i < _requiredItems.Length; i++)
        {
          var each = _requiredItems[i];
          if (each == null || string.IsNullOrWhiteSpace(each.CurrentIdentifier))
            continue;

          if (yielded.Add(each.CurrentIdentifier))
            yield return each.CurrentIdentifier;
        }
      }

      if (yielded.Add(IntravenousSet.Identifier))
        yield return IntravenousSet.Identifier;
    }
  }

  public class IntravenousLineConnectionRuntime : MonoBehaviour
  {
    [SerializeField] private IntravenousLineConnectionPoint _startPoint;
    [SerializeField] private IntravenousLineConnectionPoint _endPoint;
    [SerializeField] private LineRenderer _lineRenderer;

    [SerializeField, Min(2)] private int _segments = 18;
    [SerializeField, Min(0f)] private float _sagAmount = 0.015f;

    [SerializeField] private bool _usePhysicsSimulation = true;
    [SerializeField, Min(1)] private int _simulationStepsPerFrame = 2;
    [SerializeField, Min(1)] private int _solverIterations = 8;
    [SerializeField, Range(0f, 1f)] private float _gravityScale = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _velocityDamping = 0.1f;
    [SerializeField, Min(0f)] private float _slackLength = 0.06f;

    [SerializeField] private bool _collideWithWorld = true;
    [SerializeField, Min(0.0005f)] private float _collisionRadius = 0.006f;
    [SerializeField] private LayerMask _collisionMask = ~0;
    [SerializeField] private QueryTriggerInteraction _triggerInteraction = QueryTriggerInteraction.Ignore;

    private const float Epsilon = 0.000001f;
    private readonly Collider[] _overlapResults = new Collider[24];

    private Vector3[] _points = Array.Empty<Vector3>();
    private Vector3[] _previousPoints = Array.Empty<Vector3>();
    private bool _simulationInitialized;

    public IntravenousLineConnectionPoint StartPoint => _startPoint;
    public IntravenousLineConnectionPoint EndPoint => _endPoint;

    public void Bind(
      IntravenousLineConnectionPoint startPoint,
      IntravenousLineConnectionPoint endPoint,
      LineRenderer lineRenderer,
      int segments,
      float sagAmount,
      bool usePhysicsSimulation,
      int simulationStepsPerFrame,
      int solverIterations,
      float gravityScale,
      float velocityDamping,
      float slackLength,
      bool collideWithWorld,
      float collisionRadius,
      LayerMask collisionMask,
      QueryTriggerInteraction triggerInteraction)
    {
      _startPoint = startPoint;
      _endPoint = endPoint;
      _lineRenderer = lineRenderer;

      _segments = Mathf.Max(2, segments);
      _sagAmount = Mathf.Max(0f, sagAmount);

      _usePhysicsSimulation = usePhysicsSimulation;
      _simulationStepsPerFrame = Mathf.Max(1, simulationStepsPerFrame);
      _solverIterations = Mathf.Max(1, solverIterations);
      _gravityScale = Mathf.Clamp01(gravityScale);
      _velocityDamping = Mathf.Clamp01(velocityDamping);
      _slackLength = Mathf.Max(0f, slackLength);

      _collideWithWorld = collideWithWorld;
      _collisionRadius = Mathf.Max(0.0005f, collisionRadius);
      _collisionMask = collisionMask;
      _triggerInteraction = triggerInteraction;

      _simulationInitialized = false;
      EnsureBuffers();
      RefreshLine();
    }

    private void LateUpdate()
    {
      RefreshLine();
    }

    private void OnDestroy()
    {
      if (_startPoint != null)
        _startPoint.UnregisterConnectedLineObject(gameObject);

      if (_endPoint != null)
        _endPoint.UnregisterConnectedLineObject(gameObject);
    }

    private void RefreshLine()
    {
      if (_lineRenderer == null)
      {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
          return;
      }

      if (_startPoint == null || _endPoint == null)
      {
        _lineRenderer.enabled = false;
        _lineRenderer.positionCount = 0;
        return;
      }

      Vector3 start = _startPoint.transform.position;
      Vector3 end = _endPoint.transform.position;

      _lineRenderer.enabled = true;

      if (!_usePhysicsSimulation || !Application.isPlaying)
      {
        RenderAnalytic(start, end);
        return;
      }

      RenderPhysics(start, end);
    }

    private void RenderAnalytic(Vector3 start, Vector3 end)
    {
      EnsureBuffers();

      int count = _points.Length;
      for (int i = 0; i < count; i++)
      {
        float t = i / (count - 1f);
        Vector3 point = Vector3.Lerp(start, end, t);
        point += Vector3.down * (Mathf.Sin(t * Mathf.PI) * _sagAmount);
        _points[i] = point;
      }

      _lineRenderer.positionCount = count;
      _lineRenderer.SetPositions(_points);
      _simulationInitialized = false;
    }

    private void RenderPhysics(Vector3 start, Vector3 end)
    {
      EnsureBuffers();
      int count = _points.Length;

      if (!_simulationInitialized ||
          (_points[0] - start).sqrMagnitude > 0.5f ||
          (_points[count - 1] - end).sqrMagnitude > 0.5f)
      {
        InitializeSimulation(start, end);
      }

      int last = count - 1;
      _points[0] = start;
      _points[last] = end;
      _previousPoints[0] = start;
      _previousPoints[last] = end;

      float deltaTime = Mathf.Clamp(Time.deltaTime, 1f / 240f, 1f / 30f);
      float stepDt = deltaTime / Mathf.Max(1, _simulationStepsPerFrame);
      int stepCount = Mathf.Max(1, _simulationStepsPerFrame);

      Vector3 gravityVector = Physics.gravity.sqrMagnitude > Epsilon ? Physics.gravity : Vector3.down * 9.81f;
      Vector3 gravityStep = gravityVector * _gravityScale * stepDt * stepDt;
      float dampingFactor = 1f - _velocityDamping;

      float totalLength = Vector3.Distance(start, end) + _slackLength;
      float targetSegmentLength = totalLength / (count - 1);

      for (int step = 0; step < stepCount; step++)
      {
        Integrate(gravityStep, dampingFactor);

        for (int i = 0; i < _solverIterations; i++)
        {
          SolveDistanceConstraints(targetSegmentLength, start, end);
          if (_collideWithWorld)
            ResolveCollisions();
        }
      }

      _lineRenderer.positionCount = count;
      _lineRenderer.SetPositions(_points);
    }

    private void EnsureBuffers()
    {
      int count = Mathf.Max(2, _segments);
      if (_points.Length == count)
        return;

      _points = new Vector3[count];
      _previousPoints = new Vector3[count];
      _simulationInitialized = false;
    }

    private void InitializeSimulation(Vector3 start, Vector3 end)
    {
      int count = _points.Length;
      for (int i = 0; i < count; i++)
      {
        float t = i / (count - 1f);
        Vector3 point = Vector3.Lerp(start, end, t);
        point += Vector3.down * (Mathf.Sin(t * Mathf.PI) * _sagAmount);

        _points[i] = point;
        _previousPoints[i] = point;
      }

      _simulationInitialized = true;
    }

    private void Integrate(Vector3 gravityStep, float dampingFactor)
    {
      for (int i = 1; i < _points.Length - 1; i++)
      {
        Vector3 current = _points[i];
        Vector3 velocity = (current - _previousPoints[i]) * dampingFactor;
        _previousPoints[i] = current;
        _points[i] = current + velocity + gravityStep;
      }
    }

    private void SolveDistanceConstraints(float targetLength, Vector3 start, Vector3 end)
    {
      int last = _points.Length - 1;
      _points[0] = start;
      _points[last] = end;

      for (int i = 0; i < _points.Length - 1; i++)
      {
        Vector3 p1 = _points[i];
        Vector3 p2 = _points[i + 1];
        Vector3 delta = p2 - p1;
        float distance = delta.magnitude;
        if (distance < Epsilon)
          continue;

        float distanceError = distance - targetLength;
        Vector3 correction = delta * (distanceError / distance);

        if (i == 0)
        {
          p2 -= correction;
        }
        else if (i + 1 == last)
        {
          p1 += correction;
        }
        else
        {
          Vector3 half = correction * 0.5f;
          p1 += half;
          p2 -= half;
        }

        _points[i] = p1;
        _points[i + 1] = p2;
      }

      _points[0] = start;
      _points[last] = end;
    }

    private void ResolveCollisions()
    {
      for (int i = 1; i < _points.Length - 1; i++)
      {
        Vector3 previous = _previousPoints[i];
        Vector3 current = _points[i];
        Vector3 movement = current - previous;
        float movementDistance = movement.magnitude;

        if (movementDistance > Epsilon)
        {
          if (Physics.SphereCast(
                previous,
                _collisionRadius,
                movement / movementDistance,
                out RaycastHit hit,
                movementDistance,
                _collisionMask,
                _triggerInteraction))
          {
            current = hit.point + hit.normal * _collisionRadius;
            _previousPoints[i] = Vector3.Lerp(previous, current, 0.5f);
          }
        }

        int overlapCount = Physics.OverlapSphereNonAlloc(
          current,
          _collisionRadius,
          _overlapResults,
          _collisionMask,
          _triggerInteraction);

        for (int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++)
        {
          Collider collider = _overlapResults[overlapIndex];
          if (collider == null || !collider.enabled)
            continue;

          Vector3 closest = GetClosestPointSafe(collider, current);
          Vector3 toPoint = current - closest;
          float distance = toPoint.magnitude;

          if (distance < Epsilon)
          {
            Vector3 fallbackDirection = current - collider.bounds.center;
            if (fallbackDirection.sqrMagnitude < Epsilon)
              fallbackDirection = Vector3.up;

            current = closest + fallbackDirection.normalized * _collisionRadius;
            continue;
          }

          if (distance < _collisionRadius)
            current = closest + (toPoint / distance) * _collisionRadius;
        }

        _points[i] = current;
      }
    }

    private static Vector3 GetClosestPointSafe(Collider collider, Vector3 point)
    {
      if (collider == null)
        return point;

      if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
        return collider.ClosestPoint(point);

      if (collider is MeshCollider meshCollider && meshCollider.convex)
        return collider.ClosestPoint(point);

      // Fallback for unsupported collider types (eg non-convex MeshCollider).
      return collider.bounds.ClosestPoint(point);
    }
  }
}
