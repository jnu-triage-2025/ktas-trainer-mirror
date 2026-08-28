using TriageTrainer.Entity;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.Entity.OxyLine;
using UnityEngine;

namespace TriageTrainer.Patient
{
  public class PatientTypeBMaleState : PatientStateABC
  {
    [SerializeField]
    private PatientTypeBMaleTreatmentDisplayState treatmentDisplayState = new PatientTypeBMaleTreatmentDisplayState();

    [Header("Line Connection Points")]
    [Tooltip("이 환자(B, 남성)의 정맥로 — 20G 캐뉼라를 삽입하는 우측 팔 부위 — 에 배치된 " +
             "Intravenous Line Connection Point 입니다. 시나리오 patient_b_c_ct 의 " +
             "\"생리식염수 연결\" 처치에서 침대 생리식염수 걸이의 연결 지점과 이어붙일 환자 측 끝점으로 사용합니다.\n\n" +
             "이 참조는 사람이 직접 배선합니다. 식별자 문자열로 검색하지 않으므로 비워두면 연결이 성립하지 않습니다. " +
             "온라인에서는 자동 연결이 FishNet 스폰된 컴포넌트만 허용하므로, 반드시 이 환자 네트워크 프리팹의 " +
             "자식 오브젝트에 배치된 포인트를 지정해야 합니다.")]
    [SerializeField]
    private IntravenousLineConnectionPoint intravenousLineConnectionPoint;

    [SerializeField]
    private OxyLineConnectionPoint oxyLineConnectionPoint;

    public override PatientTreatmentDisplayStateABC TreatmentDisplayState => treatmentDisplayState;
    public override bool RestoresLegacyPatientControllerDefaultsOnInspectorReset => true;

    /// <summary>이 환자 유형의 정맥로 측 IV 연결 지점(Inspector 배선).</summary>
    public IntravenousLineConnectionPoint IntravenousLineConnectionPoint => intravenousLineConnectionPoint;

    protected override void ConfigureRuntimeReferences(PatientController controller)
    {
      controller.SetOxygenMaskAttachmentPointFromPatientComponent(
        oxyLineConnectionPoint != null ? oxyLineConnectionPoint : FindSingleOxygenInterface());
      controller.SetPatientBCIvAttachmentPointFromPatientComponent(intravenousLineConnectionPoint);
    }
  }
}
