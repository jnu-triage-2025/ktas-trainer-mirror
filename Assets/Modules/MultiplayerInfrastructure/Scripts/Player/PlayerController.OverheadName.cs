using System.Collections.Generic;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// PlayerController의 머리 위 이름표 구현.
  ///
  /// NPC 이름표와 같은 <see cref="EntityOverheadLabelUIController"/> 채널 스택을 사용하므로
  /// 글자 크기/외곽선/거리 페이드가 NPC 이름표와 동일하게 렌더링되고, 같은 앵커에 퀘스트 마크가
  /// 올라오면 이름표 위에 쌓인다.
  /// </summary>
  public partial class PlayerController
  {
    private const string OverheadNameChannel = "player-name";

    // NPC 이름표(npc-name)와 같은 단에 둔다. 퀘스트 마크(100)가 항상 이름 위에 놓이게 하기 위함이다.
    private const int OverheadNameChannelOrder = 0;

    [Header("Overhead Name")]
    [Tooltip("플레이어의 DisplayName을 머리 위 이름표로 표시한다.")]
    [SerializeField] private bool _overheadNameEnabled = true;
    [Tooltip("로컬 소유자 자신의 머리 위에도 이름표를 표시한다. 1인칭 시점에서는 화면을 가릴 수 있다.")]
    [SerializeField] private bool _overheadNameShownForOwner;
    [Tooltip("카메라와 이 거리(m)보다 멀어지면 머리 위 이름표를 숨긴다. 0 이하이면 거리 제한 없이 항상 표시한다.")]
    [SerializeField, Min(0f)] private float _overheadNameMaxVisibleDistance = 16f;
    [SerializeField] private Color _overheadNameColor = Color.white;

    // 로컬 피어에서 살아 있는 PlayerController 전체. 누군가의 관전 상태가 바뀌면
    // "누가 누구의 이름표를 볼 수 있는가"가 함께 바뀌므로 일괄 갱신에 사용한다.
    private static readonly List<PlayerController> _overheadNameInstances = new();

    private Transform _overheadNameAnchor;
    private bool _overheadNameActive;

    private void OnStartClient_AnyPeer_OverheadName()
    {
      if (!_overheadNameInstances.Contains(this))
        _overheadNameInstances.Add(this);
      _overheadNameActive = true;

      // 이름표 UI 컨트롤러는 오버레이 씬에서 나중에 올라올 수 있다(플레이어 스폰이 먼저인 씬 구성).
      // 준비 시점에 다시 시도해야 첫 진입에서 이름표가 누락되지 않는다.
      EntityOverheadLabelUIController.ActiveInstanceChanged += RefreshOverheadNameLabel;
      RefreshAllOverheadNameLabels();
    }

    private void OnStopClient_AnyPeer_OverheadName()
    {
      TeardownOverheadName();
      RefreshAllOverheadNameLabels();
    }

    private void OnDestroy_OverheadName()
    {
      TeardownOverheadName();
    }

    private void TeardownOverheadName()
    {
      if (!_overheadNameActive)
        return;

      EntityOverheadLabelUIController.ActiveInstanceChanged -= RefreshOverheadNameLabel;
      _overheadNameInstances.Remove(this);
      _overheadNameActive = false;

      // 파괴된 앵커는 컨트롤러 LateUpdate의 stale 정리가 제거하지만, 명시적으로 먼저 해제한다.
      EntityOverheadLabelUIController.ActiveInstance?.RemoveLabels(_overheadNameAnchor);

      // 디스폰 후 같은 인스턴스가 다시 스폰되면 앵커를 새로 만들므로, 남은 앵커를 여기서 정리한다.
      if (_overheadNameAnchor != null)
        Destroy(_overheadNameAnchor.gameObject);
      _overheadNameAnchor = null;
    }

    /// <summary>이 플레이어의 이름표를 현재 표시명/관전 상태/소유 여부에 맞춰 갱신한다.</summary>
    private void RefreshOverheadNameLabel()
    {
      if (!ShouldShowOverheadName())
      {
        EntityOverheadLabelUIController.ActiveInstance?.RemoveLabel(_overheadNameAnchor, OverheadNameChannel);
        return;
      }

      EntityOverheadLabelUIController.Resolve()?.SetLabel(
        ResolveOverheadNameAnchor(),
        OverheadNameChannel,
        OverheadNameChannelOrder,
        new EntityOverheadLabelUIController.LabelContent(UserDisplayName.Trim(), _overheadNameColor),
        _overheadNameMaxVisibleDistance);
    }

    /// <summary>관전 상태 변화처럼 다른 플레이어의 이름표 노출 조건까지 바꾸는 사건에서 사용한다.</summary>
    private static void RefreshAllOverheadNameLabels()
    {
      for (int i = _overheadNameInstances.Count - 1; i >= 0; i--)
      {
        var each = _overheadNameInstances[i];
        if (each == null)
        {
          _overheadNameInstances.RemoveAt(i);
          continue;
        }

        each.RefreshOverheadNameLabel();
      }
    }

    private bool ShouldShowOverheadName()
    {
      if (!_overheadNameEnabled || !_overheadNameActive)
        return false;

      // 서버 전용 인스턴스와 Lite 클론은 화면 표현을 만들지 않는다.
      if (!IsClientStarted || MppmLiteMode.IsHeadless)
        return false;

      if (string.IsNullOrWhiteSpace(UserDisplayName))
        return false;

      if (IsOwner && !_overheadNameShownForOwner)
        return false;

      // 관전자의 몸은 일반 플레이어의 카메라에서 컬링된다. 이름표만 남으면 위치가 노출되므로 함께 숨긴다.
      return !IsSpectator || IsLocalViewerSpectator();
    }

    private static bool IsLocalViewerSpectator()
    {
      for (int i = 0; i < _overheadNameInstances.Count; i++)
      {
        var each = _overheadNameInstances[i];
        if (each != null && each.IsOwner)
          return each.IsSpectator;
      }

      return false;
    }

    // 이름표를 띄울 머리 위 앵커. UI 컨트롤러가 이 위치를 화면에 투영해 라벨을 배치한다.
    private Transform ResolveOverheadNameAnchor()
    {
      if (_overheadNameAnchor == null)
      {
        var anchorObject = new GameObject("Overhead Name Anchor");
        anchorObject.transform.SetParent(transform, false);
        _overheadNameAnchor = anchorObject.transform;
      }

      _overheadNameAnchor.localPosition = new Vector3(0f, GetOverheadNameHeight(), 0f);
      return _overheadNameAnchor;
    }

    // CharacterController 캡슐의 윗면을 머리 높이로 본다. 캐릭터 모델이 바뀌면 center 도 함께 갱신되므로
    // Renderer 바운즈를 훑지 않고도 모델별 키를 따라간다.
    // 라벨과 머리 사이 여유 간격은 UI 컨트롤러의 worldHeightOffset 이 담당한다.
    private float GetOverheadNameHeight()
    {
      if (_characterController == null)
        _characterController = GetComponent<CharacterController>();

      if (_characterController == null)
        return 2f;

      return _characterController.center.y + _characterController.height * 0.5f;
    }
  }
}
