[for 인간 작업자: 아래 프롬프트 수정; 이 문구는 제거]

지금부터 (Interactable Object)에 대해서 구현하려고 한다. 이 Interactable은 MultiplayerInfrastructure.InteractableEntity.IInteractable 클래스를 상속하여 구현되어야 한다. 

- 플레이어 화면에 표시될 이름(DisplayText): (수정)
- 플레이어 화면에 표시될 아이콘(DisplayIcon): (2D png 파일명으로 수정. 없다면 `null`로 적을 것)
- 플레이어 화면에 표시될 색상(DisplayColor)은 Unity의 White정의를 사용한다.

[for 인간 작업자: Interactable의 역할에 대해서 구체적으로 기술. 플레이어가 이 Interactable이 구현되는 이유, 기대되는 상황, 다른 Interactable, 시스템, 구성요소와의 상호작용; 이 문구는 제거]

[for 인간 작업자: 상호작용 시 동작, 필요한 조건에 대해서 자세히 기술; 이 문구는 제거]
- 이 Interactable에는 Interact 핸들러가 구현된다. 이 Interactable을 Interact하면, (구체적으로 어떤 일이 일어나야하는지 기술)


[for 인간 작업자: 이하는 수정하지 말 것. 이 문구는 제거]

이 Interactable과 관련된 모든 구현체의 경로는 /Assets/Modules/TriageTrainer/Prefabs/Interactables/(Interactable 식별자)/ 하위에 위치한다.

Interactable은 MultiplayerInfrastructure.InteractableEntity 네임스페이스의 IInteractable 인터페이스를 구현하여야 한다. 

그 외에 더 자세한 내용은 /Documents/AGENTS.md와 /Documents/api-references/를 참고하여라.

구현은 다른 시스템과 조화되어야 한다: 기반 시스템은 다른 모듈들도 의존할 수 있고, 현재의 시스템과 다른 모듈들을 모두 고쳐도 다른 브랜치와 병합이 용이하지 않을 수 있다. 따라서 기반 시스템은 쉽게 변경될 수 없다. 따라서 구현은 주어진 경로를 벗어나서는 안된다.  

만약 기반 시스템의 수정이 필요할 경우에는, `/.gitlab/issue_templates/Feature Proposal.md`에 따라 기능 제안서를 작성하여 `/Agents/Proposals/` 경로에 저장하여라.
