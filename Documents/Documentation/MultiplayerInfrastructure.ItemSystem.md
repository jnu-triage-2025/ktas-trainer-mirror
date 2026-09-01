# <a id="MultiplayerInfrastructure_ItemSystem"></a> Namespace MultiplayerInfrastructure.ItemSystem

### Namespaces

 [MultiplayerInfrastructure.ItemSystem.Examples](MultiplayerInfrastructure.ItemSystem.Examples.md)

### Classes

 [EquipmentAttributeHelper](MultiplayerInfrastructure.ItemSystem.EquipmentAttributeHelper.md)

장비 Attribute 검사를 위한 정적 헬퍼 클래스입니다.
Item 인스턴스의 타입에 적용된 장비 Attribute를 리플렉션으로 확인합니다.
결과는 타입별로 캐시되어 반복 조회 시 O(1)입니다.

 [EquipmentShadowSpriteProvider](MultiplayerInfrastructure.ItemSystem.EquipmentShadowSpriteProvider.md)

장비 슬롯의 그림자 스프라이트를 제공하는 정적 클래스입니다.

<p>
그림자 스프라이트는 <code>.png</code> 파일로 Resources 폴더에 존재하며,
리소스 경로가 장비 슬롯 타입별로 하드코딩됩니다.
최초 로드 후 캐시되어 재사용됩니다.
</p>

<p>
Z-Ordering: 장비 슬롯에서 그림자는 z+2(UI 위, 아이템 아래)에 위치합니다.
</p>

 [EquippableGloveAttribute](MultiplayerInfrastructure.ItemSystem.EquippableGloveAttribute.md)

이 특성(Attribute)이 적용된 <xref href="MultiplayerInfrastructure.ItemSystem.Item" data-throw-if-not-resolved="false"></xref> 파생 클래스는
Glove(장갑) 장비 슬롯에 장착할 수 있음을 선언합니다.

<p>
적용 대상: Item 파생 클래스 (클래스 선언부 위)
</p>
<example>
<pre><code class="lang-csharp">[EquippableGlove]
public class SterileGloves : MedicalItem
{
  public const string Identifier = "sterile_gloves";
}</code></pre>
</example>

<p>
<code>Inherited = true</code> 이므로 부모 클래스에 적용하면 자식 클래스에도 자동 상속됩니다.
</p>

 [IntendedMissing3DModelAttribute](MultiplayerInfrastructure.ItemSystem.IntendedMissing3DModelAttribute.md)

이 특성(Attribute)이 적용된 <xref href="MultiplayerInfrastructure.ItemSystem.Item" data-throw-if-not-resolved="false"></xref> 파생 클래스는
3D 모델(프리팹)이 의도적으로 누락되었음을 선언합니다.

<p>
적용 대상: Item 파생 클래스 (클래스 선언부 위)
</p>
<example>
<pre><code class="lang-csharp">[IntendedMissing3DModel]
public class Scissors : MedicalItem
{
  public const string Identifier = "scissors";
}</code></pre>
</example>

<p>
<code>Inherited = true</code> 이므로 부모 클래스에 적용하면 자식 클래스에도 자동 상속됩니다.
</p>

 [IntendedMissingItemSpriteAttribute](MultiplayerInfrastructure.ItemSystem.IntendedMissingItemSpriteAttribute.md)

이 특성(Attribute)이 적용된 <xref href="MultiplayerInfrastructure.ItemSystem.Item" data-throw-if-not-resolved="false"></xref> 파생 클래스는
아이콘 스프라이트가 의도적으로 누락되었음을 선언합니다.

<p>
적용 대상: Item 파생 클래스 (클래스 선언부 위)
</p>
<example>
<pre><code class="lang-csharp">[IntendedMissingItemSprite]
public class Gauze : MedicalItem
{
  public const string Identifier = "gauze";
}</code></pre>
</example>

<p>
<code>Inherited = true</code> 이므로 부모 클래스에 적용하면 자식 클래스에도 자동 상속됩니다.
</p>

 [Item](MultiplayerInfrastructure.ItemSystem.Item.md)

모든 아이템의 기반 추상 클래스입니다.

■ Definitions (정의 레이어)
  파생 클래스에서 public const 필드로 선언합니다.
  기반 클래스의 virtual 프로퍼티는 리플렉션으로 읽어오며, 인스턴스화 시 Instance 초기값으로 사용됩니다.

  예)
    public const string Identifier   = "my_item";
    public const string DisplayName  = "My Item";
    public const string Description  = "설명";

  MyItem.Identifier 처럼 인스턴스 없이도 컴파일 타임 상수로 접근할 수 있습니다.
  같은 이름의 const를 자식 클래스에서 다시 선언하면 부모 값을 숨깁니다 (new 권고).

■ Instance (상태 레이어)
  런타임 중 변하는 현재 값들. 생성자에서 Definitions 값으로 초기화됩니다.
  CurrentSerializedDerivedAttributes를 통해 파생 클래스 고유 상태를 직렬화/역직렬화합니다.

 [ItemCombineRecipe](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.md)

아이템 자동 조합 레시피 정의입니다.

레시피는 하나 이상의 재료 아이템(Identifier + 필요 수량)과
생성될 결과 아이템(Identifier + 생성 수량)으로 구성됩니다.

예시: 후두경 블레이드 1개 + 후두경 손잡이 1개 → 후두경 1개
  new ItemCombineRecipe("laryngoscope")
    .Requires("laryngoscope_blade", 1)
    .Requires("laryngoscope_handle", 1)
    .Produces(1)

수량 비율 예시: A 2개 + B 1개 → C 1개
  new ItemCombineRecipe("c")
    .Requires("a", 2)
    .Requires("b", 1)
    .Produces(1)

 [ItemCombineRecipeRegistry](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipeRegistry.md)

아이템 자동 조합 레시피를 관리하는 정적 레지스트리입니다.

레시피 등록:
  ItemCombineRecipeRegistry.Register(
    new ItemCombineRecipe("laryngoscope")
      .Requires("laryngoscope_blade", 1)
      .Requires("laryngoscope_handle", 1)
      .Produces(1));

조합 확인은 <xref href="MultiplayerInfrastructure.ItemSystem.ItemCombineRecipeRegistry.TryGetMatchingRecipe(System.Collections.Generic.IReadOnlyDictionary%7bSystem.String%2cSystem.Int32%7d%2cMultiplayerInfrastructure.ItemSystem.ItemCombineRecipe%40)" data-throw-if-not-resolved="false"></xref> 로 수행합니다.

 [ItemMissingAssetSuppression](MultiplayerInfrastructure.ItemSystem.ItemMissingAssetSuppression.md)

아이템 3D 모델(프리팹) 및 아이콘 스프라이트 누락 warning의 노이즈를 줄이기 위한 억제 헬퍼입니다.

<p>
<b>선언적(Attribute) 억제</b>: <xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissing3DModelAttribute" data-throw-if-not-resolved="false"></xref> 또는
<xref href="MultiplayerInfrastructure.ItemSystem.IntendedMissingItemSpriteAttribute" data-throw-if-not-resolved="false"></xref> 를 Item 파생 클래스에 적용하면
해당 클래스(및 <code>Inherited=true</code> 로 자식 클래스)의 누락 warning이 자동 생략됩니다.
</p>

<p>
<b>프로그래밍(RegisterIdentifier) 억제</b>: Item 클래스가 아닌 식별자(UI 아이콘 등)는
<xref href="MultiplayerInfrastructure.ItemSystem.ItemMissingAssetSuppression.RegisterSuppressedSpriteIdentifier(System.String)" data-throw-if-not-resolved="false"></xref> / <xref href="MultiplayerInfrastructure.ItemSystem.ItemMissingAssetSuppression.RegisterSuppressedModelIdentifier(System.String)" data-throw-if-not-resolved="false"></xref>
로 등록하면 해당 identifier의 warning이 생략됩니다.
</p>

 [ItemObject](MultiplayerInfrastructure.ItemSystem.ItemObject.md)

인게임에서 아이템 하나를 나타내는 MonoBehaviour 컴포넌트입니다.

■ 런타임 계층 구조
  ItemObject  ← 이 컴포넌트를 보유. 코드에 의해 즉석 생성됨
  └ ItemGroundedModel  ← Resources/Models/Items/{identifier} 에서 로드된 3D 모델

■ 사용 방법
  var obj = ItemObject.Spawn(myStoneItem, transform.position);

 [SceneItemPlacement](MultiplayerInfrastructure.ItemSystem.SceneItemPlacement.md)

에디터에서 아이템을 씬에 컴파일 타임으로 배치할 때 사용하는 컴포넌트입니다.

■ 동작
  Start() 시점에 Registry에서 _itemIdentifier 에 해당하는 Item 인스턴스를 생성하고,
  자신의 위치에 ItemObject.Spawn 을 호출한 뒤 이 GameObject를 제거합니다.

■ 실행 순서
  TTRegistryPreloader.Awake() → (모든 Awake 완료) → SceneItemPlacement.Start()
  Start() 를 사용하므로 레지스트리 등록이 완료된 이후에 안전하게 아이템을 생성합니다.

■ 에디터 사용법
  1. 아이템을 놓을 위치에 빈 GameObject를 생성합니다.
  2. 이 컴포넌트를 추가합니다.
  3. 인스펙터의 드롭다운에서 아이템 종류를 선택합니다.
  4. 필요하면 Stack Count 를 조정합니다.

■ 주의
  _itemIdentifier 가 비어 있거나 Registry에 등록되지 않은 값이면 아무것도 스폰되지 않습니다.

 [StaticObjectDisplayment](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.md)

맵에 사전 배치되어 있으며 다른 제어 흐름(상호작용/시나리오 등)에 의해 표시(Show)/비표시(Hide)될 수 있는
정적 오브젝트의 공통 골격(추상 베이스)입니다.

<p>
<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 과 마찬가지로 Rigidbody 물리로 스폰하지 않으며, 네트워크 스폰 오브젝트가
아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"입니다. Interactable 하며(상호작용 항목이 있고),
상호작용/외부 흐름에 따라 렌더러 또는 게임오브젝트를 켜고 끌 수 있습니다.
(편의를 위해 Registry 에는 <xref href="MultiplayerInfrastructure.Registry.EntityType.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 로 등록됩니다.)
</p>

<p>
<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 는 "획득 상호작용 → 설정에 따라 표시 On/Off" 라는 하나의 구체적인 동작을
가지지만, <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment" data-throw-if-not-resolved="false"></xref> 는 <b>구조만</b> 공유합니다. 실제 상호작용 동작
(무엇을 소비/적용하고 언제 표시할지)은 파생 구현마다 다르므로, <xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.Interact(UnityEngine.Transform)" data-throw-if-not-resolved="false"></xref> 와
<xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment.CanInteract(UnityEngine.Transform)" data-throw-if-not-resolved="false"></xref> 를 파생 클래스가 정의합니다. 베이스는 등록/콜라이더 보장/표시 토글 유틸리티만
제공합니다.
</p>

 [StaticObjectDisplaymentService](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentService.md)

<xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment" data-throw-if-not-resolved="false"></xref> 의 "표시(적용/설치) 여부"를 서버 권위로 관리하는 정적 서비스입니다.

<p>
<xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment" data-throw-if-not-resolved="false"></xref> 는 네트워크 오브젝트가 아니라 각 프로세스에 로컬로 존재하는
"맵의 일부"이므로, 표시 상태의 진실 원천(source of truth)은 서버에만 존재해야 합니다. 이 서비스가
entityIdentifier 단위로 "표시됨(설치/적용됨)" 집합을 보관하며, 신규 접속자 동기화 시 이 상태를 사용합니다.
(<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItemService" data-throw-if-not-resolved="false"></xref> 와 동일한 서버 권위 · static 상태 설계.)
</p>

<p>모든 변이 메서드는 서버에서만 호출되어야 합니다.</p>

 [StaticPlacedItem](MultiplayerInfrastructure.ItemSystem.StaticPlacedItem.md)

유니티 에디터에 사전 배치되는 정적 아이템입니다.

<p>
<xref href="MultiplayerInfrastructure.ItemSystem.ItemObject" data-throw-if-not-resolved="false"></xref>(월드 드롭 아이템)와 달리 Rigidbody 물리로 스폰/산란하지 않습니다.
네트워크 스폰 오브젝트가 아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"이며, 엔티티가 아닌 것에 가깝지만
(Registry에는 편의를 위해 <xref href="MultiplayerInfrastructure.Registry.EntityType.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 로 등록됨) Interactable 합니다.
상호작용(획득)하면 아이템을 얻고, 설정에 따라 맵에서 사라지게 할 수 있습니다.
</p>

<p>
배치가 에디터 표현 그대로 유지되므로 서버 시작 시 위치가 흩어지거나 서로 충돌하는 문제가 없습니다.
</p>

<p>
상태(Remains)의 진실 원천은 서버이며 <xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItemService" data-throw-if-not-resolved="false"></xref> 가 관리합니다.
상호작용 요청은 <xref href="MultiplayerInfrastructure.Player.PlayerController" data-throw-if-not-resolved="false"></xref> 의 서버 권위 픽업 프로토콜로 위임됩니다.
</p>

 [StaticPlacedItemService](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemService.md)

<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 의 남은 획득 가능 횟수(Remains)를 서버 권위로 관리하는 정적 서비스입니다.

<p>
StaticPlacedItem 은 네트워크 오브젝트가 아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"이므로,
상태의 진실 원천(source of truth)은 서버에만 존재해야 합니다. 이 서비스가 그 역할을 합니다.
</p>

<p>모든 변이 메서드는 서버에서만 호출되어야 합니다(PlayerTagService 와 동일한 규약).</p>

키 구성:
- 전역(Global) 모드: entityIdentifier 하나당 Remains 하나.
- 로컬(Local) 모드: (entityIdentifier, userIdentifier) 조합당 Remains 하나.

### Structs

 [ItemCombineRecipe.RecipeIngredient](MultiplayerInfrastructure.ItemSystem.ItemCombineRecipe.RecipeIngredient.md)

레시피 재료 항목입니다.

 [StaticPlacedItemPickupReward](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemPickupReward.md)

<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 을 1회 획득(Pickup)할 때 지급되는 보상 및 상태 변화 정의입니다.
(사양의 OnPickupTry)

 [StaticPlacedItemState](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemState.md)

<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 의 런타임 상태 정의입니다. (사양의 State)

인스펙터에 배치된 값은 "초기 상태"이며, 런타임 중 서버가 권위 있게 관리합니다.
- <xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItemVanishMode.VanishedGlobalOnPickup" data-throw-if-not-resolved="false"></xref>: 서버 전역에서 하나의 Remains.
- <xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItemVanishMode.VanishedLocalOnPickup" data-throw-if-not-resolved="false"></xref>: 플레이어(UserIdentifier)마다 이 초기값에서 시작.

 [TypedActionResult<T\>](MultiplayerInfrastructure.ItemSystem.TypedActionResult\-1.md)

### Enums

 [ActionResult](MultiplayerInfrastructure.ItemSystem.ActionResult.md)

 [StaticObjectDisplaymentShareMode](MultiplayerInfrastructure.ItemSystem.StaticObjectDisplaymentShareMode.md)

<xref href="MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment" data-throw-if-not-resolved="false"></xref> 의 "표시(설치/적용)" 상태를 어떻게 다룰지 결정합니다.

 [StaticPlacedItemVanishBehavior](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemVanishBehavior.md)

<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 이 특정 플레이어에게 "사라짐(Vanished)" 상태가 되었을 때의 표현 방식입니다.

어떤 값이든 사라짐 상태에서는 상호작용(획득)이 항상 비활성화됩니다.
(보이지 않는데 계속 획득 가능한 혼란을 막기 위함)

 [StaticPlacedItemVanishMode](MultiplayerInfrastructure.ItemSystem.StaticPlacedItemVanishMode.md)

<xref href="MultiplayerInfrastructure.ItemSystem.StaticPlacedItem" data-throw-if-not-resolved="false"></xref> 의 획득 처리(Pickup) 방식입니다.

