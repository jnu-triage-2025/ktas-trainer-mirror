# AGENTS

If local environment is..
- Windows: Use PowerShell for command execution, winget for package installation
- MacOS: sh, zsh, bash for command execution, brew for package installation
  - Validate that local enviroment has Xcode Command Line Tools installed, if not, install it using `xcode-select --install`
  - Validate that local enviroment has Homebrew installed, if not, install it using `/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"`
- Linux: sh, bash for command execution, apt (or equivalent) for package installation

## Requirements

Check the local environment and install the following tools if they are not already installed: (`rg`, `fd`, `tree`, `ast-grep`, `yq`, `wget`, `node`, `dotnet`, `python`, `git`)
- It may be expected to use runtime version manager for node, dotnet, python, etc. (e.g., `nvm`, `asdf`, `pyenv`, etc.) check if the runtime version manager is installed, if not, install it using the official installation script for each runtime version manager.  
- When install `dotnet`, install runtime that supports C# 9.0. 

## GitLab Work Items

- GitLab Issue/Work Item 생성·조회·수정 요청에는 `Tools/gitlab-work-items/` 경로의 GitLab Work Items Tool을 사용한다.
- Tool 호출은 `Tools/gitlab-work-items/gitlab_work_items.py`를 통해 수행하며, `gitlab_is_available`, `gitlab_work_item_index`, `gitlab_get_work_item`, `gitlab_create_work_item`, `gitlab_update_work_item`, `gitlab_comment_work_item` 등의 함수를 제공한다.
- Tool 사용 전에 `GITLAB_TOKEN` 환경 변수가 설정되어 있어야 한다. 쉘 프로필(`source ~/.profile` 또는 `source ~/.zshrc` 등)을 실행하여 토큰을 로드한다. 토큰이 없거나 인증 오류가 발생하면 사용자에게 알리고, 토큰이 설정된 세션에서 재시도하도록 요청한다.
- `curl` 등 직접 HTTP 요청으로 우회하지 않는다.
- Tool이 가용하면 Skill의 표준 절차(가용성 확인, 프로젝트·기존 항목 확인, 증빙 기반 등록)를 따른다.

## Fluent Korean

- **이슈 생성**, **문서화**, **텍스트 콘텐츠 생성**, **사용자 응답**에는 `Tools/fluent-korean/`의 `fluent-korean` 출력 스타일을 적용하여 의미가 분명하고 자연스러운 한국어로 작성한다.
- GitLab Issue 본문, 기능 제안서, Setup Guide, Documented Reference 등 코드 밖의 한글 텍스트가 대상이다.
- 전체 절차는 `Agents/SKILLS/fluent-korean/SKILL.md`에 따른다. 작업 전 또는 최종 검토 전에 `Tools/fluent-korean/plugins/fluent-korean/output-styles/fluent-korean.md` 전문을 읽고 적용한다.
- **적용 제외:** 코드 주석, 변수명, 로그 문자열, 커밋 메시지, 인용문, 코드 블록. 이 텍스트에는 프로젝트의 기존 관례를 따른다.
- 서브모듈을 읽을 수 없더라도 작업을 중단하지 않으며, 별도 경고 문구를 추가하지 않는다.

## Cellular

- 코드 구성·규모 분석 요청에는 `Tools/cellular`(서브모듈)의 cellular runner를 사용한다.
- 사용법과 표준 절차는 `Agents/SKILLS/cellular/SKILL.md`를 따른다.
- 색인·조회·내보내기 명령은 저장소 루트에서 실행하고 프로젝트 설정은 `.cellular/config.json`이 담당한다.

## Code Mirror

- 코드·라이선스 필터링 이력 미러링 작업에는 `Agents/SKILLS/code-mirror/SKILL.md`를 따른다.
- 대상 원격 저장소에 push하거나 기존 참조를 재작성하는 작업은 사용자에게 명시적으로 요청받은 경우에만 수행한다.
- 절대로 Tool 사용 중에 `GIT_HTTP_TOKEN` 환경 변수를 직접 액세스하려고 시도해서는 안된다.

## Index

- [Unity.md](./Unity.md) : When using Unity
- [fluent-korean/](./fluent-korean/) : 명확하고 자연스러운 한국어 출력 스타일
- [cellular/](./cellular/) : 코드 구성 색인·트리맵 시각화
