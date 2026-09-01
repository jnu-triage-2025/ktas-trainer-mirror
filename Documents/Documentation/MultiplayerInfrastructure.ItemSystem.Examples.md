# <a id="MultiplayerInfrastructure_ItemSystem_Examples"></a> Namespace MultiplayerInfrastructure.ItemSystem.Examples

### Classes

 [StoneBlock](MultiplayerInfrastructure.ItemSystem.Examples.StoneBlock.md)

돌 블록 아이템 구현 예시입니다.

■ 등록 방법 (게임 초기화 코드에서 1회):
  Registry.Registry.RegisterItemDefinition&lt;StoneBlock&gt;(StoneBlock.Identifier);

■ 인스턴스 생성:
  var stone = Registry.Registry.CreateItemInstance(StoneBlock.Identifier) as StoneBlock;

■ 월드에 스폰:
  ItemObject.Spawn(stone, spawnPosition);

 [TemplateItem](MultiplayerInfrastructure.ItemSystem.Examples.TemplateItem.md)

[TODO: 아이템 설명 작성]

■ 등록 (게임 초기화 코드에서 1회):
  Registry.Registry.RegisterItemDefinition&lt;TemplateItem&gt;(TemplateItem.Identifier);

■ 인스턴스 생성:
  var item = Registry.Registry.CreateItemInstance(TemplateItem.Identifier) as TemplateItem;

■ 월드에 스폰:
  ItemObject.Spawn(item, spawnPosition);

 [WoodBlock](MultiplayerInfrastructure.ItemSystem.Examples.WoodBlock.md)

나무 블록 아이템 구현 예시입니다.

■ 등록 방법 (게임 초기화 코드에서 1회):
  Registry.Registry.RegisterItemDefinition&lt;WoodBlock&gt;(WoodBlock.Identifier);

■ 인스턴스 생성:
  var wood = Registry.Registry.CreateItemInstance(WoodBlock.Identifier) as WoodBlock;

■ 월드에 스폰:
  ItemObject.Spawn(wood, spawnPosition);

