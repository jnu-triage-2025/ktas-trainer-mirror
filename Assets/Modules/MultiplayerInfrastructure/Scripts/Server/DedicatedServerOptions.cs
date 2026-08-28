using System;
using System.Collections.Generic;
using System.Globalization;

using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Session;

namespace MultiplayerInfrastructure.Server
{
  /// <summary>
  /// 데디케이티드 서버(헤드리스) 실행 옵션입니다.
  /// 커맨드라인 인자를 해석하여 서버 세션을 시작하는 데 필요한 값을 담습니다.
  /// 이 타입은 Unity API에 의존하지 않으므로 EditMode 테스트에서 그대로 검증할 수 있습니다.
  /// </summary>
  public sealed class DedicatedServerOptions
  {
    /// <summary>플레이어 빌드에서도 데디케이티드 모드를 강제하는 스위치입니다.</summary>
    public const string DedicatedServerSwitch = "dedicatedserver";

    /// <summary><see cref="DedicatedServerSwitch"/>의 짧은 별칭입니다.</summary>
    public const string DedicatedServerSwitchAlias = "server";

    public const string DefaultBindAddress = "0.0.0.0";
    public const string DefaultSessionName = "KTAS Dedicated Server";
    public const int DefaultTargetFrameRate = 60;

    /// <summary>커맨드라인에 데디케이티드 서버 스위치가 명시되었는지 여부입니다.</summary>
    public bool IsExplicitlyRequested { get; private set; }

    /// <summary>서버 소켓이 바인딩할 주소입니다. 기본값은 모든 인터페이스입니다.</summary>
    public string BindAddress { get; private set; } = DefaultBindAddress;

    /// <summary>서버가 수신할 포트입니다.</summary>
    public ushort Port { get; private set; } = DefaultsSessionInformationModel.port;

    /// <summary>LAN 목록과 로그에 표시할 세션 이름입니다.</summary>
    public string SessionName { get; private set; } = DefaultSessionName;

    /// <summary>LAN 검색 브로드캐스트를 사용할지 여부입니다.</summary>
    public bool UseLanDiscovery { get; private set; } = true;

    /// <summary>세션에서 사용할 데이터팩 식별자 목록입니다.</summary>
    public IReadOnlyList<string> DatapackIds { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// 런타임 데이터팩 폴더의 경로입니다.
    /// 값이 없으면 실행 파일과 같은 위치의 <c>DataPacks</c> 폴더를 사용합니다.
    /// </summary>
    public string DatapacksPath { get; private set; }

    /// <summary>서버 루프의 목표 프레임 레이트입니다. 0 이하이면 제한하지 않습니다.</summary>
    public int TargetFrameRate { get; private set; } = DefaultTargetFrameRate;

    /// <summary>서버가 진입할 시작 씬 이름입니다.</summary>
    public string StartScene { get; private set; } = DefaultsSceneControl.IngameSceneName;

    /// <summary>해석하지 못한 값에 대한 경고 메시지입니다.</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    private readonly List<string> _warnings = new List<string>();

    /// <summary>
    /// 커맨드라인 인자를 해석합니다.
    /// <paramref name="defaults"/>가 주어지면 인자로 지정되지 않은 값의 기본값으로 사용합니다.
    /// </summary>
    public static DedicatedServerOptions Parse(IReadOnlyList<string> args, SessionConfiguration defaults = null)
    {
      var options = new DedicatedServerOptions();
      options.ApplyDefaults(defaults);

      if (args == null)
        return options;

      for (int i = 0; i < args.Count; i++)
      {
        if (!TrySplitArgument(args[i], out var name, out var inlineValue))
          continue;

        switch (name)
        {
          case DedicatedServerSwitch:
          case DedicatedServerSwitchAlias:
            options.IsExplicitlyRequested = true;
            break;

          case "port":
            options.Port = options.ParsePort(ReadValue(args, ref i, inlineValue), options.Port);
            break;

          case "bind":
          case "bindaddress":
            options.BindAddress = NormalizeText(ReadValue(args, ref i, inlineValue), options.BindAddress);
            break;

          case "sessionname":
            options.SessionName = NormalizeText(ReadValue(args, ref i, inlineValue), options.SessionName);
            break;

          case "datapacks":
            options.DatapackIds = ParseDatapackIds(ReadValue(args, ref i, inlineValue), options.DatapackIds);
            break;

          case "nodatapacks":
            options.DatapackIds = Array.Empty<string>();
            break;

          case "datapackspath":
          case "datapackpath":
            options.DatapacksPath = NormalizeText(ReadValue(args, ref i, inlineValue), options.DatapacksPath);
            break;

          case "lanbroadcast":
            options.UseLanDiscovery = options.ParseBoolean(
              ReadValue(args, ref i, inlineValue), "lanBroadcast", options.UseLanDiscovery);
            break;

          case "nolanbroadcast":
            options.UseLanDiscovery = false;
            break;

          case "targetframerate":
          case "fps":
            options.TargetFrameRate = options.ParseFrameRate(
              ReadValue(args, ref i, inlineValue), options.TargetFrameRate);
            break;

          case "startscene":
            options.StartScene = NormalizeText(ReadValue(args, ref i, inlineValue), options.StartScene);
            break;
        }
      }

      return options;
    }

    /// <summary>세션 설정 파일의 값을 기본값으로 적용합니다.</summary>
    private void ApplyDefaults(SessionConfiguration defaults)
    {
      if (defaults == null)
        return;

      if (defaults.port != 0)
        Port = defaults.port;

      if (!string.IsNullOrWhiteSpace(defaults.sessionName))
        SessionName = defaults.sessionName;

      if (defaults.datapacks != null && defaults.datapacks.Length > 0)
        DatapackIds = new List<string>(defaults.datapacks);
    }

    /// <summary>
    /// <c>-name</c>, <c>--name</c>, <c>-name=value</c> 형식을 소문자 이름과 인라인 값으로 분리합니다.
    /// 스위치가 아닌 인자는 <c>false</c>를 반환합니다.
    /// </summary>
    private static bool TrySplitArgument(string argument, out string name, out string inlineValue)
    {
      name = null;
      inlineValue = null;

      if (string.IsNullOrWhiteSpace(argument) || argument[0] != '-')
        return false;

      var trimmed = argument.TrimStart('-');
      if (trimmed.Length == 0)
        return false;

      var separatorIndex = trimmed.IndexOf('=');
      if (separatorIndex >= 0)
      {
        name = trimmed.Substring(0, separatorIndex).ToLowerInvariant();
        inlineValue = trimmed.Substring(separatorIndex + 1);
        return name.Length > 0;
      }

      name = trimmed.ToLowerInvariant();
      return true;
    }

    /// <summary>인라인 값이 없으면 다음 인자를 값으로 소비합니다.</summary>
    private static string ReadValue(IReadOnlyList<string> args, ref int index, string inlineValue)
    {
      if (inlineValue != null)
        return inlineValue;

      var nextIndex = index + 1;
      if (nextIndex >= args.Count)
        return null;

      var next = args[nextIndex];
      if (string.IsNullOrEmpty(next) || next[0] == '-')
        return null;

      index = nextIndex;
      return next;
    }

    private static string NormalizeText(string value, string fallback)
      => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static IReadOnlyList<string> ParseDatapackIds(string value, IReadOnlyList<string> fallback)
    {
      if (string.IsNullOrWhiteSpace(value))
        return fallback;

      var identifiers = new List<string>();
      var tokens = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0; i < tokens.Length; i++)
      {
        var token = tokens[i].Trim();
        if (token.Length > 0 && !identifiers.Contains(token))
          identifiers.Add(token);
      }

      return identifiers.Count > 0 ? identifiers : fallback;
    }

    private ushort ParsePort(string value, ushort fallback)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        _warnings.Add("port 옵션에 값이 없어 기본값을 사용합니다.");
        return fallback;
      }

      if (!ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed == 0)
      {
        _warnings.Add($"port 값 '{value}'을(를) 해석할 수 없어 기본값 {fallback}을(를) 사용합니다.");
        return fallback;
      }

      return parsed;
    }

    private int ParseFrameRate(string value, int fallback)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        _warnings.Add("targetFrameRate 옵션에 값이 없어 기본값을 사용합니다.");
        return fallback;
      }

      if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
      {
        _warnings.Add($"targetFrameRate 값 '{value}'을(를) 해석할 수 없어 기본값 {fallback}을(를) 사용합니다.");
        return fallback;
      }

      return parsed;
    }

    private bool ParseBoolean(string value, string optionName, bool fallback)
    {
      if (string.IsNullOrWhiteSpace(value))
        return true;

      if (bool.TryParse(value, out var parsed))
        return parsed;

      switch (value.Trim().ToLowerInvariant())
      {
        case "1":
        case "on":
        case "yes":
          return true;
        case "0":
        case "off":
        case "no":
          return false;
      }

      _warnings.Add($"{optionName} 값 '{value}'을(를) 해석할 수 없어 기본값 {fallback}을(를) 사용합니다.");
      return fallback;
    }

    /// <summary>로그에 남길 요약 문자열을 만듭니다.</summary>
    public override string ToString()
    {
      var datapacks = DatapackIds.Count == 0 ? "<none>" : string.Join(",", DatapackIds);
      var datapacksPath = string.IsNullOrWhiteSpace(DatapacksPath) ? "<default>" : DatapacksPath;
      return $"bind={BindAddress}:{Port}, session='{SessionName}', lanBroadcast={UseLanDiscovery}, " +
             $"targetFrameRate={TargetFrameRate}, startScene={StartScene}, " +
             $"datapacks=[{datapacks}], datapacksPath={datapacksPath}";
    }
  }
}
