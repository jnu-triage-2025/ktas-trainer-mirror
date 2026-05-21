using System;
namespace TriageTrainer.Patient
{
  [Serializable]
  public struct PatientTreatmentDisplayModel
  {
    #region InsertingSyringe
    /**
     * 환자에게 주사바늘을 삽입한 상태를 표시
     * 환자에 표시할 상태는 좌/우측 팔에 주사바늘이 삽입되어있는 상태 여부만임
     */ 

    /// <summary>
    /// 좌측 팔에 18G 주사기가 삽입된 상태를 표시할지 여부
    /// </summary>
    public bool Syringe18GInsertedIntoLeftArm;

    /// <summary>
    /// 우측 팔에 18G 주사기가 삽입된 상태를 표시할지 여부
    /// </summary>
    public bool Syringe18GInsertedIntoRightArm;

    /// <summary>
    /// 좌측 팔에 20G 주사기가 삽입된 상태를 표시할지 여부
    /// </summary>
    public bool Syringe20GInsertedIntoLeftArm;

    /// <summary>
    /// 우측 팔에 20G 주사기가 삽입된 상태를 표시할지 여부
    /// </summary>
    public bool Syringe20GInsertedIntoRightArm;

    /// <summary>
    /// 환자의 쇄골에 중앙정맥 카테터가 삽입된 상태를 표시할지 여부
    /// </summary>
    public bool CentralVenousCatheterInsertedIntoSubclavian;

    #endregion  // InsertingSyringe

    #region EndotrachealTubeTreatments
    /**
     * 기도삽관 과정 및 기도삽관 후 발전 가능한 상황에 대한 정리
     * 
     * 기도삽관 최초 과정:
     *  1. 후두경을 구강에 삽입
     *  2. 스타일렛이 삽입된 기관삽관관을 구강에 삽입
     *  3. 스타일렛 제거
     *  4. (선택적) 앰부백 마스크를 기관삽관관에 연결
     */

    /// <summary>
    /// 환자에 후두경 삽입
    /// </summary>
    public bool LaryngoscopeInserted;

    /// <summary>
    /// 환자 기도삽관 중간과정 (스타일렛 꽂힘)
    /// </summary>
    public bool EndotrachealTubeStyletInserted;

    /// <summary>
    /// 환자 기도삽관 완료 (스타일렛 제거, T-Piece 미연결)
    /// </summary>
    public bool EndotrachealTubeInsertDone;

    /// <summary>
    /// 환자 기도삽관 완료 (스타일렛 제거, T-Piece 연결)
    /// </summary>
    public bool TPieceAttachedToNasalCannula;

    /// <summary>
    /// T-Piece 제거
    /// 환자의 기도삽관이 완료된 상태에서 앰부백 마스크가 삽관관에 연결된 상태를 표시할지 여부
    /// </summary>
    public bool AmbuBagAttachedToEndotrachealTube;

    #endregion  // EndotrachealTubeTreatments
    #region ApplyGauzeDressing
    /**
     * 환자에게 거즈 드레싱을 적용한 상태를 표시할 때 필요한 정보들을 정리
     * 환자의 팔과 얼굴(눈썹)에 거즈 드레싱을 적용한 상태를 표시할 수 있도록 구성
     */
    
    /// <summary>
    /// 환자의 흉부에 거즈를 덧댄 경우
    /// </summary>
    public bool GauzePatchedOnThorax;

    /// <summary>
    /// 환자의 흉부에 거즈 드레싱을 완료한 경우
    /// </summary>
    public bool GauzeDressingDoneOnThorax;

    /// <summary>
    /// 환자의 우측 팔에 거즈를 덧댄 경우
    /// </summary>
    public bool GauzePatchedOnRightArm;

    /// <summary>
    /// 환자의 우측 팔에 거즈 드레싱을 완료한 경우
    /// </summary>
    public bool GauzeDressingDoneOnRightArm;

    /// <summary>
    /// 환자의 좌측 팔에 거즈를 덧댄 경우
    /// </summary>
    public bool GauzePatchedOnLeftArm;

    /// <summary>
    /// 환자의 좌측 팔에 거즈 드레싱을 완료한 경우
    /// </summary>
    public bool GauzeDressingDoneOnLeftArm;

    /// <summary>
    /// 환자의 우측 눈썹에 거즈를 덧댄 경우
    /// </summary>
    public bool GauzePatchedOnRightEyebrow;

    /// <summary>
    /// 환자의 우측 눈썹에 거즈 드레싱을 완료한 경우
    /// </summary>
    public bool GauzeDressingDoneOnRightEyebrow;

    /// <summary>
    /// 환자의 좌측 눈썹에 거즈를 덧댄 경우
    /// </summary>
    public bool GauzePatchedOnLeftEyebrow;

    /// <summary>
    /// 환자의 좌측 눈썹에 거즈 드레싱을 완료한 경우
    /// </summary>
    public bool GauzeDressingDoneOnLeftEyebrow;
    
    #endregion  // ApplyGauzeDressing
    #region NasalCannulaTreatments
    /**
     * 비강 캐뉼라 적용 및 발전 가능한 관련 상태 서술
     */
    /// <summary>
    /// 환자의 코에 비강 캐뉼라를 적용한 경우
    /// </summary>
    public bool NasalCannulaApplied;
    #endregion  // NasalCannulaTreatments
    #region OtherTreatments
    /**
     * 미분류
     */
    /// <summary>
    /// 환자의 얼굴에 앰부백 마스크가 씌워진 상태를 표시할지 여부
    /// </summary>
    // 비활성화됨: 시나리오 상 기능 요구사항에서 제외됨
    // public bool AmbuBagMaskOnFace { get; set; }

    /// <summary>
    /// 환자의 목에 경부 고정대가 착용된 상태를 표시할지 여부
    /// </summary>
    public bool CervicalCollarOnNeck;

    /// <summary>
    /// 환자에게 연결된 수액걸이 스탠드를 표시할지 여부
    /// </summary>
    public bool IntravenousStandAttached;

    /// <summary>
    /// 환자에게 연결된 수액걸이를 표시할지 여부
    /// </summary>
    public bool IntravenousHangerAttached;

    /// <summary>
    /// 환자에게 연결된 수액을 표시할지 여부
    /// </summary>
    public bool IntravenousFluidAttached;
    #endregion  // OtherTreatments
  }
}

/**
 * AGENTS.md
 *
 * 위의 플래그들은 유니티에서 이 데이터 모델이 적용된 GameObject의 자식 GameObject들의 활성화/비활성화 여부를 결정하는 데 사용한다.
 * 
 * 따라서 실제로 자식 GameObject들을 Reference하는 데이터 구조가 필요하다.
 * 이 데이터 구조를 표현하는 `PatientTreatmentDisplayingChildGameObjects` 클래스를 생성하여라.
 * 현재 이 파일이 위치한 경로와 같은 경로에 생성한다.
 * 
 * `PatientTreatmentDisplayingChildGameObjects` 클래스는 이 클래스에 1:1 대응하여야 한다.
 *   - 동시에, 이러한 이유로, 생성하는 소스코드에는 일체의 주석을 작성하지 않는다.
 *     - 파일 최상단에 "이 파일은 자동 생성된 파일"이며, 현재 이 파일을 참고하라는 주석 정도만 작성한다.
 *   - 실제 정의에서 각 필드는 `public GameObject (항목명);` 형태로 작성될 것이다.
 *   - 네임스페이스는 이 클래스가 사용하는 네임스페이스와 동일한 것을 사용하여라.
 *
 * 
 * 예시:
 * ```cs
 * using System;
 * using UnityEngine;
 * 
 * namespace TriageTrainer.Patient
 * {
 *   [Serializable]
 *   public class PatientTreatmentDisplayingChildGameObjects
 *   {
 *     public GameObject Syringe18GInsertedIntoLeftArm;
 *     public GameObject Syringe18GInsertedIntoRightArm;
 *     public GameObject Syringe20GInsertedIntoLeftArm;
 * ...(후략)
 */
