# API 레퍼런스: `MultiplayerInfrastructure.Scenario.ScenarioServerInternalSignalRegistry`

> **네임스페이스:** `MultiplayerInfrastructure.Scenario`  
> **형태:** `static class`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioServerInternalSignalRegistry.cs`

---

## 0. 문서 목적

이 클래스는 서버 내부 신호를 register/resolve 방식으로 매칭하는 전용 레지스트리입니다.
서버 내부에서 특정 플레이어 또는 서버 자신에게만 귀속되는 흐름을 다룰 때 사용합니다.

---

## 1. 공개 API

```csharp
public const string SelfTarget = "@s"
public const string ServerTarget = "@m"
public static string NormalizeTarget(string targetId)
public static bool Register(string targetId, string signalId, Action onResolved)
public static bool Resolve(string targetId, string signalId)
public static void Clear(string targetId, string signalId)
public static void ClearAll()
```

---

## 2. 동작 개요

- `Register(targetId, signalId, onResolved)`
  - 수신자가 먼저 대기를 걸 때 사용합니다.
  - 같은 대상/신호가 이미 `Resolve` 된 상태면 즉시 콜백을 실행합니다.
  - 아니면 대기열에 보관합니다.
- `Resolve(targetId, signalId)`
  - 발신자가 먼저 신호를 넣을 때 사용합니다.
  - 같은 대상/신호를 기다리는 수신자가 있으면 즉시 콜백을 실행합니다.
  - 아니면 pending 신호로 보관합니다.

둘 중 어느 쪽이 먼저 오더라도 FIFO 순서로 매칭됩니다.

---

## 3. 대상 식별자

- `@s`: 실행자 자신
- `@m`: 서버 권위 대상
- 그 외 문자열: 일반 플레이어 또는 시스템 대상 식별자

`NormalizeTarget` 은 빈 문자열을 `@m`으로 정규화합니다.

---

## 4. 사용 예시

```csharp
ScenarioServerInternalSignalRegistry.Register(
  ScenarioServerInternalSignalRegistry.ServerTarget,
  "enter_treatment_ready",
  () => Debug.Log("ready"));

ScenarioServerInternalSignalRegistry.Resolve(
  ScenarioServerInternalSignalRegistry.ServerTarget,
  "enter_treatment_ready");
```

위 예시는 수신 대기가 먼저든 발신이 먼저든 동일하게 동작합니다.
