# Tools

이 폴더는 프로젝트 운영/검증/유지보수를 위한 보조 도구 스크립트를 모아두는 위치입니다.

기본 원칙:
- 가능하면 독립 실행 가능(단일 파일)로 유지합니다.
- 실행 위치와 인자 사용법을 파일 상단 주석에 명시합니다.
- 실패 시 종료 코드(0 성공, 0 이외 실패)를 명확히 반환합니다.

## 현재 도구

### code-heatmap (uv project)

Unity에 의존하지 않고 소스 파일의 텍스트량을 주식 히트맵 같은 폴더 트리맵 SVG로 렌더합니다. 기본값은 문자 수, 루트 기준 폴더 깊이 5, 파스텔 파일 형식 색상입니다. 따라서 이 프로젝트에서는 `Assets/Modules/*/Scripts/*` 수준을 자연스럽게 구분합니다.

실행 예시 (working directory: 저장소 루트):

- `uv run --project Tools/code-heatmap code-heatmap --output /tmp/code-heatmap.svg`
- `uv run --project Tools/code-heatmap code-heatmap --metric lines --color-by module --colors 'MultiplayerInfrastructure:#A8D8EA,TriageTrainer:#F7C5CC'`
- `uv run --project Tools/code-heatmap code-heatmap --refs main,HEAD~10,v1.0 --resolution-mode proportional --output /tmp/history.svg`
- `uv run --project Tools/code-heatmap code-heatmap --refs main,HEAD~10 --resolution-mode fixed --fixed-layout --output /tmp/proportions.svg`

주요 옵션:

- `--extensions`: 포함할 확장자. 기본값은 C# 계열, JSON, UXML, USS와 shader/HLSL/asmdef 파일입니다.
- `--depth`: 폴더 표시 깊이(기본 5). 더 깊은 경로는 마지막 블록으로 접습니다.
- `--metric characters|lines`: 블록 넓이의 기준(기본 `characters`).
- `--color-by type|module`, `--colors '키:#RRGGBB,...'`: 파일 형식 또는 `Assets/Modules/<모듈>` 기준 색상 및 인라인 재정의입니다.
- `--refs`: 커밋 해시·브랜치·태그를 쉼표로 여러 개 지정해 한 SVG에서 비교합니다.
- `--resolution-mode proportional|fixed`: 기본 `proportional`은 Git 시점별 총량에 비례해 패널 크기도 변화시켜 크기 차이를 보입니다. `fixed`는 모든 패널 캔버스를 같게 합니다. 여기에 `--fixed-layout`을 더하면 내부 전체 면적도 정규화하여 구성 비율만 비교합니다.

SVG의 사각형에 마우스를 올리면 파일 경로와 측정값이 표시됩니다. 검증은 `uv run --project Tools/code-heatmap python -m unittest discover -s Tools/code-heatmap/tests`로 실행합니다.

### validate-documentation-links.sh

Documentation된Markdown 문서의 링크 대상 파일 존재 여부를 검사합니다.

실행 예시 (working directory: Tools):
- `./validate-documentation-links.sh`
- `./validate-documentation-links.sh ../Documents`
- `./validate-documentation-links.sh ../Documents/README.md ../Documents/working-guide`

### unitydiff

(Unity YAML을 pretty-print해 diff로 보여주는 diff 드라이버. `.gitattributes`의 `diff=unitydiff` 항목 참조.)

### unity-merge (submodule)

Unity YAML(.unity/.prefab/.asset) 3-way 머지 도구. 이 저장소에서는 두 가지로 연결됩니다.

- **merge driver**: `.gitattributes`의 `merge=unity-merge` 매핑 + 저장소 루트 `.gitconfig`의 `unity-merge` 드라이버 정의. `.gitconfig`는 `git config include.path ../.gitconfig`로 활성화합니다.
- **pre-commit 훅**: `.githooks/pre-commit` — 커밋 직전에 스테이징된 Unity YAML 파일을 `.unity-merge/records/` 누적 저장소에 추가합니다. 기존 이력 레코드는 보존되며 `git config core.hooksPath .githooks`로 활성화합니다.

### setup-unity-merge.sh / setup-unity-merge.bat

unity-merge를 즉시 사용 가능한 상태로 구성합니다: 서브모듈 초기화/갱신 → unity-merge 빌드(툴체인 없으면 설치 시도) → 위 git 설정 두 가지를 이 저장소 로컬에만 등록합니다.

실행 예시 (working directory: 저장소 루트):
- `./Tools/setup-unity-merge.sh`
- `Tools\setup-unity-merge.bat`
