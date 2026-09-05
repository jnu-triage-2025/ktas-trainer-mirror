using FishNet.Connection;

namespace MultiplayerInfrastructure.Command
{
  /// <summary>
  /// 인자에 따라 권한 검사를 면제할 수 있는 커맨드가 구현하는 선택 인터페이스.
  ///
  /// <para>
  /// <see cref="ChatCommandService"/> 는 <see cref="IChatCommandModel.PermissionIdentifier"/> 검사에
  /// 실패한 뒤에만 이 판단을 묻는다. 즉 권한이 있는 사용자는 이 판단과 무관하게 실행할 수 있고,
  /// 권한이 없는 사용자는 이 메서드가 true 를 돌려주는 인자 조합만 실행할 수 있다.
  /// 데이터팩 별칭이 실행하는 대상 커맨드에도 별칭이 전달할 인자로 같은 판단을 적용한다.
  /// </para>
  ///
  /// <para>
  /// 구현은 인자만 보고 판단해야 하며 부수 효과를 일으키면 안 된다. 인자가 불완전하거나
  /// 해석할 수 없으면 false 를 돌려주어 거부 쪽으로 판단한다.
  /// </para>
  /// </summary>
  public interface IChatCommandPermissionExemption
  {
    /// <summary>
    /// 주어진 인자로 실행할 때 권한 검사를 면제할지 반환한다.
    /// </summary>
    /// <param name="sender">실행 요청자. 서버 콘솔이면 null 일 수 있다.</param>
    /// <param name="args">커맨드 이름을 제외한 인자.</param>
    public bool IsExemptFromPermission(NetworkConnection sender, string[] args);
  }
}
