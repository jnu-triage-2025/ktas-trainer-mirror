# MultiplayerInfrastructure.Item.Item

## 개요
`Item`은 인게임 세계에 배치되는 아이템 오브젝트이며, `NetworkBehaviour` + `IInteractable` 구현체입니다. 플레이어가 상호작용하면 인벤토리로 아이템을 넣고, 성공 시 네트워크에서 디스폰됩니다. 기본 아이템 데이터는 `ItemBaseModelSO` 또는 `ItemData`로부터 초기화되며, 필요 시 아이콘을 설정합니다.

이 문서는 `Item`을 상속해 커스텀 아이템을 만들고, `ItemRegistry`에 등록하려는 개발자를 위한 API 레퍼런스입니다.

## Public 메서드

### Interact(Transform interactor)
플레이어가 아이템과 상호작용할 때 호출됩니다. 기본 구현은 다음 순서로 동작합니다.

1. `interactor`가 null이면 경고 로그를 남기고 중단합니다.
2. `interactor`의 상위에서 `NetworkObject`를 찾습니다. 없으면 경고 로그를 남기고 중단합니다.
3. `ServerHandlePickup`을 호출하여 서버에서 인벤토리 추가 및 디스폰을 처리합니다.

상호작용 시스템은 `IInteractable`을 통해 이 메서드를 호출합니다. 따라서 커스텀 동작이 필요하면 **서브클래스에서 IInteractable을 명시적으로 다시 구현**하는 방식이 가장 안전합니다.

#### 예제: 인터랙션 조건을 추가하고 기본 픽업을 유지하기
```csharp
using FishNet.Object;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Item;
using UnityEngine;

public class QuestLockedItem : Item, IInteractable
{
  [SerializeField] private bool _questComplete;

  // IInteractable을 명시적으로 다시 구현해 인터페이스 호출 경로를 교체합니다.
  string IInteractable.DisplayText => _questComplete ? base.DisplayText : "조건 미충족";
  Sprite IInteractable.DisplayIcon => base.DisplayIcon;
  Color IInteractable.DisplayColor => _questComplete ? base.DisplayColor : Color.gray;

  void IInteractable.Interact(Transform interactor)
  {
    if (!_questComplete)
      return;

    // 기존 픽업 로직을 재사용합니다.
    base.Interact(interactor);
  }
}
```

> 참고: `Item.Interact`는 `virtual`이 아니므로 `override`는 불가능합니다. `IInteractable`을 명시적으로 다시 구현하면, 상호작용 시스템이 인터페이스를 통해 호출할 때 커스텀 로직이 적용됩니다.

### ServerHandlePickup(NetworkObject interactorNob)
`ServerRpc`로 선언된 서버 전용 처리 메서드입니다. 클라이언트에서 호출되며 서버에서 다음을 수행합니다.

1. `interactorNob`에서 `PlayerController`를 찾습니다. 없으면 경고 로그 후 중단합니다.
2. 아이템 데이터가 유효한지 검사합니다. 유효하지 않으면 경고 로그 후 중단합니다.
3. `PlayerController.TryAddItemToInventory`로 인벤토리에 추가를 시도합니다.
4. 성공 시 이 `Item` 오브젝트를 `Despawn()`하여 월드에서 제거합니다.

#### 예제: 서버에서 직접 픽업 처리 호출하기
```csharp
using FishNet.Object;
using MultiplayerInfrastructure.Item;
using UnityEngine;

public class DebugPickupButton : MonoBehaviour
{
  [SerializeField] private Item _targetItem;
  [SerializeField] private NetworkObject _playerNetworkObject;

  public void Pickup()
  {
    if (_targetItem == null || _playerNetworkObject == null)
      return;

    // ServerRpc이므로 클라이언트에서 호출해 서버로 전달합니다.
    _targetItem.ServerHandlePickup(_playerNetworkObject);
  }
}
```

## 커스텀 아이템 제작 및 등록 가이드
아래 절차는 `Item`을 상속한 커스텀 아이템을 만들고 시스템에 등록하는 전체 플로우를 설명합니다. 스크립트 작성부터 리소스 준비, `ItemRegistry` 등록, 동작 확인까지 한 번에 따라갈 수 있도록 구성했습니다.

### 1) 아이템 데이터 모델 생성
`ItemBaseModelSO`는 아이템의 기본 메타데이터를 정의합니다. 프로젝트에서 ScriptableObject 에셋을 생성하고, 최소한 다음 값을 채워 주세요.

- `identifier`: 아이템 고유 식별자 (예: `quest_scroll`)
- `displayName`: 인게임 표시 이름
- `maxStackCount`, `hasDurability`, `maxDurability`: 스택/내구도 설정
- `Item Texture Identifier`: 아이콘 로딩용 식별자

이미지 파일 이름은 `Item Texture Identifier`와 일치해야 합니다.

### 2) 커스텀 Item 스크립트 작성
`Item.Interact`는 `virtual`이 아니므로, 커스텀 상호작용이 필요하면 `IInteractable`을 명시적으로 다시 구현합니다.

```csharp
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Item;
using UnityEngine;

public class QuestScrollItem : Item, IInteractable
{
  [SerializeField] private string _questId = "quest_scroll_01";

  string IInteractable.DisplayText => "퀘스트 스크롤";
  Sprite IInteractable.DisplayIcon => base.DisplayIcon;
  Color IInteractable.DisplayColor => Color.cyan;

  void IInteractable.Interact(Transform interactor)
  {
    // 커스텀 로직을 먼저 실행
    GrantQuestIfPossible(interactor);

    // 기본 픽업 로직을 그대로 사용
    base.Interact(interactor);
  }

  private void GrantQuestIfPossible(Transform interactor)
  {
    // 게임별 조건 체크 로직을 구현하세요.
  }
}
```

### 3) 프리팹 준비 및 컴포넌트 설정
1. 아이템 프리팹을 만들고 커스텀 `Item` 스크립트를 추가합니다.
2. `_itemBaseModel`에 1)에서 만든 `ItemBaseModelSO`를 할당합니다.
3. `_itemData`는 비워두는 것을 권장합니다. 런타임에 `ItemBaseModelSO`에서 자동 초기화됩니다.
4. 아이템이 상호작용 대상이 되려면 프리팹 레이어를 `PickupItem`으로 설정합니다.

### 4) 아이콘 스프라이트 준비
아이콘은 `Resources` 폴더 아래에 두어야 합니다. 기본 경로는 `ItemRegistry.iconResourcesPath` 설정에 의해 결정됩니다.

권장 경로 예시:
- `Assets/Resources/Textures/ItemIcons/quest_scroll.png`

스프라이트 임포트 설정:
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single

### 5) ItemRegistry에 등록
`ItemRegistry` 에셋의 `Registered Items`에 항목을 추가하고 다음을 할당합니다.

- `Item Data Model SO`: 1)에서 만든 모델
- `Item Prefab`: 3)에서 만든 프리팹
- `Item Sprite`: 비워두면 `Resources`에서 자동 로딩됩니다

### 6) 동작 확인 체크
플레이어가 아이템에 접근하면 상호작용 힌트가 표시되고, 상호작용 시 인벤토리에 추가되는지 확인합니다.

문제 진단 팁:
- 상호작용 힌트가 안 뜨면: 레이어가 `PickupItem`인지 확인
- 아이콘이 기본 이미지로 뜨면: `Item Texture Identifier`와 이미지 파일 이름을 확인
- 픽업이 안 되면: `ItemData` 유효성(`identifier`, `currCount`) 확인

## 등록 및 사용 시 체크리스트
- 아이템 프리팹에 `Item`(또는 파생 클래스) 컴포넌트를 추가합니다.
- `_itemBaseModel` 또는 `_itemData` 중 하나만 채우는 사용 패턴을 권장합니다.
- 프리팹의 레이어를 `PickupItem`으로 설정합니다.
- `ItemRegistry`의 `Registered Items`에 `ItemBaseModelSO`와 프리팹을 등록합니다.

이 과정의 상세 가이드는 [Assets/Modules/MultiplayerInfrastructure/Scripts/Item/README.md](../../Assets/Modules/MultiplayerInfrastructure/Scripts/Item/README.md)에서 확인할 수 있습니다.
