namespace TriageTrainer.ItemDefinitions
{
  public class PatientMonitor : MedicalItem
  {
    public const string Identifier   = "patient_monitor";
    public const string DisplayName  = "환자 모니터";
    public const string Description  = "환자 모니터 설치 위치에서 부착하여 생체 신호를 확인합니다.";

    // 설치/회수 단위가 한 대이므로 스택을 허용하지 않는다.
    public new const bool IsStackable = false;
    public new const int MaxStackCount = 1;
  }
}
