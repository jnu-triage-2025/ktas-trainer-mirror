---
title: "세션 설정 파일"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

# 세션 설정 파일

기본 서버 세션 설정은 `Assets/StreamingAssets/Session/session.config.json`에 둔다.

```json
{
  "address": "localhost",
  "port": 7777,
  "sessionName": "MyFishSession",
  "datapacks": ["usability"]
}
```

IntroScene이 열릴 때 `SessionConfigurationService`가 이 파일을 읽는다. 파일이 없거나 유효하지 않으면 코드의 기본값을 사용한다.

- `address`: 호스트 세션의 바인딩/접속 주소
- `port`: 호스트 개방에 사용할 FishNet 포트
- `sessionName`: LAN 검색에 표시할 세션 이름
- `datapacks`: IntroScene에서 별도 선택을 확정하지 않았을 때 활성화할 데이터팩 ID 목록

IntroScene에서 데이터팩 선택을 완료하면 사용자가 선택한 목록이 해당 세션 설정의 기본 목록보다 우선한다.
