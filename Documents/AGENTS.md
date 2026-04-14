# AGENTS.md

Before starting any work, thoroughly review all documentation materials under /Documents/. These documents contain the coding conventions, architecture guidelines, and implementation instructions required for this project.

In most cases, a non-expert human operator will instruct the AI to generate implementations. Therefore, even if processing is done internally in English, the output must be provided in **Korean**, which is the primary language of these operators.

## Notices When Writing Scripts

Within MultiplayerInfrastructure, there are several implementations where the namespace name and class name is same. Representative examples include: `MultiplayerInfrastructure.Item.Item`, `MultiplayerInfrastructure.Entity.Entity`. In such cases, identifiers must be written explicitly as: `Item.Item`, `Entity.Entity` Otherwise, compilation errors may occur. If a human operator reports a compilation error stating that a namespace does not contain a method, or if such an error log is provided as input, you should first investigate this namespace/class name ambiguity issue as a possible cause.

## Project-Specific Tasks


Most tasks will target TriageTrainer (implementations specific to this project). In such cases, all work must be limited to the path under /Assets/Modules/TriageTrainer/. In most cases, each task result will be independent (e.g., “create a new item”). In such situations, save the output under /Assets/Modules/TriageTrainer/Prefabs/(TypeOfWork)/(Identifier)/ (e.g., /Assets/Modules/TriageTrainer/Prefabs/Items/health_potion/).

Scripts are generally named as `(PascalCaseIdentifier)Controller.cs`. For example, for the `health_potion` item, the script name should be `HealthPotionController.cs`.

After generating these scripts, you must additionally create: 1. a Setup Guide and 2. a Documented Reference.  
1. The Setup Guide must be written in Korean and explain in detail how to configure and set up the implementation within Unity Editor. Since this task is expected to be performed by a non-expert human operator, it should be written as thoroughly as possible.  
2. The Documented Reference must be written in Korean for readers with relevant domain expertise. This documentation must be saved under /Documents/api-references/ as `(TypeOfWork)/identifier.md`.  
For item 1, the result must also be output directly in the interface (typically chat) so that the human operator can immediately continue the work.

## When System-Level Modifications Are Required

In particular, the modules under /Assets/Modules/MultiplayerInfrastructure/ serve a very critical role in this project. These modules must be designed to be reusable in other projects as well; therefore, any modifications to them must be carried out with great caution.

If modifications to this system become necessary, prepare a feature proposal and example implementation in accordance with .gitlab/issue_templates/Feature Proposal - detailed.md, and create a new directory under /Agents/Proposals/ to store them. The proposal must describe in detail the necessity of the change, the contents of the change, and the expected impact.

Only create and push a commit for the contents written as part of this proposal. In many cases, security policies may prevent direct Git operations. If this happens, provide the human user with guidance on how to commit the changes manually. The human user is expected to use SourceTree, git commands, or similar tools.

## CHECK CURRENT BRANCH

Before starting any work, always check which branch you are currently on. Never perform work directly on the `main` branch or on branches that other team members are working on. Always create or switch to a dedicated branch for your work.

## Generated: Post-processing in Documentation

문서화 작업 Post Process (요약, plain text)
문서 작성/이동/이름 변경이 끝나면 반드시 후처리를 수행한다.
첫째, 문서 색인을 갱신한다. 최소 대상은 Documents/requirements/README.md의 DOC-INDEX 블록이며, 현재 파일 구조와 제목(프론트매터 title 또는 H1)이 반영되도록 업데이트한다.
둘째, 경로 변경이 있으면 상대 링크를 재검토한다. requirements 하위 문서를 이동했으면 ../../, ../../../ 같은 경로가 깨질 가능성이 높으므로 우선 확인한다.
셋째, 링크 검증 도구를 실행한다. 기본 실행 위치는 Tools이며 validate-documentation-links.sh를 사용한다.
실행 예시 1: cd Tools && ./validate-documentation-links.sh ../Documents
실행 예시 2: cd Tools && ./validate-documentation-links.sh ../Documents/requirements
실행 예시 3: cd Tools && ./validate-documentation-links.sh ../Documents/requirements/*.md
넷째, 검증 결과가 FAIL이면 MISSING 항목을 문서 단위로 수정한 뒤 동일 명령으로 재검증해서 OK를 확인한다.
다섯째, 템플릿 문서(_template.md)는 플레이스홀더 링크를 포함할 수 있으므로 실제 검증 결과 해석 시 구분해서 본다.
여섯째, 구조 개편이 있었으면 requirements 루트 안내 문구(분류 설명, domain 규칙, 색인 링크)까지 같이 정합화한다.
