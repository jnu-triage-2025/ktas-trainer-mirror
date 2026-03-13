# 2026-03-10 변경 노트: SceneItemPlacement 작성/시각화 개선

## 요약

이번 변경은 씬 authored 아이템 배치 워크플로를 문서와 에디터 양쪽에서 정리하는 작업입니다.
핵심은 다음과 같습니다.

1. `SceneItemPlacement`의 Scene 패널 시각화 옵션 추가
2. Scene 카메라 기준 표시 범위 설정 지원
3. 관련 사용자 가이드/아이템 문서 최신화

---

## 1) Scene 패널 표시 옵션 추가

새 메뉴가 추가되었습니다.

- `Tools/Multiplayer Infrastructure/Scene Item Visualization/Show In Scene View`
- `Tools/Multiplayer Infrastructure/Scene Item Visualization/Settings...`

### 동작

- `Show In Scene View`
  - `SceneItemPlacement`의 Gizmo와 라벨 전체 표시·숨김 토글
- `Settings...`
  - 현재 Scene 카메라 기준으로 표시 범위를 설정
  - 설정 범위 안에 있는 배치만 Scene 패널에 표시
  - 범위가 `0`이면 거리 제한 없이 항상 표시

---

## 2) SceneItemPlacement Gizmo 표시 규칙

`SceneItemPlacement`는 Scene 패널에서 다음 정보를 제공합니다.

- identifier가 설정된 경우: 초록 구 + 초록 라벨
- identifier가 비어 있는 경우: 빨간 구 + 빨간 경고 라벨
- 라벨 형식: `itemIdentifier xStackCount`

이제 씬에 많은 아이템이 배치되어 있어도, 현재 작업 중인 카메라 주변만 보이도록 제한할 수 있습니다.

---

## 3) 문서 반영 대상

다음 문서가 최신 동작을 반영하도록 갱신되었습니다.

- `Documents/item.md`
- `Documents/item-authoring.md`
- `Documents/item-implementation-guide.md`
- `Documents/multiplayer-infrastructure-guide.md`

추가된 내용:

- `SceneItemPlacement`의 Scene 패널 시각화 규칙
- 메뉴 경로
- Scene 카메라 기준 표시 범위 옵션
- 범위 `0`의 의미(항상 표시)

---

## 4) 현재 권장 작성 흐름

1. 빈 GameObject를 만든다.
2. `SceneItemPlacement`를 붙인다.
3. Item 드롭다운에서 identifier를 선택한다.
4. 필요 시 `Stack Count`를 설정한다.
5. Scene 패널이 복잡하면 시각화 메뉴에서 표시를 끄거나 표시 범위를 줄인다.

---

## 참고

이 변경은 런타임 스폰 규칙 자체를 바꾸는 것이 아니라, 이미 도입된 **server-authoritative scene/world item 구조**를 더 쉽게 authoring/검수할 수 있도록 돕는 에디터 기능과 문서 보강입니다.
