---
title: "Datapack 작성 가이드"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

# 데이터팩 작성 가이드

데이터팩은 텍스트 JSON 파일이며 파일명은 반드시 `*.datapack.json`이어야 한다.

## 저장 위치

개발 기본 팩은 다음 Unity 프로젝트 경로에 둔다.

`Assets/StreamingAssets/DataPacks/`

게임 실행 시 이 파일들은 다음 런타임 폴더로 복사된다.

`{Application.persistentDataPath}/DataPacks/` (`GameLogs`와 같은 위치)

사용자가 추가한 파일도 런타임 폴더의 하위 폴더에서 재귀적으로 검색된다.

기본 세션에서 활성화할 데이터팩은 [session.config.json](../../../Assets/StreamingAssets/Session/session.config.json)의 `datapacks` 배열에 적는다. 현재 기본값은 `usability`이다.

## JSON 형식

구조 검증에는 [datapack.scheme.json](../../../Assets/Modules/MultiplayerInfrastructure/Resources/Schema/datapack.scheme.json)을 사용한다.

```json
{
  "packId": "default-training",
  "displayName": "기본 훈련 규칙",
  "gameRules": [
    { "name": "runningSpeedMultiplier", "value": "1.5" }
  ],
  "commandAliases": [
    { "name": "rules", "target": "/gamerule" }
  ],
  "periodicCommands": [
    { "command": "/help", "intervalSeconds": 30, "runImmediately": false }
  ],
  "eventHandlers": [
    { "eventIdentifier": "training:on-start", "command": "/help" }
  ]
}
```

`packId`는 팩 간에 유일해야 한다. `displayName`이 없으면 파일명이 UI에 표시된다.

`gameRules`는 서버 세션 시작 시 적용된다. IntroScene의 활성 목록에서 위에 있는 팩이 높은 우선순위를 가지며, 같은 게임 규칙은 낮은 팩부터 높은 팩 순서로 적용된다.

`commandAliases`의 `name`은 `/` 없이 작성한다. 예를 들어 `rules`는 `/rules`가 `/gamerule`을 실행하도록 만든다. 별칭 대상 명령의 권한 검사는 대상 명령에 대해 다시 수행된다.

예를 들어 `usability.datapack.json`은 다음 명령어를 제공한다.

- `/nurse_a` → 자기 자신에게 `nurse_a` 태그 추가
- `/nurse_b` → 자기 자신에게 `nurse_b` 태그 추가
- `/nurse_c` → 자기 자신에게 `nurse_c` 태그 추가
- `/nurse_d` → 자기 자신에게 `nurse_d` 태그 추가

## UI 동작

IntroScene에서 `플레이` 화면의 `데이터 팩...` 버튼을 누르면 선택 창이 열린다.

- 좌측: 인식된 비활성 팩
- 우측: 활성 팩
- 우측 목록의 위쪽: 높은 우선순위
- 올바른 JSON이 아니어도 파일은 목록에 표시되지만 활성화할 수 없음

완료 시 활성 팩 ID 목록이 세션 시작 요청에 저장된다.

## 검증 범위

JSON Schema는 필드 타입, 필수 필드, 명령어 문자열의 기본 형태를 검증한다. 다음 항목은 런타임 검증 영역이다.

- `packId` 중복
- 존재하지 않는 게임 규칙
- 존재하지 않는 별칭 대상 명령
- 별칭 순환 호출
- 서로 다른 파일에서 같은 이벤트를 덮어쓰는 경우

따라서 스키마 통과만으로 게임 내 실행 성공이 보장되지는 않는다.
