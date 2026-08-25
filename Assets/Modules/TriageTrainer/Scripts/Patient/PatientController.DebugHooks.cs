using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 검증 편의를 위한 PatientController 디버그 훅(에디터/IndevScene 전용).
  ///
  /// 아이템·플레이어 없이도 처치 시각 표현(<see cref="TreatmentDisplay"/>)과 아이템 사용 효과,
  /// 사정 신호를 인스펙터 ContextMenu 로 단독 검증할 수 있게 한다.
  /// </summary>
  public partial class PatientController
  {
    [Header("Debug (검증용)")]
    [Tooltip("ContextMenu > Debug Show/Hide Treatment Display 로 토글할 표현.")]
    [SerializeField] private TreatmentDisplay _debugTreatmentDisplay = TreatmentDisplay.GauzePatchedOnThorax;

    [Tooltip("ContextMenu > Debug Apply Item Use 로 사용할 아이템 식별자. 예: gauze, neckstabilizer, ambubag.")]
    [SerializeField] private string _debugItemIdentifier = "gauze";

    [ContextMenu("Debug/Show Treatment Display")]
    private void Debug_ShowTreatmentDisplay()
    {
      ShowTreatmentDisplay(_debugTreatmentDisplay);
      Debug.Log($"[PatientController:DEBUG] ShowTreatmentDisplay({_debugTreatmentDisplay}) on '{Identifier}'", this);
    }

    [ContextMenu("Debug/Hide Treatment Display")]
    private void Debug_HideTreatmentDisplay()
    {
      HideTreatmentDisplay(_debugTreatmentDisplay);
      Debug.Log($"[PatientController:DEBUG] HideTreatmentDisplay({_debugTreatmentDisplay}) on '{Identifier}'", this);
    }

    [ContextMenu("Debug/Apply Item Use")]
    private void Debug_ApplyItemUse()
    {
      bool handled = ApplyItemUse(_debugItemIdentifier);
      Debug.Log($"[PatientController:DEBUG] ApplyItemUse('{_debugItemIdentifier}') on '{Identifier}' => {handled}", this);
    }
  }
}
