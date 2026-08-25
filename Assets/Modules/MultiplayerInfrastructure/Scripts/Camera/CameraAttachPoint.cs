using UnityEngine;

namespace MultiplayerInfrastructure.Camera
{
  /// <summary>
  /// 플레이어(또는 관전 대상) 측에 위치하는 카메라 부착 지점입니다.
  ///
  /// 이전에는 <c>PlayerController</c>가 "CameraHolder"라는 이름의 GameObject를 직접 생성하여
  /// 카메라 피벗 겸 뷰모델 부착점으로 사용했습니다. 그러나 "카메라를 실제로 들고 다니는 래퍼"(<see cref="CameraHolder"/>)와
  /// "카메라가 와서 붙는 대상"의 책임이 하나의 Transform에 뒤섞여 있어, 관전자 위치 표시 등
  /// 부착점 고유의 시각 표현을 확장하기 어려웠습니다.
  ///
  /// <see cref="CameraAttachPoint"/>는 그 "부착 대상" 책임만을 담당합니다.
  /// - 상하 시야각(pitch) 피벗 회전을 보관/적용합니다.
  /// - 뷰모델·아이템 등 카메라 기준으로 배치되어야 하는 오브젝트의 부모(부착점)를 제공합니다.
  /// - 카메라 래퍼(<see cref="CameraHolder"/>)는 이 지점에 <see cref="CameraHolder.AttachTo"/>로 부착됩니다.
  ///
  /// 향후 관전자 위치 마커 등 부착점 고유의 표현은 이 컴포넌트(또는 그 자식)에 추가합니다.
  /// </summary>
  public class CameraAttachPoint : MonoBehaviour
  {
    private Collider[] _ownerColliders;

    /// <summary>
    /// 카메라 래퍼가 부착되어 따라가는 Transform입니다. 이 컴포넌트가 붙은 Transform과 동일합니다.
    /// </summary>
    public Transform PivotTransform => transform;

    /// <summary>
    /// 뷰모델·아이템 등을 부착할 기준 Transform입니다.
    /// 현재는 피벗과 동일하지만, 별도 오프셋 노드가 필요해지면 이 접근자만 바꾸면 됩니다.
    /// </summary>
    public Transform AttachmentRoot => transform;

    /// <summary>
    /// 이 부착점을 소유한 오브젝트(플레이어)의 콜라이더 목록입니다.
    /// 카메라 충돌 검사(SphereCast)에서 플레이어 자신을 무시하는 데 사용됩니다.
    /// </summary>
    public Collider[] OwnerColliders => _ownerColliders;

    /// <summary>
    /// 상하 시야각(pitch)을 로컬 회전으로 적용합니다.
    /// 좌우(yaw)는 플레이어 본체 회전이 담당하므로 여기서는 다루지 않습니다.
    /// </summary>
    /// <param name="pitchDegrees">X축(pitch) 각도(도 단위).</param>
    public void SetPitch(float pitchDegrees)
    {
      transform.localRotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
    }

    /// <summary>
    /// 지정한 부모 아래에 "CameraAttachPoint"라는 이름의 부착점 GameObject를 생성합니다.
    /// </summary>
    /// <param name="parent">부착점의 부모(플레이어 루트).</param>
    /// <param name="localHeightOffset">
    /// 부착점을 부모 기준으로 위로 올리는 높이입니다.
    /// 발밑(y=0)에서 캐스트를 시작하면 카메라 충돌 검사가 플레이어 콜라이더 안에서 시작되어
    /// 카메라가 강제로 1인칭으로 당겨지므로, 눈높이 정도로 올립니다.
    /// </param>
    public static CameraAttachPoint Create(Transform parent, float localHeightOffset = 0f)
    {
      var go = new GameObject("CameraAttachPoint");
      go.transform.SetParent(parent);
      go.transform.localPosition = new Vector3(0f, localHeightOffset, 0f);
      go.transform.localRotation = Quaternion.identity;

      var attachPoint = go.AddComponent<CameraAttachPoint>();
      // 부모(플레이어 루트)에 속한 콜라이더를 캐시한다.
      // 부착점 자신은 콜라이더가 없으므로 부모 기준으로 수집한다.
      attachPoint._ownerColliders = parent != null
        ? parent.GetComponentsInChildren<Collider>(true)
        : System.Array.Empty<Collider>();
      return attachPoint;
    }
  }
}
