---
name: cellular
description: Git 커밋 이력을 색인해 모듈·언어별 코드 구성(파일·라인·문자 수)을 측정하고 트리맵으로 시각화하는 도구. 코드 규모 추이 조회, 커밋 간 비교, 리팩터링 전후 모듈 비중 확인에 쓴다.
---

# Cellular — 코드 구성 색인·시각화

Git 커밋 이력에서 모듈·언어별 코드 구성(파일·라인·문자 수)을 측정해 색인으로 저장하고 트리맵으로 시각화하는 도구다. 코드 규모 추이 조회, 커밋 간 비교, 리팩터링 전후 모듈 비중 확인에 쓴다. `Tools/cellular` 서브모듈의 Rust runner가 색인을 만들고 결과는 뷰어(cellular-codemetrics.pages.dev)에서 연다.

## 사전 준비

1. 서브모듈을 초기화한다: `git submodule update --init Tools/cellular`
2. Rust 툴체인(cargo)을 확인하고 없으면 rustup으로 설치한다.
3. `Tools/cellular/runner`에서 `cargo build --release`로 빌드한다. 바이너리는 `Tools/cellular/runner/target/release/cellular`에 생긴다.

## 표준 절차

1. 색인: 저장소 루트에서 `cellular` 뒤에 커밋 목록을 붙여 실행한다. 커밋 목록은 해시·브랜치·태그·`HEAD~N`·`date:YYYY-MM-DD`·`all`을 쉼표로 묶어 지정한다. `index_depth` 같은 기본값은 루트 `.cellular/config.json`이 담당한다.
2. 확인: `--list`로 스냅샷을 조회한다. 모듈별 상세는 `--modules`를 붙인다. 색인 무결성이 의심되면 `--verify`로 검사한다.
3. 시각화: `--export`로 `프로젝트 이름.cellexport` 파일을 만든 뒤 뷰어에서 연다. File → Open 메뉴나 파일 끌어놓기로 열 수 있다.
4. 대화형 조회가 필요하면 `--terminal`로 TUI를 띄운다.

## 주의

- 색인 데이터는 `~/.cellular/index/`에 저장된다. 저장소에는 `.cellular/config.json`만 남는다.
- `.cellexport` 파일은 루트 `.gitignore`로 제외된다.
- 이미 색인한 커밋은 다시 측정하지 않는다. 다시 측정하려면 `--force`를 붙인다.
- 서브모듈 안의 파일은 수정하지 않는다.
