using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 에디터에서 아이템을 씬에 컴파일 타임으로 배치할 때 사용하는 컴포넌트입니다.
  ///
  /// ■ 동작
  ///   Start() 시점에 Registry에서 _itemIdentifier 에 해당하는 Item 인스턴스를 생성하고,
  ///   자신의 위치에 ItemObject.Spawn 을 호출한 뒤 이 GameObject를 제거합니다.
  ///
  /// ■ 실행 순서
  ///   TTRegistryPreloader.Awake() → (모든 Awake 완료) → SceneItemPlacement.Start()
  ///   Start() 를 사용하므로 레지스트리 등록이 완료된 이후에 안전하게 아이템을 생성합니다.
  ///
  /// ■ 에디터 사용법
  ///   1. 아이템을 놓을 위치에 빈 GameObject를 생성합니다.
  ///   2. 이 컴포넌트를 추가합니다.
  ///   3. 인스펙터의 드롭다운에서 아이템 종류를 선택합니다.
  ///   4. 필요하면 Stack Count 를 조정합니다.
  ///
  /// ■ 주의
  ///   _itemIdentifier 가 비어 있거나 Registry에 등록되지 않은 값이면 아무것도 스폰되지 않습니다.
  /// </summary>
  public class SceneItemPlacement : MonoBehaviour
  {
    [SerializeField] private string _itemIdentifier;
    [SerializeField, Min(1)] private int _stackCount = 1;

    /// <summary>인스펙터에서 설정한 아이템 식별자입니다.</summary>
    public string ItemIdentifier => _itemIdentifier;

    private void Start()
    {
      if (string.IsNullOrWhiteSpace(_itemIdentifier))
      {
        Debug.LogWarning($"[SceneItemPlacement] '{gameObject.name}': itemIdentifier가 비어 있습니다. 스폰하지 않습니다.");
        Destroy(gameObject);
        return;
      }

      var item = Registry.Registry.CreateItemInstance(_itemIdentifier);
      if (item == null)
      {
        Debug.LogWarning($"[SceneItemPlacement] '{gameObject.name}': identifier '{_itemIdentifier}' 를 Registry에서 찾을 수 없습니다.");
        Destroy(gameObject);
        return;
      }

      item.CurrentStackCount = Mathf.Max(1, _stackCount);

      var spawned = ItemObject.Spawn(item, transform.position);
      if (spawned != null)
        spawned.transform.rotation = transform.rotation;

      Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
      bool hasId = !string.IsNullOrWhiteSpace(_itemIdentifier);

      // 배치 마커 구 (식별자 있으면 초록, 없으면 빨강)
      Gizmos.color = hasId
        ? new Color(0.2f, 0.9f, 0.3f, 0.55f)
        : new Color(0.95f, 0.2f, 0.2f, 0.55f);
      Gizmos.DrawSphere(transform.position, 0.15f);

      // 와이어프레임 구
      Gizmos.color = hasId
        ? new Color(0.1f, 0.7f, 0.2f, 1f)
        : new Color(0.8f, 0.1f, 0.1f, 1f);
      Gizmos.DrawWireSphere(transform.position, 0.15f);

      // 라벨
      string label = hasId ? _itemIdentifier : "(identifier 미설정)";
      UnityEditor.Handles.Label(
        transform.position + Vector3.up * 0.26f,
        label,
        new GUIStyle(UnityEditor.EditorStyles.miniLabel)
        {
          normal = { textColor = hasId ? Color.green : Color.red }
        });
    }
#endif
  }
}
