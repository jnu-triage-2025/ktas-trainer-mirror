---
title: "크로스헤어 UI"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 크로스헤어 UI

| 항목 | 내용 |
| :-: | :-: |
| ID | (`miui_crosshair`) |
| 깊이 | 0 |
| (*깊이 1인 경우), 다른 깊이 1의 UI 대체 표시 가능 여부 | 해당없음 |
| 진입 시 마우스 잠금 | 잠금 |
| 진입조건 | - 로컬 플레이어 시작 시 `SetCrosshairVisible(true)` 호출 |
| 탈출조건 | - 없음. 코드로 제어 (`SetCrosshairVisible(false)`) |

## 개요

화면 중앙 조준점(HUD)을 표시하는 상시성 UI입니다.

## 상세

크로스헤어 UI는 `CrosshairUIController`가 `UIDocument` 루트에서 `crosshair-root` 요소를 찾아 표시 상태만 제어합니다. 레이캐스트 판정이나 상호작용 로직은 수행하지 않으며, 시각 요소 표시 책임만 가집니다.

로컬 플레이어 시작 시 `PlayerController`가 컨트롤러를 찾아 `SetCrosshairVisible(true)`를 호출해 기본 표시 상태를 보장합니다. 필요 시 다른 시스템에서 `SetCrosshairVisible(false)`를 호출해 숨길 수 있습니다.

깊이 0 HUD로서 오버레이 스택에 참여하지 않고, 마우스 잠금 상태를 변경하지 않습니다.
