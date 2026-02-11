[for 인간 작업자: 아래 프롬프트 수정; 이 문구는 제거]

지금부터 (아이템의 역할)에 대해서 구현하려고 한다. 이 아이템은 MultiplayerInfrastructure.Item.Item 클래스를 상속하여 구현되어야 한다. 

[for 인간 작업자: 아이템의 역할에 대해서 구체적으로 기술. 플레이어가 이 아이템을 획득해야 하는 이유, 기대되는 상황, 다른 아이템, 시스템, 구성요소와의 상호작용; 이 문구는 제거]

[for 인간 작업자: (/Assets/Modules/TriageTrainer/InteractableObjects/Items)에 아이템 정의(.asset 파일, ScriptableObject로 인식되는)가 이미 존재할 경우; 이 문구는 제거]
- (.asset 파일명)을 이용하려고 한다.

[for 인간 작업자: 좌클릭 액션 필요하면; 이 문구는 제거]
- 이 아이템에는 Attack 핸들러가 구현된다. 이 아이템을 Attack하면, (구체적으로 어떤 일이 일어나야하는지 기술)

[for 인간 작업자: 우클릭 액션 필요하면; 이 문구는 제거]
- 이 아이템에는 Use 핸들러가 구현된다. 이 아이템을 Use하면, (구체적으로 어떤 일이 일어나야하는지 기술)

[for 인간 작업자: 이 아이템의 보유 여부에 따라 게임플레이에 어떤 변화가 일어나는지 기술; 이 문구는 제거]
- 이 아이템은 (구체적인 상황 조건) 하에서 시나리오를 분기하는 데 플래그로서 활용될 수 있다. (구체적으로 어떤 상황인지 기술)


[for 인간 작업자: 이하는 수정하지 말 것. 이 문구는 제거]

MultiplayerInfrastructure.Item.Item 클래스는 이미 Interactable 인터페이스를 구현하고 있으므로, Interactable 구현을 필요로 하지는 않는다. 다만 Item.Item이 요구하는 구현을 처리하여야 한다.  

이 아이템과 관련된 모든 구현체의 경로는 /Assets/Modules/TriageTrainer/Prefabs/Items/(아이템 식별자)/ 하위에 위치한다.

아이템은 MultiplayerInfrastructure.Item 네임스페이스의 Item 클래스를 상속하여 구현되어야 한다. 특히 아이템의 사용에 있어서, 좌클릭은 OnAttack 메서드, 우클릭은 OnUse 메서드를 오버라이드하여 구현하여라. 

그 외에 더 자세한 내용은 /Documents/AGENTS.md와 /Documents/api-references/를 참고하여라.

구현은 다른 시스템과 조화되어야 한다: 기반 시스템은 다른 모듈들도 의존할 수 있고, 현재의 시스템과 다른 모듈들을 모두 고쳐도 다른 브랜치와 병합이 용이하지 않을 수 있다. 따라서 기반 시스템은 쉽게 변경될 수 없다. 따라서 구현은 주어진 경로를 벗어나서는 안된다.  

만약 기반 시스템의 수정이 필요할 경우에는, `/.gitlab/issue_templates/Feature Proposal.md`에 따라 기능 제안서를 작성하여 `/Agents/Proposals/` 경로에 저장하여라.
