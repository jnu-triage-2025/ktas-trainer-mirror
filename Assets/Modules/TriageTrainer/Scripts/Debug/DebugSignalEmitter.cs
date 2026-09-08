using Input = MultiplayerInfrastructure.Automation.PlayerInput;
using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.DebugTools
{
  /// <summary>
  /// IndevScene 등 검증용으로, 임의의 시나리오 인터랙션 신호(sig.*)를 손쉽게 발생/해제하는 디버그 컴포넌트.
  ///
  /// <para>사용처</para>
  /// <list type="bullet">
  /// <item>평면 월드맵에 빈 GameObject 로 임시 배치하여 게이트(Validator/Parallel) 통과를 검증한다.</item>
  /// <item>구역 진입을 흉내내는 트리거 박스로 쓰거나, 키 입력으로 특정 신호를 즉시 올린다.</item>
  /// </list>
  ///
  /// <para>주의</para>
  /// 디버그 전용이다. 실제 게임플레이 배선(아이템 사용/사정/연결 등)을 대체하지 않으며,
  /// 빌드/프로덕션 씬에는 배치하지 않는 것을 전제로 한다. 신호는
  /// <see cref="ScenarioInteractionSignals.Raise"/>(서버 권한 라우팅)로 올린다.
  /// </summary>
  public sealed class DebugSignalEmitter : MonoBehaviour
  {
    [Header("발생시킬 신호(sig. 접두사 제외)")]
    [Tooltip("예: enter_triage_zone, apply_gauze, check_avpu_gcs_patient_a")]
    [SerializeField] private List<string> _signals = new();

    [Header("발생 조건")]
    [Tooltip("이 콜라이더(IsTrigger)에 _playerTag 태그 오브젝트가 들어오면 신호를 올린다.")]
    [SerializeField] private bool _raiseOnTriggerEnter = true;
    [SerializeField] private string _playerTag = "Player";

    [Tooltip("이 키를 누르면 신호를 올린다(KeyCode.None 이면 비활성).")]
    [SerializeField] private KeyCode _raiseKey = KeyCode.None;

    [Tooltip("이 키를 누르면 신호를 내린다(clear). KeyCode.None 이면 비활성.")]
    [SerializeField] private KeyCode _clearKey = KeyCode.None;

    [Header("옵션")]
    [Tooltip("TriggerEnter 1회만 발생시키고 이후 무시한다.")]
    [SerializeField] private bool _triggerOnce = true;
    [SerializeField] private bool _logToConsole = true;

    private bool _triggered;

    private void Update()
    {
      if (_raiseKey != KeyCode.None && Input.GetKeyDown(_raiseKey))
        RaiseAll();

      if (_clearKey != KeyCode.None && Input.GetKeyDown(_clearKey))
        ClearAll();
    }

    private void OnTriggerEnter(Collider other)
    {
      if (!_raiseOnTriggerEnter)
        return;

      if (_triggerOnce && _triggered)
        return;

      if (!string.IsNullOrEmpty(_playerTag) && !other.CompareTag(_playerTag))
        return;

      _triggered = true;
      RaiseAll();
    }

    [ContextMenu("Raise Signals Now")]
    public void RaiseAll()
    {
      for (int i = 0; i < _signals.Count; i++)
      {
        var s = _signals[i];
        if (string.IsNullOrWhiteSpace(s))
          continue;

        ScenarioInteractionSignals.Raise(s);
        if (_logToConsole)
          Debug.Log($"[DebugSignalEmitter] Raised '{ScenarioInteractionSignals.Normalize(s)}'", this);
      }
    }

    [ContextMenu("Clear Signals Now")]
    public void ClearAll()
    {
      for (int i = 0; i < _signals.Count; i++)
      {
        var s = _signals[i];
        if (string.IsNullOrWhiteSpace(s))
          continue;

        ScenarioInteractionSignals.Clear(s);
        if (_logToConsole)
          Debug.Log($"[DebugSignalEmitter] Cleared '{ScenarioInteractionSignals.Normalize(s)}'", this);
      }

      _triggered = false;
    }
  }
}
