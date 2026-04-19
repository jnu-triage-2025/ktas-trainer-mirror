# LineConnector Sandbox

## 빠른 검증 방법

1. 빈 씬을 열고 빈 GameObject를 하나 만듭니다. (이름 예: `LineConnectorSandboxRoot`)
2. 아래 컴포넌트를 같은 오브젝트에 추가합니다.
   - `IntravenousLineGrounded`
   - `LineConnectorSandboxBootstrap`
3. Play를 누르면 `PointA`, `PointB`가 자동 생성되고 선이 연결됩니다.
4. `LineConnectorSandboxBootstrap`의 `animatePointB`를 켜면 PointB가 좌우로 움직이며 선이 실시간 갱신됩니다.
5. `IntravenousLineGrounded`에서 아래 값을 조절해 확인합니다.
   - `renderMode = TubeMesh` : 고품질 투명 튜브 메쉬 렌더링
   - `lineColor.a = 0.1 ~ 0.2` : 약 10~20% 불투명도
   - `tubeRadius = 0.003 ~ 0.006` : 튜브 반지름
   - `tubeSides = 12 ~ 20` : 원형 단면 품질
   - `tubeSmoothness = 0.85 ~ 1.0` : 하이라이트 선명도
   - `usePhysicsSimulation = true` : 중력 + 충돌 기반 변형 활성화
   - `gravityScale = 0.12 ~ 0.25` : 가벼운 수액 라인 느낌
   - `slackLength = 0.04 ~ 0.08` : 바닥으로 떨어지지 않고 자연스럽게 처짐
   - `velocityDamping = 0.08 ~ 0.15` : 흔들림 감쇠
   - `collideWithWorld = true` : 콜라이더 접촉 반응
   - `collisionRadius = 0.004 ~ 0.007` : 접촉면 맞춤 정도

## 렌더링 모드

- `TubeMesh` : 권장. 실제 튜브처럼 입체 단면이 보입니다.
- `LineRenderer` : 기존 방식. 저비용 디버그/임시 확인용.

## 프리셋 빠른 적용

- `IntravenousLineGrounded` 인스펙터에서 `preset`을 선택합니다.
- 컴포넌트 우측 메뉴에서 `Preset/Apply Selected Preset` 실행 시 즉시 값이 반영됩니다.
- 자주 쓰는 단축 메뉴:
   - `Preset/IV Light`
   - `Preset/Oxygen Line`
   - `Preset/Suction Line`
   - `Preset/Defib Cable`

권장 시작점:

- 수액줄 느낌: `IV Light`
- 더 투명하고 가벼운 라인: `Oxygen Line`
- 두껍고 약간 무거운 라인: `Suction Line`
- 불투명 케이블: `Defib Cable`

## 핑크색(Shader Missing) 대응

- 스크립트가 현재 Render Pipeline(HDRP/URP/Built-in)에 맞는 셰이더를 우선 선택합니다.
- 여전히 핑크색이면 `IntravenousLineGrounded` 오브젝트의 Material을 직접 지정하세요.
- 투명 재질을 직접 만들 때는 Surface를 Transparent로 두고 알파를 0.1~0.2로 맞추세요.

## 콜라이더 변형 팁

- 라인이 닿는 물체에 Collider가 있어야 접촉 변형이 보입니다.
- 너무 딱딱하거나 떨리면 `solverIterations`를 10~12로 올리고 `velocityDamping`를 높이세요.
- 너무 늘어지면 `slackLength`를 낮추고 `gravityScale`를 줄이세요.
- 너무 바닥에 붙으면 `collisionRadius`를 약간 키우고 `gravityScale`를 낮추세요.

## 확인 포인트

- A/B 위치 변경 시 선이 즉시 따라오는지
- A/B 중 하나가 사라지면 선이 비활성화되는지
- `segments`, `tubeRadius`, `tubeSides` 값 변경이 정상 반영되는지
- 콜라이더 표면을 따라 라인이 자연스럽게 밀리며 변형되는지

## 레거시 호환 점검 (TwoPointLineConnector)

- 기존 씬에서 `TwoPointLineConnector`를 그대로 둔 상태로 Play를 실행합니다.
- 라인이 정상 렌더링되면, 래퍼 컴포넌트가 `pointA`/`pointB`를 `IntravenousLineGrounded` 엔드포인트로 자동 바인딩한 것입니다.
- Play 중 `pointA` 또는 `pointB` 오브젝트를 이동했을 때 라인이 즉시 추적되는지 확인합니다.
- 새 배치부터는 `IntravenousLineGrounded` 사용을 권장합니다. `TwoPointLineConnector`는 기존 씬 호환 전용입니다.
