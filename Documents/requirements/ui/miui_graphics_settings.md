---
title: "그래픽 설정 UI"
doc_type: requirement
status: active
updated: 2026-04-14
---

# 그래픽 설정 UI

| 항목 | 내용 |
| :-: | :-: |
| ID | (`miui_graphics_settings`) |
| 깊이 | 2 |
| (*깊이 1인 경우), 다른 깊이 1의 UI 대체 표시 가능 여부 | 해당없음 |
| 진입 시 마우스 잠금 | 해제 |
| 진입조건 | - ESC 메뉴의 `Graphics Settings` 버튼 클릭<br>- 이 `id`로 UI를 쿼리하여 오버레이 스택 push |
| 탈출조건 | - 키보드 `Esc` 입력으로 오버레이 스택 pop<br>- `Close` 버튼 클릭 |

## 개요

텍스처 품질 프리셋 선택과 적용/저장을 담당하는 설정 오버레이 UI입니다.

## 상세

그래픽 설정 UI는 `GraphicsSettingsUIController`가 품질 프리셋 타일(ultra/high/medium/low)을 동적으로 구성하고, 현재 선택 상태를 시각적으로 표시합니다.

ESC 메뉴에서 진입하는 깊이 2 오버레이이며, 열릴 때 현재 `TexturePerformanceService` 상태를 읽어 UI를 동기화합니다. 사용자가 옵션을 변경하면 pending 상태를 유지하고, `적용 및 저장` 시점에 실제 품질 설정을 반영합니다.

닫기(`Close` 또는 `Esc`) 시에는 오버레이 스택에서 pop되며, 상위 ESC 메뉴로 제어가 복귀합니다.