---
title: "다이얼로그 패널 UI"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 다이얼로그 패널 UI

| 항목 | 내용 |
| :-: | :-: |
| ID | (`miui_dialogue_panel`) |
| 깊이 | 1 |
| (*깊이 1인 경우), 다른 깊이 1의 UI 대체 표시 가능 여부 | 불가 |
| 진입 시 마우스 잠금 | 해제 |
| 진입조건 | - 시나리오 시작 시 `StartScenario()` 호출에 의해 진입<br>- 이 `id`로 UI를 쿼리하여 `ShowPanel()`/오버레이 push 호출 |
| 탈출조건 | - 시나리오 종료 시 `EndScenario()` 호출<br>- 없음. 코드로 제어 |

## 개요

시나리오 대화 텍스트 타이핑, 선택지 표시, 선택 처리까지 담당하는 오버레이 UI입니다.

## 상세

다이얼로그 UI는 `DialoguePanelUIController`가 시나리오 시작/종료와 함께 라이프사이클을 관리합니다. 시작 시 `StartScenario()`에서 패널을 표시하고 `UIOverlayStack`에 자신을 push하여 입력/커서 모드를 UI 상태로 전환합니다.

텍스트 노드는 타이핑 효과로 표시되며, 타이핑 중 입력이 들어오면 즉시 완성 표시로 전환됩니다. 선택지 노드는 `InteractableObjectHintUIController`와 연동되어 목록 선택/확정이 이루어집니다.

시나리오 종료 시 `EndScenario()`가 호출되면 선택 상태와 대기 상태를 정리하고 패널을 숨긴 뒤 오버레이 스택에서 pop합니다. 이 과정에서 플레이어 오버레이 모드도 해제됩니다.
