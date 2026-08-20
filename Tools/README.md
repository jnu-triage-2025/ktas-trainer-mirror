# Tools

이 폴더는 프로젝트 운영/검증/유지보수를 위한 보조 도구 스크립트를 모아두는 위치입니다.

기본 원칙:
- 가능하면 독립 실행 가능(단일 파일)로 유지합니다.
- 실행 위치와 인자 사용법을 파일 상단 주석에 명시합니다.
- 실패 시 종료 코드(0 성공, 0 이외 실패)를 명확히 반환합니다.

## 현재 도구

### cellular (submodule) (구 code-heatmap)

Git 이력에서 커밋별 코드 구성(모듈·언어·파일·라인·문자 수)을 색인하는 Rust runner다. 색인을 .cellexport 파일로 내보내 뷰어(cellular-codemetrics.pages.dev)에서 트리맵으로 확인한다. 프로젝트 설정은 루트 .cellular/config.json에 들어 있고 색인 데이터는 프로필 디렉터리(~/.cellular/)에 쌓여 저장소에는 남지 않는다.

빌드 (working directory: Tools/cellular/runner):

- `cargo build --release` → `target/release/cellular`

실행 예시 (working directory: 저장소 루트):

- `Tools/cellular/runner/target/release/cellular main,HEAD~20` — 커밋 색인 생성
- `Tools/cellular/runner/target/release/cellular --list --modules` — 스냅샷·모듈 조회
- `Tools/cellular/runner/target/release/cellular --export` — ktas-trainer.cellexport 내보내기
- `Tools/cellular/runner/target/release/cellular --terminal` — 대화형 TUI

코드 히트맵을 생성하라는 지시가 주어지면, cli 툴로 `.cellexport`를 생성하는 것이 목표이다. 또한 생성된 파일을 `cellular-codemetrics.pages.dev`에서 열어보라고 안내하여라.  

### validate-documentation-links.sh

Documentation된 Markdown 문서의 링크 대상 파일 존재 여부를 검사합니다.

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
