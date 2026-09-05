using UnityEngine;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 머리 위 이름표를 띄울 위치를 프리팹에서 지정하는 표식 컴포넌트.
  ///
  /// <para>
  /// 이름표를 달 수 있는 엔티티(NPC, 플레이어)의 프리팹 하위에 빈 오브젝트를 만들고 이 컴포넌트를 붙이면,
  /// 그 오브젝트의 위치가 이름표의 앵커가 된다. 부착점이 없으면 각 엔티티가 런타임에 머리 높이를 추정해
  /// 앵커를 만들지만, 추정값은 이동 판정용 캡슐이나 Renderer 바운즈를 근거로 삼기 때문에 실제 머리보다
  /// 높게 잡히기 쉽다. 부착점을 배치하면 작업자가 에디터에서 눈으로 보면서 위치를 정할 수 있다.
  /// </para>
  ///
  /// <para>
  /// 앵커에서 위로 더해지는 여유 간격은 <see cref="EntityOverheadLabelUIController"/> 의
  /// worldHeightOffset 이 담당하므로, 부착점은 머리 끝 높이에 두면 된다.
  /// </para>
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class NameTagDisplayAttachPoint : MonoBehaviour
  {
  }
}
