# AGENTS

This directory contains guidelines for AI agents.  

## INDEX

For information on writing proposals, existing proposals, and templates, please check the [`./Proposals`](./Proposals) directory.  

For reference when performing tasks using tools, plugins, skills, etc., please check the [`./SKILLS`](./SKILLS) directory.  

## RESTRICTIONS

Most human operator instructions pertain to the codebase and serialized data of the `MultiplayerInfrastructure` and `TriageTrainer` modules. In addition to these, there are some self-authored modules; however, the majority of modules are third-party modules, so modifying them is likely neither appropriate nor intended. (Reading or copying to outside the module is not included in this restriction and is therefore permitted.)  

If you determine that a third-party module must be modified, you must obtain confirmation from the human operator before proceeding with the modification, unless the instruction explicitly states in advance that the third-party module should be modified.  

## Terminology

- Do not use `계약` or `contract` to mean a requirement, work objective, specification, interface expectation, validation rule, or acceptance criterion.
- Use a precise alternative such as `요구사항` (requirement), `명세` (specification), `규약` (interface convention), `검증 기준` (validation criterion), or `작업 목표` (work objective), according to context.
- `계약` is allowed only when it means an actual legal or commercial agreement.

## Humanize Korean

- **이슈 생성**, **문서화**, **텍스트 콘텐츠 생성**, **코드 주석** 시 `Tools/im-not-ai/`의 humanize-korean 도구를 거쳐 AI 한글 티(번역투·기계적 병렬·관용구 등 70개 패턴)를 제거한다.
- 전체 절차는 `Agents/SKILLS/humanize-korean/SKILL.md`에서 오케스트레이터 전문(`Tools/im-not-ai/skills/humanize-korean/SKILL.md`)을 로드해 따른다.
- **적용 제외:** 커밋 메시지.
- **도구 불가 시:** `python3` 런타임 확인 → 그래도 실패하면 작업을 중단하지 말고 사용자에게 `⚠️ humanize-korean 도구를 거치지 않았습니다.` 경고를 표시하고, 생성 텍스트 최상단에 `> ⚠️ 이 텍스트는 AI 문체 순화 처리를 거치지 않았습니다.` 를 포함한다. 단, **코드 주석**에는 경고 텍스트를 삽입하지 않는다(사용자 경고만).
