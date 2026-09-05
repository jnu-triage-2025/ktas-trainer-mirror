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
  ],
  "tagDefinitions": [
    { "identifier": "observer", "requiresPermission": false, "description": "참관자 역할" }
  ]
}
```

`packId`는 팩 간에 유일해야 한다. `displayName`이 없으면 파일명이 UI에 표시된다.

`gameRules`는 서버 세션 시작 시 적용된다. IntroScene의 활성 목록에서 위에 있는 팩이 높은 우선순위를 가지며, 같은 게임 규칙은 낮은 팩부터 높은 팩 순서로 적용된다.

`commandAliases`의 `name`은 `/` 없이 작성한다. 예를 들어 `rules`는 `/rules`가 `/gamerule`을 실행하도록 만든다. 별칭 대상 명령의 권한 검사는 대상 명령에 대해 다시 수행된다. `target`에 세미콜론(`;`)으로 여러 명령을 연결하면 순서대로 실행하며, alias에 전달한 추가 인수는 마지막 명령에만 전달된다.

예를 들어 `usability.datapack.json`은 다음 명령어를 제공한다.

- `/nurse_a` → 자기 자신에게 `nurse_a` 태그 추가
- `/nurse_b` → 자기 자신에게 `nurse_b` 태그 추가
- `/nurse_c` → 자기 자신에게 `nurse_c` 태그 추가
- `/nurse_d` → 자기 자신에게 `nurse_d` 태그 추가

`tagDefinitions`는 팩이 활성화된 동안 유효한 플레이어 태그 정의를 선언한다. 각 항목의 `identifier`는 태그 식별자이고, `requiresPermission`은 `/tag add`, `/tag remove`, `/tag change`로 그 태그를 다룰 때 `tag` 권한이 필요한지를 뜻한다. 이 값을 생략하면 권한이 필요한 것으로 간주된다. `requiresPermission`을 `false`로 선언한 태그는 권한이 없는 참가자도 자기 자신이나 다른 대상에게 붙이거나 뗄 수 있으므로, 별칭이 `/tag add @self ...`처럼 태그를 부여하도록 만들 때에는 그 태그를 이 목록에 함께 선언해야 훈련생이 별칭을 실행할 수 있다. 정의되지 않은 태그는 항상 권한을 요구한다.

같은 태그를 여러 팩이 정의하면 우선순위가 높은 팩의 정의가 유효하고, 팩이 비활성화되면 이전 정의로 되돌아간다. `nurse_a`부터 `nurse_d`까지의 간호사 역할 태그는 `TriageTrainer` 모듈의 내장 정의 파일([triage_roles.tags.json](../../../Assets/Modules/TriageTrainer/Resources/Tag/triage_roles.tags.json))에서 이미 권한이 필요 없는 태그로 선언되어 있으므로, 데이터팩이 따로 선언하지 않아도 된다. 내장 정의 파일의 형식과 권한 정책의 자세한 내용은 [Tag 모듈 README](../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Tag/README.md)를 참고한다.

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
