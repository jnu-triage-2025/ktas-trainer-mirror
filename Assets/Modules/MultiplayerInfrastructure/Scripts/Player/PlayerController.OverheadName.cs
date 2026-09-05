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
    [Tooltip("머리 위 이름표의 높이 보정(m). 음수이면 아래로 내려온다. " +
             "프리팹에 NameTagDisplayAttachPoint 가 배치되어 있으면 그 위치가 우선하므로 이 값은 쓰이지 않는다. " +
             "부착점이 없을 때 쓰는 CharacterController 캡슐은 캐릭터 모델보다 여유 있게 잡혀 있어서, " +
             "캡슐 윗면을 그대로 쓰면 이름표가 머리에서 지나치게 떨어져 보인다.")]
    [SerializeField] private float _overheadNameHeightOffset = -0.4f;
    [SerializeField] private Color _overheadNameColor = Color.white;

    // 로컬 피어에서 살아 있는 PlayerController 전체. 누군가의 관전 상태가 바뀌면
    // "누가 누구의 이름표를 볼 수 있는가"가 함께 바뀌므로 일괄 갱신에 사용한다.
    private static readonly List<PlayerController> _overheadNameInstances = new();

    private Transform _overheadNameAnchor;
    private NameTagDisplayAttachPoint _nameTagAttachPoint;
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

#if UNITY_EDITOR
    // 플레이 중에 인스펙터로 높이 보정을 조정하면 즉시 반영되게 한다.
    // 앵커 위치는 라벨을 갱신할 때만 다시 계산하므로, 이 호출이 없으면 다음 사건까지 반영되지 않는다.
    private void OnValidate_OverheadName()
    {
      if (Application.isPlaying && _overheadNameActive)
        RefreshOverheadNameLabel();
    }
#endif

    private void TeardownOverheadName()
    {
      if (!_overheadNameActive)
        return;

      EntityOverheadLabelUIController.ActiveInstanceChanged -= RefreshOverheadNameLabel;
      _overheadNameInstances.Remove(this);
      _overheadNameActive = false;

      // 파괴된 앵커는 컨트롤러 LateUpdate의 stale 정리가 제거하지만, 명시적으로 먼저 해제한다.
      EntityOverheadLabelUIController.ActiveInstance?.RemoveLabels(CurrentOverheadNameAnchor);

      // 프리팹에 배치된 부착점은 그대로 두고, 런타임에 만든 폴백 앵커만 정리한다.
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
        EntityOverheadLabelUIController.ActiveInstance?.RemoveLabel(CurrentOverheadNameAnchor, OverheadNameChannel);
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
      // 프리팹이 이름표 부착점을 지정했다면 그 위치를 그대로 쓴다(작업자가 에디터에서 눈으로 맞춘 높이).
      var attachPoint = ResolveNameTagAttachPoint();
      if (attachPoint != null)
        return attachPoint.transform;

      if (_overheadNameAnchor == null)
      {
        var anchorObject = new GameObject("Overhead Name Anchor");
        anchorObject.transform.SetParent(transform, false);
        _overheadNameAnchor = anchorObject.transform;
      }

      _overheadNameAnchor.localPosition = new Vector3(0f, GetOverheadNameHeight(), 0f);
      return _overheadNameAnchor;
    }

    /// <summary>
    /// 지금 라벨이 붙어 있는 앵커를 돌려준다. 해제 경로가 폴백 앵커를 새로 만들지 않도록,
    /// 부착점도 폴백 앵커도 없으면 null 을 돌려준다.
    /// </summary>
    private Transform CurrentOverheadNameAnchor
    {
      get
      {
        var attachPoint = ResolveNameTagAttachPoint();
        return attachPoint != null ? attachPoint.transform : _overheadNameAnchor;
      }
    }

    // 프리팹에 배치된 이름표 부착점. 캐릭터 모델이 런타임에 교체되면 함께 파괴될 수 있으므로 없을 때 다시 찾는다.
    private NameTagDisplayAttachPoint ResolveNameTagAttachPoint()
    {
      if (_nameTagAttachPoint == null)
        _nameTagAttachPoint = GetComponentInChildren<NameTagDisplayAttachPoint>(true);

      return _nameTagAttachPoint;
    }

    // 부착점이 없을 때 쓰는 폴백 높이. CharacterController 캡슐의 윗면을 기준으로 삼는다.
    // 캐릭터 모델이 바뀌면 center 도 함께 갱신되므로 Renderer 바운즈를 훑지 않고도 모델별 키를 따라간다.
    // 다만 캡슐은 이동 판정을 위해 모델보다 여유 있게 잡혀 있으므로, 실제 머리 높이에 맞도록
    // _overheadNameHeightOffset 으로 보정한다. (UI 컨트롤러의 worldHeightOffset 이 여기에 더해진다.)
    private float GetOverheadNameHeight()
    {
      if (_characterController == null)
        _characterController = GetComponent<CharacterController>();

      float capsuleTop = _characterController == null
        ? 2f
        : _characterController.center.y + _characterController.height * 0.5f;

      return capsuleTop + _overheadNameHeightOffset;
    }
  }
}
