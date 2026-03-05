# TTS

derived from Supertonic

Add TTS Support
uses supertonic

- static / dynamic 으로 나누어 생성하도록 함
- static의 경우, 자료가 존재하지 않거나 수동으로 호출되었을 때 생성
- dynamic의 경우, 실행 과정에서 준비 후 생성

다음의 문장에 대해서, 다음과 같이 정의됨:

> "{환자 A}를 처치실로 이동해야 합니다. 플레이어 B, C, D는 각각 환자 침대의 손잡이를 클릭하여 이동을 준비하십시오."

```json
[
  {
    "identifier": "example-1",
    "basetext": "{patient-a}를 처치실로 이동해야 합니다.",
    "variables": {
      "patient-a": "환자 A"
    }
  },
  {
    "identifier": "example-2",
    "basetext": "{player-b}, {player-c}, {player-d}는 각각 환자 침대의 손잡이를 클릭하여 이동을 준비하십시오.",
    "variables": {
      "player-b": "플레이어 B",
      "player-c": "플레이어 C",
      "player-d": "플레이어 D"
    }
  }
]
```

basetext의 비 변수 부분은 static, 변수 부분은 dynamic.

static 생성 과정 중에 만약 문장 중간에 변수가 있다면, 중간을 잘라서 static 부분만 생성한다. 중간에 끼인 변수가 2개라면 static tts 결과물은 3개가 되어야 한다.  

dynamic은 변경 가능해야 하므로, C# 코드에서 관리되고, 이들의 기본값은 위 json 코드의 `variables` 하위의 값들이다. 이들은 C# 코드에서 런타임으로 생성하는데, 게임 시작 과정(로딩 과정)에서 생성하도록 한다.
