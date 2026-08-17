# Code Heatmap

독립적인 uv 프로젝트입니다. Unity 또는 서드파티 Python 패키지에 의존하지 않고, 소스 파일의 텍스트량을 주식 히트맵 같은 폴더 트리맵 SVG로 렌더합니다. 기본값은 문자 수, 루트 기준 폴더 깊이 5, 파스텔 파일 형식 색상입니다. 따라서 이 프로젝트에서는 `Assets/Modules/*/Scripts/*` 수준을 자연스럽게 구분합니다.

## 실행

저장소 루트에서 실행합니다.

```sh
uv run --project Tools/code-heatmap code-heatmap --output /tmp/code-heatmap.svg
uv run --project Tools/code-heatmap code-heatmap --metric lines --color-by module --colors 'MultiplayerInfrastructure:#A8D8EA,TriageTrainer:#F7C5CC'
uv run --project Tools/code-heatmap code-heatmap --refs main,HEAD~10,v1.0 --resolution-mode proportional --output /tmp/history.svg
uv run --project Tools/code-heatmap code-heatmap --refs 'date:2026-08-17,main,date:2026-08-01-9-30' --timezone Asia/Seoul --select-date-query-result-is-multiple median --select-date-query-result-is-none rewind --output /tmp/history.svg
uv run --project Tools/code-heatmap code-heatmap --refs main,HEAD~10 --resolution-mode fixed --output /tmp/proportions.svg
```

## 주요 옵션

- `--extensions`: 포함할 확장자. 기본값은 C# 계열, JSON, UXML, USS와 shader/HLSL/asmdef 파일입니다.
- `--ignore-config`: 줄 단위 glob 제외 설정입니다. `code-heatmap.ignore.template`을 `code-heatmap.ignore`으로 복사해 사용하며, 기본 `.*`은 숨김 파일·폴더를 제외합니다. 로컬 설정 파일은 해당 프로젝트의 `.gitignore`에 포함됩니다.
- `--depth`: 폴더 표시 깊이(기본 5). 한계 깊이의 폴더는 하위 파일 전체를 합산한 하나의 집계 블록으로 표시합니다. 예를 들어 기본값은 `Assets/Modules/<모듈>/Scripts/<하위 폴더>`의 하위를 개별 파일 대신 하나의 집계 블록으로 표시합니다.
- `--metric characters|lines`: 블록 넓이의 기준(기본 `characters`).
- `--color-by type|module`, `--colors '키:#RRGGBB,...'`: 파일 형식 또는 `Assets/Modules/<모듈>` 기준 색상 및 인라인 재정의입니다.
- `--refs`: 커밋 해시·브랜치·태그를 쉼표로 여러 개 지정해 한 SVG에서 비교합니다.
- `--refs`의 `date:...`: `date:YYYY-MM-DD`, `date:YYYY-MM-DD-h`, `date:YYYY-MM-DD-hh`, 분·초까지의 형식으로 모든 로컬·원격 참조의 커미터 날짜 구간을 지정합니다. 해시·브랜치·태그와 함께 배치할 수 있습니다.
- `--select-date-query-result-is-multiple latest|oldest|median`: 날짜 구간에 여러 커밋이 있을 때 선택 기준(기본 `latest`)입니다.
- `--select-date-query-result-is-none fast-forward|ff|rewind|rw`: 일치 커밋이 없을 때 날짜 구간 직후의 첫 커밋 또는 직전의 마지막 커밋을 고릅니다(기본 `fast-forward`).
- `--timezone`: `date:` 해석에 사용할 IANA 시간대입니다. 기본값은 시스템 지역 시간대이며, 재현 가능한 비교에는 `Asia/Seoul`처럼 명시하는 것을 권장합니다.
- `--resolution-mode proportional|fixed`: 기본 `proportional`은 Git 시점별 총량에 비례해 패널 크기도 변화시켜 크기 차이를 보입니다. `fixed`는 모든 패널을 같은 크기로 고정해 내부 구성 비율을 비교합니다.

## 제외 규칙

로컬 제외 규칙은 `code-heatmap.ignore.template`을 `code-heatmap.ignore`으로 복사해 설정합니다. 후자는 Git에서 제외되며, 없을 때도 템플릿이 자동 적용됩니다. 기본 규칙 `.*`은 점으로 시작하는 파일과 폴더를 무시합니다. 각 줄은 파일명·폴더명 또는 저장소 상대 경로에 적용되는 glob 패턴입니다.

## 레이아웃

블록 배치는 Squarified Treemap 알고리즘을 사용합니다. 면적은 측정값에 비례하고, 긴 줄 대신 가깝게 정사각형인 블록들을 여러 행·열로 배치합니다.

SVG의 사각형에 마우스를 올리면 파일 경로와 측정값이 표시됩니다.

## 테스트

```sh
uv run --project Tools/code-heatmap python -m unittest discover -s Tools/code-heatmap/tests
```
