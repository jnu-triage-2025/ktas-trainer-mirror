using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;

namespace TriageTrainer.Utils
{
  /// <summary>
  /// 역할 태그로 잠근 게임플레이 판정에 "담당자 부재" 대체 규칙을 적용한다.
  /// <para>
  /// 운영 시나리오는 모니터 닫기(nurse_b), 중심정맥 라인(nurse_c), 산소 처치(nurse_d)처럼 특정 역할만
  /// 수행할 수 있는 판정을 서버에서 강제한다. 담당자가 이탈했거나 재접속으로 태그를 잃으면 그 판정은
  /// 아무도 통과할 수 없고, 그 신호를 기다리는 게이트는 상한까지 헛되이 기다린다. 접속 중인 어떤
  /// 플레이어도 요구 역할을 갖고 있지 않을 때에만 다른 플레이어의 수행을 허용한다.
  /// </para>
  /// </summary>
  public static class TriageRoleGate
  {
    /// <summary>
    /// <paramref name="userIdentifier"/> 가 <paramref name="requiredRoleTag"/> 를 갖고 있거나, 접속 중인
    /// 플레이어 가운데 그 역할을 가진 사람이 아무도 없으면 참을 돌려준다.
    /// </summary>
    public static bool IsAllowed(string userIdentifier, string requiredRoleTag)
    {
      if (string.IsNullOrWhiteSpace(requiredRoleTag))
        return true;
      if (string.IsNullOrWhiteSpace(userIdentifier))
        return false;
      if (PlayerTagService.HasTag(userIdentifier, requiredRoleTag))
        return true;
      return !IsRoleHeldByAnyConnectedPlayer(requiredRoleTag);
    }

    /// <summary>접속 중인 플레이어 가운데 <paramref name="roleTag"/> 를 가진 사람이 있는지 확인한다.</summary>
    public static bool IsRoleHeldByAnyConnectedPlayer(string roleTag)
    {
      if (string.IsNullOrWhiteSpace(roleTag))
        return false;
      foreach (var pair in UserDescriptorService.GetAll())
      {
        var descriptor = pair.Value;
        if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.Identifier))
          continue;
        if (PlayerTagService.HasTag(descriptor.Identifier, roleTag))
          return true;
      }
      return false;
    }
  }
}
