using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 이름으로 지정한 자식 오브젝트 표시(<see cref="PatientController.SetNamedChildActive"/>)의
  /// 네트워크 동기화 파셜.
  ///
  /// <para>
  /// 자식 토글 자체는 로컬 <c>GameObject.SetActive</c> 연산이다. 이 표시를 바꾸는 호출자는
  /// 시나리오 이벤트(예: <c>attach_defibrillatorpad</c>)이고 시나리오 그래프는 서버만 순회하므로,
  /// 로컬 적용만 하면 제세동 패드 같은 표시가 서버 피어에서만 보이고 원격 피어에서는 프리팹의
  /// 초기 숨김 상태에 머문다.
  /// </para>
  ///
  /// <para>
  /// <see cref="PatientController.SetTreatmentDisplayNetworked"/> 와 달리 여기서는
  /// <c>BufferLast</c> 에 의존하지 않는다. FishNet 은 RPC 메서드마다 마지막 호출 하나만
  /// 버퍼링하므로, 한 이벤트가 패드 두 개를 연달아 켜면 늦게 입장한 피어는 마지막 하나만 받는다.
  /// 대신 서버가 덮어쓴 표시를 모아 두었다가, 관찰자가 이 오브젝트를 관측하기 시작할 때
  /// 전체 스냅숏을 보낸다.
  /// </para>
  /// </summary>
  public partial class PatientController
  {
    /// <summary>서버가 프리팹 초기 상태와 다르게 덮어쓴 자식 표시(자식 이름 → 활성 여부).</summary>
    private readonly Dictionary<string, bool> _namedChildDisplayOverrides = new(StringComparer.Ordinal);

    /// <summary>
    /// 서버 컨텍스트에서 자식 표시 변경을 기록하고 모든 관찰자에게 전파한다.
    /// 오프라인이거나 원격 클라이언트에서 호출된 경우에는 로컬 적용만 유효하다.
    /// </summary>
    private void PublishNamedChildActive(string childName, bool active)
    {
      if (!IsFishNetServerStarted || string.IsNullOrWhiteSpace(childName))
        return;

      _namedChildDisplayOverrides[childName.Trim()] = active;
      RpcSetNamedChildActive(childName, active);
    }

    /// <summary>
    /// 관찰자가 이 환자를 관측하기 시작할 때(최초 스폰 및 늦은 입장) 누적된 표시 덮어쓰기를 보낸다.
    /// </summary>
    public override void OnSpawnServer(NetworkConnection connection)
    {
      base.OnSpawnServer(connection);

      if (_namedChildDisplayOverrides.Count == 0)
        return;

      var childNames = new string[_namedChildDisplayOverrides.Count];
      var activeStates = new bool[_namedChildDisplayOverrides.Count];
      int index = 0;
      foreach (var pair in _namedChildDisplayOverrides)
      {
        childNames[index] = pair.Key;
        activeStates[index] = pair.Value;
        index++;
      }

      TargetSyncNamedChildDisplays(connection, childNames, activeStates);
    }

    [ObserversRpc]
    private void RpcSetNamedChildActive(string childName, bool active)
    {
      // 호스트는 서버 경로에서 이미 같은 인스턴스에 적용했다.
      if (IsFishNetServerStarted)
        return;

      ApplyNamedChildActiveLocal(childName, active);
    }

    [TargetRpc]
    private void TargetSyncNamedChildDisplays(
      NetworkConnection connection,
      string[] childNames,
      bool[] activeStates)
    {
      if (IsFishNetServerStarted || childNames == null || activeStates == null)
        return;

      int count = Math.Min(childNames.Length, activeStates.Length);
      for (int i = 0; i < count; i++)
        ApplyNamedChildActiveLocal(childNames[i], activeStates[i]);
    }
  }
}
