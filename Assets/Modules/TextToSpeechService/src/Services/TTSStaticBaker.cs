using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TextToSpeechService
{
  /// <summary>
  /// transcripts.json의 Static 세그먼트를 사전 합성(bake)하여
  /// StreamingAssets/TTS/Baked/{identifier}/{segmentIndex}.wav 로 저장하는 순수 C# 코어.
  ///
  /// UnityEngine / UnityEditor에 의존하지 않는다. 진행 로그/UI는 호출 측 책임이다.
  /// </summary>
  public static class TTSStaticBaker
  {
    /// <summary>bake 대상 Static 세그먼트 작업 항목.</summary>
    public readonly struct StaticSegmentJob
    {
      public StaticSegmentJob(string identifier, int index, string text)
      {
        Identifier = identifier;
        Index = index;
        Text = text;
      }

      public string Identifier { get; }
      public int Index { get; }
      public string Text { get; }
    }

    /// <summary>진행 콜백 인자.</summary>
    public readonly struct Progress
    {
      public Progress(int doneCount, int totalCount, string identifier, int index, bool skipped)
      {
        DoneCount = doneCount;
        TotalCount = totalCount;
        Identifier = identifier;
        Index = index;
        Skipped = skipped;
      }

      public int DoneCount { get; }
      public int TotalCount { get; }
      public string Identifier { get; }
      public int Index { get; }
      public bool Skipped { get; }
      public float Ratio => TotalCount == 0 ? 1f : (float)DoneCount / TotalCount;
    }

    /// <summary>transcripts.json을 파싱하여 Static 세그먼트 작업 목록을 수집한다.</summary>
    public static List<StaticSegmentJob> CollectStaticJobs(string transcriptJsonPath)
    {
      var jobs = new List<StaticSegmentJob>();

      string rawJson = File.ReadAllText(transcriptJsonPath);
      var transcripts = JsonSerializer.Deserialize<SpeechTranscript[]>(
        rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? Array.Empty<SpeechTranscript>();

      foreach (var transcript in transcripts)
      {
        var segments = TranscriptParser.Parse(transcript);
        for (int i = 0; i < segments.Count; i++)
        {
          if (segments[i].Type == SegmentType.Static)
            jobs.Add(new StaticSegmentJob(transcript.Identifier, i, segments[i].Text));
        }
      }

      return jobs;
    }

    /// <summary>
    /// transcripts.json의 Static 세그먼트를 합성하여 WAV로 저장한다(동기).
    /// </summary>
    /// <param name="transcriptJsonPath">transcripts.json 절대 경로</param>
    /// <param name="streamingAssetsPath">Application.streamingAssetsPath</param>
    /// <param name="onnxDir">ONNX 모델 디렉터리 절대 경로</param>
    /// <param name="voiceStylePath">음성 스타일 JSON 절대 경로</param>
    /// <param name="language">언어 코드</param>
    /// <param name="totalStep">Diffusion 스텝 수</param>
    /// <param name="speed">발화 속도 배율</param>
    /// <param name="overwrite">기존 파일 덮어쓰기 여부</param>
    /// <param name="onProgress">진행 콜백</param>
    public static void BakeAll(
      string transcriptJsonPath,
      string streamingAssetsPath,
      string onnxDir,
      string voiceStylePath,
      string language,
      int totalStep,
      float speed,
      bool overwrite,
      Action<Progress> onProgress = null)
    {
      var jobs = CollectStaticJobs(transcriptJsonPath);

      using var core = new TTSCore(onnxDir, voiceStylePath);

      for (int t = 0; t < jobs.Count; t++)
      {
        var job = jobs[t];
        string outPath = TTSCore.GetBakedClipPath(streamingAssetsPath, job.Identifier, job.Index);

        if (!overwrite && File.Exists(outPath))
        {
          onProgress?.Invoke(new Progress(t + 1, jobs.Count, job.Identifier, job.Index, skipped: true));
          continue;
        }

        float[] wav = core.Synthesize(job.Text, language, totalStep, speed);

        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        Supertonic.Helper.WriteWavFile(outPath, wav, core.SampleRate);

        onProgress?.Invoke(new Progress(t + 1, jobs.Count, job.Identifier, job.Index, skipped: false));
      }
    }
  }
}
