using System;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using UnityEngine;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 환자 "정맥라인 캐뉼라(18G~20G)" 상호작용 지원/가능 여부 및 상호작용 항목 부분 구현.
  ///
  /// <para>
  /// 이 환자에게 정맥라인 캐뉼라 상호작용이 <b>지원되는지(Preset/Config)</b>와, 현재 시점에
  /// <b>실제로 가능한지(State)</b>를 구분해서 관리한다. 다른 인터랙션 계열
  /// (<see cref="InteractConfig"/>, <see cref="TriageAssessmentConfig"/>)과
  /// 동일한 관례를 따른다: Config(정적/시나리오 제어) + 런타임 조건을 결합해 상호작용 가능 여부를 판정한다.
  /// </para>
  ///
  /// <para>
  /// 상호작용 항목(<see cref="PatientIntravenousLineCannulaInteract"/>)은 <c>BuildInteractEntries</c> 에서
  /// <c>_interacts</c> 에 등록되며, 플레이어가 <b>캐뉼라(18G/20G)를 손에 든 경우에만</b> 힌트로 노출된다.
  /// </para>
  /// </summary>
  public partial class PatientController
  {
    /// <summary>정맥라인 캐뉼라 상호작용으로 인정하는 손에 든 아이템 식별자(18G/20G).</summary>
    private static readonly string[] IntravenousLineCannulaItemIdentifiers =
    {
      TriageTrainer.ItemDefinitions.Cannula18g.Identifier,
      TriageTrainer.ItemDefinitions.Cannula20g.Identifier,
    };

    [Serializable]
    public struct IntravenousLineCannulaConfig
    {
      [Tooltip("이 환자(모델)가 18G~20G 캐뉼라를 이용한 정맥라인 확보 상호작용 기능을 지원하는지 여부. " +
               "환자 유형별로 다르게 설정 가능하며(팔 모델 유무 등), 상호작용 노출 게이팅의 기준 데이터로 사용된다.")]
      [SerializeField] private bool _supported;

      [Tooltip("상호작용 힌트에 표시할 문구.")]
      [SerializeField] private string _displayText;

      [SerializeField] private Sprite _displayIcon;

      public bool Supported
      {
        get => _supported;
        set => _supported = value;
      }

      public string DisplayText => string.IsNullOrWhiteSpace(_displayText) ? "정맥라인 캐뉼라 확보" : _displayText;

      public Sprite DisplayIcon => _displayIcon;
    }

    [Header("Intravenous Line Cannula (정맥라인 캐뉼라)")]
    [SerializeField] private IntravenousLineCannulaConfig _intravenousLineCannulaConfig;

    /// <summary>
    /// State: 현재 시점에 이 환자에게 정맥라인 캐뉼라 상호작용을 수행할 수 있는지 여부(런타임).
    /// 예) 이미 양팔에 캐뉼라가 삽입되어 더 이상 삽입할 필요가 없는 경우 false로 전환하는 등 시나리오 진행에 따라 갱신된다.
    /// </summary>
    [SerializeField] private bool _intravenousLineCannulaInteractable = true;

    /// <summary>
    /// Preset/Config: 정맥라인 캐뉼라(18G~20G) 상호작용 기능 지원 여부.
    /// </summary>
    public bool IntravenousLineCannulaSupported
    {
      get => _intravenousLineCannulaConfig.Supported;
      set => _intravenousLineCannulaConfig.Supported = value;
    }

    /// <summary>
    /// State: 정맥라인 캐뉼라(18G~20G) 상호작용 가능 여부(런타임).
    /// </summary>
    public bool IntravenousLineCannulaInteractable
    {
      get => _intravenousLineCannulaInteractable;
      set => _intravenousLineCannulaInteractable = value;
    }

    /// <summary>
    /// 정맥라인 캐뉼라 상호작용이 실제로 가능한지(Config가 지원하고, 현재 State도 가능한 경우).
    /// </summary>
    public bool CanInteractIntravenousLineCannula => IntravenousLineCannulaSupported && IntravenousLineCannulaInteractable;

    /// <summary>시나리오 진행에 따라 정맥라인 캐뉼라 상호작용 가능 여부(State)를 켜고 끈다.</summary>
    public void SetIntravenousLineCannulaInteractable(bool interactable)
    {
      _intravenousLineCannulaInteractable = interactable;
    }

    /// <summary>
    /// 정맥라인 캐뉼라 상호작용 항목. 플레이어가 캐뉼라(18G/20G)를 손에 들고, 이 환자가 상호작용을
    /// 지원/가능한 상태일 때만 노출된다.
    /// </summary>
    private sealed class PatientIntravenousLineCannulaInteract : IInteract, IInteractorConditional
    {
      private readonly PatientController _owner;

      public PatientIntravenousLineCannulaInteract(PatientController owner) { _owner = owner; }

      public string DisplayText => _owner._intravenousLineCannulaConfig.DisplayText;
      public Sprite DisplayIcon => _owner._intravenousLineCannulaConfig.DisplayIcon;
      public bool AllowDisplayIconFallback => true;
      public Color DisplayColor => Color.white;

      public bool CanInteract(Transform interactor)
      {
        // (1) 환자가 이 기능을 지원(Config)하고 현재 가능(State)한지.
        if (!_owner.CanInteractIntravenousLineCannula)
          return false;

        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        if (player == null)
          return false;

        // (2) 플레이어가 손에 든 아이템이 캐뉼라(18G/20G)인지.
        return _owner.IsHandlingIntravenousLineCannula(player);
      }

      public void Interact(Transform interactor)
      {
        _owner.PerformIntravenousLineCannulaInsertion(interactor);
      }
    }

    /// <summary>정맥라인 캐뉼라 상호작용 항목을 등록한다(<c>BuildInteractEntries</c> 에서 호출).</summary>
    private void AddIntravenousLineCannulaInteract()
    {
      _interacts.Add(new PatientIntravenousLineCannulaInteract(this));
    }

    /// <summary>플레이어가 손에 캐뉼라(18G/20G)를 들고 있는지 판정한다.</summary>
    private bool IsHandlingIntravenousLineCannula(PlayerController player)
    {
      if (player == null)
        return false;

      string heldIdentifier = player.HandlingItem?.CurrentIdentifier;
      if (string.IsNullOrWhiteSpace(heldIdentifier))
        return false;

      for (int i = 0; i < IntravenousLineCannulaItemIdentifiers.Length; i++)
      {
        if (string.Equals(heldIdentifier, IntravenousLineCannulaItemIdentifiers[i], StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    /// <summary>
    /// 정맥라인 캐뉼라 삽입(상호작용 확정)을 처리한다.
    /// 시나리오 게이팅 신호를 올리고, 힌트를 즉시 갱신한다.
    /// </summary>
    private void PerformIntravenousLineCannulaInsertion(Transform interactor)
    {
      var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
      if (player == null)
        return;

      // 상호작용 시점의 조건을 서버/클라이언트 공통으로 다시 방어한다.
      if (!CanInteractIntravenousLineCannula || !IsHandlingIntravenousLineCannula(player))
        return;

      // 처치 표현 및 상세 삽입 로직(좌/우 팔 구분, 아이템 소비 등)은 후속 작업에서 연결한다.
      // 여기서는 시나리오 게이팅 신호만 올려 흐름이 진행되도록 한다.
      string signal = ResolveSignalTemplate("apply_intravenous_line_cannula_{id}");
      if (!string.IsNullOrWhiteSpace(signal))
        MultiplayerInfrastructure.Scenario.ScenarioInteractionSignals.Raise(signal);

      player.RefreshInteractableHintsNow();
    }
  }
}
