# API 레퍼런스: `MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay`

> **네임스페이스:** `MultiplayerInfrastructure.Scenario`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/ScenarioNetworkRelay.cs`

---

## 0. 문서 목적

`ScenarioNetworkRelay`는 시나리오 게이팅용 **완료 신호(`sig.*`)를 서버 권한(authoritative)으로 모으기 위한 네트워크 중계기**입니다. 다인 협력 시나리오에서 "한 플레이어의 인터랙션 완료가 다른 플레이어 브랜치의 게이트(Validator `waitForCondition`)를 통과시키는" 동작을 가능하게 합니다(제안서 G-8, P1 단계).

배경: 완료 신호는 `ScenarioInteractionSignals`를 통해 `RegistryType.RuntimeState`(정적·비네트워크) 레지스트리에 기록됩니다. 인터랙션은 각 클라이언트 컨텍스트에서 발생하므로, 신호가 클라이언트 로컬에만 남으면 다른 플레이어/서버가 이를 볼 수 없습니다. 본 중계기는 클라이언트 신호를 `ServerRpc`로 서버에 보고하여 서버의 단일 권위 RuntimeState에 기록되게 합니다.

---

## 1. 클래스 개요

```csharp
public sealed class ScenarioNetworkRelay : NetworkBehaviour
```

- FishNet `NetworkBehaviour`. 씬에 **스폰된 네트워크 오브젝트로 1개** 존재해야 합니다(싱글톤).
- `Awake`에서 싱글톤 등록, `OnDestroy`에서 해제.

### `static ScenarioNetworkRelay Instance`
씬에 배치된 중계기 인스턴스. 없으면 `null`.

---

## 2. 공개 정적 메서드

### `RaiseAuthoritative(string normalizedSignalId)`
신호를 권한 경로로 올립니다.

| 컨텍스트 | 동작 |
|---|---|
| 서버(`IsServerStarted`) | `ScenarioInteractionSignals.RegisterLocal` 로 즉시 권위 기록 |
| 클라이언트(`IsClientStarted` + 중계기 존재) | `CmdRaiseScenarioSignal` 로 서버에 보고 |
| 네트워크 비활성/중계기 부재 | 로컬 기록(단일 플레이어/오프라인 폴백) |

> 입력은 **정규화된**(`sig.` 접두사 포함) 식별자여야 합니다. 보통 `ScenarioInteractionSignals.Raise`가 정규화 후 호출합니다.

### `ClearAuthoritative(string normalizedSignalId)`
신호를 권한 경로로 내립니다(사이클 반복 시 재설정). 분기 규칙은 `RaiseAuthoritative`와 동일.

---

## 3. 내부 ServerRpc

```csharp
[ServerRpc(RequireOwnership = false)] private void CmdRaiseScenarioSignal(string normalizedSignalId)
[ServerRpc(RequireOwnership = false)] private void CmdClearScenarioSignal(string normalizedSignalId)
```

`RequireOwnership = false` 이므로 임의 클라이언트가 호출 가능. 서버에서 `RegisterLocal`/`UnregisterLocal` 수행.

---

## 4. 호출 관계

```
게임플레이(Item.OnGet 등)
  └─ ScenarioInteractionSignals.Raise("click_xxx")   // 기존 코드 변경 불필요
       └─ Normalize -> "sig.click_xxx"
            └─ ScenarioNetworkRelay.RaiseAuthoritative("sig.click_xxx")
                 ├─ 서버: RuntimeState 직접 등록
                 └─ 클라: CmdRaiseScenarioSignal -> (서버) RuntimeState 등록
                          └─ Validator(RegistryContains, RuntimeState, sig.click_xxx) 가 서버에서 통과
```

---

## 5. 주의 / 제약

- **씬 배치 필요**: NetworkBehaviour 이므로, 신호 공유가 동작하려면 중계기가 네트워크 스폰되어 있어야 합니다(설정 가이드 참조). 없으면 자동으로 로컬 폴백되어 **기존(클라 로컬) 동작과 동일**합니다(회귀 없음).
- 본 단계(P1)는 **신호 공유**만 담당합니다. 그래프 실행 권위 이전·표현 RPC(P2·P3)는 후속 작업입니다. 따라서 현재는 "호스트가 시나리오를 실행하고 다른 클라가 인터랙션"하는 구성에서 게이팅이 동작합니다(순수 클라 단독 실행의 다인 분배는 P2·P3 필요).
- 관련 제안서: `Agents/Proposals/2026-06-24-scenario-parallel-execution/server-authoritative-execution-spec.md`
