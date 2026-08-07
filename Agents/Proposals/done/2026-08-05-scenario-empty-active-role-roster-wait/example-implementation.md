# 예제 구현

```csharp
yield return WaitForInitialActiveRoleRoster(node);
if (_currentGraph == null)
{
  yield break;
}

if (!TryAllocateParallel(node, players, allocation))
{
  EndScenario();
}
```

`WaitForInitialActiveRoleRoster`는 역할 명단 조회가 정상이고 인원이 0명인 동안만 대기한다. 한 명 이상의 역할 보유자가 확인되면 기존 `TryAllocateActiveRoleParallel`이 접속한 역할을 할당하고, 실제 부재 역할은 `skipAbsentRoleBranches` 설정에 따라 생략한다.

단독 디버그 모드에서는 같은 플레이어가 여러 역할 태그를 보유해도 역할별 roster 항목으로 확장한다. `patient_b_c_ct`의 시작 흐름은 다음처럼 구성한다.

```text
P_ANNOUNCE_ARRIVAL (WaitMode.None, ANNOUNCE 실행)
  -> P_ARRIVAL (역할별 Quest_Arrive_Triage 부여 및 도착 신호 대기)
  -> P_TRIAGE
```
