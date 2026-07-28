using System.Collections;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
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
  public class ItemObject : MonoBehaviour
  {
    // ─── 3D 모델 Resources 루트 경로 ───────────────────────────────────────────
    private const string ModelRootPath = "Models/Items";

    // ─── 공개 접근자 ───────────────────────────────────────────────────────────
    /// <summary>이 ItemObject가 보유한 Item 인스턴스입니다.</summary>
    public Item Item { get; private set; }

    /// <summary>
    /// 서버가 부여한 전역 엔티티 식별자입니다.
    /// null 이면 엔티티 저장소에 등록되지 않습니다.
    /// </summary>
    public string Identifier { get; private set; }

    /// <summary>로드된 3D 모델 자식 오브젝트입니다. 모델이 없으면 null 입니다.</summary>
    public GameObject GroundedModel { get; private set; }

    // ─── 내부 ──────────────────────────────────────────────────────────────────
    private Coroutine _animCoroutine;
    private bool _isGrounded;
    private float _nextAutoPickupAttemptTime;
    private Vector3 _groundedPosition;
    private Quaternion _groundedRotation;
    private bool _groundedPoseInitialized;
    private static PlayerController _localPlayer;

    private const float GroundedBobAmplitude = 0.02f;
    private const float GroundedBobFrequency = 0.5f;
    private const float GroundedRotationSpeed = 20f;
    private const float AutoPickupRadius = 1.3f;
    private const float AutoPickupRetryInterval = 0.5f;

    // ─── 팩토리 메서드 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 지정 위치에 Item을 나타내는 ItemObject를 즉석으로 생성합니다.
    /// Collider는 자동으로 추가되며, 이동은 Rigidbody 없이 transform으로 처리됩니다.
    /// </summary>
    /// <param name="item">데이터 소스. null 불가.</param>
    /// <param name="position">월드 스폰 위치.</param>
    /// <param name="throwForce">
    /// 기존 호출부 호환을 위해 유지되는 인자입니다. 현재 월드 아이템은 물리 impulse를 사용하지 않습니다.
    /// </param>
    public static ItemObject Spawn(Item item, Vector3 position, Vector3? throwForce = null, string entityIdentifier = null)
    {
      if (item == null)
      {
        Debug.LogWarning("[ItemObject] Spawn: item is null.");
        return null;
      }

      var go = new GameObject($"ItemObject_{item.CurrentIdentifier}");
      go.transform.position = position;

      int pickupLayer = LayerMask.NameToLayer("PickupItem");
      go.layer = pickupLayer >= 0 ? pickupLayer : 0;

      // 박스 콜라이더 기본 추가 (모델 로드 후 적절히 조정 가능)
      var collider = go.AddComponent<BoxCollider>();
      collider.size = Vector3.one * 0.3f;
      collider.isTrigger = true;

      var comp = go.AddComponent<ItemObject>();
      comp.Initialize(item, entityIdentifier);

      // Rigidbody 물리 대신 transform 기반으로 부유/회전한다.
      // throwForce는 기존 호출부 호환을 위해 인자로만 유지한다.
      comp.SetGrounded();

      return comp;
    }

    // ─── 초기화 ───────────────────────────────────────────────────────────────

    private void Update()
    {
      AnimateGroundedState();
      TryAutoPickup();
    }

    private void SetGrounded()
    {
      _isGrounded = true;
      _groundedPoseInitialized = false;
    }

    private void AnimateGroundedState()
    {
      // SceneItemPlacement가 Spawn 직후 회전을 적용할 수 있으므로 첫 프레임에 기준 포즈를 캡처한다.
      if (!_groundedPoseInitialized)
      {
        _groundedPosition = transform.position;
        _groundedRotation = transform.rotation;
        _groundedPoseInitialized = true;
      }

      float elapsed = Time.time;
      transform.position = _groundedPosition + Vector3.up *
        (Mathf.Sin(elapsed * Mathf.PI * 2f * GroundedBobFrequency) * GroundedBobAmplitude);
      transform.rotation = _groundedRotation *
        Quaternion.Euler(0f, elapsed * GroundedRotationSpeed, 0f);
    }

    private void TryAutoPickup()
    {
      if (Time.time < _nextAutoPickupAttemptTime || Item == null)
        return;

      var player = ResolveLocalPlayer();
      if (player == null)
        return;

      if ((player.transform.position - transform.position).sqrMagnitude >
          AutoPickupRadius * AutoPickupRadius)
        return;

      _nextAutoPickupAttemptTime = Time.time + AutoPickupRetryInterval;
      if (string.IsNullOrWhiteSpace(Identifier))
        player.TryPickupWorldItem(this);
      else
        player.TryPickupWorldItem(Identifier);
    }

    private static PlayerController ResolveLocalPlayer()
    {
      if (_localPlayer != null && _localPlayer.IsOwner)
        return _localPlayer;

      _localPlayer = Registry.Registry.GetFirstEntityComponent<PlayerController>(
        EntityType.Player,
        each => each != null && each.IsOwner);
      return _localPlayer;
    }

    /// <summary>서버 권위 물리 동기화에 사용하는 현재 상태입니다.</summary>
    public bool IsGrounded => _isGrounded;
    public Vector3 AuthoritativePosition
      => _isGrounded && _groundedPoseInitialized ? _groundedPosition : transform.position;
    public Quaternion AuthoritativeRotation
      => _isGrounded && _groundedPoseInitialized ? _groundedRotation : transform.rotation;
    /// <summary>서버에서 받은 물리/부유 상태를 클라이언트 표현에 적용합니다.</summary>
    public void ApplyAuthoritativeState(
      Vector3 position,
      Quaternion rotation,
      bool grounded)
    {
      _isGrounded = grounded;
      transform.SetPositionAndRotation(position, rotation);
      _groundedPosition = position;
      _groundedRotation = rotation;
      _groundedPoseInitialized = grounded;
    }

    private void OnDestroy()
    {
      if (!string.IsNullOrWhiteSpace(Identifier))
        Registry.Registry.UnregisterEntity(Identifier);
    }

    private void Initialize(Item item, string entityIdentifier)
    {
      Item = item;
      Identifier = entityIdentifier;
      LoadModel();
      ConfigureNonBlockingColliders();

      if (!string.IsNullOrWhiteSpace(Identifier))
      {
        Registry.Registry.RegisterEntity(Identifier, EntityType.ItemObject, gameObject, displayName: gameObject.name);
      }
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
        GroundedModel = Instantiate(prefab);
        GroundedModel.name = "ItemGroundedModel";
        GroundedModel.gameObject.SetActive(true);
        GroundedModel.transform.SetParent(transform, false);
        GroundedModel.transform.localPosition = Vector3.zero;
        GroundedModel.transform.localRotation = Quaternion.identity;
        RemoveModelRigidbodies();
      }
      else
      {
        if (!ItemMissingAssetSuppression.ShouldSuppressModelMissingWarning(Item))
        {
          Debug.LogWarning(
            $"[ItemObject] 모델 프리팹을 찾지 못했습니다. (경로: Resources/{path}) " +
            $"기본 큐브로 대체합니다. 의도된 누락이면 {Item.GetType().Name} 클래스에 " +
            $"[IntendedMissing3DModel] 특성을 적용하세요.");
        }
        GroundedModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GroundedModel.name  = "ItemGroundedModel";
        GroundedModel.transform.SetParent(transform, false);
        GroundedModel.transform.localScale = Vector3.one * 0.25f;
        // 기본 BoxCollider는 이미 추가했으므로 큐브의 콜라이더는 제거
        var primCollider = GroundedModel.GetComponent<Collider>();
        if (primCollider != null) Destroy(primCollider);
      }
    }

    /// <summary>
    /// 모델 프리팹에 포함된 Rigidbody를 제거합니다.
    /// 월드 아이템의 이동과 회전은 ItemObject가 transform으로 직접 처리하므로,
    /// 모델 자체의 Rigidbody가 별도 물리 시뮬레이션을 시작하면 회전 중심이 분리됩니다.
    /// </summary>
    private void RemoveModelRigidbodies()
    {
      var rigidbodies = GetComponentsInChildren<Rigidbody>(includeInactive: true);
      for (int i = 0; i < rigidbodies.Length; i++)
      {
        var rigidbody = rigidbodies[i];
        if (rigidbody == null)
          continue;

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        Destroy(rigidbody);
      }
    }

    /// <summary>
    /// 월드 아이템의 Collider는 감지/거리 계산 용도일 뿐 플레이어 이동을 막으면 안 됩니다.
    /// 특히 Rigidbody를 제거한 모델 프리팹의 일반 Collider는 정적 장애물처럼 동작할 수 있으므로
    /// 아이템 계층 전체를 Trigger로 강제한다.
    /// </summary>
    private void ConfigureNonBlockingColliders()
    {
      var colliders = GetComponentsInChildren<Collider>(includeInactive: true);
      for (int i = 0; i < colliders.Length; i++)
      {
        if (colliders[i] != null)
          colliders[i].isTrigger = true;
      }
    }

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
