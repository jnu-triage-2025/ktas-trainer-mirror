# Example Implementation: 시나리오 트리거 존 활성 시나리오 게이팅

대상 파일:

- `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioController.cs`
- `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioTriggerZone.cs`

## 1. ScenarioController.cs — 재생 여부 공개 API

`#region Properties` 에 읽기 전용 프로퍼티를 추가한다.

```csharp
/// <summary>
/// 활성화된(재생 중인) 시나리오 그래프가 존재하는지 여부.
/// </summary>
/// <remarks>
/// Local/ServerAuthoritative/ClientPresentation 모든 실행 모드에서 그래프가 시작되어
/// 종료되지 않은 동안 true 이다. <see cref="IsActive"/> 는 현재 노드 실행 상태(_state)
/// 기반이라 클라이언트 표시 모드나 즉시 진행 노드 체인 사이에서는 false 일 수 있으므로,
/// "시나리오가 재생 중인가" 판정에는 이 프로퍼티를 사용해야 한다.
/// </remarks>
public bool HasActiveScenario => _currentGraph != null;
```

- `_currentGraph` 는 `StartScenarioInternal`(서버/로컬)과 `BeginPresentationScenario`(클라이언트
  표시)에서 설정되고, `EndScenario`/`EndPresentationScenario` 에서 해제된다.

## 2. ScenarioTriggerZone.cs — 감지 게이팅

### 2-1. 재생 여부 헬퍼

```csharp
/// <summary>
/// 현재 활성화된(재생 중인) 시나리오가 존재하는지 반환한다.
/// ScenarioController 가 없는 씬에서는 false 로 간주한다.
/// </summary>
private static bool IsScenarioPlaying =>
  ScenarioController.Instance != null && ScenarioController.Instance.HasActiveScenario;
```

### 2-2. TryTrigger — 미재생 게이팅 + 대상별 전용 존 오탐 오류 제거

```csharp
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
  // 재생 중이 아니면 올린 신호가 소비되지 않고 다음 시작 시 초기화되므로 진입을 무시한다
  // (쿨다운/1회 트리거 상태도 소비하지 않는다).
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
```

### 2-3. TryRaisePerEntitySignal — 미재생 게이팅

```csharp
private void TryRaisePerEntitySignal(GameObject other)
{
  if (string.IsNullOrWhiteSpace(_perEntitySignalTemplate) || other == null)
    return;

  // 대상별 신호는 재생 중인 시나리오의 게이트/카운터에서만 소비된다.
  // 재생 중이 아니면 발신하지 않는다(_perEntityRaised 의 재생 전 오염도 방지된다).
  if (!IsScenarioPlaying)
    return;

  // ... 이하 기존 동작 동일 ...
}
```

## 동작 매트릭스

| 존 구성 | 재생 중 | 미재생 |
|---|---|---|
| 그래프 지정(시나리오 시작용) | 시나리오 시작 요청(컨트롤러가 중복 시작 거부) | 시나리오 자동 시작 (기존 유지) |
| 진입 신호 존 | 진입 신호 발신 | 무시 |
| 대상별 신호 존 | 대상별 신호 발신 | 무시 |
| 완전 미구성 | `LogError` (기존 유지) | 무시 |
