using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.Datapack;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Problem;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Chat
{
  public class ChatService : NetworkBehaviour
  {
    private const int MaxChatMessageLength = 512;
    private const int MaxCommandLineLength = 2048;
    [Header("ChatSettings")]
    [SerializeField, Min(0f)]
    private float _messageCooldownSeconds = DefaultsChatControl.MessageCooldownSeconds;

    [Header("References")]
    [SerializeField] private ChatUIController _uiController;
    [SerializeField] private ChatCommandService _commandService;
    [SerializeField] private DatapackRuntimeService _datapackRuntime;
    public ChatCommandService CommandService => _commandService;

    private readonly Dictionary<int, float> _lastMessageTimes = new();
    private readonly Dictionary<int, int> _lastProblemSheetGradeByClientId = new();
    private readonly Dictionary<int, HashSet<string>> _issuedProblemsByClientId = new();

    /// <summary>
    /// 시스템 권한 실행(TryExecuteSystemCommand) 재진입 깊이. 0보다 크면 커맨드 정의가
    /// 발생시킨 시스템 메시지를 플레이어 채팅창이 아닌 서버 로그로만 기록한다.
    /// (시나리오 ExecuteCommand 노드가 owner 컨텍스트로 실행되더라도 성공/진행 메시지를
    /// 플레이어에게 스팸하지 않도록 한다.)
    /// </summary>
    private int _systemExecutionContextDepth;

    private void Awake()
    {
      // ChatService / ChatCommandService / ChatUIController 는 서로 다른 GameObject에
      // 배치될 수 있다. 직렬화된 참조 → 같은 GameObject → 씬 전역 순으로 해결한다.
      // (GetComponent는 비활성 GameObject의 컴포넌트도 반환하므로, 전역 fallback도
      // 비활성 객체를 포함해 동일하게 동작하도록 Include를 사용한다.)
      Registry.Registry.Register(RegistryType.Service, Registry.Registry.TypeKey<ChatService>(), this);

      if (_uiController == null)
        _uiController = GetComponent<ChatUIController>();
      if (_uiController == null)
        _uiController = FindFirstObjectByType<ChatUIController>(FindObjectsInactive.Include);

      if (_commandService == null)
        _commandService = GetComponent<ChatCommandService>();
      if (_commandService == null)
        _commandService = FindFirstObjectByType<ChatCommandService>(FindObjectsInactive.Include);

      if (_datapackRuntime == null)
        _datapackRuntime = GetComponent<DatapackRuntimeService>();

      if (_datapackRuntime == null)
        _datapackRuntime = gameObject.AddComponent<DatapackRuntimeService>();

      if (_uiController == null || _commandService == null)
      {
        Debug.LogError("ChatService missing required references (UI or CommandService).", this);
        enabled = false;
        return;
      }
      _commandService.Initialize(this);

      _uiController.OnSubmitted += HandleLocalSubmission;
    }

    private void OnDestroy()
    {
      // 분리 배치 시 ChatUIController는 ChatService(NetworkObject)보다 오래 생존할 수 있으므로
      // 구독을 명시적으로 해제한다. (같은 GameObject 구성에서도 재스폰 중복 구독을 방지한다.)
      if (_uiController != null)
        _uiController.OnSubmitted -= HandleLocalSubmission;

      // 재스폰 등으로 새 인스턴스가 이미 등록되었다면 그 등록을 지우지 않도록
      // 현재 등록된 인스턴스가 자신일 때만 해제한다.
      if (Registry.Registry.TryGet<ChatService>(RegistryType.Service, Registry.Registry.TypeKey<ChatService>(), out var registered)
          && registered == this)
      {
        Registry.Registry.Unregister(RegistryType.Service, Registry.Registry.TypeKey<ChatService>());
      }
    }

    private void HandleLocalSubmission(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return;

      if (raw.StartsWith("/"))
      {
        ExecuteCommandServerRpc(raw[1..]);
        return;
      }

      SendChatServerRpc(raw);
    }

    #region Networking

    [ServerRpc(RequireOwnership = false)]
    private void SendChatServerRpc(string rawMessage, NetworkConnection sender = null)
    {
      if (sender == null || string.IsNullOrWhiteSpace(rawMessage))
        return;
      rawMessage = SanitizeChatMessage(rawMessage);
      if (rawMessage.Length == 0)
        return;

      if (!CanSendMessage(sender, out string cooldownMessage))
      {
        SendSystemMessage(sender, cooldownMessage);
        return;
      }

      string displayName = GetDisplayName(sender);
      string senderUuid = string.Empty;
      if (UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor))
        senderUuid = descriptor.Identifier;

      string formatted = $"<{displayName}> {rawMessage}";
      ReceiveChatObserversRpc(formatted);
      MarkMessageSent(sender);

      // 로그 기록 (서버 측)
      GameLogService.WriteChat(
        $"<{displayName}> {rawMessage}",
        string.IsNullOrEmpty(senderUuid) ? displayName : senderUuid);
    }

    [ObserversRpc]
    private void ReceiveChatObserversRpc(string formattedLine)
    {
      Debug.Log($"[ChatService] Received chat message: {formattedLine}");
      _uiController.AppendMessage(formattedLine, showToastWhenHidden: true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExecuteCommandServerRpc(string commandLine, NetworkConnection sender = null)
    {
      if (sender == null || string.IsNullOrWhiteSpace(commandLine)
          || commandLine.Length > MaxCommandLineLength)
        return;

      // 커맨드 로그 기록 (서버 측)
      string senderName = GetDisplayName(sender);
      string senderUuid = string.Empty;
      if (UserDescriptorService.TryGetByClientId(sender.ClientId, out var desc))
        senderUuid = desc.Identifier;
      GameLogService.WriteCommand(
        $"/{commandLine} (by {senderName})",
        string.IsNullOrEmpty(senderUuid) ? senderName : senderUuid);

      TryExecuteCommandInternal(commandLine, sender, out _);
    }

    /// <summary>
    /// CPR 디버그 Escape 규칙을 서버와 모든 관찰 클라이언트에 동일하게 적용한다.
    /// 게임룰 명령은 서버에서만 실행되지만 Escape 입력은 각 소유 클라이언트가 판정하므로,
    /// static 값만 변경해서는 원격 디버그 플레이어에게 규칙이 전달되지 않는다.
    /// </summary>
    public bool TrySetDebugIntCprPlayingEscapeKeyServer(bool enabled)
    {
      if (InstanceFinder.IsOffline)
      {
        ApplyDebugIntCprPlayingEscapeKey(enabled);
        return true;
      }

      if (!IsServerInitialized)
        return false;

      ApplyDebugIntCprPlayingEscapeKey(enabled);
      SyncDebugIntCprPlayingEscapeKeyObserversRpc(enabled);
      return true;
    }

    [ObserversRpc(BufferLast = true)]
    private void SyncDebugIntCprPlayingEscapeKeyObserversRpc(bool enabled)
    {
      ApplyDebugIntCprPlayingEscapeKey(enabled);
    }

    private static void ApplyDebugIntCprPlayingEscapeKey(bool enabled)
    {
      ScenarioGameRules.DEBUG_INT_CPR_PLAYING_ESCAPE_KEY = enabled;
    }

    [TargetRpc]
    private void TargetRunScenario(
      NetworkConnection conn,
      string scenarioIdentifier,
      int ownerClientId,
      bool allowMultipleRoleBranchesForSinglePlayer,
      string entrypointIdentifier)
    {
      if (!Registry.Registry.TryGetScenarioGraph(scenarioIdentifier, out ScenarioGraph graph, out string error))
      {
        Debug.LogWarning($"[ChatService] Failed to load scenario '{scenarioIdentifier}': {error}");
        return;
      }

      if (ScenarioController.Instance == null)
      {
        Debug.LogWarning("[ChatService] ScenarioController is missing on this client.");
        return;
      }

      // 호환 실행 경로에서는 클라이언트가 자체 상태기를 실행하므로 서버의 확정 규칙을 먼저 적용한다.
      ScenarioGameRules.AllowMultipleRoleBranchesForSinglePlayer = allowMultipleRoleBranchesForSinglePlayer;
      int? owner = ownerClientId >= 0 ? ownerClientId : (int?)null;
      ScenarioController.Instance.StartScenario(graph, null, owner);

      // 진입 지점이 지정되면 처음부터 다시 밟지 않고 그 지점으로 바로 옮긴다. 접속이 끊겼다가 다시
      // 들어온 참가자를 나머지 인원이 진행 중인 단계에 합류시키는 용도다. 시작 직후라 비울 상태가
      // 없으므로 clearState 는 기본값(true)을 그대로 쓴다. 세계 상태는 서버에 이미 살아 있으므로
      // 준비 체인은 돌리지 않는다.
      if (!string.IsNullOrWhiteSpace(entrypointIdentifier)
          && !ScenarioController.Instance.TryEnterManualEntrypoint(
            entrypointIdentifier, true, out string entryError, runSetupChain: false))
      {
        Debug.LogWarning($"[ChatService] Manual entry '{entrypointIdentifier}' after scenario start was rejected: {entryError}");
      }

      // 시작과 수동 진입은 이 피어의 로컬 신호를 비운다. 서버에 이미 올라가 있는 신호를 받아 와야
      // 다른 참가자가 먼저 끝낸 단계를 기다리는 게이트가 타임아웃 없이 열린다. 서버(호스트)는
      // 자기 레지스트리가 곧 원본이라 요청할 것이 없다.
      ScenarioNetworkRelay.RequestRaisedSignalSnapshot();
    }

    [ObserversRpc(BufferLast = true)]
    private void SyncValidatorBlockLogTargetsObserversRpc(int targets)
    {
      if (ScenarioController.Instance != null)
      {
        ScenarioController.Instance.ValidatorBlockLogTargets = (ScenarioValidatorBlockLogTarget)targets;
      }
    }

    [TargetRpc]
    private void TargetShowTitle(NetworkConnection conn, string title, string subtitle)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ShowTitle(title, subtitle);
    }

    [TargetRpc]
    private void TargetShowSubtitle(NetworkConnection conn, string subtitle)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ShowSubtitle(subtitle);
    }

    [TargetRpc]
    private void TargetShowActionbar(NetworkConnection conn, string actionbar)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ShowActionbar(actionbar);
    }

    [TargetRpc]
    private void TargetClearTitle(NetworkConnection conn)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ClearAll();
    }

    [TargetRpc]
    private void TargetResetTitle(NetworkConnection conn)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.ResetTimesAndSubtitle();
    }

    [TargetRpc]
    private void TargetSetTitleTimes(NetworkConnection conn, int fadeInTicks, int stayTicks, int fadeOutTicks)
    {
      var ui = GetTitleUIController();
      if (ui == null)
        return;

      ui.SetTimes(fadeInTicks, stayTicks, fadeOutTicks);
    }

    [TargetRpc]
    private void TargetRunProblemSheet(NetworkConnection conn, string problemSetIdentifier, int startIndex, bool singleProblemMode)
    {
      var controller = Registry.Registry.Get<ProblemSheetUIController>(
        RegistryType.UI,
        Registry.Registry.TypeKey<ProblemSheetUIController>());

      if (controller == null)
      {
        Debug.LogWarning("[ChatService] ProblemSheetUIController is missing on this client.");
        return;
      }

      if (!controller.OpenProblemSet(problemSetIdentifier, startIndex, singleProblemMode))
      {
        Debug.LogWarning($"[ChatService] Failed to open problem set '{problemSetIdentifier}'.");
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReportProblemGradeServerRpc(string problemSetIdentifier, int problemIndex,
      int selectedChoiceIndex, string shortAnswer, NetworkConnection sender = null)
    {
      if (sender == null)
        return;

      if (!Registry.Registry.TryGetProblemSet(problemSetIdentifier, out var set, out _))
        return;

      if (set?.Problems == null || problemIndex < 0 || problemIndex >= set.Problems.Count)
        return;

      string issuedKey = BuildIssuedProblemKey(problemSetIdentifier, problemIndex);
      if (!_issuedProblemsByClientId.TryGetValue(sender.ClientId, out var issued) || !issued.Contains(issuedKey))
      {
        Debug.LogWarning($"[ChatService] Rejected unissued or repeated problem grade from client {sender.ClientId}.");
        return;
      }

      var problem = set.Problems[problemIndex];
      bool correct = problem?.Choice != null
        ? ProblemAnswerEvaluator.EvaluateChoice(problem.Choice, selectedChoiceIndex)
        : ProblemAnswerEvaluator.EvaluateShortAnswer(problem?.ShortAnswer, shortAnswer);
      int normalizedCode = correct ? 0 : 1;
      _lastProblemSheetGradeByClientId[sender.ClientId] = normalizedCode;

      if (correct || problem?.Grading?.RetryOnWrong == false)
        issued.Remove(issuedKey);

      if (correct)
        ProblemRewardService.ApplyOnCorrect(problem, sender.ClientId);
    }

    #endregion

    #region Helpers

    internal static string SanitizeChatMessage(string rawMessage)
    {
      string normalized = (rawMessage ?? string.Empty).Trim();
      if (normalized.Length > MaxChatMessageLength)
        normalized = normalized[..MaxChatMessageLength];
      return normalized.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private bool CanSendMessage(NetworkConnection sender, out string message)
    {
      message = string.Empty;

      if (!_lastMessageTimes.TryGetValue(sender.ClientId, out float lastTime))
        return true;

      float elapsed = Time.time - lastTime;
      if (elapsed >= _messageCooldownSeconds)
        return true;

      message = $"You must wait {(_messageCooldownSeconds - elapsed):0.00}s before sending another message.";
      return false;
    }

    private void MarkMessageSent(NetworkConnection sender)
    {
      _lastMessageTimes[sender.ClientId] = Time.time;
    }

    public void SendSystemMessage(NetworkConnection conn, string message)
    {
      // 시스템 권한 실행 중에는 메시지를 플레이어 채팅창에 전파하지 않고 서버 로그로만 기록한다.
      if (conn != null && _systemExecutionContextDepth == 0)
        ReceiveChatObserversRpc(FormatPlayerSystemMessage(conn, message));
      else
        Debug.Log($"[System] {message}");
    }

    private string FormatPlayerSystemMessage(NetworkConnection conn, string message)
    {
      return $"({GetDisplayName(conn)}) {message}";
    }

    public bool TryDispatchScenario(string scenarioIdentifier, IEnumerable<NetworkConnection> targets, out string error)
      => TryDispatchScenario(scenarioIdentifier, targets, null, out error);

    /// <summary>
    /// 대상에게 시나리오를 시작시킨다.
    /// </summary>
    /// <param name="entrypointIdentifier">
    /// 지정하면 대상 피어는 시작 직후 이 ManualEntrypoint 로 바로 옮겨 간다. 접속이 끊겼다가 다시
    /// 들어온 참가자를 진행 중인 단계에 합류시키는 용도이므로, 대상별 독립 상태기를 도는 호환 실행
    /// 경로에서만 쓸 수 있다. 서버 권위 실행은 그래프가 하나뿐이라 <c>/scenario enter</c> 로 옮긴다.
    /// </param>
    public bool TryDispatchScenario(
      string scenarioIdentifier,
      IEnumerable<NetworkConnection> targets,
      string entrypointIdentifier,
      out string error)
    {
      error = string.Empty;

      if (!IsServerInitialized)
      {
        error = "Scenario execution can only be invoked on the server.";
        return false;
      }

      if (!Registry.Registry.PreloadScenarioGraph(scenarioIdentifier, validateWithSchema: true)
          || !Registry.Registry.TryGetScenarioGraph(scenarioIdentifier, out ScenarioGraph graph, out _)
          || graph == null)
      {
        error = $"Scenario '{scenarioIdentifier}' is not registered.";
        return false;
      }

      if (targets == null)
      {
        error = "No target players were matched.";
        return false;
      }

      var resolvedTargets = targets.Where(target => target != null).ToList();
      if (resolvedTargets.Count == 0)
      {
        error = "No target players were matched.";
        return false;
      }

      // 역할 태그가 중복 부여된 세션은 시작해도 첫 ByRole 병렬에서 모든 브랜치가 건너뛰어져,
      // 아무도 수행하지 않은 단계의 신호를 기다리다 멈춘다. 원인과 증상이 멀리 떨어져 있어
      // 운영자가 찾기 어려우므로 시작 자체를 거부하고 바로잡을 명령을 안내한다.
      if (!ScenarioController.TryValidateActiveRoleRosterForStart(graph, out string rosterError))
      {
        error = $"Scenario '{scenarioIdentifier}' cannot start: {rosterError}. "
                + "Check the role tags with '/tag show @a' and fix them with '/tag remove' or '/tag change' before starting.";
        return false;
      }

      bool hasEntrypoint = !string.IsNullOrWhiteSpace(entrypointIdentifier);

      // 호환 실행 경로에서는 대상으로 지정된 피어만 그래프를 돌린다. 역할 보유자가 대상에서 빠지면
      // 그 역할의 브랜치는 어디에서도 실행되지 않아 나머지 인원의 게이트가 타임아웃으로만 넘어간다.
      // 처음 시작할 때는 역할 보유자 전원이 대상에 포함되어야 한다. 진입 지점을 지정한 재합류 시작은
      // 한 명만을 대상으로 하므로 이 검사에서 제외한다.
      if (!hasEntrypoint
          && ScenarioController.TryGetActiveRoleHolderClientIds(graph, out var roleHolderClientIds))
      {
        var targetClientIds = new HashSet<int>(resolvedTargets.Select(target => (int)target.ClientId));
        var missing = roleHolderClientIds
          .Where(clientId => !targetClientIds.Contains(clientId))
          .Select(clientId => UserDescriptorService.TryGetByClientId(clientId, out var descriptor)
                              && descriptor != null
                              && !string.IsNullOrWhiteSpace(descriptor.DisplayName)
            ? descriptor.DisplayName
            : $"client {clientId}")
          .ToList();
        if (missing.Count > 0)
        {
          error = $"Scenario '{scenarioIdentifier}' cannot start: players holding a declared role are not targeted "
                  + $"[{string.Join(", ", missing)}]. Start it for every participant with '@a', "
                  + "or use '/scenario execute <target> <scenario> <entrypoint>' to re-join a single player.";
          return false;
        }
      }

      if (hasEntrypoint
          && !ScenarioController.TryFindManualEntrypoint(graph, entrypointIdentifier, out _))
      {
        var available = ScenarioController.CollectManualEntrypointIdentifiers(graph);
        error = available.Count == 0
          ? $"Scenario '{scenarioIdentifier}' declares no manual entrypoint."
          : $"Manual entrypoint '{entrypointIdentifier}' not found. Available: {string.Join(", ", available)}";
        return false;
      }

      // 진입 지점을 지정한 시작은 다시 들어온 참가자만을 대상으로 한다. 이미 그래프를 돌고 있는
      // 서버(호스트) 자신을 대상에 넣으면 호스트의 진행과 서버 측 신호 상태가 통째로 초기화되어
      // 나머지 인원의 진행까지 잃는다.
      if (hasEntrypoint
          && ScenarioController.Instance != null
          && ScenarioController.Instance.HasActiveScenario)
      {
        var localConnection = InstanceFinder.ClientManager?.Connection;
        int removed = localConnection != null
          ? resolvedTargets.RemoveAll(target => target.ClientId == localConnection.ClientId)
          : 0;
        if (removed > 0 && resolvedTargets.Count == 0)
        {
          error = "The host is already running a scenario; a re-entry start must target the rejoined player only.";
          return false;
        }
      }

      // G-8 P3: Relay가 있는 현재 구성에서는 서버가 그래프를 한 번만 실행한다.
      // TargetRpc는 더 이상 각 클라이언트의 독립 상태기를 시작하는 데 쓰이지 않고,
      // 표시 전용 세션 준비에만 사용된다. Relay가 없는 레거시 씬은 하위호환을 위해
      // 기존 대상별 로컬 실행 경로를 유지한다.
      // 진입 지점이 지정된 시작은 대상별 상태기가 필요하므로 서버 권위 실행을 시도하지 않는다.
      int ownerId = resolvedTargets[0].ClientId >= 0 ? (int)resolvedTargets[0].ClientId : -1;
      if (!hasEntrypoint
          && ScenarioNetworkRelay.TryStartAuthoritativeScenario(scenarioIdentifier, ownerId, resolvedTargets))
        return true;

      foreach (var target in resolvedTargets)
      {
        int targetOwnerId = target.ClientId >= 0 ? (int)target.ClientId : -1;
        TargetRunScenario(
          target,
          scenarioIdentifier,
          targetOwnerId,
          ScenarioGameRules.AllowMultipleRoleBranchesForSinglePlayer,
          hasEntrypoint ? entrypointIdentifier.Trim() : string.Empty);
      }

      return true;
    }

    public bool TrySetValidatorBlockLogTargets(ScenarioValidatorBlockLogTarget targets, out string error)
    {
      error = string.Empty;

      if (!IsServerInitialized)
      {
        error = "Validator block logging can only be changed on the server.";
        return false;
      }

      var controller = ScenarioController.Instance;
      if (controller == null)
      {
        error = "ScenarioController instance is not available.";
        return false;
      }

      controller.ValidatorBlockLogTargets = targets;
      SyncValidatorBlockLogTargetsObserversRpc((int)targets);
      return true;
    }

    public bool TryDispatchTitle(
      IEnumerable<NetworkConnection> targets,
      string title,
      string subtitle,
      out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetShowTitle(target, title, subtitle),
        out error);
    }

    public bool TryDispatchSubtitle(IEnumerable<NetworkConnection> targets, string subtitle, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetShowSubtitle(target, subtitle),
        out error);
    }

    public bool TryDispatchActionbar(IEnumerable<NetworkConnection> targets, string actionbar, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetShowActionbar(target, actionbar),
        out error);
    }

    public bool TryDispatchTitleClear(IEnumerable<NetworkConnection> targets, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetClearTitle(target),
        out error);
    }

    public bool TryDispatchTitleReset(IEnumerable<NetworkConnection> targets, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetResetTitle(target),
        out error);
    }

    public bool TryDispatchTitleTimes(IEnumerable<NetworkConnection> targets, int fadeInTicks, int stayTicks, int fadeOutTicks, out string error)
    {
      return DispatchToTargets(
        targets,
        target => TargetSetTitleTimes(target, fadeInTicks, stayTicks, fadeOutTicks),
        out error);
    }

    public bool TryDispatchProblemSheet(
      string problemSetIdentifier,
      IEnumerable<NetworkConnection> targets,
      int startIndex,
      bool singleProblemMode,
      out string error)
    {
      error = string.Empty;

      if (!IsServerInitialized)
      {
        error = "ProblemSheet execution can only be invoked on the server.";
        return false;
      }

      if (!Registry.Registry.PreloadProblemSet(problemSetIdentifier))
      {
        error = $"Problem set '{problemSetIdentifier}' is not registered.";
        return false;
      }
      if (!Registry.Registry.TryGetProblemSet(problemSetIdentifier, out var set, out _)
          || set?.Problems == null || set.Problems.Count == 0)
      {
        error = $"Problem set '{problemSetIdentifier}' is invalid.";
        return false;
      }

      if (targets == null)
      {
        error = "No target players were matched.";
        return false;
      }

      bool anyTarget = false;
      int safeStartIndex = Mathf.Max(0, startIndex);
      foreach (var target in targets)
      {
        if (target == null)
          continue;

        anyTarget = true;
        _lastProblemSheetGradeByClientId[target.ClientId] = 1;
        if (!_issuedProblemsByClientId.TryGetValue(target.ClientId, out var issued))
        {
          issued = new HashSet<string>(StringComparer.Ordinal);
          _issuedProblemsByClientId[target.ClientId] = issued;
        }
        int lastIndex = singleProblemMode ? safeStartIndex : set.Problems.Count - 1;
        for (int index = safeStartIndex; index <= lastIndex && index < set.Problems.Count; index++)
          issued.Add(BuildIssuedProblemKey(problemSetIdentifier, index));
        TargetRunProblemSheet(target, problemSetIdentifier, safeStartIndex, singleProblemMode);
      }

      if (!anyTarget)
      {
        error = "No target players were matched.";
        return false;
      }

      return true;
    }

    public void ReportProblemAnswer(string problemSetIdentifier, int problemIndex,
      int selectedChoiceIndex, string shortAnswer)
    {
      if (string.IsNullOrWhiteSpace(problemSetIdentifier) || problemIndex < 0)
        return;

      if ((shortAnswer?.Length ?? 0) > 256)
        return;
      ReportProblemGradeServerRpc(problemSetIdentifier, problemIndex, selectedChoiceIndex, shortAnswer);
    }

    private static string BuildIssuedProblemKey(string problemSetIdentifier, int problemIndex)
      => $"{problemSetIdentifier}\u001f{problemIndex}";

    public int GetLastProblemSheetGradeCode(NetworkConnection sender)
    {
      if (sender == null)
        return 1;

      return _lastProblemSheetGradeByClientId.TryGetValue(sender.ClientId, out int code)
        ? (code == 0 ? 0 : 1)
        : 1;
    }

    public bool TryExecuteSystemCommand(string commandLine, out string result)
    {
      return TryExecuteSystemCommand(commandLine, null, out result);
    }

    /// <summary>
    /// 서버 권한으로 커맨드를 실행한다. <paramref name="executionContext"/>는 대상 셀렉터(@s 등)
    /// 해결에만 사용되며, 권한 검사는 수행되지 않는다(시스템 권한 실행). 실행 중 커맨드 정의가
    /// 발생시킨 시스템 메시지는 플레이어 채팅창이 아닌 서버 로그로 기록된다.
    /// 시나리오 ExecuteCommand 노드처럼 "서버가 특정 플레이어를 대신해" 실행하는 경우
    /// 해당 플레이어의 연결을 컨텍스트로 전달한다.
    /// </summary>
    public bool TryExecuteSystemCommand(string commandLine, NetworkConnection executionContext, out string result)
    {
      // 서버 콘솔 커맨드 로그 기록
      GameLogService.WriteCommand($"/{commandLine} (by server)", "server");

      _systemExecutionContextDepth++;
      try
      {
        return TryExecuteCommandInternal(commandLine, executionContext, out result, bypassPermissionCheck: true);
      }
      finally
      {
        _systemExecutionContextDepth--;
      }
    }

    private bool TryExecuteCommandInternal(string commandLine, NetworkConnection sender, out string result, bool bypassPermissionCheck = false)
    {
      if (_commandService == null)
      {
        result = "ChatCommandService is not available.";
        SendSystemMessage(sender, result);
        return false;
      }

      if (!TryExecuteCommandLineWithPipeline(commandLine, sender, out var outputValues, out var error, out bool alreadyReported, bypassPermissionCheck))
      {
        result = string.IsNullOrWhiteSpace(error) ? "Command execution failed." : error;

        // 커맨드 계층이 오류를 사용자에게 이미 보여주지 않았을 때만 여기서
        // 오류를 노출한다. 시스템 메시지가 켜진 채 실행되는 커맨드는 자신의
        // 사용법/오류를 스스로 보고하므로, 다시 보내면 출력이 중복된다.
        if (!alreadyReported)
          SendSystemMessage(sender, result);

        return false;
      }

      result = outputValues.Count > 0
        ? string.Join(", ", outputValues)
        : "Executed command.";
      return true;
    }

    private bool TryExecuteCommandLineWithPipeline(string commandLine, NetworkConnection sender, out List<string> outputValues, out string error, out bool alreadyReported, bool bypassPermissionCheck = false)
    {
      outputValues = new List<string>();
      error = string.Empty;
      alreadyReported = false;

      string trimmed = commandLine?.Trim();
      if (string.IsNullOrWhiteSpace(trimmed))
      {
        error = "Usage: /help";
        return false;
      }

      string[] stages = SplitAndTrim(trimmed, '|', removeEmpty: false);
      if (stages.Length == 0)
      {
        error = "Usage: /help";
        return false;
      }

      bool suppressFirstStageMessages = stages.Length > 1;
      if (!TryExecuteParallelStage(stages[0], sender, suppressFirstStageMessages, out outputValues, out error, out alreadyReported, bypassPermissionCheck))
        return false;

      for (int i = 1; i < stages.Length; i++)
      {
        if (!TryExecutePipeTargetStage(stages[i], outputValues, sender, out outputValues, out error, out alreadyReported, bypassPermissionCheck))
          return false;
      }

      return true;
    }

    private bool TryExecuteParallelStage(
      string stage,
      NetworkConnection sender,
      bool suppressSystemMessages,
      out List<string> outputValues,
      out string error,
      out bool alreadyReported,
      bool bypassPermissionCheck = false)
    {
      outputValues = new List<string>();
      error = string.Empty;
      alreadyReported = false;

      var commands = SplitAndTrim(stage, '&', removeEmpty: true);
      if (commands.Length == 0)
      {
        error = "Invalid command stage.";
        return false;
      }

      for (int i = 0; i < commands.Length; i++)
      {
        if (!TryExecuteSingleCommand(commands[i], sender, suppressSystemMessages, out var values, out error, out alreadyReported, bypassPermissionCheck))
          return false;

        if (values != null && values.Count > 0)
          outputValues.AddRange(values);
      }

      return true;
    }

    private bool TryExecutePipeTargetStage(
      string stage,
      IReadOnlyList<string> inputValues,
      NetworkConnection sender,
      out List<string> outputValues,
      out string error,
      out bool alreadyReported,
      bool bypassPermissionCheck = false)
    {
      outputValues = new List<string>();
      error = string.Empty;
      alreadyReported = false;

      if (string.IsNullOrWhiteSpace(stage))
      {
        error = "Invalid pipeline target stage.";
        return false;
      }

      int placeholderCount = CountPlaceholders(stage);
      if (placeholderCount > 0)
      {
        int inputCount = inputValues?.Count ?? 0;
        if (inputCount < placeholderCount)
        {
          error = $"Pipeline requires {placeholderCount} values but received {inputCount}.";
          return false;
        }

        string resolved = stage;
        for (int i = 0; i < placeholderCount; i++)
        {
          resolved = ReplaceFirstPlaceholder(resolved, inputValues[i]);
        }

        stage = resolved;
      }

      if (!TryExecuteParallelStage(stage, sender, suppressSystemMessages: false, out outputValues, out error, out alreadyReported, bypassPermissionCheck))
        return false;

      return true;
    }

    private static string[] SplitAndTrim(string input, char separator, bool removeEmpty)
    {
      if (string.IsNullOrEmpty(input))
        return Array.Empty<string>();

      var parts = new List<string>();
      var current = new StringBuilder();
      bool inString = false;
      bool escaped = false;
      foreach (char character in input)
      {
        if (character == '"' && !escaped)
          inString = !inString;
        if (character == separator && !inString)
        {
          parts.Add(current.ToString().Trim());
          current.Clear();
          escaped = false;
          continue;
        }
        current.Append(character);
        escaped = character == '\\' && !escaped;
        if (character != '\\')
          escaped = false;
      }
      parts.Add(current.ToString().Trim());

      if (!removeEmpty)
        return parts.ToArray();

      var filtered = new List<string>(parts.Count);
      for (int i = 0; i < parts.Count; i++)
      {
        if (!string.IsNullOrEmpty(parts[i]))
          filtered.Add(parts[i]);
      }

      return filtered.ToArray();
    }

    /// <summary>인용 문자열 내부의 공백을 보존하여 명령 인자를 토큰화한다.</summary>
    private static string[] TokenizeCommandLine(string commandLine)
    {
      if (string.IsNullOrWhiteSpace(commandLine))
        return Array.Empty<string>();

      var tokens = new List<string>();
      var current = new StringBuilder();
      bool inString = false;
      bool escaped = false;
      foreach (char character in commandLine)
      {
        if (character == '"' && !escaped)
          inString = !inString;
        if (char.IsWhiteSpace(character) && !inString)
        {
          if (current.Length > 0)
          {
            tokens.Add(current.ToString());
            current.Clear();
          }
          escaped = false;
          continue;
        }
        current.Append(character);
        escaped = character == '\\' && !escaped;
        if (character != '\\')
          escaped = false;
      }
      if (current.Length > 0)
        tokens.Add(current.ToString());
      return tokens.ToArray();
    }

    private bool TryExecuteSingleCommand(
      string commandLine,
      NetworkConnection sender,
      bool suppressSystemMessages,
      out IReadOnlyList<string> pipelineValues,
      out string error,
      out bool alreadyReported,
      bool bypassPermissionCheck = false)
    {
      pipelineValues = Array.Empty<string>();
      error = string.Empty;
      alreadyReported = false;

      if (string.IsNullOrWhiteSpace(commandLine))
      {
        error = "Usage: /help";
        return false;
      }

      string[] parts = TokenizeCommandLine(commandLine);
      if (parts.Length == 0)
      {
        error = "Usage: /help";
        return false;
      }

      string command = parts[0];
      string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

      bool handled = _commandService.TryExecute(command, args, sender, suppressSystemMessages, bypassPermissionCheck, out pipelineValues, out error);
      if (!handled)
      {
        error = $"Unknown command: {command}";
        return false;
      }

      if (!string.IsNullOrWhiteSpace(error))
      {
        // 커맨드가 존재하고 실행되었다. 시스템 메시지가 켜져 있으면 커맨드가
        // 이미 자체 오류/사용법 출력을 사용자에게 전달했으므로, 호출자가 다시
        // 출력해서는 안 된다(힌트 중복 방지).
        alreadyReported = !suppressSystemMessages;
        return false;
      }

      return true;
    }

    private static int CountPlaceholders(string input)
    {
      if (string.IsNullOrEmpty(input))
        return 0;

      int count = 0;
      for (int i = 0; i < input.Length - 1; i++)
      {
        if (input[i] == '{' && input[i + 1] == '}')
        {
          count++;
          i++;
        }
      }

      return count;
    }

    private static string ReplaceFirstPlaceholder(string input, string value)
    {
      int index = input.IndexOf("{}", StringComparison.Ordinal);
      if (index < 0)
        return input;

      return input.Substring(0, index)
             + (value ?? string.Empty)
             + input.Substring(index + 2);
    }

    /// <summary>서버에서 모든 접속자에게 시스템 메시지를 채팅으로 전파합니다.</summary>
    [Server]
    public void BroadcastSystemMessage(string message)
    {
      if (string.IsNullOrWhiteSpace(message))
        return;

      ReceiveChatObserversRpc($"<color=#FFD700>[System]</color> {message}");
    }

    public string GetDisplayName(NetworkConnection conn)
    {
      if (conn == null)
        return "Server";

      if (UserDescriptorService.TryGetByClientId(conn.ClientId, out var descriptor)
          && !string.IsNullOrWhiteSpace(descriptor.DisplayName))
      {
        return descriptor.DisplayName;
      }

      // 플레이어 스폰 전 등 설명자가 아직 등록되지 않은 경우에만 연결 ID를 예비값으로 사용한다.
      return conn.ClientId.ToString();
    }

    private TitleUIController GetTitleUIController()
    {
      return Registry.Registry.Get<TitleUIController>(RegistryType.UI, Registry.Registry.TypeKey<TitleUIController>());
    }

    private bool DispatchToTargets(
      IEnumerable<NetworkConnection> targets,
      System.Action<NetworkConnection> dispatch,
      out string error)
    {
      error = string.Empty;

      if (!IsServerInitialized)
      {
        error = "Title command can only be invoked on the server.";
        return false;
      }

      if (targets == null)
      {
        error = "No target players were matched.";
        return false;
      }

      bool anyTarget = false;
      foreach (var target in targets)
      {
        if (target == null)
          continue;

        anyTarget = true;
        dispatch?.Invoke(target);
      }

      if (!anyTarget)
      {
        error = "No target players were matched.";
        return false;
      }

      return true;
    }
    #endregion
  }
}
