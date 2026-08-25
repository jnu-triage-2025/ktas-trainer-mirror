#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.TTS
{
  /// <summary>
  /// ONNX 모델 파일이 없을 때 플레이 모드 진입을 막고 사용자에게 보여주는 모달 대화상자.
  ///
  /// <see cref="EditorUtility.DisplayDialogComplex"/>는 체크박스를 지원하지 않으므로,
  /// "앞으로도 계속 무시하기" 체크박스를 넣기 위해 커스텀 <see cref="EditorWindow"/>로 구현했다.
  ///
  /// 버튼: [다운로드 창 열기] / [무시] / [취소]
  ///   · 다운로드 창 열기: TTS > Download Models 창을 연다.
  ///   · 무시: 이번 플레이 모드 진입에서만(또는 체크박스 선택 시 앞으로도 계속) 모델 누락 경고 없이 진행한다.
  ///   · 취소: 아무것도 하지 않고 플레이 모드에 진입하지 않는다.
  /// </summary>
  internal sealed class TTSMissingModelsDialog : EditorWindow
  {
    public enum DialogChoice
    {
      Cancel,
      OpenDownloadWindow,
      Ignore,
    }

    public struct Result
    {
      public DialogChoice Choice;
      public bool IgnorePermanently;
    }

    private string _onnxDirPath;
    private IReadOnlyList<string> _missing;
    private bool _ignorePermanently;
    private Result _result;
    private bool _closedByButton;

    /// <summary>
    /// 대화상자를 모달로 띄우고 사용자의 선택 결과를 반환한다. (동기 호출, 창이 닫힐 때까지 블록)
    /// </summary>
    public static Result Show(string onnxDirPath, IReadOnlyList<string> missing)
    {
      var win = CreateInstance<TTSMissingModelsDialog>();
      win._onnxDirPath = onnxDirPath;
      win._missing = missing;
      win._result = new Result { Choice = DialogChoice.Cancel, IgnorePermanently = false };
      win.titleContent = new GUIContent("TTS 모델 없음");
      win.minSize = new Vector2(460, 260);
      win.maxSize = new Vector2(460, 320);
      win.ShowModalUtility();
      return win._result;
    }

    private void OnGUI()
    {
      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("플레이 모드를 시작할 수 없습니다.", EditorStyles.boldLabel);
      EditorGUILayout.Space(4);

      EditorGUILayout.LabelField($"다음 ONNX 모델 파일이 없습니다 ({_onnxDirPath}):", EditorStyles.wordWrappedLabel);
      EditorGUILayout.Space(2);

      using (new EditorGUI.IndentLevelScope())
      {
        foreach (var file in _missing)
          EditorGUILayout.LabelField($"·  {file}");
      }

      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("TTS > Download Models 에서 다운로드하시겠습니까?", EditorStyles.wordWrappedLabel);

      EditorGUILayout.Space(8);
      _ignorePermanently = EditorGUILayout.ToggleLeft(
        "앞으로도 계속 무시하기 (Project Settings > TTS 에서 다시 켤 수 있습니다)",
        _ignorePermanently);

      EditorGUILayout.Space(12);

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("취소"))
          CloseWith(DialogChoice.Cancel);

        if (GUILayout.Button("무시"))
          CloseWith(DialogChoice.Ignore);

        if (GUILayout.Button("다운로드 창 열기"))
          CloseWith(DialogChoice.OpenDownloadWindow);
      }
    }

    private void CloseWith(DialogChoice choice)
    {
      _result = new Result { Choice = choice, IgnorePermanently = _ignorePermanently };
      _closedByButton = true;
      Close();
    }

    private void OnDestroy()
    {
      // 창을 닫기(X) 버튼으로 닫은 경우에는 취소로 취급한다.
      if (!_closedByButton)
        _result = new Result { Choice = DialogChoice.Cancel, IgnorePermanently = false };
    }
  }
}
#endif
