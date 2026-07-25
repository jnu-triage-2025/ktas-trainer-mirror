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
  /// 변수를 포함하지 않는(=bake 가능한) 인라인 텍스트(Dialogue/DisinteractableDialogue/Choice/Quiz 콘텐츠)를 수집하고
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

      /// <summary>
      /// 이 job에 사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.
      /// baked 경로에 voice 폴더가 포함되는지 결정한다.
      /// </summary>
      public string VoiceIdentifier;

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

      /// <summary>
      /// 현재 어떤 시나리오 노드에서도 사용하지 않는(=orphan) baked WAV 절대 경로 목록.
      /// 노드/시나리오 삭제, PlayTTS 해제, 텍스트 변경 등으로 더 이상 참조되지 않는 파일들이다.
      /// (dirty 작업의 stale 파일도 여기에 포함된다.)
      /// </summary>
      public readonly List<string> OrphanedBakedPaths = new List<string>();

      /// <summary>사용하지 않는 baked 파일 수.</summary>
      public int OrphanCount => OrphanedBakedPaths.Count;

      /// <summary>bake가 필요한(missing 또는 dirty) 작업이 있으면 true.</summary>
      public bool NeedsBake => MissingCount > 0 || DirtyCount > 0;

      /// <summary>정리할 사용하지 않는 baked 파일이 있으면 true.</summary>
      public bool HasOrphans => OrphanedBakedPaths.Count > 0;
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

      CollectOrphans(streamingAssetsPath, result);

      return result;
    }

    /// <summary>
    /// BakedInline 폴더 내 모든 .wav 파일 중 현재 어떤 job에서도 참조하지 않는
    /// 파일(=orphan)을 수집한다. 현재 유효한 대상은 각 job의 ExpectedBakedPath 이다.
    /// </summary>
    private static void CollectOrphans(string streamingAssetsPath, ScanResult result)
    {
      string inlineRoot = Path.Combine(streamingAssetsPath, TTSCore.BakedInlineAudioSubdir);
      if (!Directory.Exists(inlineRoot)) return;

      // 현재 유효한(사용 중인) baked 경로 집합
      var validPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var job in result.Jobs)
        validPaths.Add(Path.GetFullPath(job.ExpectedBakedPath));

      foreach (var wav in Directory.GetFiles(inlineRoot, "*.wav", SearchOption.AllDirectories))
      {
        if (!validPaths.Contains(Path.GetFullPath(wav)))
          result.OrphanedBakedPaths.Add(wav);
      }
    }

    /// <summary>
    /// 사용하지 않는(orphan) baked WAV 파일과 그 .meta를 삭제한다.
    /// 삭제 후 비게 된 하위 디렉터리도 정리한다.
    /// </summary>
    /// <returns>실제로 삭제한 파일 수.</returns>
    public static int DeleteOrphans(IEnumerable<string> orphanedPaths)
    {
      int deleted = 0;
      var touchedDirs = new HashSet<string>();

      foreach (var path in orphanedPaths)
      {
        try
        {
          if (File.Exists(path))
          {
            File.Delete(path);
            deleted++;
          }

          string meta = path + ".meta";
          if (File.Exists(meta)) File.Delete(meta);

          string dir = Path.GetDirectoryName(path);
          if (!string.IsNullOrEmpty(dir)) touchedDirs.Add(dir);
        }
        catch (Exception e)
        {
          Debug.LogWarning($"[ScenarioTTSBakeScanner] orphan 삭제 실패 ({path}): {e.Message}");
        }
      }

      // 비게 된 시나리오 하위 디렉터리 정리
      foreach (var dir in touchedDirs)
      {
        try
        {
          if (Directory.Exists(dir) && Directory.GetFiles(dir).Length == 0
              && Directory.GetDirectories(dir).Length == 0)
          {
            Directory.Delete(dir);
            string dirMeta = dir + ".meta";
            if (File.Exists(dirMeta)) File.Delete(dirMeta);
          }
        }
        catch { /* ignore */ }
      }

      return deleted;
    }

    private static void CollectFromGraph(ScenarioGraph graph, string streamingAssetsPath, ScanResult result)
    {
      foreach (var node in graph.Nodes.Values)
      {
        switch (node)
        {
          case ScenarioDialogueNode dialogue when dialogue.PlayTTS:
            AddJob(graph.Identifier, dialogue.Identifier, dialogue.DialogueContent, streamingAssetsPath, result,
              voiceIdentifier: dialogue.TtsVoiceIdentifier);
            break;

          case ScenarioDisinteractableDialogueNode disinteractable when disinteractable.PlayTTS:
            AddJob(graph.Identifier, disinteractable.Identifier, disinteractable.DialogueContent, streamingAssetsPath, result,
              voiceIdentifier: disinteractable.TtsVoiceIdentifier);
            break;

          case ScenarioChoiceNode choice when choice.PlayTTS:
            AddJob(graph.Identifier, choice.Identifier, choice.DialogueContent, streamingAssetsPath, result,
              voiceIdentifier: choice.TtsVoiceIdentifier);
            break;

          case ScenarioQuizNode quiz when quiz.PlayTTS:
            AddJob(graph.Identifier, quiz.Identifier, quiz.Question, streamingAssetsPath, result,
              voiceIdentifier: quiz.TtsVoiceIdentifier);
            // 피드백 텍스트는 런타임에서 별도 노드 식별자 접미어로 재생된다.
            AddJob(graph.Identifier, quiz.Identifier + "_feedbackCorrect", quiz.FeedbackCorrect, streamingAssetsPath, result,
              voiceIdentifier: quiz.TtsVoiceIdentifier);
            AddJob(graph.Identifier, quiz.Identifier + "_feedbackIncorrect", quiz.FeedbackIncorrect, streamingAssetsPath, result,
              voiceIdentifier: quiz.TtsVoiceIdentifier);
            break;
        }
      }
    }

    private static void AddJob(
      string scenarioId, string nodeId, string text, string streamingAssetsPath, ScanResult result,
      string voiceIdentifier = null)
    {
      if (string.IsNullOrWhiteSpace(text)) return;

      // 변수를 포함하면 bake 대상에서 제외 (런타임 즉석 합성).
      if (ContainsVariable(text)) return;

      string hash         = TTSCore.ComputeTextHash(text);
      string expectedPath = TTSCore.GetBakedInlineClipPath(streamingAssetsPath, scenarioId, nodeId, text, voiceIdentifier);
      bool   isBaked      = File.Exists(expectedPath);

      var job = new InlineTTSJob
      {
        ScenarioIdentifier = scenarioId,
        NodeIdentifier     = nodeId,
        Text               = text,
        VoiceIdentifier    = string.IsNullOrEmpty(voiceIdentifier) ? null : voiceIdentifier,
        ExpectedBakedPath  = expectedPath,
        IsBaked            = isBaked,
      };

      // 같은 노드에 대한 stale 파일(구버전 해시) 탐색 → dirty 판정.
      //
      // 주의: 노드 식별자가 서로 접두어 관계일 수 있다(예: "N001" 과 "N001_1", "N001_retry_a").
      // 단순 "{nodeId}_*" 글롭은 다른 노드의 파일까지 오탐하므로,
      // "{sanitizedNodeId}_{16자리 hex}.wav" 형태로 정확히 일치하는 파일만 stale 후보로 본다.
      string nodeDir  = Path.GetDirectoryName(expectedPath);
      string fileStem = Path.GetFileNameWithoutExtension(expectedPath); // "{sanitizedNodeId}_{hash}"
      // fileStem 끝의 "_{hash}"(= '_' + 16 hex)를 제거하면 정확한 sanitizedNodeId 를 얻는다.
      string sanitizedNodeId = fileStem.Length > hash.Length + 1
        ? fileStem.Substring(0, fileStem.Length - hash.Length - 1)
        : fileStem;

      var exactPattern = new Regex(
        "^" + Regex.Escape(sanitizedNodeId) + @"_[0-9a-f]{16}\.wav$",
        RegexOptions.IgnoreCase);

      if (nodeDir != null && Directory.Exists(nodeDir))
      {
        string expectedFull = Path.GetFullPath(expectedPath);
        foreach (var wav in Directory.GetFiles(nodeDir, "*.wav"))
        {
          if (!exactPattern.IsMatch(Path.GetFileName(wav))) continue;
          if (Path.GetFullPath(wav) != expectedFull)
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
    /// <param name="language">기본 목소리에 사용할 언어 코드</param>
    /// <param name="totalStep">기본 목소리 Diffusion 스텝 수</param>
    /// <param name="speed">기본 목소리 발화 속도</param>
    /// <param name="voiceStyleName">기본 목소리 스타일 파일명</param>
    /// <param name="voiceProfiles">
    /// 추가 목소리 프로파일 목록. null이면 기본 목소리만 사용한다.
    /// 각 프로파일이 지정한 voice identifier로 bake된 job은 해당 프로파일의 스타일로 합성된다.
    /// </param>
    public static void BakeAllNeededSynchronously(
      string language = "ko", int totalStep = 5, float speed = 1.05f, string voiceStyleName = "F1",
      TextToSpeechService.TTSVoiceProfile[] voiceProfiles = null)
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

      string defaultStylePath = TTSCore.GetVoiceStylePath(sa, voiceStyleName);

      // voiceIdentifier → (stylePath, language, totalStep, speed) 매핑 구성
      var voiceStyleMap = new Dictionary<string, (string stylePath, string lang, int step, float spd)>(
        StringComparer.Ordinal);
      if (voiceProfiles != null)
      {
        foreach (var p in voiceProfiles)
        {
          if (p == null || string.IsNullOrEmpty(p.VoiceIdentifier)) continue;
          if (voiceStyleMap.ContainsKey(p.VoiceIdentifier)) continue;
          string sp = TTSCore.GetVoiceStylePath(sa, string.IsNullOrEmpty(p.VoiceStyleName) ? voiceStyleName : p.VoiceStyleName);
          string lg = string.IsNullOrEmpty(p.Language) ? language : p.Language;
          int    st = p.TotalStep > 0 ? p.TotalStep : totalStep;
          float  sd = p.Speed > 0f ? p.Speed : speed;
          voiceStyleMap[p.VoiceIdentifier] = (sp, lg, st, sd);
        }
      }

      // TTSCore 인스턴스 캐시 (style path → core)
      var coreCache = new Dictionary<string, TTSCore>(StringComparer.Ordinal);

      try
      {
        EditorUtility.DisplayProgressBar("Scenario Inline TTS Bake", "준비 중...", 0f);

        // 기본 코어 사전 생성
        coreCache[defaultStylePath] = new TTSCore(onnxDir, defaultStylePath);

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

          // job의 voice identifier에 맞는 core 선택
          string jobStylePath = defaultStylePath;
          string jobLang      = language;
          int    jobStep      = totalStep;
          float  jobSpeed     = speed;

          if (!string.IsNullOrEmpty(job.VoiceIdentifier)
              && voiceStyleMap.TryGetValue(job.VoiceIdentifier, out var voiceParams))
          {
            jobStylePath = voiceParams.stylePath;
            jobLang      = voiceParams.lang;
            jobStep      = voiceParams.step;
            jobSpeed     = voiceParams.spd;
          }

          if (!coreCache.TryGetValue(jobStylePath, out var core))
          {
            core = new TTSCore(onnxDir, jobStylePath);
            coreCache[jobStylePath] = core;
          }

          float[] wav = core.Synthesize(job.Text, jobLang, jobStep, jobSpeed);
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
        foreach (var c in coreCache.Values)
          try { c?.Dispose(); } catch { /* ignore */ }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
      }
    }
  }
}
#endif
