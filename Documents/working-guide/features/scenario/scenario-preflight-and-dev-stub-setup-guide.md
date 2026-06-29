---
title: "Scenario 사전 검증(Preflight) & 개발 스텁 설정 가이드"
domain: "content-definitions.scenario"
progress: "3-implemented"
flags: []
---

## 이 문서는 무엇인가요?

이 가이드는 두 가지 새 기능을 Unity 에디터에서 설정하는 방법을 비전문가 운영자도 따라 할 수 있게
설명합니다.

1. **사전 검증(Preflight)**: 시나리오를 시작할 때, 그 시나리오가 필요로 하는 씬 요소(인터랙션
   대상, 트리거존, 이벤트 핸들러, 아이템, 웨이포인트, 프리셋)가 준비되어 있는지 자동으로 점검하고
   누락을 경고하는 기능입니다.
2. **개발 스텁(Dev Stub)**: IndevScene 같은 개발 씬에서 실제 오브젝트가 없을 때, 간이 원기둥/큐브를
   자동 생성해 흐름을 끝까지 시연할 수 있게 하는 도구입니다.

---

## 1. 사전 검증(Preflight) 설정

### 어디서 설정하나요?

`ScenarioController` 컴포넌트(시나리오를 구동하는 오브젝트)의 인스펙터에 **"Preflight (사전 요구사항
검증)"** 항목이 추가되어 있습니다.

### 설정 항목

- **Preflight Enabled** (기본 켜짐): 사전 검증 기능을 켜고 끕니다. 끄면 기존과 100% 동일하게
  동작합니다.
- **Preflight Policy**
  - **Warn To Console** (기본 켜짐): 누락을 Unity 콘솔에 경고로 출력합니다.
  - **Warn To In Game Chat** (기본 켜짐): 누락을 인게임 채팅창에 경고로 출력합니다.
  - **Missing Behavior** (기본 `Continue With Warning`):
    - `Continue With Warning`: 누락이 있어도 경고만 남기고 시나리오를 정상 시작합니다.
    - `Abort Start`: 누락이 1건이라도 있으면 시나리오를 시작하지 않고 중단합니다.

> 컴포넌트를 처음 추가하면 위 기본값(콘솔/채팅 둘 다 경고 + 계속 진행)으로 자동 설정됩니다.

### 동작 예시

개발 씬에서 `disaster_intro` 시나리오를 시작하면, 인터랙션 대상이 없을 경우 다음과 같은 경고가
콘솔과 채팅에 출력됩니다.

```
[ScenarioPreflight] Scenario 'disaster_intro': 3 missing, 9 unverified.
  - MISSING InteractionTarget 'Triage_zone_Trigger' (node 'I001')
  - MISSING InteractionTarget 'Preproom_Trigger' (node 'I002')
  ...
  - unverified Signal 'sig.click_vital_set' (node 'V001')
  ...
```

- `MISSING`: 반드시 있어야 하는데 씬/레지스트리에서 못 찾은 항목입니다.
- `unverified`: 런타임 신호 등 시작 시점에 존재 여부를 단정할 수 없는 항목입니다(참고용).

---

## 2. 개발 스텁(Dev Stub) 설정

### 어떤 도구인가요?

- **`ScenarioDevStubSpawner`**: 대상 시나리오가 요구하는 인터랙션 대상/트리거존을 간이 오브젝트로
  자동 생성합니다.
- **`ScenarioDevStub`**: 스포너가 생성하는 간이 오브젝트에 붙는 컴포넌트(직접 붙일 일은 거의 없음).

### 설정 순서

1. 개발 씬(IndevScene)에서 빈 GameObject 를 하나 만들고 이름을 예: `DevStubSpawner` 로 짓습니다.
2. 그 오브젝트에 **`ScenarioDevStubSpawner`** 컴포넌트를 추가합니다.
3. 인스펙터에서 다음을 설정합니다.
   - **Scenario Json**: 시연할 시나리오의 `.scenario.json` 파일(예: `disaster_intro.scenario`)을
     끌어다 놓습니다.
   - **Origin**(선택): 생성 오브젝트들이 배치될 기준 위치. 비우면 스포너 자신의 위치를 씁니다.
   - **Spacing / Columns**: 생성 오브젝트 간격과 한 줄에 놓을 개수.
   - **Interactable Scale / Trigger Zone Scale**: 원기둥/큐브의 크기.
   - **Skip Existing** (기본 켜짐): 이미 씬에 실제 오브젝트가 있는 대상은 건너뜁니다.
4. 컴포넌트 우측 상단의 점 3개 메뉴(⋮) 또는 컨텍스트 메뉴에서 **"Spawn Stubs For Scenario"** 를
   실행합니다.
   - 일반 인터랙션 대상 → **원기둥(Cylinder)** 으로 생성됩니다(클릭하면 신호 발생).
   - 식별자에 `trigger`/`zone`/`room` 이 들어간 대상 → **납작하고 넓은 통과형 큐브(Cube)** 로
     생성됩니다(플레이어가 통과하면 신호 발생).
5. 정리하려면 **"Clear Spawned Stubs"** 를 실행합니다.

### 동작 원리(요약)

- 생성된 스텁은 요구 식별자 그대로 레지스트리에 등록되어 사전 검증의 InteractionTarget 점검을
  충족시킵니다.
- 플레이어가 원기둥을 상호작용하거나 큐브를 통과하면, 식별자 기반 신호(`sig.<id>`,
  `sig.click_<id>`, `sig.select_<id>`, `sig.enter_<id>`)를 올려 시나리오 게이트(Validator)를
  통과시킵니다.

> 주의: 개발 스텁은 **개발 씬 전용**입니다. 본 게임 씬에는 배치하지 마세요. 본 게임 씬에서는 실제
> 인터랙션 오브젝트/트리거존이 신호를 올립니다.

---

## 참조

- [scenario-preflight-requirements](../../scenario/scenario-preflight-requirements.md)
- [indev-scene-verification-guide](./indev-scene-verification-guide.md)
- [validator-gate-timeout-setup-guide](./validator-gate-timeout-setup-guide.md)
