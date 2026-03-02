using System.Collections;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// 인게임에서 아이템 하나를 나타내는 MonoBehaviour 컴포넌트입니다.
  ///
  /// ■ 런타임 계층 구조
  ///   ItemObject  ← 이 컴포넌트를 보유. 코드에 의해 즉석 생성됨
  ///   └ ItemGroundedModel  ← Resources/Models/Items/{identifier} 에서 로드된 3D 모델
  ///
  /// ■ 사용 방법
  ///   var obj = ItemObject.Spawn(myStoneItem, transform.position);
  /// </summary>
  [RequireComponent(typeof(Rigidbody))]
  public class ItemObject : MonoBehaviour
  {
    // ─── 3D 모델 Resources 루트 경로 ───────────────────────────────────────────
    private const string ModelRootPath = "Models/Items";

    // ─── 공개 접근자 ───────────────────────────────────────────────────────────
    /// <summary>이 ItemObject가 보유한 Item 인스턴스입니다.</summary>
    public Item Item { get; private set; }

    /// <summary>로드된 3D 모델 자식 오브젝트입니다. 모델이 없으면 null 입니다.</summary>
    public GameObject GroundedModel { get; private set; }

    // ─── 내부 ──────────────────────────────────────────────────────────────────
    private Rigidbody _rigidbody;
    private Coroutine _animCoroutine;

    // ─── 팩토리 메서드 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 지정 위치에 Item을 나타내는 ItemObject를 즉석으로 생성합니다.
    /// Rigidbody, Collider는 자동으로 추가됩니다.
    /// </summary>
    /// <param name="item">데이터 소스. null 불가.</param>
    /// <param name="position">월드 스폰 위치.</param>
    /// <param name="throwForce">
    /// 0보다 크면 Rigidbody에 해당 방향으로 impulse를 가합니다. (드롭/던지기)
    /// </param>
    public static ItemObject Spawn(Item item, Vector3 position, Vector3? throwForce = null)
    {
      if (item == null)
      {
        Debug.LogWarning("[ItemObject] Spawn: item is null.");
        return null;
      }

      var go = new GameObject($"ItemObject_{item.CurrentIdentifier}");
      go.transform.position = position;
      go.layer = LayerMask.NameToLayer("PickupItem");

      // 박스 콜라이더 기본 추가 (모델 로드 후 적절히 조정 가능)
      var collider = go.AddComponent<BoxCollider>();
      collider.size = Vector3.one * 0.3f;

      var comp = go.AddComponent<ItemObject>();
      comp.Initialize(item);

      if (throwForce.HasValue && throwForce.Value.sqrMagnitude > 0.0001f)
        comp._rigidbody.AddForce(throwForce.Value, ForceMode.Impulse);

      return comp;
    }

    // ─── 초기화 ───────────────────────────────────────────────────────────────

    private void Awake()
    {
      _rigidbody = GetComponent<Rigidbody>();
    }

    private void Initialize(Item item)
    {
      Item = item;
      LoadModel();
    }

    /// <summary>
    /// Resources/Models/Items/{identifier} 에서 프리팹을 로드하여 자식으로 인스턴스화합니다.
    /// 프리팹이 없으면 기본 큐브를 대신 사용합니다.
    /// </summary>
    private void LoadModel()
    {
      if (Item == null) return;

      string path = $"{ModelRootPath}/{Item.CurrentIdentifier}";
      var prefab = Resources.Load<GameObject>(path);

      if (prefab != null)
      {
        GroundedModel = Instantiate(prefab, transform);
        GroundedModel.name = "ItemGroundedModel";
        GroundedModel.transform.localPosition = Vector3.zero;
        GroundedModel.transform.localRotation = Quaternion.identity;
      }
      else
      {
        Debug.LogWarning(
          $"[ItemObject] 모델 프리팹을 찾지 못했습니다. (경로: Resources/{path}) " +
          $"기본 큐브로 대체합니다.");
        GroundedModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GroundedModel.name  = "ItemGroundedModel";
        GroundedModel.transform.SetParent(transform, false);
        GroundedModel.transform.localScale = Vector3.one * 0.25f;
        // 기본 BoxCollider는 이미 추가했으므로 큐브의 콜라이더는 제거
        var primCollider = GroundedModel.GetComponent<Collider>();
        if (primCollider != null) Destroy(primCollider);
      }
    }

    // ─── 애니메이션 ───────────────────────────────────────────────────────────

    /// <summary>공격 시 아이템을 앞으로 짧게 밀었다가 되돌리는 애니메이션입니다.</summary>
    public void TriggerAttackAnimation()
    {
      if (_animCoroutine != null) StopCoroutine(_animCoroutine);
      _animCoroutine = StartCoroutine(AnimAttack());
    }

    /// <summary>사용 시 아이템을 위-아래로 짧게 튀기는 애니메이션입니다.</summary>
    public void TriggerUseAnimation()
    {
      if (_animCoroutine != null) StopCoroutine(_animCoroutine);
      _animCoroutine = StartCoroutine(AnimBob());
    }

    private IEnumerator AnimAttack()
    {
      if (GroundedModel == null) yield break;

      const float duration = 0.12f;
      const float distance = 0.15f;
      Vector3 origin = GroundedModel.transform.localPosition;
      Vector3 target = origin + Vector3.forward * distance;

      float t = 0f;
      while (t < 1f)
      {
        t = Mathf.Clamp01(t + Time.deltaTime / (duration * 0.5f));
        GroundedModel.transform.localPosition = Vector3.Lerp(origin, target, t);
        yield return null;
      }
      t = 0f;
      while (t < 1f)
      {
        t = Mathf.Clamp01(t + Time.deltaTime / (duration * 0.5f));
        GroundedModel.transform.localPosition = Vector3.Lerp(target, origin, t);
        yield return null;
      }
      GroundedModel.transform.localPosition = origin;
      _animCoroutine = null;
    }

    private IEnumerator AnimBob()
    {
      if (GroundedModel == null) yield break;

      const float duration  = 0.18f;
      const float amplitude = 0.10f;
      Vector3 origin = GroundedModel.transform.localPosition;

      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.deltaTime;
        float y = Mathf.Sin(elapsed / duration * Mathf.PI) * amplitude;
        GroundedModel.transform.localPosition = origin + Vector3.up * y;
        yield return null;
      }
      GroundedModel.transform.localPosition = origin;
      _animCoroutine = null;
    }
  }
}
