#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio.Native
{
  /// <summary>
  /// WASAPI(MMDevice API)로 Windows의 오디오 엔드포인트를 훑습니다.
  ///
  /// COM 인터페이스를 <c>[ComImport]</c>로 선언하는 대신 vtable 슬롯을 직접 읽어
  /// 함수 포인터로 부릅니다. IL2CPP는 <c>[ComImport]</c> 기반 인터페이스 마샬링을 온전히
  /// 지원하지 않지만 함수 포인터 마샬링은 지원하므로, 스크립팅 백엔드를 바꿔도 그대로 돕니다.
  ///
  /// 식별자는 엔드포인트 ID 문자열(<c>{0.0.0.00000000}.{guid}</c> 꼴)입니다.
  /// 장치 이름과 달리 재부팅이나 이름 변경에도 유지되므로 설정 저장에 알맞습니다.
  /// </summary>
  public sealed class WindowsAudioDeviceEnumerator : IAudioDeviceEnumerator
  {
    // ──────────────────────────────────────────────────────────────────────────
    // COM 상수
    // ──────────────────────────────────────────────────────────────────────────
    private static readonly Guid ClsidMMDeviceEnumerator =
      new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IidIMMDeviceEnumerator =
      new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");

    // PKEY_Device_FriendlyName: 사운드 설정에 뜨는 이름("스피커(Realtek(R) Audio)")
    private static readonly Guid FriendlyNameFormatId =
      new Guid("a45c254e-df1c-4efd-8020-67d146a850e0");
    private const uint FriendlyNamePropertyId = 14;

    private const int DataFlowRender = 0;   // eRender
    private const int DataFlowCapture = 1;  // eCapture
    private const int RoleConsole = 0;      // eConsole
    private const uint DeviceStateActive = 0x00000001;
    private const uint StgmRead = 0;
    private const uint ClsCtxInprocServer = 1;
    private const ushort VtLpwstr = 31;

    private const int SOk = 0;
    private const int CoENotInitialized = unchecked((int)0x800401F0);
    private const int RpcEChangedMode = unchecked((int)0x80010106);
    private const uint CoinitApartmentThreaded = 0x2;

    // vtable 슬롯(IUnknown 3칸 뒤부터 각 인터페이스 고유 메서드가 이어진다)
    private const int SlotRelease = 2;
    private const int SlotEnumAudioEndpoints = 3;
    private const int SlotGetDefaultAudioEndpoint = 4;
    private const int SlotCollectionGetCount = 3;
    private const int SlotCollectionItem = 4;
    private const int SlotDeviceOpenPropertyStore = 4;
    private const int SlotDeviceGetId = 5;
    private const int SlotPropertyStoreGetValue = 5;

    // ──────────────────────────────────────────────────────────────────────────
    // IAudioDeviceEnumerator
    // ──────────────────────────────────────────────────────────────────────────
    public bool IsSupported(AudioDeviceKind kind) => true;

    public IReadOnlyList<AudioDeviceDescriptor> Enumerate(AudioDeviceKind kind)
    {
      IntPtr enumerator = IntPtr.Zero;
      IntPtr collection = IntPtr.Zero;

      try
      {
        if (!TryCreateEnumerator(out enumerator))
          return Array.Empty<AudioDeviceDescriptor>();

        int dataFlow = kind == AudioDeviceKind.Output ? DataFlowRender : DataFlowCapture;
        string defaultId = GetDefaultEndpointId(enumerator, dataFlow);

        int hr = Invoke<EnumAudioEndpointsDelegate>(enumerator, SlotEnumAudioEndpoints)(
          enumerator, dataFlow, DeviceStateActive, out collection);
        if (hr != SOk || collection == IntPtr.Zero)
        {
          Debug.LogWarning($"[AudioDevice] WASAPI 엔드포인트 열거 실패 (hr=0x{hr:X8}).");
          return Array.Empty<AudioDeviceDescriptor>();
        }

        hr = Invoke<CollectionGetCountDelegate>(collection, SlotCollectionGetCount)(collection, out uint count);
        if (hr != SOk)
          return Array.Empty<AudioDeviceDescriptor>();

        var devices = new List<AudioDeviceDescriptor>((int)count);
        for (uint i = 0; i < count; i++)
        {
          IntPtr device = IntPtr.Zero;
          try
          {
            if (Invoke<CollectionItemDelegate>(collection, SlotCollectionItem)(collection, i, out device) != SOk)
              continue;

            string id = ReadDeviceId(device);
            if (string.IsNullOrEmpty(id))
              continue;

            string name = ReadFriendlyName(device);
            devices.Add(new AudioDeviceDescriptor(
              id,
              string.IsNullOrWhiteSpace(name) ? id : name,
              string.Equals(id, defaultId, StringComparison.Ordinal)));
          }
          finally
          {
            Release(ref device);
          }
        }

        return devices;
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] Windows 오디오 장치를 훑는 중 문제가 생겼습니다: {exception.Message}");
        return Array.Empty<AudioDeviceDescriptor>();
      }
      finally
      {
        Release(ref collection);
        Release(ref enumerator);
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 구현
    // ──────────────────────────────────────────────────────────────────────────
    private static bool TryCreateEnumerator(out IntPtr enumerator)
    {
      var clsid = ClsidMMDeviceEnumerator;
      var iid = IidIMMDeviceEnumerator;

      int hr = CoCreateInstance(ref clsid, IntPtr.Zero, ClsCtxInprocServer, ref iid, out enumerator);
      if (hr == CoENotInitialized)
      {
        // Unity 메인 스레드는 보통 COM이 이미 초기화되어 있지만, 아닌 경우를 대비한다.
        int initHr = CoInitializeEx(IntPtr.Zero, CoinitApartmentThreaded);
        if (initHr < 0 && initHr != RpcEChangedMode)
        {
          Debug.LogWarning($"[AudioDevice] COM 초기화에 실패했습니다 (hr=0x{initHr:X8}).");
          return false;
        }

        hr = CoCreateInstance(ref clsid, IntPtr.Zero, ClsCtxInprocServer, ref iid, out enumerator);
      }

      if (hr != SOk || enumerator == IntPtr.Zero)
      {
        Debug.LogWarning($"[AudioDevice] MMDeviceEnumerator를 만들지 못했습니다 (hr=0x{hr:X8}).");
        enumerator = IntPtr.Zero;
        return false;
      }

      return true;
    }

    private static string GetDefaultEndpointId(IntPtr enumerator, int dataFlow)
    {
      IntPtr device = IntPtr.Zero;
      try
      {
        int hr = Invoke<GetDefaultAudioEndpointDelegate>(enumerator, SlotGetDefaultAudioEndpoint)(
          enumerator, dataFlow, RoleConsole, out device);

        // 장치가 하나도 없으면 E_NOTFOUND가 온다. 오류가 아니라 정상적인 "없음"이다.
        return hr == SOk && device != IntPtr.Zero ? ReadDeviceId(device) : null;
      }
      finally
      {
        Release(ref device);
      }
    }

    private static string ReadDeviceId(IntPtr device)
    {
      IntPtr raw = IntPtr.Zero;
      try
      {
        if (Invoke<DeviceGetIdDelegate>(device, SlotDeviceGetId)(device, out raw) != SOk || raw == IntPtr.Zero)
          return null;
        return Marshal.PtrToStringUni(raw);
      }
      finally
      {
        if (raw != IntPtr.Zero)
          Marshal.FreeCoTaskMem(raw);
      }
    }

    private static string ReadFriendlyName(IntPtr device)
    {
      IntPtr store = IntPtr.Zero;
      var value = default(PropVariant);
      try
      {
        if (Invoke<DeviceOpenPropertyStoreDelegate>(device, SlotDeviceOpenPropertyStore)(
              device, StgmRead, out store) != SOk || store == IntPtr.Zero)
          return null;

        var key = new PropertyKey { FormatId = FriendlyNameFormatId, PropertyId = FriendlyNamePropertyId };
        if (Invoke<PropertyStoreGetValueDelegate>(store, SlotPropertyStoreGetValue)(store, ref key, out value) != SOk)
          return null;

        return value.ValueType == VtLpwstr && value.PointerValue != IntPtr.Zero
          ? Marshal.PtrToStringUni(value.PointerValue)
          : null;
      }
      finally
      {
        PropVariantClear(ref value);
        Release(ref store);
      }
    }

    private static TDelegate Invoke<TDelegate>(IntPtr comObject, int slot) where TDelegate : Delegate
    {
      IntPtr vtable = Marshal.ReadIntPtr(comObject);
      IntPtr method = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
      return Marshal.GetDelegateForFunctionPointer<TDelegate>(method);
    }

    private static void Release(ref IntPtr comObject)
    {
      if (comObject == IntPtr.Zero)
        return;

      try
      {
        Invoke<ReleaseDelegate>(comObject, SlotRelease)(comObject);
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] COM 객체 해제에 실패했습니다: {exception.Message}");
      }
      finally
      {
        comObject = IntPtr.Zero;
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 상호 운용 선언
    // ──────────────────────────────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
      public Guid FormatId;
      public uint PropertyId;
    }

    /// <summary>PROPVARIANT 중 문자열(VT_LPWSTR)만 읽으면 되므로 필요한 칸만 겹쳐 둔다.</summary>
    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
      [FieldOffset(0)] public ushort ValueType;
      [FieldOffset(2)] public ushort Reserved1;
      [FieldOffset(4)] public ushort Reserved2;
      [FieldOffset(6)] public ushort Reserved3;
      [FieldOffset(8)] public IntPtr PointerValue;
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReleaseDelegate(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EnumAudioEndpointsDelegate(IntPtr self, int dataFlow, uint stateMask, out IntPtr collection);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDefaultAudioEndpointDelegate(IntPtr self, int dataFlow, int role, out IntPtr device);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CollectionGetCountDelegate(IntPtr self, out uint count);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CollectionItemDelegate(IntPtr self, uint index, out IntPtr device);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int DeviceOpenPropertyStoreDelegate(IntPtr self, uint stgmAccess, out IntPtr store);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int DeviceGetIdDelegate(IntPtr self, out IntPtr id);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int PropertyStoreGetValueDelegate(IntPtr self, ref PropertyKey key, out PropVariant value);

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(ref Guid classId, IntPtr outer, uint context,
      ref Guid interfaceId, out IntPtr instance);

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr reserved, uint flags);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant value);
  }
}
#endif
