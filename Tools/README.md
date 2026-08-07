# Tools

이 폴더는 프로젝트 운영/검증/유지보수를 위한 보조 도구 스크립트를 모아두는 위치입니다.

기본 원칙:
- 가능하면 독립 실행 가능(단일 파일)로 유지합니다.
- 실행 위치와 인자 사용법을 파일 상단 주석에 명시합니다.
- 실패 시 종료 코드(0 성공, 0 이외 실패)를 명확히 반환합니다.

## 현재 도구

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

- **merge driver**: `.gitattributes`의 `merge=unity-yaml` 매핑 + 저장소 루트 `.gitconfig`의 `unity-yaml` 드라이버 정의. `.gitconfig`는 `git config include.path ../.gitconfig`로 활성화합니다.
- **pre-commit 훅**: `.githooks/pre-commit` — 커밋 직전에 스테이징된 Unity YAML 파일의 파싱 인덱스를 `.unity-merge/INDEX`에 갱신합니다. `git config core.hooksPath .githooks`로 활성화하며 이 저장소에만 적용됩니다.

### setup-unity-merge.sh / setup-unity-merge.bat

unity-merge를 즉시 사용 가능한 상태로 구성합니다: 서브모듈 초기화/갱신 → unity-merge 빌드(툴체인 없으면 설치 시도) → 위 git 설정 두 가지를 이 저장소 로컬에만 등록합니다.

실행 예시 (working directory: 저장소 루트):
- `./Tools/setup-unity-merge.sh`
- `Tools\setup-unity-merge.bat`
