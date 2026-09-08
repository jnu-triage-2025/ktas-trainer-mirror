using PlayerPrefs = MultiplayerInfrastructure.Automation.ProfilePlayerPrefs;
using TextToSpeechService;
using UnityEngine;

namespace MultiplayerInfrastructure.TTS
{
  /// <summary>
  /// TTS 엔진을 쓸지 말지를 이 기기에 저장하고 엔진에 적용하는 설정 항목입니다.
  ///
  /// 값은 PlayerPrefs에 남으므로 게임을 다시 켜도 유지됩니다. 끄면
  /// <see cref="TextToSpeechService.TTSService"/> 가 ONNX 세션과 음성 캐시를 메모리에서 내리고,
  /// 아직 올리지 않았다면 아예 올리지 않습니다. 다시 켜면 초기화를 새로 시작합니다.
  ///
  /// 상태 자체는 <see cref="TTSEngineSwitch"/> 가 들고 있습니다. 저장소를 오가는 일만
  /// 여기서 담당해, 켜고 끈 값이 두 군데로 갈라지지 않게 합니다.
  /// </summary>
  public static class TTSEnginePreference
  {
    private const string PlayerPrefsKey = "MultiplayerInfrastructure.TTSEngineDisabled";

    /// <summary>true면 TTS 엔진을 쓰지 않습니다.</summary>
    public static bool IsDisabled => TTSEngineSwitch.IsDisabled;

    /// <summary>설정 값을 즉시 적용하고 저장합니다.</summary>
    public static void SetDisabled(bool disabled)
    {
      TTSEngineSwitch.SetDisabled(disabled);
      PlayerPrefs.SetInt(PlayerPrefsKey, disabled ? 1 : 0);
      PlayerPrefs.Save();
    }

    /// <summary>
    /// 저장된 값을 읽어 적용합니다.
    ///
    /// TTSService가 Awake에서 모델을 적재하기 전에 끝나야 꺼 둔 설정이 헛돌지 않으므로
    /// 씬을 불러오기 전에 실행합니다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void LoadAndApply()
    {
      TTSEngineSwitch.SetDisabled(PlayerPrefs.GetInt(PlayerPrefsKey, 0) != 0);
    }
  }
}
