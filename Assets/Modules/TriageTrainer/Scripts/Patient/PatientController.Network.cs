using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Registry;

namespace TriageTrainer.Entity
{
  public partial class PatientController : ISpawnedEntityIdentifierReceiver
  {
    // 런타임 식별자(서버 권위 + 전 피어 복제). 프리셋 스폰 시 서버가 ApplySpawnedEntityIdentifier 로 설정하면
    // 스폰 페이로드에 담겨 모든 클라이언트로 동기화된다. 이것이 비어 있으면 프리팹 기본값(_identifier)을 사용한다.
    // SyncVar 가 아니면 원격 클라에서는 식별자가 기본값("patient")으로 남아, 침대의 식별자 기반 결합(repose)이
    // 원격 피어에서 해석되지 않는 문제가 발생한다(스폰은 되나 결합 상태가 복제되지 않음).
    private readonly SyncVar<string> _runtimeIdentifier = new SyncVar<string>(string.Empty);

    private string _registeredEntityIdentifier;

    /// <summary>
    /// EditMode 및 pre-spawn 객체에서는 NetworkBehaviour 캐시가 아직 없을 수 있다.
    /// FishNet 상태 프로퍼티를 직접 읽지 않고 이 안전한 경계를 사용한다.
    /// </summary>
    private bool IsFishNetServerStarted =>
      NetworkObject != null && NetworkObject.NetworkManager != null && IsServerStarted;

    private bool IsFishNetClientInitialized =>
      NetworkObject != null && NetworkObject.NetworkManager != null && IsClientInitialized;

    private string EffectiveIdentifier =>
      string.IsNullOrWhiteSpace(_runtimeIdentifier.Value) ? _identifier : _runtimeIdentifier.Value;

    /// <summary>
    /// 엔티티 프리셋 스폰 시 인스턴스 식별자를 주입받는다(ISpawnedEntityIdentifierReceiver).
    /// 서버에서 호출되며(프리셋 스폰은 서버 컨텍스트), 스폰 전 SyncVar 에 기록되어 전 피어로 복제된다.
    /// </summary>
    public void ApplySpawnedEntityIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      string trimmed = identifier.Trim();
      _identifier = trimmed; // 로컬 즉시 반영(서버에서 OnStartClient 전 RaisePatientInteractionSignals 등에 대비)

      // EditMode/offline instances may have a PatientController before FishNet has
      // populated NetworkBehaviour's NetworkObject cache. The local identifier is
      // still valid in that state; only touch the SyncVar on an initialized server.
      if (IsFishNetServerStarted)
      {
        _runtimeIdentifier.Value = trimmed;
      }

      // 서버에서 이미 등록된 뒤(재주입)라면 갱신.
      if (!string.IsNullOrWhiteSpace(_registeredEntityIdentifier)
          && !string.Equals(_registeredEntityIdentifier, trimmed, System.StringComparison.Ordinal))
      {
        UnregisterPatientEntity();
        RegisterPatientEntity();
      }
    }

    public override void OnStartClient()
    {
      base.OnStartClient();

      // 스폰 페이로드로 동기화된 런타임 식별자를 모든 피어에서 반영한 뒤 등록한다.
      _runtimeIdentifier.OnChange += OnRuntimeIdentifierChanged;
      RegisterPatientEntity();
      WallAttachedOxyflowmeter.AttachmentStateChanged += OnAnyOxyflowmeterAttachmentChanged;

      // 트리아지 상태값 동기화 구독 + 초기 오버헤드 태그 반영.
      InitializeTriageSync();
      InitializeRecognitionCheckSync();
      InitializeNurseDStageSync();
      UpdateTriageOverheadLabel(AssessedTriage);
    }

    public override void OnStopClient()
    {
      TeardownTriageSync();
      TeardownRecognitionCheckSync();
      TeardownNurseDStageSync();
      DestroyTriageOverheadLabel();
      _runtimeIdentifier.OnChange -= OnRuntimeIdentifierChanged;
      WallAttachedOxyflowmeter.AttachmentStateChanged -= OnAnyOxyflowmeterAttachmentChanged;
      UnregisterPatientEntity();
      base.OnStopClient();
    }

    private void OnDestroy()
    {
      TeardownRecognitionCheckSync();
      TeardownNurseDStageSync();
      WallAttachedOxyflowmeter.AttachmentStateChanged -= OnAnyOxyflowmeterAttachmentChanged;
      UnregisterPatientEntity();
    }

    private void OnRuntimeIdentifierChanged(string previous, string next, bool asServer)
    {
      // 식별자가 늦게(스폰 후) 변경되어도 등록 식별자를 갱신한다.
      if (string.IsNullOrWhiteSpace(next))
        return;

      if (!string.Equals(_registeredEntityIdentifier, next, System.StringComparison.Ordinal))
      {
        UnregisterPatientEntity();
        RegisterPatientEntity();
      }
    }

    private void RegisterPatientEntity()
    {
      string id = EffectiveIdentifier;
      if (string.IsNullOrWhiteSpace(id))
        return;

      // 동기화된 식별자를 로컬 _identifier 에도 반영(신호/표시 등에서 Identifier 일관성 유지).
      _identifier = id;
      _registeredEntityIdentifier = id;
      Registry.RegisterEntity(
        _registeredEntityIdentifier,
        EntityType.Patient,
        gameObject,
        displayName: GetPatientDisplayName(id),
        ownerUserIdentifier: null,
        clientId: null,
        isNetworked: true);
    }

    public void RefreshPatientDisplayName()
    {
      if (string.IsNullOrWhiteSpace(_registeredEntityIdentifier))
        return;

      Registry.UpdateEntityDisplayName(
        _registeredEntityIdentifier,
        GetPatientDisplayName(_registeredEntityIdentifier));
    }

    /// <summary>
    /// 화면에 보여줄 환자 이름. 프로필 이름이 없으면 빈 값이다.
    /// 환자를 특정해야 하는 상호작용 문구에서 쓴다(예: 수액 줄 해제).
    /// </summary>
    public string PatientDisplayName => GetPatientDisplayName(null);

    private string GetPatientDisplayName(string fallback)
    {
      return _patientDescriptor != null && !string.IsNullOrWhiteSpace(_patientDescriptor.name)
        ? _patientDescriptor.name.Trim()
        : fallback;
    }

    private void UnregisterPatientEntity()
    {
      if (string.IsNullOrWhiteSpace(_registeredEntityIdentifier))
        return;

      Registry.UnregisterEntity(_registeredEntityIdentifier);
      _registeredEntityIdentifier = null;
    }
  }
}
