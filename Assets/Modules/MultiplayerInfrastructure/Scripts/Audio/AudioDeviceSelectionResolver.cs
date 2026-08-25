using System;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 저장된 장치 선택값을 현재 장치 목록과 대조하는 순수 함수 모음입니다.
  ///
  /// Unity에 기대지 않으므로 에디터 테스트에서 그대로 검증할 수 있습니다.
  /// </summary>
  public static class AudioDeviceSelectionResolver
  {
    /// <summary>"시스템 설정을 따름"을 뜻하는 예약 식별자입니다.</summary>
    public const string SystemDefaultId = "";

    /// <summary>시스템 설정 항목에 붙는 이름입니다.</summary>
    public const string SystemDefaultLabelBase = "시스템 설정";

    /// <summary>장치 이름을 아직 알아내지 못했을 때 괄호 안에 넣는 문구입니다.</summary>
    public const string UnknownDeviceLabel = "확인할 수 없음";

    public static bool IsSystemDefault(string deviceId) => string.IsNullOrEmpty(deviceId);

    /// <summary>
    /// 저장된 식별자가 현재 목록에 남아 있으면 그대로, 사라졌으면 시스템 설정으로 되돌립니다.
    /// </summary>
    /// <param name="storedId">저장되어 있던 장치 식별자</param>
    /// <param name="devices">지금 인식된 장치 목록</param>
    /// <returns>실제로 적용할 식별자. 시스템 설정이면 <see cref="SystemDefaultId"/>.</returns>
    public static string Reconcile(string storedId, IReadOnlyList<AudioDeviceDescriptor> devices)
      => Find(storedId, devices) != null ? storedId : SystemDefaultId;

    /// <summary>저장된 식별자에 해당하는 장치를 찾습니다. 없으면 null입니다.</summary>
    public static AudioDeviceDescriptor Find(string deviceId, IReadOnlyList<AudioDeviceDescriptor> devices)
    {
      if (IsSystemDefault(deviceId) || devices == null)
        return null;

      for (int i = 0; i < devices.Count; i++)
      {
        var device = devices[i];
        if (device != null && string.Equals(device.Id, deviceId, StringComparison.Ordinal))
          return device;
      }

      return null;
    }

    /// <summary>목록에서 운영체제 기본 장치를 찾습니다. 없으면 null입니다.</summary>
    public static AudioDeviceDescriptor FindSystemDefault(IReadOnlyList<AudioDeviceDescriptor> devices)
    {
      if (devices == null)
        return null;

      for (int i = 0; i < devices.Count; i++)
      {
        if (devices[i] != null && devices[i].IsSystemDefault)
          return devices[i];
      }

      return null;
    }

    /// <summary>
    /// 시스템 설정 항목의 표시 문구를 만듭니다.
    ///
    /// 요구사항대로 지금 운영체제가 쓰고 있는 장치 이름을 괄호에 함께 적습니다.
    /// 기본 장치를 알아내지 못한 플랫폼에서는 "시스템 설정(확인할 수 없음)"이 됩니다.
    /// </summary>
    public static string BuildSystemDefaultLabel(IReadOnlyList<AudioDeviceDescriptor> devices)
      => BuildSystemDefaultLabel(FindSystemDefault(devices)?.DisplayName);

    /// <inheritdoc cref="BuildSystemDefaultLabel(IReadOnlyList{AudioDeviceDescriptor})"/>
    public static string BuildSystemDefaultLabel(string systemDefaultName)
    {
      var name = string.IsNullOrWhiteSpace(systemDefaultName) ? UnknownDeviceLabel : systemDefaultName.Trim();
      return $"{SystemDefaultLabelBase}({name})";
    }

    /// <summary>
    /// 드롭다운에 넣을 문구 목록을 만듭니다. 0번은 항상 시스템 설정 항목입니다.
    /// 목록의 순서는 <paramref name="devices"/>와 1칸씩 어긋나므로 인덱스로 짝지어 쓰세요.
    ///
    /// 항상 <c>devices.Count + 1</c>개를 돌려줍니다. 중간에서 항목을 건너뛰면 그 뒤 장치가
    /// 한 칸씩 밀려 <see cref="ChoiceIdAt"/>가 엉뚱한 장치를 짚게 되므로, 비어 있는 자리도
    /// 자리표시자 문구로 채웁니다.
    /// </summary>
    public static List<string> BuildChoiceLabels(IReadOnlyList<AudioDeviceDescriptor> devices)
    {
      var labels = new List<string> { BuildSystemDefaultLabel(devices) };
      if (devices == null)
        return labels;

      for (int i = 0; i < devices.Count; i++)
      {
        var device = devices[i];
        if (device == null)
          labels.Add(UnknownDeviceLabel);
        else
          labels.Add(device.IsSystemDefault ? $"{device.DisplayName} (기본)" : device.DisplayName);
      }

      return labels;
    }

    /// <summary>선택된 식별자에 해당하는 드롭다운 인덱스를 구합니다. 못 찾으면 0(시스템 설정)입니다.</summary>
    public static int IndexOfChoice(string deviceId, IReadOnlyList<AudioDeviceDescriptor> devices)
    {
      if (IsSystemDefault(deviceId) || devices == null)
        return 0;

      for (int i = 0; i < devices.Count; i++)
      {
        var device = devices[i];
        if (device != null && string.Equals(device.Id, deviceId, StringComparison.Ordinal))
          return i + 1;
      }

      return 0;
    }

    /// <summary>드롭다운 인덱스를 장치 식별자로 되돌립니다.</summary>
    public static string ChoiceIdAt(int index, IReadOnlyList<AudioDeviceDescriptor> devices)
    {
      if (index <= 0 || devices == null || index - 1 >= devices.Count)
        return SystemDefaultId;

      return devices[index - 1]?.Id ?? SystemDefaultId;
    }
  }
}
