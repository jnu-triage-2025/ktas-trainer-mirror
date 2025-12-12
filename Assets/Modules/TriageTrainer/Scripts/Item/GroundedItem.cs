using FishNet.Object;
using TriageTrainer.Scripts.InteractableEntity;
using UnityEngine;

/// <remarks>
/// 게임이 시작되면 실제로 사용되는 아이템 정보는 Item Instance Model이지만,
/// Grounded Item은 Item Base Model을 기준으로 아이템 정보를 인스턴스화합니다.
/// 
/// 아이템 기본값 정보로부터 이 아이템 인스턴스를 생성하고자 한다면 Item Base Model을 사용하세요.
/// 만약 이 아이템은 일시적으로 특별한 속성을 가지도록 의도되었다면 Item Instance Model에 값을 추가하세요. 
/// 
/// Item Instance Model은 모든 필드가 채워져야 유효합니다.
/// </remarks>
public partial class GroundedItem : NetworkBehaviour, IInteractable
{
  [Header("Item Data")]
  [SerializeField] private ItemBaseModelSO _itemBaseModel;
  [SerializeField] private ItemInstanceModelDTO _itemInstanceModel;

  [SerializeField] private string _itemIdentifier;
  public string ItemIdentifier => _itemIdentifier;

  public Sprite Icon
  {
    get
    {
      return ItemRegistry.Instance.GetItemIcon(_itemInstanceModel.identifier);
    }
  }

  void Awake()
  {
    if (!(
      _itemInstanceModel != null &&
      _itemInstanceModel.IsValid()
    ))
    {
      if (_itemBaseModel == null)
      {
        Debug.LogError("GroundedItem은 유효한 Item Base Model 또는 Item Instance Model이 필요합니다.");
        return;
      }

      _itemInstanceModel = new ItemInstanceModelDTO(_itemBaseModel);
    }
  }
}
