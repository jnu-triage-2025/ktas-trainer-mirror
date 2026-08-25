#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio.Native
{
  /// <summary>
  /// CoreAudio(AudioObject) 속성 조회로 macOS의 오디오 장치를 훑습니다.
  ///
  /// 식별자는 장치 UID(<c>kAudioDevicePropertyDeviceUID</c>)입니다. AudioDeviceID는 재부팅이나
  /// 재연결마다 달라지지만 UID는 유지되므로 설정 저장에 알맞습니다.
  ///
  /// macOS는 입출력을 한 장치가 겸하는 경우가 있어(예: USB 헤드셋),
  /// 요청한 방향에 채널이 실제로 있는 장치만 골라 돌려줍니다.
  /// </summary>
  public sealed class MacAudioDeviceEnumerator : IAudioDeviceEnumerator
  {
    private const string CoreAudio = "/System/Library/Frameworks/CoreAudio.framework/CoreAudio";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private const uint SystemObject = 1;
    private const uint PropertyDevices = 0x64657623;              // 'dev#'
    private const uint PropertyDefaultOutputDevice = 0x644F7574;  // 'dOut'
    private const uint PropertyDefaultInputDevice = 0x64496E20;   // 'dIn '
    private const uint PropertyStreamConfiguration = 0x736C6179;  // 'slay'
    private const uint PropertyObjectName = 0x6C6E616D;           // 'lnam'
    private const uint PropertyDeviceUid = 0x75696420;            // 'uid '
    private const uint ScopeGlobal = 0x676C6F62;                  // 'glob'
    private const uint ScopeInput = 0x696E7074;                   // 'inpt'
    private const uint ScopeOutput = 0x6F757470;                  // 'outp'
    private const uint ElementMain = 0;
    private const uint EncodingUtf8 = 0x08000100;
    private const int NoErr = 0;

    public bool IsSupported(AudioDeviceKind kind) => true;

    public IReadOnlyList<AudioDeviceDescriptor> Enumerate(AudioDeviceKind kind)
    {
      try
      {
        var deviceIds = ReadDeviceIds();
        if (deviceIds.Length == 0)
          return Array.Empty<AudioDeviceDescriptor>();

        uint scope = kind == AudioDeviceKind.Output ? ScopeOutput : ScopeInput;
        uint defaultSelector = kind == AudioDeviceKind.Output
          ? PropertyDefaultOutputDevice
          : PropertyDefaultInputDevice;
        uint defaultDeviceId = ReadDefaultDeviceId(defaultSelector);

        var devices = new List<AudioDeviceDescriptor>(deviceIds.Length);
        foreach (uint deviceId in deviceIds)
        {
          if (CountChannels(deviceId, scope) == 0)
            continue;

          string uid = ReadStringProperty(deviceId, PropertyDeviceUid, ScopeGlobal);
          if (string.IsNullOrEmpty(uid))
            continue;

          string name = ReadStringProperty(deviceId, PropertyObjectName, ScopeGlobal);
          devices.Add(new AudioDeviceDescriptor(
            uid,
            string.IsNullOrWhiteSpace(name) ? uid : name,
            deviceId == defaultDeviceId));
        }

        return devices;
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] macOS 오디오 장치를 훑는 중 문제가 생겼습니다: {exception.Message}");
        return Array.Empty<AudioDeviceDescriptor>();
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 속성 조회
    // ──────────────────────────────────────────────────────────────────────────
    private static uint[] ReadDeviceIds()
    {
      var address = MakeAddress(PropertyDevices, ScopeGlobal);
      if (AudioObjectGetPropertyDataSize(SystemObject, ref address, 0, IntPtr.Zero, out uint size) != NoErr || size == 0)
        return Array.Empty<uint>();

      int count = (int)(size / sizeof(uint));
      IntPtr buffer = Marshal.AllocHGlobal((int)size);
      try
      {
        if (AudioObjectGetPropertyData(SystemObject, ref address, 0, IntPtr.Zero, ref size, buffer) != NoErr)
          return Array.Empty<uint>();

        var ids = new uint[count];
        for (int i = 0; i < count; i++)
          ids[i] = unchecked((uint)Marshal.ReadInt32(buffer, i * sizeof(uint)));
        return ids;
      }
      finally
      {
        Marshal.FreeHGlobal(buffer);
      }
    }

    private static uint ReadDefaultDeviceId(uint selector)
    {
      var address = MakeAddress(selector, ScopeGlobal);
      uint size = sizeof(uint);
      IntPtr buffer = Marshal.AllocHGlobal((int)size);
      try
      {
        return AudioObjectGetPropertyData(SystemObject, ref address, 0, IntPtr.Zero, ref size, buffer) == NoErr
          ? unchecked((uint)Marshal.ReadInt32(buffer))
          : 0u;
      }
      finally
      {
        Marshal.FreeHGlobal(buffer);
      }
    }

    /// <summary>
    /// 해당 방향(scope)의 총 채널 수를 셉니다. 0이면 그 방향으로는 쓸 수 없는 장치입니다.
    ///
    /// AudioBufferList는 <c>{ UInt32 mNumberBuffers; AudioBuffer mBuffers[1]; }</c>이고
    /// AudioBuffer가 포인터를 품고 있어 64비트에서 8바이트 정렬을 받습니다.
    /// 그래서 첫 버퍼는 8바이트째부터, 버퍼 하나는 16바이트입니다.
    /// </summary>
    private static int CountChannels(uint deviceId, uint scope)
    {
      var address = MakeAddress(PropertyStreamConfiguration, scope);
      if (AudioObjectGetPropertyDataSize(deviceId, ref address, 0, IntPtr.Zero, out uint size) != NoErr || size < sizeof(uint))
        return 0;

      IntPtr buffer = Marshal.AllocHGlobal((int)size);
      try
      {
        if (AudioObjectGetPropertyData(deviceId, ref address, 0, IntPtr.Zero, ref size, buffer) != NoErr)
          return 0;

        int bufferCount = Marshal.ReadInt32(buffer);
        int channels = 0;
        for (int i = 0; i < bufferCount; i++)
        {
          int offset = 8 + (i * 16);
          if (offset + sizeof(uint) > size)
            break;
          channels += Marshal.ReadInt32(buffer, offset);
        }
        return channels;
      }
      finally
      {
        Marshal.FreeHGlobal(buffer);
      }
    }

    private static string ReadStringProperty(uint deviceId, uint selector, uint scope)
    {
      var address = MakeAddress(selector, scope);
      uint size = (uint)IntPtr.Size;
      IntPtr buffer = Marshal.AllocHGlobal(IntPtr.Size);
      IntPtr cfString = IntPtr.Zero;
      try
      {
        if (AudioObjectGetPropertyData(deviceId, ref address, 0, IntPtr.Zero, ref size, buffer) != NoErr)
          return null;

        cfString = Marshal.ReadIntPtr(buffer);
        return CopyCFString(cfString);
      }
      finally
      {
        // CoreAudio는 CFStringRef를 복사본으로 넘기므로 부른 쪽이 놓아주어야 한다.
        if (cfString != IntPtr.Zero)
          CFRelease(cfString);
        Marshal.FreeHGlobal(buffer);
      }
    }

    private static string CopyCFString(IntPtr cfString)
    {
      if (cfString == IntPtr.Zero)
        return null;

      long length = CFStringGetLength(cfString);
      if (length <= 0)
        return string.Empty;

      // UTF-8은 문자당 최대 4바이트, 끝의 널 문자까지 한 칸 더.
      long capacity = (length * 4) + 1;
      IntPtr buffer = Marshal.AllocHGlobal((int)capacity);
      try
      {
        return CFStringGetCString(cfString, buffer, capacity, EncodingUtf8)
          ? Marshal.PtrToStringUTF8(buffer)
          : null;
      }
      finally
      {
        Marshal.FreeHGlobal(buffer);
      }
    }

    private static AudioObjectPropertyAddress MakeAddress(uint selector, uint scope)
      => new AudioObjectPropertyAddress { Selector = selector, Scope = scope, Element = ElementMain };

    // ──────────────────────────────────────────────────────────────────────────
    // 상호 운용 선언
    // ──────────────────────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct AudioObjectPropertyAddress
    {
      public uint Selector;
      public uint Scope;
      public uint Element;
    }

    [DllImport(CoreAudio)]
    private static extern int AudioObjectGetPropertyDataSize(uint objectId,
      ref AudioObjectPropertyAddress address, uint qualifierDataSize, IntPtr qualifierData, out uint dataSize);

    [DllImport(CoreAudio)]
    private static extern int AudioObjectGetPropertyData(uint objectId,
      ref AudioObjectPropertyAddress address, uint qualifierDataSize, IntPtr qualifierData,
      ref uint dataSize, IntPtr data);

    [DllImport(CoreFoundation)]
    private static extern long CFStringGetLength(IntPtr theString);

    [DllImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CFStringGetCString(IntPtr theString, IntPtr buffer, long bufferSize, uint encoding);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr reference);
  }
}
#endif
