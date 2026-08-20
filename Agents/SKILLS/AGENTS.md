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

## Humanize Korean

- **이슈 생성**, **문서화**, **텍스트 콘텐츠 생성**, **코드 주석** 시 `Tools/im-not-ai/`의 humanize-korean 도구를 거쳐 AI 한글 티(번역투·기계적 병렬·관용구 등 70개 패턴)를 제거한다.
- GitLab Issue 본문, 기능 제안서, Setup Guide, Documented Reference, XML documentation comments 등 한글 텍스트가 모두 대상이다.
- 전체 절차는 `Agents/SKILLS/humanize-korean/SKILL.md`에서 오케스트레이터 전문(`Tools/im-not-ai/skills/humanize-korean/SKILL.md`)을 로드해 따른다.
- **적용 제외:** 커밋 메시지.
- **도구 불가 시:** `python3` 런타임 확인 → 그래도 실패하면 작업을 중단하지 말고 사용자에게 `⚠️ humanize-korean 도구를 거치지 않았습니다.` 경고를 표시하고, 생성 텍스트 최상단에 `> ⚠️ 이 텍스트는 AI 문체 순화 처리를 거치지 않았습니다.` 를 포함한다. 단, **코드 주석**에는 경고 텍스트를 삽입하지 않는다(사용자 경고만).

## Cellular

- 코드 구성·규모 분석 요청에는 `Tools/cellular`(서브모듈)의 cellular runner를 사용한다.
- 사용법과 표준 절차는 `Agents/SKILLS/cellular/SKILL.md`를 따른다.
- 색인·조회·내보내기 명령은 저장소 루트에서 실행하고 프로젝트 설정은 `.cellular/config.json`이 담당한다.

## Index

- [Unity.md](./Unity.md) : When using Unity
- [humanize-korean/](./humanize-korean/) : AI 한글 텍스트 윤문 (AI 티 제거)
- [cellular/](./cellular/) : 코드 구성 색인·트리맵 시각화
