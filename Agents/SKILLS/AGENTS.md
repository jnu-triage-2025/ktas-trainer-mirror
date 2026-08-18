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

- AI가 쓴 한글 텍스트의 AI 티(번역투·기계적 병렬·관용구 등 70개 패턴)를 제거해 자연스러운 한국어로 윤문한다.
- `Tools/im-not-ai/` 경로의 humanize-korean 도구를 사용한다. 전체 절차는 `Agents/SKILLS/humanize-korean/SKILL.md`에서 오케스트레이터 전문(`Tools/im-not-ai/skills/humanize-korean/SKILL.md`)을 로드해 따른다.
- Shim(`Tools/im-not-ai/scripts/prepare_monolith_input.py`)의 `route_hint`가 경로(light 1콜 / standard 2콜 / heavy 3+콜)를 결정하며, 사용자 명시(`--strict`, `가볍게`)가 오버라이드한다.
- 변경률 게이트(`verify_gates.py`)와 구조 게이트가 과윤문을 결정적으로 방지한다.
- 트리거: "AI 티 없애줘", "AI 윤문", "ChatGPT 티 제거", "번역투 고쳐", "사람이 쓴 것처럼", "humanize Korean" 등.

## Index

- [Unity.md](./Unity.md) : When using Unity
- [humanize-korean/](./humanize-korean/) : AI 한글 텍스트 윤문 (AI 티 제거)
