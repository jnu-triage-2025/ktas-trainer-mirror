---
title: "ESC 메뉴 UI"
doc_type: requirement
status: active
updated: 2026-04-14
---

# ESC 메뉴 UI

| 항목 | 내용 |
| :-: | :-: |
| ID | (`miui_escape_menu`) |
| 깊이 | 1 |
| (*깊이 1인 경우), 다른 깊이 1의 UI 대체 표시 가능 여부 | 불가 |
| 진입 시 마우스 잠금 | 해제 |
| 진입조건 | - 오버레이 스택이 비어 있을 때 키보드 `Esc` 입력<br>- 이 `id`로 UI를 쿼리하여 오버레이 스택 push |
| 탈출조건 | - 키보드 `Esc` 재입력 (top일 때 pop)<br>- `Resume` 버튼 클릭 |

## 개요

게임 일시 메뉴 진입점으로, 키 설정/그래픽 설정/타이틀 복귀 액션을 제공합니다.

## 상세

ESC 메뉴는 `GameEscapeMenuUIController`가 `UIOverlayStack` 기반으로 열고 닫습니다. `Esc` 입력으로 열릴 때 플레이어 입력 잠금과 커서 해제가 동반되며, 최상단에서 `Esc` 또는 `Resume`으로 닫힙니다.

메뉴 내부에서 `Key Config` 또는 `Graphics Settings` 버튼을 누르면 각각의 깊이 2 UI를 오버레이 스택에 push합니다. 이로 인해 ESC 메뉴 위에 하위 설정창이 중첩 표시됩니다.

`Title` 버튼은 네트워크 상태를 정리한 뒤 인트로 씬으로 전환합니다. 따라서 본 UI는 단순 표시 메뉴를 넘어 인게임 종료 경로의 허브 역할을 수행합니다.
