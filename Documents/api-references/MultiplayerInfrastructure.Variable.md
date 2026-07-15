# API 레퍼런스: MultiplayerInfrastructure.Variable.SessionVariableService

> **네임스페이스:** `MultiplayerInfrastructure.Variable`  
> **유형:** 정적 클래스  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/Variable/SessionVariableService.cs`

---

## 0. 개요

`SessionVariableService`는 Minecraft의 Scoreboard 시스템과 유사한 세션 내 정수 변수 저장소입니다.  
**Objective(목표) 단위**로 변수를 구성하며, 각 Objective에서 플레이어별 정수 점수를 기록합니다.

주요 용도:
- `/scoreboard` 커맨드의 백엔드 저장소
- 시나리오 중 플레이어 역할별 점수 집계
- 세션 내 전역 카운터 관리

---

## 1. 설계 원칙

- **서버 전용 변경:** 모든 변경 메서드(`AddObjective`, `SetScore` 등)는 서버에서만 호출 가능합니다. 클라이언트에서 호출하면 경고 후 무시됩니다.
- **Objective 기반:** 변수는 Objective 이름을 범주로, 플레이어 식별자를 키로 하는 2단계 사전에 저장됩니다.
- **비영속적:** 세션 변수는 런타임 메모리에만 존재하며, 세션 종료 시 소멸합니다.
- **오버플로 방지:** 정수 연산 시 `int.MaxValue`/`int.MinValue`에서 포화(saturate)됩니다.

---

## 2. ObjectiveDefinition

```csharp
public sealed class ObjectiveDefinition
{
    public string Name { get; }     // Objective 이름 (대소문자 무관)
    public string Criteria { get; } // Objective 기준/설명 문자열
}
```

---

## 3. Objective 관리

```csharp
// Objective 추가
bool AddObjective(string objective, string criteria, out string error)

// Objective 제거 (해당 Objective의 모든 점수도 제거)
bool RemoveObjective(string objective, out string error)

// 모든 Objective 조회
IReadOnlyCollection<ObjectiveDefinition> GetObjectives()

// Objective 존재 여부 확인
bool ContainsObjective(string objective)
```

---

## 4. 점수 관리

```csharp
// 특정 플레이어의 특정 Objective 점수 조회
bool TryGetScore(string userIdentifier, string objective, out int value)

// 점수 설정 (절대값)
bool SetScore(string userIdentifier, string objective, int value, out string error)

// 점수 추가 (현재값 + delta, 포화 처리)
bool AddScore(string userIdentifier, string objective, int delta, out string error)

// 점수 차감 (현재값 - delta, 포화 처리)
bool RemoveScore(string userIdentifier, string objective, int delta, out string error)

// 특정 플레이어의 특정 Objective 점수 초기화 (항목 제거)
bool ResetScore(string userIdentifier, string objective, out string error)

// 특정 플레이어의 모든 Objective 점수 초기화
bool ResetAllScores(string userIdentifier, out string error)

// 특정 플레이어의 모든 Objective 점수 사전 조회
IReadOnlyDictionary<string, int> GetScoresForUser(string userIdentifier)
```

---

## 5. 연산 적용 (ApplyOperation)

두 플레이어/Objective 간 점수 연산을 한 번에 적용합니다:

```csharp
bool ApplyOperation(
    string targetIdentifier,    // 결과를 저장할 플레이어 식별자
    string targetObjective,     // 결과를 저장할 Objective
    string operation,           // 연산자
    string sourceIdentifier,    // 소스 플레이어 식별자
    string sourceObjective,     // 소스 Objective
    out string error)
```

**지원 연산자:**

| 연산자 | 의미 |
|---|---|
| `+=` | target += source |
| `-=` | target -= source |
| `*=` | target *= source |
| `/=` | target /= source (0 나누기 시 오류) |
| `%=` | target %= source (0 나머지 시 오류) |
| `=` | target = source |
| `<` | target = min(target, source) |
| `>` | target = max(target, source) |
| `><` | target과 source 값 교환 (swap) |

---

## 6. 사용 예시

```csharp
// Objective 생성
SessionVariableService.AddObjective("triage_score", "dummy", out var error);

// 점수 기록
SessionVariableService.SetScore(playerUuid, "triage_score", 10, out error);

// 점수 조회
if (SessionVariableService.TryGetScore(playerUuid, "triage_score", out int score))
{
    Debug.Log($"트리아지 점수: {score}");
}

// 점수 추가
SessionVariableService.AddScore(playerUuid, "triage_score", 5, out error);
```

---

## 7. 관련 문서

- [Commands.md](../guide/Commands.md) — `/scoreboard` 커맨드 사용법
- [MultiplayerInfrastructure.Tag.PlayerTagService.md](./MultiplayerInfrastructure.Tag.PlayerTagService.md) — 플레이어 태그 시스템
