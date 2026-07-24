
# Scenario Graph Authoring — 사용자 설명서

이 문서는 Unity 에디터용 `Scenario Graph Authoring` 창의 사용법과 내부 동작을 설명합니다. 이 도구는 `MultiplayerInfrastructure.Scenario` 시스템의 시나리오(대사, 선택, 이동, 사운드, 병렬 등)를 시각적으로 작성·편집·검증·저장하기 위해 제공됩니다.

## 개요
- 목적: 시나리오 그래프를 시각적으로 편집하고, JSON으로 저장/로드하며, 런타임 로더와 동일한 경로로 검증하기 위함.
- 주요 기능: 노드 추가/편집/삭제, 포트 연결(흐름 작성), 노드 타입 변경, 자동 레이아웃, 저장(.scenario.json) 및 에디터 위치 저장(.scenario.editor.json), 유효성 검사(Validate).

## 빠른 시작
1. Unity 상단 메뉴에서 `TriageTrainer → Multiplayer Infrastructure → Multiplayer Scenario → Scenario Graph Authoring` 을 선택하여 창을 엽니다.
2. 툴바에서 `New Graph` 를 눌러 새 그래프를 만듭니다.
3. `Add Node` 버튼을 눌러 노드 타입 검색창을 열고(또는 그래프에서 노드 생성 요청) 노드를 추가합니다.
4. 노드를 클릭해 오른쪽 Inspector에서 필드를 편집합니다.
5. 노드의 포트를 드래그하여 다른 노드와 연결(흐름 구성)합니다.
6. `Save File`로 시나리오 JSON(.scenario.json)을 저장합니다(같은 폴더에 `.scenario.editor.json`로 노드 위치가 별도 저장됩니다).
7. `Validate`로 저장-로드 루틴을 통해 구조적 유효성(역직렬화 가능 여부)을 확인합니다.

## UI 구성 요소
- 상단 툴바: `New Graph`, `Add Node`, `Open File`, `Save File`, `Validate`.
- 툴바 메뉴: `File`(New/Open/Save 등 — `Open Recent` 하위 메뉴 포함. 아래 "최근 연 파일" 참고).
- 툴바 필드: `Graph ID`(그래프 식별자), `Tags(csv)`, `Default Init`(기본 진입 노드 식별자 — 아래 "DefaultInit 표시" 참고).
- Graph Area(왼쪽/중앙): 그래프 캔버스(노드, 엣지, MiniMap 포함). 노드 드래그·연결·삭제 가능.
- Inspector Panel(오른쪽): 선택된 노드의 세부 속성 편집(스크롤 가능). 노드 타입 변경 드롭다운 포함.

## DefaultInit (기본 진입 노드) 표시
- `DefaultInit`는 `startNodeIdentifier` 없이 시나리오를 시작할 때 사용되는 기본 진입 노드입니다. 시나리오 JSON의 `defaultEntrypoint` 필드에 저장되며, 값이 없으면 하위호환으로 `nodes`의 첫 번째 노드가 사용됩니다.
- 표시: `defaultEntrypoint`가 가리키는 노드의 타이틀바에 금색 `★ Default Init` 배지와 금색 타이틀 색상이 표시됩니다(런타임 실행 강조의 초록 테두리와 동시에 표시될 수 있습니다).
- 설정 방법:
	- 툴바의 `Default Init` 필드에 노드 식별자를 직접 입력하거나,
	- 노드 우클릭 메뉴에서 `Set as Default Init` / `Clear Default Init` 을 사용합니다.
- 참조 유지: 표시된 노드의 식별자를 변경(Rename)하면 `defaultEntrypoint`도 함께 갱신되고, 노드를 삭제하면 자동으로 해제됩니다.
- 존재하지 않는 식별자를 가리키는 경우 하단 디버그 패널에 Error 진단이 표시됩니다.

## 최근 연 파일 (Open Recent)
- `File` 메뉴의 `Open Recent` 하위 메뉴에서 최근에 열었거나 저장한 시나리오 파일(최대 12개, 최신 순)을 바로 열 수 있습니다. 항목은 "파일이름 (상위폴더)" 형태로 표시됩니다.
- 목록은 프로젝트 로컬 캐시인 `Library/ScenarioGraphEditor/recent_files.json` 에 저장됩니다. 버전 관리에는 포함되지 않지만 에디터를 재시작해도 유지되며, 파일 삭제 시 목록에서도 자동 제외됩니다.
- 하위 메뉴 하단의 `Clear Recent` 로 목록을 비울 수 있습니다.

## 노드 기본 동작
- 생성: `Add Node`로 타입을 선택하면 새 노드가 그래프에 추가됩니다. 기본 식별자는 `type_1` 형식으로 자동 생성됩니다.
- 식별자(Identifier): 각 노드의 고유 키입니다. Inspector에서 변경 가능하며 변경 시 그래프 내 모든 참조(NextIdentifier, Choice/Parallel 참조)가 자동 갱신됩니다.
- 연결: 노드의 출력 포트(Next, Choice 옵션, Parallel 브랜치 등)를 다른 노드의 입력 포트에 연결하여 흐름을 만듭니다.
- 삭제: 노드를 삭제하면 참조가 자동으로 null 처리되어 깨진 참조가 남지 않습니다.
- 타입 변경: Inspector의 타입 드롭다운으로 `ChangeNodeType`을 호출해 노드 타입을 교체할 수 있습니다. 내부적으로는 동일한 `identifier`를 가진 새 도메인 객체로 대체됩니다. 타입 간 필드 자동 매핑은 보장되지 않으므로 변경 후 필드를 확인/입력하세요.

## 저장 및 로드
- 저장(`Save File`): 그래프(도메인 모델)를 DTO로 변환 후 JSON 직렬화하여 저장합니다. 동일 폴더에 `.scenario.editor.json` 파일로 노드 위치(좌표)를 저장합니다.
	- JSON 인코딩: UTF‑8, 한글이 `\uXXXX`로 이스케이프 되지 않도록 설정되어 있습니다.
	- Pretty printing: 사람이 읽기 쉬운 들여쓰기가 적용됩니다.
- 열기(`Open File`): `.scenario.json`을 읽어 도메인 그래프를 복원합니다. 같은 이름의 `.scenario.editor.json`이 있으면 노드 위치를 복원합니다. 없으면 자동 레이아웃을 적용합니다.

## 파일 포맷(요약)
- 최상위: `nodes` 딕셔너리 — 키는 노드 ID, 값은 해당 노드 객체.
- 각 노드는 `identifier`와 `nodeType`을 반드시 포함해야 합니다.
- 주요 nodeType: `Dialogue`, `Choice`, `Sound`, `PlayerMove`, `CameraTarget`, `Parallel` 등.
- enum 타입 필드는 JSON에서 문자열로 표현되며 로더에서 적절한 enum으로 변환됩니다(정확한 이름 사용 권장).

자세한 필드 설명 및 예제 JSON은 프로젝트의 시나리오 가이드 README(상위 스크립트 폴더)를 참고하세요.

## 자동 레이아웃 (AutoLayout)
- `.scenario.editor.json`이 존재하지 않을 때 자동으로 실행됩니다.
- 진입 노드(들)를 찾아 BFS로 노드 깊이(레벨)를 계산하고, 레벨별로 좌→우 배치합니다.
- 기본 간격: 가로 약 360px, 세로 약 200px. 복잡한 그래프는 수동 위치 조정 후 저장 권장.

## 검증(Validate)
- `Validate`는 현재 그래프를 JSON으로 변환한 뒤 다시 로드해 역직렬화 과정에서 발생하는 예외를 확인합니다.
- 에러가 발생하면 대화 상자로 예외 메시지가 표시됩니다. 이를 통해 누락 필드, 잘못된 enum 값 등을 사전 검출할 수 있습니다.

## .scenario.editor.json (에디터 상태 파일)
- 역할: 노드 위치(좌표)를 저장.
- 각 노드 위치는 직렬화 가능한 `SerializableVector2` 형태로 저장되어 Unity 타입 직렬화 문제를 회피합니다.
- 레이아웃 복원이 필요 없거나 파일이 손상되었다면 `.scenario.editor.json`을 삭제하면 자동 레이아웃이 적용됩니다.

## 사용 예제 (단계별)
1. 새 그래프 생성: `New Graph` 클릭.
2. `Add Node` → `Dialogue` 추가 → Inspector에서 `speakerName`, `dialogueContent` 입력.
3. `Add Node` → `Choice` 추가 → 옵션 추가 및 각 옵션의 `displayText`와 `nextNodeIdentifier` 설정.
4. 노드를 포트로 연결하여 흐름 구성(또는 `nextIdentifier` 직접 입력).
5. `Save File`로 `scenario_graph.scenario.json` 저장(같은 폴더에 `scenario_graph.scenario.editor.json` 자동 생성).
6. `Validate`로 저장-로드 검증 수행.

## 권장 작업 흐름 및 팁
- 항상 식별자를 의미 있게 설정하세요. 식별자는 노드 간 참조의 핵심입니다.
- 노드 타입 변경 후 필수 필드를 수동으로 확인하세요(자동으로 필드가 옮겨지지 않을 수 있음).
- 대규모 그래프는 자동 레이아웃 후 수동 정렬 → 저장을 권장합니다.
- JSON을 수동 편집할 때는 `nodes` 딕셔너리 구조와 `identifier`/`nodeType` 일치 여부를 확인하세요.

## 자주 발생하는 문제와 해결법
- 저장 실패: 노드가 없다면 저장이 불가합니다. 노드를 추가하세요.
- 중복 식별자 오류: Rename 시 기존 식별자와 충돌하면 에러가 뜨며 변경이 거부됩니다. 다른 식별자를 사용하세요.
- 로드 오류/검증 실패: 오류 메시지(예: 누락 필드, enum 오타)를 확인하고 JSON을 수정하세요.
- 노드 위치 엉김: `.scenario.editor.json`을 삭제하면 자동 레이아웃이 적용됩니다.

## 내부 요약(짧게)
- 저장: 도메인 → DTO → JsonSerializer(커스텀 컨버터 포함) → 파일.
- 로드: 파일 → DTO → 도메인 → 에디터 뷰 생성 → (있으면) .scenario.editor.json 위치 복원.
- 주요 함수: `SaveGraphToJson()`, `OpenGraphFromJson()`, `AutoLayoutNodes()`, `ChangeNodeType()`, `ValidateGraphUsingRuntimeValidator()`.
