# Factor Directories

- `Assets/Modules` : 다른 패키지, 작업물들과 구분되는 단위. 특별한 사례가 아니라면 대부분의 작업물은 이 폴더 아래의 특정한 모듈의 하나로서 존재해야 합니다.

- `Assets/Modules/MultiplayerInfrastructure` : 이 프로젝트에서 요구되는 백엔드 시스템을 모두 정의합니다. 이 모듈은 다른 주제의 프로젝트에서도 바로 사용 가능하도록, 이 프로젝트에 대한 구체적인 구현이 존재하지 않아야 합니다.

- `Assets/Modules/TriageTrainer` : 이 프로젝트를 위해 정의된 모든 콘텐츠와 데이터.

- `Assets/Modules/(기타)` : 팀 외부에서 제작되어 에셋 스토어 등을 통해 추가된 패키지들은 독립적으로 이들 폴더 아래에 배치됩니다.  

### `(...)/Resources`

각 모듈 아래에는 독립적으로 리소스 폴더가 존재할 수 있습니다. 이 경우, 외부에서 제작된 패키지가 아니라면 되도록 아래의 규칙을 따라야 합니다.

- `(...)/Resources/Textures` : 텍스처 리소스
    - `(...)/Resources/Textures/Items` : 아이템 텍스처
- `(...)/Resources/Models` : 3D 모델 리소스
- `(...)/Resources/Audio` : 오디오 리소스
- `(...)/Resources/Prefabs` : 프리팹 리소스
- `(...)/Resources/ScriptableObjects` : `ItemDataModelSO` 등의 스크립터블 오브젝트 리소스
