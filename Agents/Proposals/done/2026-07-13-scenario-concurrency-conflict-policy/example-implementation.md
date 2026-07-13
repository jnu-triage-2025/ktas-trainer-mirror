# 예시 구현: 시나리오 동시 실행 충돌 정책

> 본 문서는 같은 폴더의 `Feature Proposal - Scenario Concurrency Conflict Policy.md`
> 채택 시 적용할 참조 구현이다. `MultiplayerInfrastructure` 는 재사용 전제 모듈이므로, 실제 반영은
> 인간 작업자 검토·승인 후 진행한다(AGENTS.md). 라인 번호는 2026-07-13 기준(iv_signal_debug 버그
> 수정 반영 후)이며, 반영 시점에 재확인이 필요하다.

## 1) 신규 파일: `Scripts/Scenario/ScenarioConcurrencyConflictPolicy.cs`

`ScenarioPreflightPolicy`/`ScenarioPlayerTagMatchMode` 스타일을 미러링한다.

```csharp
namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 두 개 이상의 시나리오 흐름이 동시에 대화창 계열 UI(Dialogue/Choice/Quiz)를
  /// 점유하려 할 때의 처리 정책.
  /// </summary>
  public enum ScenarioConcurrencyConflictPolicy
  {
    /// <summary>경고만 남기고 그대로 진행한다(undefined behavior 허용). 기본값이자 기존 동작.</summary>
    Warn,

    /// <summary>나중에 대화창을 점유하려 한 흐름을 취소한다(먼저 점유한 흐름 보존).</summary>
    Cancel,

    /// <summary>진행 중인 모든 시나리오를 중단한다(EndScenario).</summary>
    Panic,
  }

  public static class ScenarioConcurrencyConflictPolicyExtensions
  {
    /// <summary>커맨드/직렬화 문자열 파싱(숫자 별칭 허용). 실패 시 false + error.</summary>
    public static bool TryParse(string raw, out ScenarioConcurrencyConflictPolicy value, out string error)
    {
      value = ScenarioConcurrencyConflictPolicy.Warn;
      error = null;
      if (string.IsNullOrWhiteSpace(raw)) { error = "정책 값이 비어 있습니다."; return false; }

      switch (raw.Trim().ToLowerInvariant())
      {
        case "warn": case "0": value = ScenarioConcurrencyConflictPolicy.Warn; return true;
        case "cancel": case "1": value = ScenarioConcurrencyConflictPolicy.Cancel; return true;
        case "panic": case "2": value = ScenarioConcurrencyConflictPolicy.Panic; return true;
        default:
          error = $"알 수 없는 정책 '{raw}'. 사용 가능: warn|cancel|panic.";
          return false;
      }
    }
  }
}
```

## 2) `Scripts/UI/Controllers/DialoguePanelUIController.cs` — 점유자 추적 API

대화창을 점유한 시나리오 흐름(그래프 식별자)을 기록/해제/조회한다.

```csharp
// 필드 추가 (기존 _currentController 근처)
private string _owningGraphIdentifier;

/// <summary>현재 대화창을 점유 중인 그래프 식별자(없으면 null).</summary>
public string CurrentDialogueOwner => _owningGraphIdentifier;

/// <summary>owningGraphIdentifier 이외의 다른 흐름이 대화창을 점유 중이면 true.</summary>
public bool IsDialogueOwnedByOther(string owningGraphIdentifier)
{
  if (string.IsNullOrEmpty(_owningGraphIdentifier)) return false;
  return !string.Equals(_owningGraphIdentifier, owningGraphIdentifier, System.StringComparison.Ordinal);
}

/// <summary>대화창 점유자를 등록/갱신한다.</summary>
public void MarkDialogueOwner(string owningGraphIdentifier)
{
  _owningGraphIdentifier = owningGraphIdentifier;
}
```

`EndScenario()`(현재 파일 231행 부근) 마지막에 점유자 해제를 추가한다:

```csharp
    public void EndScenario()
    {
      _currentController = null;
      _currentNode = null;
      // ... 기존 정리 ...
      _owningGraphIdentifier = null;   // [추가] 점유 해제
      // InteractableHintUI를 일반 모드로 복원
      if (!_interactableHintUI.IsUnityNull())
        _interactableHintUI.ExitDialogueMode();
      // ... 기존 코드 ...
    }
```

> 점유 등록 시점: `EnsureDialogueModeActive()`(638행 부근)는 "실제 대화창 노드 표시"의 단일
> 관문이므로, `ScenarioController` 가 이 노드를 실행하기 직전에 `TryClaimDialogueUI` 로
> 게이트한 뒤 `MarkDialogueOwner` 를 호출하는 구조가 가장 자연스럽다(아래 3 참조).

## 3) `Scripts/Scenario/ScenarioController.cs` — 정책 필드 + 충돌 게이트

### 3-1. 정책 필드 (43~47행 `_preflightPolicy` 인접)

```csharp
    [Header("Concurrency (동시 실행 충돌)")]
    [Tooltip("두 개 이상의 시나리오 흐름이 동시에 대화창 UI를 점유하려 할 때의 처리 정책.")]
    [SerializeField]
    private ScenarioConcurrencyConflictPolicy _concurrencyConflictPolicy = ScenarioConcurrencyConflictPolicy.Warn;

    public ScenarioConcurrencyConflictPolicy ConcurrencyConflictPolicy
    {
      get => _concurrencyConflictPolicy;
      set => _concurrencyConflictPolicy = value;
    }
```

### 3-2. `Reset()` 초기화 (182행 부근)

```csharp
    private void Reset()
    {
      _preflightEnabled = true;
      _preflightPolicy = ScenarioPreflightPolicy.Default;
      _concurrencyConflictPolicy = ScenarioConcurrencyConflictPolicy.Warn; // [추가]
    }
```

### 3-3. 공용 충돌 게이트

```csharp
    /// <summary>
    /// 대화창 UI 점유를 시도한다. 이미 다른 흐름이 점유 중이면 정책을 적용한다.
    /// 반환 true = 점유 성공(계속 진행), false = 이 흐름은 취소(노드/브랜치 중단).
    /// </summary>
    private bool TryClaimDialogueUI()
    {
      if (_uiController.IsUnityNull()) return true;
      string owner = _currentGraph?.Identifier ?? "(unknown)";

      if (!_uiController.IsDialogueOwnedByOther(owner))
      {
        _uiController.MarkDialogueOwner(owner);
        return true;
      }

      string msg = $"[ScenarioController] 대화창 UI 동시 점유 충돌: '{owner}' 가 "
                 + $"'{_uiController.CurrentDialogueOwner}' 점유 중 대화창을 요청함.";
      switch (_concurrencyConflictPolicy)
      {
        case ScenarioConcurrencyConflictPolicy.Warn:
          Debug.LogWarning(msg + " (WARN)");
          AppendSystemChatMessage(msg + " (WARN: 경고 후 계속 진행)");
          _uiController.MarkDialogueOwner(owner); // 기존 동작대로 점유권을 넘겨받음
          return true;
        case ScenarioConcurrencyConflictPolicy.Cancel:
          Debug.LogWarning(msg + " (CANCEL)");
          AppendSystemChatMessage(msg + " (CANCEL: 뒤에 요청한 흐름 취소)");
          return false;
        case ScenarioConcurrencyConflictPolicy.Panic:
          Debug.LogError(msg + " (PANIC)");
          AppendSystemChatMessage(msg + " (PANIC: 전체 시나리오 중단)");
          EndScenario();
          return false;
      }
      return true;
    }
```

### 3-4. UI 점유 노드 실행 진입부에 게이트 삽입

`ExecuteDialogueNode`(656행), `PresentChoice`(717행), `PresentQuiz`(933행 부근)에서 실제
`DisplayDialogue`/`DisplayChoice` 호출 직전에 게이트를 둔다. 예: `ExecuteDialogueNode`

```csharp
    private void ExecuteDialogueNode(ScenarioDialogueNode node)
    {
      _state = State.ExecutingDialogue;
      CancelDialogueAutoAdvance();

      if (!_uiController.IsUnityNull())
      {
        if (!TryClaimDialogueUI())   // [추가] 충돌 시 Cancel/Panic 이면 여기서 중단
        {
          // Cancel: 이 흐름의 대화 노드를 표시하지 않고 조용히 멈춘다(전역 커서 진행 안 함).
          // Panic: EndScenario() 가 이미 호출되어 상태가 Inactive 로 정리됨.
          return;
        }

        _uiController.DisplayDialogue(node.SpeakerName, node.DialogueContent, node.PortraitSpriteIdentifier, node.InteractionRequired);
        // ... 기존 TTS/AutoAdvance ...
      }
      else
      {
        Advance();
      }
    }
```

`PresentChoice`/`PresentQuiz` 도 `DisplayChoice` 호출 직전에 동일하게 `if (!TryClaimDialogueUI()) return;`
를 삽입한다. 브랜치 경로(`RunBranchChain` 내 Choice/Quiz, 2903·2923행 부근)에서는 `return` 대신
해당 브랜치 코루틴을 종료(`yield break`)하고 `_branchPromptActive`/인터셉터를 정리한다.

> 병렬 브랜치 세부: 같은 그래프의 두 브랜치가 동시에 대화창을 요청하면 `owner` 가 같아
> `IsDialogueOwnedByOther` 가 false 가 될 수 있다. 이 경우 `_branchPromptActive`(이미 프롬프트
> 표시 중) 를 함께 검사해 "같은 그래프 내 두 번째 프롬프트"도 충돌로 취급한다:
> `if (_uiController.IsDialogueOwnedByOther(owner) || _branchPromptActive) { ...정책... }`.

## 4) `Scripts/Command/CommandDefinitions/CommandDefinition.Scenario.cs` — 커맨드

`Execute`(36행)의 `signal` 서브커맨드 처리 뒤에 `conflictpolicy` 서브커맨드를 추가한다.

```csharp
      // /scenario conflictpolicy [warn|cancel|panic]
      if (args != null && args.Length >= 1
          && string.Equals(args[0], "conflictpolicy", StringComparison.OrdinalIgnoreCase))
      {
        var controller = ScenarioController.Instance;
        if (controller == null)
        {
          _chat.SendSystemMessage(sender, "ScenarioController 인스턴스를 찾을 수 없습니다.");
          return;
        }

        if (args.Length < 2)
        {
          _chat.SendSystemMessage(sender,
            $"현재 시나리오 동시 충돌 정책: {controller.ConcurrencyConflictPolicy}. "
            + "변경: /scenario conflictpolicy <warn|cancel|panic>");
          return;
        }

        if (!ScenarioConcurrencyConflictPolicyExtensions.TryParse(args[1], out var policy, out var perr))
        {
          _chat.SendSystemMessage(sender, perr);
          return;
        }

        controller.ConcurrencyConflictPolicy = policy;
        _chat.SendSystemMessage(sender, $"시나리오 동시 충돌 정책을 '{policy}' 로 설정했습니다.");
        return;
      }
```

`UsageLines`(19행)에도 한 줄 추가:

```csharp
      new UsageLine("scenario conflictpolicy <warn|cancel|panic>", "Set concurrent-dialogue conflict policy."),
```

## 5) 회귀 안전성 메모

- 대다수 시나리오는 단일 흐름이므로 점유자가 1명뿐 → `IsDialogueOwnedByOther` 가 항상 false →
  게이트가 개입하지 않는다(경로/동작 불변).
- 기본값 `Warn` 은 충돌 시에도 `MarkDialogueOwner` 로 점유권을 넘겨받아 **기존 덮어쓰기 동작을 유지**
  하며, 로그/챗 경고만 추가된다.
- `EndScenario` 에서 점유자 해제를 누락하면 다음 시나리오가 계속 충돌로 오판되므로, 2)의 해제
  추가가 필수다.
```
