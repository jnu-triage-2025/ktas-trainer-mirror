# General

일반적으로 알아둬야 하는 사항들

## 커뮤니케이션
### 리드미를 읽어주세요

리드미(`README`, `README.md`)는 각 폴더의 역할, 내용, 필수 사항, 사용법을 설명합니다.
새로 확인하는 폴더에 대해 아는 바가 없다면, 먼저 리드미가 있는지 확인해주세요.

### .md 파일

![](./_static/general__preview-md.png)

`.md` 파일은 마크다운(Markdown) 형식의 문서 파일입니다. 메모장으로도 열 수 있지만, Visual Studio Code에서 .md 파일을 열어서 `ctrl + k`(`cmd + k`)를 눌렀다 뗀 후 `v` 키를 눌러 미리보기 모드로 보는 것을 권장합니다.

## 구현할 내용물의 종류

이 프로젝트에서 구현할 내용물은 다음과 같이 구분짓고 혼동하지 않도록 주의합니다.

- 아이템(Item) : 시스템에서 항상 동작합니다. 특별한 순간에만 동작하는 것이 아니라 지속적으로 시스템과 상호작용합니다.
    - 주로 아이템 컨트롤러(Item Controller)로 이름붙이고 구현합니다.
- 인터랙터블(Interactable) : 시스템에서 항상 동작합니다. 아이템과 비슷하지만, 주로 아이템화할 수 없는 맵 상의 오브젝트(i.e. 문, 스위치 등) 등에 사용됩니다.
    - 주로 인터랙터블 컨트롤러(Interactable Controller)로 이름붙이고 구현합니다.
- 엔티티(Entity) : 시스템에서 항상 동작합니다. 아이템/인터랙터블과 비슷하지만, 주로 이동하거나 아이템의 사용 대상이 되거나, 복잡한 동작을 수행하는 오브젝트에 사용됩니다.
    - 주로 엔티티 컨트롤러(Entity Controller)로 이름붙이고 구현합니다.
- 시나리오(Scenario)
    - 특별한 시나리오 흐름 구현 : 특별한 이벤트가 발생했을 때 프로그램 소스코드로 구현합니다. 시스템에서 항상 동작하는 것이 아니라 특정 순간에만 동작합니다.
- 여기에 해당하지 않는 것들은 담당자에게 문의하세요.

## 프로젝트 구조

- Agents/ : AI 프롬프트(지시문) 템플릿 및 결과 자료
  - Agents/Proposals/ : AI가 구현의 부족함을 확인하고 작성한 기능 제안서
- Agents/Implement~~.md : AI 지시문 템플릿 및 예시
- Assets/ : 프로젝트 에셋
  - Assets/Modules/ : 모듈 단위로 분리된 프로젝트 에셋
    - Assets/Modules/MultiplayerInfrastructure/ : 게임 시스템
    - Assets/Modules/TriageTrainer/ : 이 시뮬레이션 프로젝트에 한정된 구현
- Documents/ : 프로젝트 문서
  - Documents/api-reference/ : 코드 API 레퍼런스 문서
  - Documents/scenario/ : 시나리오 정의 문서

### 모듈 원칙

- Assets/Modules : 다른 패키지나 작업물과 구분되는 단위입니다. 특별한 사례가 아니라면 대부분의 작업물은 이 폴더 아래의 특정 모듈로 존재해야 합니다.
- Assets/Modules/MultiplayerInfrastructure : 이 프로젝트에서 요구되는 백엔드 시스템을 모두 정의합니다. 다른 주제의 프로젝트에서도 바로 사용 가능하도록, 이 프로젝트에 대한 구체 구현은 포함하지 않습니다.
- Assets/Modules/TriageTrainer : 이 프로젝트에 한정된 콘텐츠와 데이터를 포함합니다. 이 프로젝트에서 사용하는 구체 구현은 모두 여기에 존재해야 합니다.
- Assets/Modules/(기타) : 팀 외부에서 제작되어 에셋 스토어 등을 통해 추가된 패키지들은 독립적으로 이 폴더 아래에 배치됩니다.

### Resources 폴더 규칙

각 모듈 아래에는 독립적으로 리소스 폴더가 존재할 수 있습니다. 외부에서 제작된 패키지가 아니라면 아래 규칙을 따릅니다.

- (...)/Resources/Textures : 텍스처 리소스
  - (...)/Resources/Textures/Items : 아이템 텍스처
- (...)/Resources/Models : 3D 모델 리소스
- (...)/Resources/Audio : 오디오 리소스
- (...)/Resources/Prefabs : 프리팹 리소스
- (...)/Resources/ScriptableObjects : 스크립터블 오브젝트 리소스
