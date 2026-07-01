#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using TextToSpeechService;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 프로젝트 내 모든 시나리오 그래프(JSON)를 스캔하여, PlayTTS 플래그가 켜져 있고
  /// 변수를 포함하지 않는(=bake 가능한) 인라인 텍스트(Dialogue/Choice/Quiz 콘텐츠)를 수집하고
  /// 사전 합성(bake)한다.
  ///
  /// 이 클래스는 에디터 전용(<c>#if UNITY_EDITOR</c>)이지만 <c>Editor</c> 폴더 밖(런타임 어셈블리)에
  /// 위치한다. 이렇게 하면 <c>Assembly-CSharp</c>(에디터 정의 포함)로 컴파일되어,
  /// 동일 어셈블리의 에디터 훅(TTSPlayModeValidator, TTSBuildPreprocessor)에서도 참조할 수 있다.
  ///
  /// 수집 결과는 다음 두 곳에서 사용된다.
  ///   · 인라인 오디오 Baker (사전 합성)
  ///   · 플레이 모드 진입 / 빌드 시 bake 상태(미bake/dirty) 검사
  /// </summary>
  public static class ScenarioTTSBakeScanner
  {
    /// <summary>TranscriptParser와 동일한 변수 플레이스홀더 패턴 ({key}).</summary>
    private static readonly Regex VariablePattern = new Regex(@"\{([^}]+)\}", RegexOptions.Compiled);

    /// <summary>텍스트가 변수({...})를 포함하면 true (=bake 불가, 런타임 즉석 합성 대상).</summary>
    public static bool ContainsVariable(string text)
      => !string.IsNullOrEmpty(text) && VariablePattern.IsMatch(text);

    /// <summary>bake 가능한 인라인 TTS 작업 항목.</summary>
    public sealed class InlineTTSJob
    {
      public string ScenarioIdentifier;
      public string NodeIdentifier;
      public string Text;

      /// <summary>이 텍스트 내용에 대응하는 baked WAV의 절대 경로 (현재 해시 기준).</summary>
      public string ExpectedBakedPath;

      /// <summary>현재 해시의 baked WAV가 존재하는지 여부.</summary>
      public bool IsBaked;

      /// <summary>
      /// 같은 노드에 대한 baked WAV가 이전 텍스트 해시로 존재하지만
      /// 현재 텍스트와 일치하지 않는 경우 true (=텍스트가 변경된 dirty 상태).
      /// </summary>
      public bool IsDirty;

      /// <summary>이 노드에 대해 존재하는 stale(구버전) baked 파일 경로 목록.</summary>
      public List<string> StaleBakedPaths = new List<string>();
    }

    /// <summary>스캔 결과 요약.</summary>
    public sealed class ScanResult
    {
      public readonly List<InlineTTSJob> Jobs = new List<InlineTTSJob>();

      /// <summary>아직 bake되지 않은(=현재 해시 파일 없음) 작업 수.</summary>
      public int MissingCount;

      /// <summary>텍스트 변경으로 stale 파일이 남은 dirty 작업 수.</summary>
      public int DirtyCount;

      /// <summary>bake가 필요한(missing 또는 dirty) 작업이 있으면 true.</summary>
      public bool NeedsBake => MissingCount > 0 || DirtyCount > 0;
    }

    /// <summary>
    /// AssetDatabase에서 *.scenario.json TextAsset을 모두 찾아 스캔한다.
    /// (에디터 전용)
    /// </summary>
    public static ScanResult ScanAllScenarios(string streamingAssetsPath)
    {
      var result = new ScanResult();

      // .scenario.json 은 TextAsset 이지만 확장자가 .json 이므로 t:TextAsset 로 검색 후 필터링.
      string[] guids = AssetDatabase.FindAssets("t:TextAsset");
      foreach (var guid in guids)
      {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (!assetPath.EndsWith(".scenario.json")) continue;

        string json;
        try
        {
          json = File.ReadAllText(assetPath);
        }
        catch
        {
          continue;
        }

        ScenarioGraph graph;
        try
        {
          // 스캔 목적상 스키마 검증은 생략(성능/관대함)한다.
          graph = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: false);
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[ScenarioTTSBakeScanner] 시나리오 파싱 실패 ({assetPath}): {ex.Message}");
          continue;
        }

        CollectFromGraph(graph, streamingAssetsPath, result);
      }

      return result;
    }

    private static void CollectFromGraph(ScenarioGraph graph, string streamingAssetsPath, ScanResult result)
    {
      foreach (var node in graph.Nodes.Values)
      {
        switch (node)
        {
          case ScenarioDialogueNode dialogue when dialogue.PlayTTS:
            AddJob(graph.Identifier, dialogue.Identifier, dialogue.DialogueContent, streamingAssetsPath, result);
            break;

          case ScenarioChoiceNode choice when choice.PlayTTS:
            AddJob(graph.Identifier, choice.Identifier, choice.DialogueContent, streamingAssetsPath, result);
            break;

          case ScenarioQuizNode quiz when quiz.PlayTTS:
            AddJob(graph.Identifier, quiz.Identifier, quiz.Question, streamingAssetsPath, result);
            // 피드백 텍스트는 런타임에서 별도 노드 식별자 접미어로 재생된다.
            AddJob(graph.Identifier, quiz.Identifier + "_feedbackCorrect", quiz.FeedbackCorrect, streamingAssetsPath, result);
            AddJob(graph.Identifier, quiz.Identifier + "_feedbackIncorrect", quiz.FeedbackIncorrect, streamingAssetsPath, result);
            break;
        }
      }
    }

    private static void AddJob(
      string scenarioId, string nodeId, string text, string streamingAssetsPath, ScanResult result)
    {
      if (string.IsNullOrWhiteSpace(text)) return;

      // 변수를 포함하면 bake 대상에서 제외 (런타임 즉석 합성).
      if (ContainsVariable(text)) return;

      string expectedPath = TTSCore.GetBakedInlineClipPath(streamingAssetsPath, scenarioId, nodeId, text);
      bool   isBaked      = File.Exists(expectedPath);

      var job = new InlineTTSJob
      {
        ScenarioIdentifier = scenarioId,
        NodeIdentifier     = nodeId,
        Text               = text,
        ExpectedBakedPath  = expectedPath,
        IsBaked            = isBaked,
      };

      // 같은 노드에 대한 stale 파일(구버전 해시) 탐색 → dirty 판정.
      string nodeDir     = Path.GetDirectoryName(expectedPath);
      string safeNode    = Path.GetFileNameWithoutExtension(expectedPath);
      // safeNode 는 "{nodeIdentifier}_{hash}" 형태. 접두어(nodeIdentifier_)만 추출.
      int    lastUnder   = safeNode.LastIndexOf('_');
      string nodePrefix  = lastUnder >= 0 ? safeNode.Substring(0, lastUnder + 1) : safeNode;

      if (nodeDir != null && Directory.Exists(nodeDir))
      {
        foreach (var wav in Directory.GetFiles(nodeDir, nodePrefix + "*.wav"))
        {
          if (Path.GetFullPath(wav) != Path.GetFullPath(expectedPath))
            job.StaleBakedPaths.Add(wav);
        }
      }

      job.IsDirty = job.StaleBakedPaths.Count > 0;

      if (!isBaked) result.MissingCount++;
      if (job.IsDirty) result.DirtyCount++;

      result.Jobs.Add(job);
    }

    // =========================================================================
    // 동기 bake (플레이 모드 진입 검사 등에서 재사용)
    // =========================================================================

    /// <summary>
    /// 스캔 후 미bake/dirty 작업을 동기적으로 모두 bake한다.
    /// 플레이 모드 진입 검사에서 사용자가 "지금 bake"를 선택했을 때 호출한다.
    /// 진행 상황은 <see cref="EditorUtility.DisplayProgressBar"/> 로 표시한다.
    /// </summary>
    public static void BakeAllNeededSynchronously(
      string language = "ko", int totalStep = 5, float speed = 1.05f, string voiceStyleName = "F1")
    {
      string sa      = Application.streamingAssetsPath;
      string onnxDir = TTSCore.GetOnnxDir(sa);

      if (!TTSCore.AreModelsPresent(onnxDir))
      {
        Debug.LogError("[ScenarioTTSBakeScanner] ONNX 모델이 없어 bake할 수 없습니다.");
        return;
      }

      var scan = ScanAllScenarios(sa);
      if (!scan.NeedsBake) return;

      string stylePath = TTSCore.GetVoiceStylePath(sa, voiceStyleName);

      try
      {
        EditorUtility.DisplayProgressBar("Scenario Inline TTS Bake", "준비 중...", 0f);
        using var core = new TTSCore(onnxDir, stylePath);

        var jobs = scan.Jobs;
        int done = 0;
        foreach (var job in jobs)
        {
          if (job.IsBaked && !job.IsDirty) { done++; continue; }

          EditorUtility.DisplayProgressBar(
            "Scenario Inline TTS Bake",
            $"{job.ScenarioIdentifier}/{job.NodeIdentifier}",
            jobs.Count == 0 ? 1f : (float)done / jobs.Count);

          if (job.StaleBakedPaths != null)
          {
            foreach (var stale in job.StaleBakedPaths)
              try { if (File.Exists(stale)) File.Delete(stale); } catch { /* ignore */ }
          }

          float[] wav = core.Synthesize(job.Text, language, totalStep, speed);
          Directory.CreateDirectory(Path.GetDirectoryName(job.ExpectedBakedPath)!);
          Supertonic.Helper.WriteWavFile(job.ExpectedBakedPath, wav, core.SampleRate);
          done++;
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"[ScenarioTTSBakeScanner] 동기 bake 실패: {ex}");
      }
      finally
      {
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
      }
    }
  }
}
#endif
