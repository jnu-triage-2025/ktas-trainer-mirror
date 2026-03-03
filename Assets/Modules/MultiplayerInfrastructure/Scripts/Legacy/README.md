# Legacy/Item

아이템은 인벤토리 상에 존재하는 데이터 값(`ItemBaseModelSO`, `ItemData`)과 인게임 상에 실제로 배치/상호작용 가능한 3D 오브젝트(`Item`)로 구성됩니다. 모든 구성 요소는 완벽히 정의되어 있어야 시스템에서 의도대로 처리될 수 있습니다.  

사전 정의된 아이템은 `ItemBaseModelSO`와 아이템 스프라이트 이미지, 그리고 유니티 게임 오브젝트 프리팹이 필요합니다.  

## 아이템을 시스템에 등록하기

이 시스템에서 새로운 아이템을 생성하고 사용하기 위해서는 몇 가지 단계를 거쳐야 합니다. 기본적으로 아이템은 고유한 식별자를 중심으로 관리되고, 아이템 텍스쳐, 인게임에서의 게임 오브젝트 프리팹 역시 이들 식별자를 활용합니다. 하지만 게임의 안정성과 기반 시스템의 구현 방법으로 인해, 단순히 추가하고자 하는 아이템의 리소스 파일의 이름을 아이템의 식별자로 설정하거나 하는 것 이상의 작업이 필요합니다.

### 1. ItemDataModelSO 생성

Project 패널의 + (추가) 버튼에서 `Triage Trainer > Item Data Model`을 선택하여 새로운 `ItemDataModelSO` 에셋을 생성합니다. 이 에셋은 아이템의 기본값 메타데이터를 담고 있으며, 아이템의 식별자, 이름, 설명, 텍스쳐 식별자 등을 설정할 수 있습니다.

![](./_static/register-item-itemdatamodelso.png)

`ItemDataModelSO`를 생성하면, 이러한 에셋을 한 곳에 모아 관리하는 것이 좋습니다. 현재는 `Assets/Modules/MultiplayerInfrastructure/ScriptableObjects/Items` 에 위치시키고 있습니다.

### 2. 아이템 리소스 파일 준비

아이템의 시각화 리소스는 두 가지 종류의 파일이 필요합니다. 프리팹 상태의 게임 오브젝트와, 이미지 파일(대부분의 경우 PNG)입니다. 프리팹은 인게임 세계에서 아이템을 표시하는 데 사용됩니다. 이미지 파일은 인벤토리 UI 등에서 아이템을 나타내는 데 사용됩니다.  

이 두 리소스 파일은 각각 다음 경로에 위치시키고 있습니다:
- 프리팹: `Assets/Modules/MultiplayerInfrastructure/Prefabs/Items/`
- 이미지: `Assets/Resources/Textures/ItemIcons/`

### 2.1. 프리팹 컴포넌트 설정

![](./_static/register-item-grounded-item.png)

프리팹에는 게임 시스템과 상호작용 가능하도록 `Item` 컴포넌트를 추가해야 합니다. `Item` 컴포넌트에는 `ItemDataModelSO` 타입의 `BaseItemDataModelSO` 필드와 `ItemData` 필드가 있습니다.  

이 타입은 모두 채우지 않고, 한 개만 채우는 것이 의도되었습니다. 런타임에서는 `BaseItemDataModelSO` 필드의 데이터를 기반으로 `ItemData` 필드가 자동으로 채워집니다. 이렇게 함으로써, 같은 아이템(식별자가 같은)의 기본값을 `ItemDataModelSO`를 통해 설정하고, 개별 아이템의 상태를 `ItemData`를 통해 관리하는 것이 의도되었습니다.  

![](./_static/register-item-prefab-layer.png)  

프리팹의 레이어는 `PickupItem` 레이어로 설정되어야 합니다. 이는 시스템이 아이템과 플레이어 간의 상호작용을 감지할 때, `PickupItem` 레이어로 설정된 오브젝트만 아이템으로 간주하고 처리하기 때문입니다.  

### 2.2. 이미지 스프라이트 설정

이미지 파일의 식별자는 `ItemDataModelSO` 에셋의 `Item Texture Identifier` 필드와 일치해야 합니다. 예를 들어, 아이템의 텍스쳐 식별자가 `health_potion`이라면, 이미지 파일의 이름도 `health_potion.png` 여야 합니다.

이는 시스템이 런타임에 리소스 폴더에서 이미지를 로드할 때, 식별자와 동일한 이름을 가진 파일을 찾기 때문입니다.  

![](./_static/register-item-sprite.png)

이미지 파일은 `Sprite` 타입으로 임포트되어야 합니다. 다음과 같이 설정되어있는지 확인해주세요:

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single

스프라이트 설정은 변경 후 저장해야 합니다. 다른 패널을 클릭하면 자동으로 저장 여부를 묻는 다이얼로그가 표시됩니다.  

### 3. 아이템 레지스트리에 등록

![](./_static/register-item-item-registry.png)

시스템에서 아이템 데이터를 관리하기 위해 중앙 집중식 레지스트리 ItemRegistry를 구현해 사용하고 있습니다. 새로운 아이템을 시스템에 등록하려면, `ItemRegistry` 에셋을 열고, `Registered Items` 리스트에 새 항목을 추가해야 합니다.  

Registered Items 리스트의 각 항목은 `ItemDataModelSO` 타입의 `Item Data Model SO` 필드와 `GameObject` 타입의 `Item Prefab` 필드로 구성되어있습니다. 각각 앞서 생성한 `ItemDataModelSO` 에셋과 프리팹을 할당합니다. 이미지는 `ItemDataModelSO` 에셋의 `Item Texture Identifier` 필드를 통해 자동으로 참조됩니다.  

