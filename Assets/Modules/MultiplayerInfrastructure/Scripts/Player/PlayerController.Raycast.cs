using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// PlayerController의 레이캐스트 처리 부분 구현.
  /// 
  /// 매 프레임 카메라 뷰포트 중앙에서 지정된 레이어를 향해 Physics.Raycast를 수행합니다.
  /// 결과는 HasHit, CrosshairHit, HitObject로 노출됩니다.
  /// </summary>
  public partial class PlayerController
  {
    [Header("Raycast Configuration")]
    [Tooltip("크로스헤어 중심선 레이캐스트 대상 레이어 마스크")]
    [SerializeField] private LayerMask _raycastLayerMask = ~0;
    [Tooltip("크로스헤어 중심선 레이캐스트의 최대 거리 (m)")]
    [SerializeField] private float _raycastMaxDistance = 10f;

    // ── 레이캐스트 결과 ──────────────────────────────────────
    /// <summary>이번 프레임 크로스헤어 중심선 레이캐스트가 어떤 콜라이더에 맞았으면 true</summary>
    public bool RaycastHasHit { get; private set; }

    /// <summary>크로스헤어 중심선 레이캐스트의 원시 RaycastHit 결과 (<see cref="RaycastHasHit"/>가 true일 때만 유효)</summary>
    public RaycastHit RaycastHit { get; private set; }

    /// <summary>레이캐스트에 맞은 GameObject, 맞지 않았으면 null</summary>
    public GameObject RaycastHitObject => RaycastHasHit ? RaycastHit.collider.gameObject : null;

    private void Awake_Raycast()
    {
      // 초기화 로직 필요시 추가
    }

    private void Update_Raycast()
    {
      PerformCrosshairRaycast();
    }

    /// <summary>
    /// 카메라 뷰포트 중앙에서 <see cref="_raycastLayerMask"/> 레이어를 향해 레이캐스트를 수행합니다.
    /// 결과는 <see cref="RaycastHasHit"/> / <see cref="RaycastHit"/> 에 저장됩니다.
    /// </summary>
    private void PerformCrosshairRaycast()
    {
      if (!IsOwner)
        return;
      if (_camControl == null)
        return;

      var camera = _camControl.Camera;
      if (camera == null)
        return;

      // 뷰포트 중앙(0.5, 0.5)에서 레이 생성
      var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

      // 광선은 로컬 플레이어 콜라이더 내부에서 시작되는 경우가 많다. 따라서
      // Raycast 한 번으로는 Player(Clone) 이 잡혀 뒤의 월드 오브젝트가 가려진다.
      // 이 PlayerController 에 속하지 않은 가장 가까운 히트를 선택한다.
      var hits = Physics.RaycastAll(ray, _raycastMaxDistance, _raycastLayerMask, QueryTriggerInteraction.Ignore);
      RaycastHit nearestHit = default;
      bool foundHit = false;
      float nearestDistance = float.MaxValue;
      for (int i = 0; i < hits.Length; i++)
      {
        var candidate = hits[i];
        if (candidate.collider == null || candidate.collider.GetComponentInParent<PlayerController>() == this)
          continue;
        if (candidate.distance < nearestDistance)
        {
          nearestDistance = candidate.distance;
          nearestHit = candidate;
          foundHit = true;
        }
      }

      if (foundHit)
      {
        RaycastHasHit = true;
        RaycastHit = nearestHit;
      }
      else
      {
        RaycastHasHit = false;
        RaycastHit = default;
      }
    }
  }
}
