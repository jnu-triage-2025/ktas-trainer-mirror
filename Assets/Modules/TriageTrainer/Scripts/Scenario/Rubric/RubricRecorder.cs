using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Scenario.Rubric
{
  /// <summary>
  /// 평가 루브릭 수행/미수행 기록 코어(G-3 첫 증분).
  ///
  /// <para>
  /// <see cref="ScenarioController"/> 의 이벤트를 구독하여, 시나리오 진행 중 각 루브릭 항목의
  /// 수행/미수행을 자동 판정·기록한다. UI(관찰자 모드)·영속화는 본 증분 범위 밖이며,
  /// 본 컴포넌트는 <see cref="RubricResultStore"/> 에 결과를 누적하고 CSV 내보내기까지 제공한다.
  /// </para>
  ///
  /// <para>판정 규칙</para>
  /// <list type="bullet">
  /// <item>수행(Performed): 매핑된 Validator 게이트 노드를 통과(다음 노드 진입)하면 기록.</item>
  /// <item>미수행(NotPerformed): 게이트가 <see cref="ScenarioController.OnValidatorWaitTimeout"/> 로
  ///   타임아웃(ForceAdvance/FailBranch)되면 기록(G-6 연계).</item>
  /// </list>
  ///
  /// 자동 신호가 없는 항목(pass_*, 신체 사정 등)은 본 증분에서 자동 판정하지 않으며
  /// 관찰자 수동 체크(<see cref="MarkManual"/>) 진입점만 제공한다.
  /// </summary>
  public sealed class RubricRecorder : MonoBehaviour
  {
    [Header("루브릭 정의 데이터팩")]
    [Tooltip("RubricDefinitionSet JSON (items: id/area/title/gateNodeIdentifier/autoSignal/perPlayer).")]
    [SerializeField] private TextAsset _rubricDefinition;

    [Header("디버그")]
    [SerializeField] private bool _logDecisions = true;

    private readonly Dictionary<string, RubricItemDefinition> _itemsByGateNode = new();
    private readonly Dictionary<string, RubricItemDefinition> _itemsById = new();
    private readonly Dictionary<string, List<RubricItemDefinition>> _itemsByAutoSignal = new();

    private RubricResultStore _store;
    private ScenarioController _controller;

    /// <summary>현재 세션의 결과 저장소. 세션 시작 전에는 null.</summary>
    public RubricResultStore Store => _store;

    private void Awake()
    {
      LoadDefinitions();
    }

    private void OnEnable()
    {
      // 컨트롤러는 네트워크 스폰 프리팹이라 이 컴포넌트의 Start 보다 늦게 생길 수 있다.
      // 생성 통지를 먼저 구독해 두어야 늦게 올라온 세션도 놓치지 않는다.
      ScenarioController.InstanceAvailable -= HandleScenarioControllerAvailable;
      ScenarioController.InstanceAvailable += HandleScenarioControllerAvailable;
      TrySubscribe();
    }

    private void OnDisable()
    {
      ScenarioController.InstanceAvailable -= HandleScenarioControllerAvailable;
      Unsubscribe();
    }

    private void HandleScenarioControllerAvailable(ScenarioController controller)
    {
      if (controller == null)
      {
        return;
      }

      TrySubscribe();
    }

    private void LoadDefinitions()
    {
      _itemsByGateNode.Clear();
      _itemsById.Clear();
      _itemsByAutoSignal.Clear();

      if (_rubricDefinition == null || string.IsNullOrWhiteSpace(_rubricDefinition.text))
      {
        Debug.LogWarning("[RubricRecorder] 루브릭 정의 데이터팩이 비어 있습니다. 자동 판정이 비활성화됩니다.", this);
        return;
      }

      RubricDefinitionSet set;
      try
      {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        set = JsonSerializer.Deserialize<RubricDefinitionSet>(_rubricDefinition.text, options);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[RubricRecorder] 루브릭 정의 파싱 실패: {ex.Message}", this);
        return;
      }

      if (set?.Items == null)
      {
        return;
      }

      foreach (var item in set.Items)
      {
        if (item == null || string.IsNullOrWhiteSpace(item.Id))
        {
          continue;
        }

        if (_itemsById.ContainsKey(item.Id))
        {
          Debug.LogWarning(
            $"[RubricRecorder] 루브릭 항목 식별자 '{item.Id}' 가 중복되어 이전 정의를 덮어씁니다.", this);
        }

        _itemsById[item.Id] = item;
        if (!string.IsNullOrWhiteSpace(item.GateNodeIdentifier))
        {
          if (_itemsByGateNode.TryGetValue(item.GateNodeIdentifier, out var previousGateItem))
          {
            Debug.LogWarning(
              $"[RubricRecorder] 게이트 노드 '{item.GateNodeIdentifier}' 에 항목 '{previousGateItem.Id}' 와 "
              + $"'{item.Id}' 가 함께 매핑되어 있습니다. 마지막 항목만 자동 판정되고 나머지는 기록되지 않습니다.",
              this);
          }

          _itemsByGateNode[item.GateNodeIdentifier] = item;
        }

        // autoSignal 매핑: 게이트 타임아웃(NotPerformed) 이후에라도
        // 매핑된 신호가 올라오면 Performed 로 기록한다(수행 우선 정책과 결합).
        if (!string.IsNullOrWhiteSpace(item.AutoSignal))
        {
          var normalized = ScenarioInteractionSignals.Normalize(item.AutoSignal);
          if (!_itemsByAutoSignal.TryGetValue(normalized, out var list))
          {
            list = new List<RubricItemDefinition>();
            _itemsByAutoSignal[normalized] = list;
          }
          list.Add(item);
        }
      }

      if (_logDecisions)
      {
        Debug.Log($"[RubricRecorder] 루브릭 항목 {_itemsById.Count}개 로드(게이트 매핑 {_itemsByGateNode.Count}개).", this);
      }
    }

    private void TrySubscribe()
    {
      if (_controller != null)
      {
        return;
      }

      _controller = ScenarioController.Instance;
      if (_controller == null)
      {
        // 컨트롤러가 아직 생성되지 않았다. InstanceAvailable 통지와 Start 에서 다시 시도한다.
        return;
      }

      _controller.OnScenarioStarted += HandleScenarioStarted;
      _controller.OnScenarioEnded += HandleScenarioEnded;
      _controller.OnNodeChanged += HandleNodeChanged;
      _controller.OnValidatorWaitTimeout += HandleValidatorWaitTimeout;
      ScenarioInteractionSignals.OnSignalRegistered += HandleSignalRegistered;
    }

    private void Start()
    {
      // Awake/OnEnable 시점에 컨트롤러가 없었다면 여기서 재구독 시도.
      TrySubscribe();
    }

    private void Unsubscribe()
    {
      if (_controller == null)
      {
        return;
      }

      _controller.OnScenarioStarted -= HandleScenarioStarted;
      _controller.OnScenarioEnded -= HandleScenarioEnded;
      _controller.OnNodeChanged -= HandleNodeChanged;
      _controller.OnValidatorWaitTimeout -= HandleValidatorWaitTimeout;
      ScenarioInteractionSignals.OnSignalRegistered -= HandleSignalRegistered;
      _controller = null;
    }

    /// <summary>
    /// autoSignal 로 매핑된 신호가 올라오면 해당 항목을 Performed 로 기록한다.
    /// 게이트 타임아웃으로 NotPerformed 가 먼저 기록됐더라도 "수행 우선" 정책에 따라
    /// 이후 실제 수행이 확인되면 Performed 로 갱신된다.
    /// </summary>
    private void HandleSignalRegistered(string normalizedSignalId)
    {
      if (_store == null || string.IsNullOrEmpty(normalizedSignalId)
          || !_itemsByAutoSignal.TryGetValue(normalizedSignalId, out var items))
      {
        return;
      }

      foreach (var item in items)
      {
        // 타임아웃 후 늦은 수행 포함: NotPerformed → Performed 승격은 Record 가 허용한다
        // (Record 는 Performed→NotPerformed 강등만 막는다).
        bool wasTimedOut = _store.TryGet(item.Id, playerId: null, out var existing)
            && existing.Status == RubricStatus.NotPerformed;
        var note = wasTimedOut ? $"signal after timeout: {normalizedSignalId}" : $"signal: {normalizedSignalId}";
        var result = _store.Record(item.Id, playerId: null, RubricStatus.Performed, note);

        if (_logDecisions && result != null)
        {
          Debug.Log($"[RubricRecorder] 수행(신호): {item.Id} ({item.Title}) ← {normalizedSignalId}", this);
        }
      }
    }

    // ── 세션 수명주기 ───────────────────────────────────────────────

    private string _pendingGateNodeIdentifier;

    private void HandleScenarioStarted()
    {
      _store = new RubricResultStore(Guid.NewGuid().ToString("N"));
      _pendingGateNodeIdentifier = null;

      if (_logDecisions)
      {
        Debug.Log($"[RubricRecorder] 세션 시작: {_store.SessionId}", this);
      }
    }

    private void HandleScenarioEnded()
    {
      // 종료 시점에 통과하지 못하고 남은 게이트는 별도 처리하지 않는다(타임아웃 정책이 담당).
      _pendingGateNodeIdentifier = null;

      if (_logDecisions && _store != null)
      {
        Debug.Log($"[RubricRecorder] 세션 종료: {_store.SessionId}\n{_store.ExportCsv()}", this);
      }
    }

    // ── 자동 판정 ───────────────────────────────────────────────────

    /// <summary>
    /// 노드 진입 이벤트. 직전에 진입했던 Validator 게이트 노드에서 "다른 노드"로 진입했다는 것은
    /// 게이트가 조건 충족으로 통과되었음을 의미하므로 Performed 로 기록한다(타임아웃은 별도 경로로 NotPerformed 기록).
    /// </summary>
    private void HandleNodeChanged(IScenarioNode node)
    {
      var enteringId = node?.Identifier;

      // 직전 보류 게이트가 있고, 지금 그 게이트가 아닌 노드로 진입했다면 통과한 것으로 본다.
      if (!string.IsNullOrEmpty(_pendingGateNodeIdentifier)
          && !string.Equals(_pendingGateNodeIdentifier, enteringId, StringComparison.Ordinal))
      {
        ResolveGatePerformed(_pendingGateNodeIdentifier);
        _pendingGateNodeIdentifier = null;
      }

      // 새로 진입한 노드가 매핑된 Validator 게이트라면 보류 상태로 둔다(통과/타임아웃 확정 대기).
      if (node is ScenarioValidatorNode && !string.IsNullOrEmpty(enteringId)
          && _itemsByGateNode.ContainsKey(enteringId))
      {
        _pendingGateNodeIdentifier = enteringId;
      }
    }

    private void ResolveGatePerformed(string gateNodeIdentifier)
    {
      if (_store == null || !_itemsByGateNode.TryGetValue(gateNodeIdentifier, out var item))
      {
        return;
      }

      // 본 증분은 팀 단위(플레이어 미구분) 기록. PerPlayer 항목의 플레이어 분배는 후속(관찰자 모드/권한)에서 확장.
      var result = _store.Record(item.Id, playerId: null, RubricStatus.Performed, note: $"gate passed: {gateNodeIdentifier}");
      if (_logDecisions && result != null)
      {
        Debug.Log($"[RubricRecorder] 수행: {item.Id} ({item.Title}) ← 게이트 {gateNodeIdentifier} 통과", this);
      }
    }

    private void HandleValidatorWaitTimeout(ScenarioValidatorNode node, ScenarioValidatorWaitTimeoutBehavior behavior)
    {
      var gateId = node?.Identifier;
      if (_store == null || string.IsNullOrEmpty(gateId) || !_itemsByGateNode.TryGetValue(gateId, out var item))
      {
        return;
      }

      // 타임아웃으로 강제 진행/실패 분기된 게이트 = 시간 내 미수행.
      // WarnAndKeepWaiting/KeepWaiting 은 이벤트가 발생해도 계속 대기하므로 미수행 확정으로 보지 않는다.
      if (behavior == ScenarioValidatorWaitTimeoutBehavior.WarnAndKeepWaiting
          || behavior == ScenarioValidatorWaitTimeoutBehavior.KeepWaiting)
      {
        return;
      }

      // 미수행 확정 → 통과 대기 해제(통과로 잘못 기록되지 않도록).
      if (string.Equals(_pendingGateNodeIdentifier, gateId, StringComparison.Ordinal))
      {
        _pendingGateNodeIdentifier = null;
      }

      var result = _store.Record(item.Id, playerId: null, RubricStatus.NotPerformed, note: $"gate timeout: {behavior}");
      if (_logDecisions && result != null)
      {
        Debug.LogWarning($"[RubricRecorder] 미수행: {item.Id} ({item.Title}) ← 게이트 {gateId} 타임아웃({behavior})", this);
      }
    }

    // ── 수동/외부 진입점 ────────────────────────────────────────────

    /// <summary>관찰자 수동 체크 등 외부에서 항목 상태를 직접 표기한다.</summary>
    public void MarkManual(string itemId, string playerId, RubricStatus status, string note = "manual")
    {
      if (_store == null)
      {
        Debug.LogWarning("[RubricRecorder] 활성 세션이 없어 수동 기록을 무시합니다.", this);
        return;
      }

      if (!_itemsById.ContainsKey(itemId))
      {
        Debug.LogWarning($"[RubricRecorder] 알 수 없는 루브릭 항목 '{itemId}'.", this);
        return;
      }

      _store.Record(itemId, playerId, status, note);
    }

    /// <summary>사정 퀴즈 오답/재응시 횟수 누적(외부에서 호출).</summary>
    public void RecordRetry(string itemId, string playerId)
    {
      if (_store == null)
      {
        Debug.LogWarning(
          $"[RubricRecorder] 활성 세션이 없어 항목 '{itemId}' 의 재응시 기록을 무시합니다.", this);
        return;
      }

      _store.IncrementRetry(itemId, playerId);
    }

    [ContextMenu("Export Rubric CSV To Console")]
    private void ExportToConsole()
    {
      if (_store == null)
      {
        Debug.LogWarning("[RubricRecorder] 활성 세션이 없습니다.", this);
        return;
      }

      Debug.Log($"[RubricRecorder] CSV\n{_store.ExportCsv()}", this);
    }
  }
}
