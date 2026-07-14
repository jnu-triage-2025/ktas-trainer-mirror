using System;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// <see cref="StaticObjectDisplayment"/> 의 "표시(설치/적용)" 상태를 어떻게 다룰지 결정합니다.
  /// </summary>
  [Serializable]
  public enum StaticObjectDisplaymentShareMode
  {
    /// <summary>
    /// (기본값) 표시 상태를 서버 권위로 관리하고 모든 클라이언트에 전파합니다.
    /// 서버가 진실 원천(<see cref="StaticObjectDisplaymentService"/>)이 되어 신규 접속자에게도 동기화됩니다.
    /// </summary>
    ServerShared = 0,

    /// <summary>
    /// 표시 상태를 각 로컬 세션(프로세스)에서만 유지하고 네트워크로 전파하지 않습니다.
    /// 상호작용한 클라이언트에서만 즉시 표시되며, 다른 클라이언트나 신규 접속자에게는 반영되지 않습니다.
    /// </summary>
    LocalOnly,
  }
}
