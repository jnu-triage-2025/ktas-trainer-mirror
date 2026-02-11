# 코딩 스타일 컨벤션

이 문서는 **Assets/Modules/TriageTrainer/** 안의 환자 모델 구현과 **Assets/Modules/MultiplayerInfrastructure/** 안의 에디터 툴 구현을 참고하여 작성한 스타일 규칙을 설명합니다. 프로젝트 전반의 코드를 이 기준에 맞추어 유지하면 코드 품질과 가독성이 높아집니다.

## 네임스페이스와 디렉터리 매핑

- 디렉터리 구조와 네임스페이스를 1:1로 대응시킵니다. 예: `Assets/Modules/TriageTrainer/Scripts/Patient/Models/PatientDescriptor.cs` → `namespace TriageTrainer.Entity.Patient`.
- 에디터 어셈블리(`EditorWindow`, `EditorGUILayout` 등)는 `MultiplayerInfrastructure.Editor`처럼 `Editor` 접미사를 포함한 네임스페이스를 사용하여 런타임 코드와 명확히 구분합니다.
- 파일마다 하나의 `public` 타입(Public class/struct)을 정의하며, 파일 이름은 타입 이름과 동일합니다.

## 타입과 멤버 네이밍

- 클래스, 구조체, 인터페이스 등 타입 이름은 PascalCase를 사용합니다.
- 데이터 필드는 lowerCamelCase로 작성하며, `PatientDescriptor`처럼 외부에 노출되는 데이터 클래스의 경우 `public` 필드로 구현할 수 있습니다.
- 에디터 윈도우나 서비스 클래스의 내부 상태 필드는 `private`/`protected`로 선언하고 `private readonly`를 최대한 활용하여 불변성을 보장합니다.
- 메서드 이름은 PascalCase, 로컬 변수는 상황에 따라 `var`을 활용하여 타입이 명확한 경우에만 생략합니다.

## 문서화와 주석

- 외부에 공개되는 타입/멤버에는 `/// <summary>` XML 문서 주석을 달고, 한국어로 의미를 설명합니다. 설명이 길면 문장을 적절히 줄바꿈하여 80자 내외로 유지합니다.
- 구현 위주의 메서드는 `//` 주석으로 핵심 의도를 보충하되, 설명적이지 않은 반복작업은 피합니다.
- `ScenarioGraphAuthoringWindow.ChangeNodeType`처럼 복잡한 동작은 단계별로 요약하는 주석을 달아 후속 검토자가 흐름을 빠르게 파악할 수 있게 합니다.

## 유니티/에디터 패턴

- 에디터 툴은 `MenuItem` 특성으로 메뉴 위치를 등록합니다(`TriageTrainer/Multiplayer Infrastructure/Multiplayer Scenario/...`). 항목 이름은 사용자에게 직관적인 문구로 작성합니다.
- `#if UNITY_EDITOR` 전처리기를 사용하여 런타임 어셈블리에서 제외할 클래스를 감싸고, 에디터 전용 로직은 `EditorWindow`, `EditorGUILayout`, `EditorUtility` API를 이용합니다.
- UI 생성 메서드(`ConstructUI`, `CreateGraphView` 등)는 책임을 분리하여 하나의 메서드가 하나의 영역만 담당하게 하고, `VisualElement`를 조합하여 명확한 레이아웃을 구성합니다.
- `OnEnable`/`OnDisable` 등 유니티 이벤트 메서드에서는 개별 구성/해제 메서드를 호출하여 초기화 흐름을 일치시킵니다.

## 에셋/데이터 클래스

- `PatientDescriptor`, `BloodPressure`처럼 순수 데이터 클래스는 필드만 정의하고, 달아둔 `summary` 설명에서 활용 용도를 적습니다.
- 열거형(`Sex`, `BloodType` 등)은 관련된 코드와 같은 폴더에 두고, 필요한 설명이 있다면 `///` 주석으로 보충합니다.
- JSON 직렬화/역직렬화가 필요한 클래스는 `System.Text.Json`을 사용하며, 런타임 데이터는 `ScenarioGraphLoader.LoadFromJson`처럼 별도 유틸리티로 책임을 분리합니다.

## 코드 정렬과 그룹화

- `using` 지시는 `System.*` → 타사 라이브러리/프로젝트 네임스페이스 → `Unity*` 순으로 배치하고, 그룹 사이에는 빈 줄 한 줄을 둡니다.
- 클래스 내 멤버는 필드 → 생성자/스타틱 초기화 → 유니티 이벤트 → 공개 API → 헬퍼 메서드 순으로 배치합니다.
- 루프나 조건문 블록은 반드시 중괄호를 사용하고, 한 줄로 정리하면 가독성이 떨어지는 짧은 `if`라도 중괄호를 유지합니다.

## 오류/검증 처리

- 유효성 검사 실패는 `EditorUtility.DisplayDialog`로 사용자에게 알려주고, `try/catch` 블록에서는 예외 메시지를 그대로 다시 던져 내부 로그에서 원인을 확인할 수 있도록 합니다.
- 사용자 입력을 받는 UI에서 `GUILayout.Button` 등은 `GUILayout.Height` 등으로 높이를 고정하여 일관된 UI 밀도를 유지합니다.

참고: 위 규칙은 `Assets/Modules/TriageTrainer/Scripts/Patient/...`의 모델 정의와 `Assets/Modules/MultiplayerInfrastructure/Editor/Scenario/...`의 에디터 윈도우 구현을 기준으로 합니다. 새로운 기능을 추가할 때 이 두 영역의 구현 스타일을 참고하여 일관된 코드를 유지해 주세요.
