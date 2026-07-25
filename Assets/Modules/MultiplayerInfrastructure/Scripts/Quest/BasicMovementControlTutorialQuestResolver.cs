using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Quest
{
  /// <summary>
  /// 기본 이동 조작 튜토리얼 전용 퀘스트 처리기.
  /// WASD와 마우스 기본 조작이 있었던 프레임의 시간을 누적한다. 총 3초 이상 조작하고
  /// 키보드와 마우스를 각각 한 번 이상 조작해야 이동 조작 퀘스트를 완료한다.
  /// 이 세부 조건은 퀘스트 표시 항목으로 노출하지 않는다.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(QuestManager))]
  public sealed class BasicMovementControlTutorialQuestResolver : MonoBehaviour
  {
    public const string QuestIdentifier = "tutorial-move";
    public const string CompletionSignalIdentifier = "tutorial_player_moved";

    private const float RequiredInputSeconds = 2f;
    private const float MouseDeltaEpsilon = 0.0001f;

    private QuestManager _questManager;
    private float _accumulatedInputSeconds;
    private bool _hasKeyboardInput;
    private bool _hasMouseInput;

    private void Awake()
    {
      _questManager = GetComponent<QuestManager>();
    }

    private void Update()
    {
      if (_questManager == null)
        _questManager = GetComponent<QuestManager>();

      if (_questManager == null
          || !_questManager.TryGetQuest(QuestIdentifier, out var quest)
          || quest == null
          || quest.Completed)
      {
        ResetProgress();
        return;
      }

      if (!TryGetBasicControlInput(out bool keyboardInput, out bool mouseInput))
        return;

      // 여러 입력이 동시에 눌려도 같은 프레임의 시간은 한 번만 더한다.
      _accumulatedInputSeconds += Time.deltaTime;
      _hasKeyboardInput |= keyboardInput;
      _hasMouseInput |= mouseInput;

      if (_accumulatedInputSeconds < RequiredInputSeconds || !_hasKeyboardInput || !_hasMouseInput)
        return;

      ScenarioInteractionSignals.Raise(CompletionSignalIdentifier);
      _questManager.CompleteQuest(QuestIdentifier);
      ResetProgress();
    }

    private void ResetProgress()
    {
      _accumulatedInputSeconds = 0f;
      _hasKeyboardInput = false;
      _hasMouseInput = false;
    }

    private static bool TryGetBasicControlInput(out bool keyboardInput, out bool mouseInput)
    {
      keyboardInput = false;
      mouseInput = false;

      // UI 조작 중의 클릭/마우스 이동은 카메라 회전이나 플레이어 기본 조작 학습으로 보지 않는다.
      if (!UIOverlayStack.IsEmpty())
        return false;

      keyboardInput = Input.GetKey(KeyCode.W)
          || Input.GetKey(KeyCode.A)
          || Input.GetKey(KeyCode.S)
          || Input.GetKey(KeyCode.D);
      bool mouseButtonPressed = Input.GetMouseButton(0)
          || Input.GetMouseButton(1)
          || Input.GetMouseButton(2);
      bool cameraRotating = Mathf.Abs(Input.GetAxisRaw("Mouse X")) > MouseDeltaEpsilon
          || Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > MouseDeltaEpsilon;
      mouseInput = mouseButtonPressed || cameraRotating;

      return keyboardInput || mouseInput;
    }
  }
}
