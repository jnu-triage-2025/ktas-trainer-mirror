using System;
using UnityEngine;

namespace TriageTrainer.Patient
{
  /// <summary>
  /// 환자 모델이 표현 가능한 치료 과정 및 상태에 대한 추상 클래스
  /// 
  /// 환자 모델의 치료가 진행 중일 때, 치료 과정 중에 시각적으로 확인 가능한 변화들에 대해 서술합니다.
  /// 대개는 특정한 장비들이 환자에 적용되거나 부착된 상태를 표시합니다.
  /// 실제로 각 환자 모델은, DisplaySupports에 정의된 내용들이 3D 모델이나 프리팹에서 구현되어있어야 합니다.
  /// </summary>
  [Serializable]
  public abstract class PatientTreatmentDisplayStateABC
  {
    /// <summary>
    /// 환자 모델이 표현 가능한 상태들의 내용을 정의
    /// true인 경우 환자 모델이 표현 가능함
    /// false인 경우 환자 모델에 관련 내용 구현되어있지 않음
    /// </summary>
    public abstract PatientTreatmentDisplayModel DisplaySupports { get; set; }

    /// <summary>
    /// 런타임 환경: 현재 환자 모델이 어떤 내용이 표시중인지 플래그
    /// </summary>
    public abstract PatientTreatmentDisplayModel DisplayState { get; set; }

    public abstract GameObject PatientModelGameObject { get; set; }

    /// <summary>
    /// 런타임 환경: 자식 GameObject 참조
    /// </summary>
    public abstract PatientTreatmentDisplayingChildGameObjects ChildGameObjects { get; set; }
  }
}
