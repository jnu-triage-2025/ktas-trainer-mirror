# 시나리오 수명주기 및 클린업

## 목적

시나리오를 명시적으로 종료하거나 재시작할 수 있어야 하며, 실행 중 시나리오가 생성하거나 변경한 값이 다음 실행에 남지 않아야 합니다.

## 그래프 명세

`Lifecycle` 노드는 다음 `operation` 값을 지원합니다.

| 값 | 동작 |
| --- | --- |
| `Cleanup` | 선택된 클린업을 수행한 뒤 `nextIdentifier`로 진행합니다. |
| `End` | 선택된 클린업을 수행하고 시나리오를 종료합니다. |
| `Restart` | 선택된 클린업을 수행하고 같은 그래프를 처음 또는 `restartEntrypointIdentifier`에서 다시 시작합니다. |

`revertTrackedChanges`의 기본값은 `true`이며, 실행 저널에 기록된 변경을 역순으로 되돌립니다. `clearRuntimeState`의 기본값도 `true`이며, 신호·카운터·타이머·바인딩 같은 런타임 상태를 비웁니다. `EndScenario`와 `/scenario end`, `/scenario restart`도 같은 정리 경로를 사용합니다.

## 역추적 저널

컨트롤러는 변경 직전에 되돌리기 작업을 저널에 push합니다. 클린업은 마지막 작업부터 pop하여 실행하므로, 같은 값이 여러 번 변경된 경우에도 가장 최근 변경부터 원래 상태까지 정확한 순서로 복원됩니다.

현재 저널 대상은 시나리오 상태 저장소와 플레이어 태그 변경입니다. 시나리오 소유 NPC·waypoint, 신호·타이머·조건 리스너·상태 바인딩은 기존의 소유 객체 및 런타임 정리 경로에서 함께 제거합니다.

## 작성 규칙

- 재시작 가능 지점은 `ManualEntrypoint`로 선언하고 `restartEntrypointIdentifier`에 그 별칭을 입력합니다.
- 종료 전에 별도의 정리 단계를 표현해야 하면 `Lifecycle/Cleanup`을 배치하고, 이후 `Lifecycle/End`로 연결합니다.
- `End`와 `Restart`는 `nextIdentifier`를 사용하지 않습니다.
