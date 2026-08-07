> 상태: **반영 완료(done, 2026-08-07)**. `ScenarioGraphEditor.ShowFileMenu`의 플랫폼별 Reveal 항목(`RevealCurrentFile`), 메뉴 오픈 시 활성 상태 재평가, 미저장/삭제 파일 비활성 처리가 구현됨.

### 개요

Scenario Graph Editor의 File 메뉴에서 현재 열린 시나리오 파일을 운영체제 파일 관리자에 표시하는 기능을 추가한다.

### 해결하려는 문제 상황

작성자가 현재 시나리오의 사이드카 파일을 확인하거나 외부 도구로 파일을 열려면 프로젝트 창 또는 저장 경로를 수동으로 탐색해야 한다.

### 사용자 경험 목표

- 현재 열린 시나리오 파일을 File 메뉴에서 한 번에 찾을 수 있다.
- 운영체제에서 널리 사용하는 메뉴 문구를 표시한다.
- 아직 저장하지 않았거나 삭제된 파일에는 실행 가능한 메뉴를 제공하지 않는다.

### 제안

- macOS: `Reveal in Finder`
- Windows: `Show in File Explorer`
- Linux: `Show in File Manager`
- `EditorUtility.RevealInFinder`에 현재 파일의 절대 경로를 전달한다.
- `currentFilePath`가 없거나 파일이 존재하지 않으면 메뉴를 비활성화한다.

### 자세한 달성 목표

- File 메뉴를 열 때마다 현재 파일 상태를 다시 평가한다.
- 파일 내용이나 프로젝트 에셋을 변경하지 않는다.
- 지원하지 않는 Editor 플랫폼에는 `Show in File Manager`를 기본 문구로 사용한다.

### 문서화

Scenario Graph Editor README의 File 메뉴 설명에 플랫폼별 항목을 추가한다.

### 가용성과 테스트

- Unity Editor 어셈블리 컴파일을 확인한다.
- 저장 전, 정상 파일, 삭제된 파일 상태에서 메뉴 활성화 여부를 확인한다.

### 구현에 성공한 구현체는 무엇이며, 성공 여부는 어떻게 측정할 수 있나요?

- 저장된 그래프에서 항목을 누르면 파일 관리자가 열리고 해당 `.scenario.json` 파일이 선택된다.
- 새 그래프나 존재하지 않는 경로에서는 항목이 비활성화된다.

### 링크, 참고사항

- Unity `EditorUtility.RevealInFinder`
