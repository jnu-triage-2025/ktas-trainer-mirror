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

## 비전문 작업자 지원

비전공자 작업자가 `content/scen-a` 브랜치에서 주로 작업을 수행한다. 이 작업자는 git에 대한 이해가 부족하고, git 사용 중에 잠재적으로 발생 가능한 문제들(= Merge, Conflict, Origin에 Local에 없는 커밋이 제 3자에 의해 푸시됨 등)을 적절히 해결하지 못할 가능성이 높다. 만약 현재의 로컬 브랜치가 `content/scen-a`라면, origin과 local의 상태를 계속해서 평가하고, 아래의 지시문을 확인하여 병합과 충돌 대응을 적절히 처리해야 한다.  

The origin of the feat/scen-a branch contains a merge commit that brought a large volume of commits over from main. Therefore, conflicts are absolutely expected to occur. Check the scale of the differences and perform the merge.
The primary concern will likely be merging Unity YAML-based files. Attempt to handle Unity YAML merges as follows:

* Using the Specialized Merge Tool:
A separate merge tool exists in HEAD as C++ source code. You can compile it by checking the instructions located under Agents/SKILLS/ in HEAD. Follow those instructions to make the tool executable and then attempt the merge. (If installation is blocked due to permission issues, instruct the user to copy, paste, and run the required commands manually.)
* Fallback to Manual/Programmatic Merge:
If there are issues that the merge tool cannot resolve, or if using the tool is inappropriate for the situation, handle the problematic data directly. You can treat it as PlainText or load it as a YAML data structure to merge and serialize it yourself.
* Final Resort:
If it still cannot be resolved, stop further merge attempts as a last resort. Discard all modifications made after the local HEAD (including any garbage generated during the merge attempts), create a new branch, and push to that branch instead. (Another developer will take over to resolve the issue from there.)

Note: There is a merge commit from scen-a into main midway through, which you can utilize effectively to reduce the scope of the differences.
When returning the execution results or help messages to the human operator, please use Korean.

