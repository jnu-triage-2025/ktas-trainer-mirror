namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 이동식 환자 침대를 지정한 포지셔닝 포인트에 붙이는 노드.
  ///
  /// <para>평소 플레이에서는 플레이어가 침대를 밀어 포인트 근처까지 가져가야 스냅이 걸린다.
  /// 이 노드는 시나리오가 그 결과만 필요할 때 쓴다. 주로 ManualEntrypoint 의 준비 체인에서
  /// 건너뛴 구간이 만들어 놨어야 할 침대 배치를 맞추는 용도다.</para>
  ///
  /// <para>침대에 환자가 결합돼 있으면 환자도 함께 따라온다(결합은 attach_patient_bed_pairs 등이
  /// 먼저 처리한다). 서버 권위에서만 적용되고, 결과는 침대 자신의 RPC 로 각 피어에 전파된다.</para>
  ///
  /// <para><b>대상 포인트가 이미 점유된 경우:</b> 포인트의 점유 정책을 그대로 따른다. 기본 설정에서는
  /// 환자가 실린 다른 침대가 있으면 배치가 거부되고(빈 침대는 치워진다), 이 노드는 실패로 끝난다.
  /// 여러 침대를 연달아 배치할 때는 서로 자리를 맞바꾸는 순서가 되지 않게 주의한다.
  /// 예를 들어 A 를 1번 자리로, B 를 0번 자리로 옮기는데 B 가 이미 1번에 있다면 A 가 먼저 막힌다.</para>
  ///
  /// <para>대상 포인트가 침대 프리팹의 허용 목록에 없으면 실행 시 목록에 보정해 넣는다. 허용 목록은
  /// 플레이어가 밀어서 붙일 수 있는 곳을 제한하는 값이고, 시나리오 지시는 그보다 우선한다.</para>
  /// </summary>
  public sealed class ScenarioBedSnapNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.BedSnap;
    public string NextIdentifier { get; set; }

    /// <summary>대상 침대 엔티티 식별자(예: <c>bed_b</c>).</summary>
    public string BedEntityIdentifier { get; set; }

    /// <summary>
    /// <see cref="BedEntityIdentifier"/> 대신 상태 저장소에서 침대 식별자를 읽어 올 키.
    /// 스폰 노드가 남긴 <c>resultStateKey</c> 를 그대로 넘길 때 쓴다.
    /// 둘 다 있으면 <see cref="BedEntityIdentifier"/> 가 우선한다.
    /// </summary>
    public string BedEntityStateKey { get; set; }

    /// <summary>붙일 포지셔닝 포인트 식별자(예: <c>zone_0:bed_snap_point</c>).</summary>
    public string SnapPointIdentifier { get; set; }

    /// <summary>
    /// true(기본)면 거리와 무관하게 침대를 포인트 위치로 옮긴 뒤 붙인다.
    /// false 면 이미 스냅 범위 안에 있을 때만 붙고, 멀리 있으면 실패로 처리한다.
    /// </summary>
    public bool Teleport { get; set; } = true;

    /// <summary>
    /// true 면 스냅에 실패했을 때 경고만 남기고 다음 노드로 넘어간다(기본).
    /// false 면 실패를 오류로 기록한다. 어느 쪽이든 시나리오는 계속 진행한다.
    /// </summary>
    public bool IgnoreFailure { get; set; } = true;
  }
}
