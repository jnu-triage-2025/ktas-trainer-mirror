using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Quest
{
  /// <summary>
  /// 기본 이동 조작 튜토리얼 전용 퀘스트 처리기.
  /// WASD, 마우스 버튼, 카메라 회전을 유발하는 마우스 이동 중 하나라도 발생한 프레임의 시간을
  /// 누적하여, 총 1초를 초과하면 이동 조작 퀘스트를 완료한다.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(QuestManager))]
  public sealed class BasicMovementControlTutorialQuestResolver : MonoBehaviour
  {
    public const string QuestIdentifier = "tutorial-move";
    public const string CompletionSignalIdentifier = "tutorial_player_moved";

    private const float RequiredInputSeconds = 1f;
    private const float MouseDeltaEpsilon = 0.0001f;

    private QuestManager _questManager;
    private float _accumulatedInputSeconds;

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
        _accumulatedInputSeconds = 0f;
        return;
      }

      if (!HasBasicControlInput())
        return;

      // 여러 입력이 동시에 눌려도 같은 프레임의 시간은 한 번만 더한다.
      _accumulatedInputSeconds += Time.deltaTime;
      if (_accumulatedInputSeconds <= RequiredInputSeconds)
        return;

      ScenarioInteractionSignals.Raise(CompletionSignalIdentifier);
      _questManager.CompleteQuest(QuestIdentifier);
      _accumulatedInputSeconds = 0f;
    }

    private static bool HasBasicControlInput()
    {
      // UI 조작 중의 클릭/마우스 이동은 카메라 회전이나 플레이어 기본 조작 학습으로 보지 않는다.
      if (!UIOverlayStack.IsEmpty())
        return false;

      bool wasdPressed = Input.GetKey(KeyCode.W)
          || Input.GetKey(KeyCode.A)
          || Input.GetKey(KeyCode.S)
          || Input.GetKey(KeyCode.D);
      bool mouseButtonPressed = Input.GetMouseButton(0)
          || Input.GetMouseButton(1)
          || Input.GetMouseButton(2);
      bool cameraRotating = Mathf.Abs(Input.GetAxisRaw("Mouse X")) > MouseDeltaEpsilon
          || Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > MouseDeltaEpsilon;

      return wasdPressed || mouseButtonPressed || cameraRotating;
    }
  }
}
