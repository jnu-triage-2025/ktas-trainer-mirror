using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 월드 오브젝트에 붙여 효과음을 자신의 현재 위치에서 서버 전역 재생하도록 하는 진입점입니다.
  /// 애니메이션 이벤트, 상호작용 코드, UnityEvent에서 <see cref="Play"/>를 호출할 수 있습니다.
  /// </summary>
  public sealed class NetworkSoundEmitter : MonoBehaviour
  {
    [SerializeField, Tooltip("Resources/Sound 아래의 AudioClip 식별자입니다.")]
    private string _soundResourceIdentifier;

    [SerializeField, Range(0f, 1f), Tooltip("효과음 음량입니다.")]
    private float _volume = 1f;

    [SerializeField, Range(0f, 1f), Tooltip("0은 2D, 1은 완전한 3D 재생입니다.")]
    private float _spatialBlend = 1f;

    /// <summary>현재 Transform의 월드 좌표를 기준으로 효과음을 공유 재생합니다.</summary>
    [ContextMenu("Play Shared Sound")]
    public bool Play()
    {
      return SoundService.PlayAtPosition(_soundResourceIdentifier, transform.position, _volume, _spatialBlend);
    }

    /// <summary>호출자가 지정한 월드 좌표를 기준으로 효과음을 공유 재생합니다.</summary>
    public bool PlayAt(Vector3 position)
    {
      return SoundService.PlayAtPosition(_soundResourceIdentifier, position, _volume, _spatialBlend);
    }
  }
}
