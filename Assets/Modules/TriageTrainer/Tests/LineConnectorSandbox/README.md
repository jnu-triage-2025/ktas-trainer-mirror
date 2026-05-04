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

---

## 동공 반응(직접 대광 반사) Sandbox

이번 샌드박스에는 아래 3개 스크립트가 추가되었습니다.

- `PupilReflexPatientSandboxBootstrap`
- `PenlightCursorProbe`
- `PupilReflexEye`

### 빠른 시작

1. 빈 씬에 빈 GameObject를 만들고 이름을 `PupilReflexSandboxRoot`로 지정합니다.
2. `PupilReflexPatientSandboxBootstrap`를 추가합니다.
3. Play를 누르면 2D 얼굴 판(Quad) + 좌/우 3D 안구가 자동 생성됩니다.
4. 마우스 커서가 펜라이트처럼 동작하며, 동공 중심을 지나갈 때만 수축 반응이 발생합니다.
5. 좌/우 눈을 각각 1회 이상 비추면 `Completed` 상태가 `Yes`가 됩니다.

### 안구 구조(레퍼런스 반영)

- 현재 구조는 `공막(Sclera) 구체` + `홍채(Iris) 링 메쉬(중앙 구멍)` + `내부 동공 챔버(Pupil)`입니다.
- 홍채는 `PupilApertureVisual`이 런타임으로 생성하는 링(annulus) 메쉬이며, 중앙이 실제로 뚫려 있습니다.
- 동공 수축은 검은 원 스케일이 아니라 `홍채 구멍 직경` 자체가 변하는 방식으로 반영됩니다.
- `Cornea (Transparent)` 설정으로 안구 표면을 투명 각막처럼 보이게 조정할 수 있습니다.
- `orientIrisTowardMainCamera`를 켜면 홍채가 항상 메인 카메라를 향하게 정렬됩니다.
- 좌/우 위치가 반대로 보이면 `swapEyeSidePlacement`를 켜거나 끄고 확인합니다.
- 동공 중심이 밝게 보이면 `scleraDiameterScale`를 낮추거나 `pupilChamberDepthMeters`를 올려 내부의 어두운 챔버가 더 잘 보이게 조정합니다.
- 화면 좌상단 상태 텍스트에 `Pupil(mm) L/R`가 표시되며, 커서를 동공 위로 통과시키면 값이 `5.00 -> 2.00` 방향으로 변합니다.

### 구현 범위 (이번 작업에 포함)

- 카메라 확대(얼굴 줌인) 연출: **미포함**
- 인벤토리에서 펜라이트 획득 조건: **미포함**
- 동공 반응 핵심(광원 판정/수축/좌우 체크): **포함**

위 두 미포함 항목은 추후 시스템 담당자가 연결할 수 있도록 분리되어 있습니다.

### 직관적 조절 항목 (Inspector)

`PupilReflexPatientSandboxBootstrap`의 `Pupil Reflex Controls (Intuitive)`에서 아래 항목을 바로 조절할 수 있습니다.

- `enablePupilChange`
   - 동공 변화 기능 ON/OFF 전역 스위치입니다.
   - `false`면 빛 판정/체크 로직은 유지되지만 실제 동공 크기 변화는 일어나지 않습니다.
- `pupilDiameterBeforeLightMm`
   - 빛 반응 전(기본 상태) 동공 직경(mm)입니다.
- `pupilDiameterAfterLightMm`
   - 빛 반응 후(수축 목표) 동공 직경(mm)입니다.
- `constrictionDurationSeconds`
   - 수축이 완료되기까지 걸리는 시간입니다.
- `constrictionTaperingSpeed` (`0~1`)
   - 수축 감속(테이퍼링) 강도입니다.
   - `0`: 거의 선형(일정한 느낌)
   - `0.5`: 기본 감속(이전 구현과 유사)
   - `1`: 초반 빠르고 후반 천천히 마무리되는 강한 테이퍼링
- `dilationDurationSeconds`
   - 빛이 사라진 뒤 원래 크기로 돌아오는 시간입니다.

기본 권장값:

- `enablePupilChange = true`
- `pupilDiameterBeforeLightMm = 5`
- `pupilDiameterAfterLightMm = 2`
- `constrictionDurationSeconds = 0.15`
- `constrictionTaperingSpeed = 0.5`
- `dilationDurationSeconds = 0.28`

### 병리(반응 유무/정도) 설정

`PupilReflexPatientSandboxBootstrap`에서 좌/우를 독립적으로 제어할 수 있습니다.

- `leftEyeReactsToLight` / `rightEyeReactsToLight`
   - `false`면 해당 눈은 빛이 닿아도 수축하지 않습니다.
- `leftEyeConstrictionAmount` / `rightEyeConstrictionAmount` (`0~1`)
   - `1`: 정상 수축(5mm -> 2mm)
   - `0`: 수축 없음
   - `0~1`: 부분 수축(예: 저반응)

실무 팁:

- 전역 ON/OFF(`enablePupilChange`)는 "전체 기능 켜기/끄기" 용도입니다.
- 좌/우 반응 차이 시뮬레이션은 `left/rightEyeReactsToLight`, `left/rightEyeConstrictionAmount`로 만듭니다.

### 펜라이트 커서(광원) 설정

`PenlightCursorProbe` 기본값:

- `lightRadiusMillimeters = 3.5`
   - 안구 직경(기본 24mm)보다 충분히 작아, 동공 중심에 맞춰야 반응합니다.
- `requirePrimaryButtonHold`
   - 필요 시 마우스 좌클릭을 누를 때만 광원이 켜지도록 변경할 수 있습니다.

### 완료 판정 및 다음 단계 연계

- 좌/우 눈 모두 1회 이상 직접광 확인 시 완료 처리됩니다.
- `PupilReflexPatientSandboxBootstrap`의 `onBothEyesChecked` UnityEvent로 다음 로직(ValidatorNode/InvokeEvent 등) 연결이 가능합니다.

### 실제 환자 프리팹에 붙일 때 팁

- 자동 생성 대신 기존 얼굴 Transform을 `faceRoot`에 지정하면, 해당 루트 아래로 눈 오브젝트만 생성/관리할 수 있습니다.
- 인벤토리 펜라이트 획득 조건을 붙일 때는 `SetProbeEnabled(false/true)`를 사용해 광원 활성 시점을 제어하세요.

---

## 동공 반응(2D Overlay UI 모듈)

기존 3D 샌드박스와 별개로, Canvas Overlay 기반의 독립 동공 반사 모듈을 추가했습니다.

- `PupilReflex` : 동공 수축/이완 애니메이션 제어
- `LightSourceTracker` : 마우스 광원 위치를 추적해 좌/우 동공 반응 트리거

### 계층 구조 예시

1. `Canvas` (Render Mode: Screen Space - Overlay)
2. `FaceOverlay` (Image)
3. `LeftEyeArea` (Empty)
4. `Iris` (Image, 갈색 계열)
5. `Pupil` (Image, 검정색 + `PupilReflex` 부착)
6. `RightEyeArea`는 좌안과 동일 구조
7. 빈 GameObject를 만들어 `LightSourceTracker` 부착 후 `leftPupil`, `rightPupil` 연결

### 임상 기본값

- 정상 동공: `normalSize = 50` (UI 기준 5mm 대응)
- 수축 동공: `constrictedSize = 20` (UI 기준 2mm 대응)
- 수축 시간: `reactionTime = 0.15`
- 병리 토글: `isReactive`를 끄면 해당 동공은 빛에 반응하지 않음

### 광원 판정 방식

- `LightSourceTracker`는 마우스 위치가 동공 `RectTransform` 타원 내부일 때만 반응시킵니다.
- 즉, 광원이 눈 주변이 아니라 동공 면적 안에 들어왔을 때만 수축합니다.

### 감속(오디오 테이퍼) 튜닝

- `PupilReflex`의 `constrictionCurve`를 조정해 초반 급격, 후반 완만한 수축을 만듭니다.
- 권장: 시작 접선은 가파르게, 종료 접선은 평평하게 설정

### 범위/제약

- 카메라 FOV/줌 제어는 포함하지 않습니다.
- 인벤토리(펜라이트 획득 여부) 연동은 포함하지 않습니다.
- 두 스크립트만으로 독립 동작하도록 구성했습니다.
