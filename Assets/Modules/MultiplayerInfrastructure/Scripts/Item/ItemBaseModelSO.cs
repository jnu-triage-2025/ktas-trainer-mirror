using System;
using MultiplayerInfrastructure.Item;
using IS = MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Player;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "New Item Base Model", menuName = "MultiplayerInfrastructure/Item Base Model")]
public class ItemBaseModelSO : ScriptableObject
{
  /// <summary>
  /// 이 아이템의 고유 식별자입니다. 아이템 텍스처 로딩을 포함하여 대부분의 처리는 이 값을 기준으로 이루어집니다.
  /// 식별자가 중복되지 않도록 주의하세요. 식별자의 중복 여부는 별도의 구현체*에서 확인합니다.
  /// 식별자는 가능한 한 알파벳 소문자와 언더스코어(_)만을 사용하여 작성하는 것을 권장합니다.
  /// 필요한 경우 수 문자도 사용할 수 있지만, 가능한 한 피해주세요.
  /// 
  /// *작성일 기준 아이템 레지스트리(ItemRegistry)에서 수행
  /// </summary>
  [SerializeField] public string identifier;

  /// <summary>
  /// 아이템의 화면에 표시될 이름입니다.
  /// </summary>
  [SerializeField] public string displayName;

  /// <summary>
  /// 아이템의 설명입니다.
  /// </summary>
  [TextArea] [SerializeField] public string description;

  /// <summary>
  /// 아이템의 내구도 여부를 나타냅니다. 이 값이 참이라면, 게임 시스템은 내구도를 고려하여 아이템 사용을 처리합니다.
  /// 내구도 값을 사용하여 약품의 용액량 등을 표현할 수 있습니다.
  /// </summary>
  [SerializeField] public bool hasDurability = false;

  /// <summary>
  /// 아이템의 기본값 최대 내구도 값입니다. 이 값은 hasDurability가 참일 때만 유효합니다.
  /// </summary>
  [SerializeField] public int maxDurability = 1;

  /// <summary>
  /// 이 아이템의 최대 적재 개수입니다. 1 이상의 값을 가져야 합니다.
  /// </summary>
  [SerializeField] public int maxStackCount = 64;

  // --- 아이템 행동 (서브클래스에서 override하여 커스터마이즈) ---

  public virtual ActionResult OnGet(PlayerController player, IS.Item item)
    => ActionResult.Success;

  public virtual ActionResult OnDrop(PlayerController player, IS.Item item)
    => ActionResult.Success;

  public virtual ActionResult OnAttack(PlayerController player, MultiplayerInfrastructure.Entity.Entity target, IS.Item item)
    => ActionResult.Success;

  public virtual ActionResult OnUse(PlayerController player, MultiplayerInfrastructure.Entity.Entity target, IS.Item item)
    => ActionResult.Success;
}
