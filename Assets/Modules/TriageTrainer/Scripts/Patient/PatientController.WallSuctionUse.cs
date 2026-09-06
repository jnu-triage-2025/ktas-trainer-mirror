using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Quest;
using TriageTrainer.Scenario;
using UnityEngine;
using MI = MultiplayerInfrastructure;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// "환자에게 흡인기 사용" 상호작용. 손에 든 물품을 환자에게 적용하는 방식(item_apply)이 아니라,
  /// 플레이어가 흡인기(WallSuction)에 양커 라인으로 연결되어 있을 때(<see cref="PlayerController.IsWallSuctionAvailable"/>)
  /// 만 노출/수행 가능하다. 연결·해제에 따른 플래그 갱신은 <see cref="WallAttachedWallSuction"/> 이 담당한다.
  /// </summary>
  public partial class PatientController
  {
    /// <summary>흡인기 사용 상호작용 식별자. 퀘스트 마크 바인딩에서 참조한다.</summary>
    public const string InteractIdWallSuctionUse = "wall_suction_use";

    /// <summary>흡인기 사용 시 <see cref="ItemUseEffects"/> 매핑을 재사용하기 위한 키(조립된 양커 팁).</summary>
    private const string WallSuctionUseItemIdentifier = TriageTrainer.ItemDefinitions.YankauerSuctionReady.Identifier;

    private sealed class PatientWallSuctionUseInteract : IInteract, IInteractorConditional, IQuestPresentationTarget
    {
      private readonly PatientController _owner;
      public PatientWallSuctionUseInteract(PatientController owner) { _owner = owner; }
      public string PresentationEntityIdentifier => _owner.Identifier;
      public string InteractionIdentifier => InteractIdWallSuctionUse;
      public string DisplayText => "환자에게 흡인기 사용";
      public Sprite DisplayIcon => null;
      public bool AllowDisplayIconFallback => false;
      public Color DisplayColor => Color.clear;

      // 시나리오 단계 노출은 레지스트리의 데이터 조건이 정한다.
      public bool CanInteract(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        return player != null
               && player.IsWallSuctionAvailable
               && _owner.CanApplyItemUse(WallSuctionUseItemIdentifier);
      }

      public void Interact(Transform interactor)
      {
        var player = interactor != null ? interactor.GetComponentInParent<PlayerController>() : null;
        _owner.RequestWallSuctionUse(player);
      }
    }

    private void AddWallSuctionUseInteract()
    {
      _interacts.Add(new PatientWallSuctionUseInteract(this));
    }

    /// <summary>
    /// 흡인기 사용을 요청한다. 인벤토리 아이템 보유·소비가 아니라 플레이어의 흡인기 라인 연결 상태
    /// (<see cref="PlayerController.IsWallSuctionAvailable"/>)만으로 판정한다.
    /// </summary>
    private void RequestWallSuctionUse(PlayerController player)
    {
      if (player == null || !player.IsWallSuctionAvailable)
        return;

      if (IsFishNetClientInitialized && !IsFishNetServerStarted)
      {
        CmdApplyWallSuctionUse();
        return;
      }

      ApplyWallSuctionUseAuthoritative(player);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdApplyWallSuctionUse(NetworkConnection sender = null)
    {
      if (!TryResolvePlayerForTreatmentSender(sender, out var player, out var actorIdentifier, out var actorDisplayName))
        return;

      using (MI.Scenario.ScenarioSignalPlayerContext.Push(actorIdentifier, actorDisplayName))
        ApplyWallSuctionUseAuthoritative(player);
    }

    private bool ApplyWallSuctionUseAuthoritative(PlayerController player)
    {
      if (player == null
          || !player.IsWallSuctionAvailable
          || !IsWithinPatientBCTreatmentDistance(player)
          || !CanApplyItemUse(WallSuctionUseItemIdentifier))
        return false;

      return ApplyItemUse(WallSuctionUseItemIdentifier);
    }
  }
}
