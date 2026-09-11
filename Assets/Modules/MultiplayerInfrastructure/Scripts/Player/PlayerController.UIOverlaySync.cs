using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private bool _overlaySyncSubscribed;

    private void OnStartClient_UIOverlaySync()
    {
      if (!IsOwner)
        return;

      UIOverlayStack.Clear();
      SubscribeOverlayStackChanged();
      SyncOverlayDrivenPlayerState();
    }

    private void OnStopClient_UIOverlaySync()
    {
      // FishNet이 종료 콜백을 실행하는 시점에는 소유권이 이미 해제될 수 있다.
      // 실제로 로컬 오버레이 동기화를 시작했던 인스턴스인지 구독 상태로 판별한다.
      if (!_overlaySyncSubscribed)
        return;

      UnsubscribeOverlayStackChanged();
      UIOverlayStack.Clear();

      // 클라이언트 종료 뒤에는 더 이상 게임 조작 상태로 돌아가지 않는다.
      // StopClient 콜백이 IntroScene.Awake보다 늦게 실행될 수 있으므로 여기서 커서를
      // 다시 잠그면 타이틀 화면은 보이지만 포인터로 조작할 수 없는 상태가 된다.
      EnterUIOverlayMode();
    }

    private void SubscribeOverlayStackChanged()
    {
      if (_overlaySyncSubscribed)
        return;

      UIOverlayStack.StackChanged += HandleOverlayStackChanged;
      _overlaySyncSubscribed = true;
    }

    private void UnsubscribeOverlayStackChanged()
    {
      if (!_overlaySyncSubscribed)
        return;

      UIOverlayStack.StackChanged -= HandleOverlayStackChanged;
      _overlaySyncSubscribed = false;
    }

    private void HandleOverlayStackChanged()
    {
      string topName = UIOverlayStack.Top?.GetType().Name ?? "none";
      Debug.Log(
        $"[PlayerController][UIOverlayStack] Changed. count={UIOverlayStack.Count}, " +
        $"top={topName}, canMove={canMove}, cursorLocked={IsCursorLocked}.",
        this);
      SyncOverlayDrivenPlayerState();
    }

    private void SyncOverlayDrivenPlayerState()
    {
      bool wasMoveEnabled = canMove;
      bool wasCursorLocked = IsCursorLocked;
      string topName = UIOverlayStack.Top?.GetType().Name ?? "none";

      if (UIOverlayStack.IsEmpty())
      {
        if (!canMove || !IsCursorLocked)
          ExitUIOverlayMode();
      }
      else if (canMove || IsCursorLocked)
        EnterUIOverlayMode();

      if (wasMoveEnabled != canMove || wasCursorLocked != IsCursorLocked)
      {
        Debug.Log(
          $"[PlayerController][UIInputState] Corrected overlay-driven state. top={topName}, " +
          $"canMove={wasMoveEnabled}->{canMove}, cursorLocked={wasCursorLocked}->{IsCursorLocked}.",
          this);
      }
    }

    /// <summary>
    /// 게임 모드 전환 등 다른 시스템이 이동·커서 상태를 바꾼 경우에도 오버레이 상태를 다시 적용한다.
    /// </summary>
    private void EnsureOverlayDrivenPlayerState() => SyncOverlayDrivenPlayerState();
  }
}
