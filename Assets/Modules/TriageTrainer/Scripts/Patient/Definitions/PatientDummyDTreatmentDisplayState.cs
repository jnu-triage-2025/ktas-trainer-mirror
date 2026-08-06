using System;
using UnityEngine;

namespace TriageTrainer.Patient
{
  /// <summary>
  /// 분류 전용 D 더미에는 시각적 처치 표현물이 없음을 정의합니다.
  /// </summary>
  [Serializable]
  public class PatientDummyDTreatmentDisplayState : PatientTreatmentDisplayStateABC
  {
    [field: SerializeField]
    public override PatientTreatmentDisplayModel DisplaySupports { get; set; } = new();

    [field: SerializeField]
    public override PatientTreatmentDisplayModel DisplayState { get; set; } = new();

    [field: SerializeField]
    public override GameObject PatientModelGameObject { get; set; }

    [field: SerializeField]
    public override PatientTreatmentDisplayingChildGameObjects ChildGameObjects { get; set; } = new();
  }
}
