
이 아이템과 관련된 모든 구현체의 경로는 /Assets/Modules/TriageTrainer/Prefabs/Items/(아이템 식별자)/ 하위에 위치한다.

아이템은 MultiplayerInfrastructure.Item 네임스페이스의 Item 클래스를 상속하여 구현되어야 한다. 특히 아이템의 사용에 있어서, 좌클릭은 OnAttack 메서드, 우클릭은 OnUse 메서드를 오버라이드하여 구현하여라. 

그 외에 더 자세한 내용은 /Documents/for-agents.md와 /Documents/api-references/를 참고하여라.

구현은 다른 시스템과 조화되어야 한다: 기반 시스템은 다른 모듈들도 의존할 수 있고, 현재의 시스템과 다른 모듈들을 모두 고쳐도 다른 브랜치와 병합이 용이하지 않을 수 있다. 따라서 기반 시스템은 쉽게 변경될 수 없다. 따라서 구현은 주어진 경로를 벗어나서는 안된다.  

만약 기반 시스템의 수정이 필요할 경우에는, `/.gitlab/issue_templates/Feature Proposal.md`에 따라 기능 제안서를 작성하여 `/Agents/Proposals/` 경로에 저장하여라.
