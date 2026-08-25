#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio.Native
{
  /// <summary>
  /// Windows의 "앱별 소리 설정"과 같은 통로로 이 프로세스의 기본 출력 장치를 지정합니다.
  ///
  /// 설정 → 시스템 → 소리 → 고급 → 앱별 볼륨 및 장치 기본 설정 화면이 쓰는 것과 같은
  /// <c>Windows.Media.Internal.AudioPolicyConfig</c> 활성화 팩터리를 부릅니다.
  /// 이 프로세스에만 걸리므로 다른 앱 소리는 건드리지 않습니다.
  ///
  /// <b>주의: 문서화되지 않은 API입니다.</b> Windows 10 1803부터 존재하고 EarTrumpet 같은
  /// 도구들이 오래 써 왔지만, 장래의 Windows 업데이트에서 사라지거나 배치가 바뀔 수 있습니다.
  ///
  /// 막아낼 수 있는 실패와 그렇지 못한 실패를 구분해 두면:
  ///   - 팩터리를 못 얻거나 IID가 맞지 않거나 호출이 오류 HRESULT를 돌려주는 경우는
  ///     false와 사유로 바꿔 돌려줍니다. 부르는 쪽은 "시스템 설정을 그대로 따름"으로 다루면 됩니다.
  ///   - <b>IID는 그대로인데 메서드 배치만 바뀌는 경우는 막지 못합니다.</b> 엉뚱한 함수 포인터를
  ///     부르게 되고, 이는 관리 예외가 아니라 액세스 위반이라 try/catch로 잡히지 않습니다.
  ///     비공개 API에 기대는 이상 남는 위험이며, 아래 catch가 가려 주지 못합니다.
  ///
  /// 인터페이스에는 IID가 다른 변종이 둘 있습니다. 메서드 배치는 같고 IID만 다르므로,
  /// 빌드 번호로 어느 쪽을 먼저 볼지 정하고 실패하면 나머지 하나로 넘어갑니다.
  /// (빌드 번호를 못 믿는 상황까지 감안한 순서 힌트일 뿐입니다.)
  ///
  /// 지정한 값은 레지스트리에 남아 다음 실행에도 유지됩니다. Windows의 앱별 설정이 원래
  /// 그렇게 동작하며, 시스템 설정을 고르면 그 기록도 함께 지웁니다.
  /// </summary>
  public sealed class WindowsAudioOutputRouter : IAudioOutputRouter
  {
    private const string ActivatableClassId = "Windows.Media.Internal.AudioPolicyConfig";

    /// <summary>Windows 10 21H2(빌드 21390) 이상에서 쓰이는 IID입니다.</summary>
    private static readonly Guid IidModern = new Guid("ab3d4648-e242-459f-b02f-541c70306324");

    /// <summary>그 이전 빌드에서 쓰이는 IID입니다.</summary>
    private static readonly Guid IidDownlevel = new Guid("2a59116d-6c4f-45e0-a74f-707e3fef9258");

    private const int ModernMinimumBuild = 21390;

    /// <summary>
    /// SetPersistedDefaultAudioEndpoint의 vtable 슬롯입니다.
    /// IInspectable 파생이므로 IUnknown 3칸 + IInspectable 3칸 뒤에 쓰지 않는 메서드가 19칸 있고,
    /// 그 다음이 이 메서드입니다. (3 + 3 + 19 = 25)
    /// </summary>
    private const int SlotSetPersistedDefaultAudioEndpoint = 25;
    private const int SlotRelease = 2;

    private const int DataFlowRender = 0;   // eRender
    private const int RoleConsole = 0;      // eConsole
    private const int RoleMultimedia = 1;   // eMultimedia

    private const int SOk = 0;
    private const int CoENotInitialized = unchecked((int)0x800401F0);
    private const int RpcEChangedMode = unchecked((int)0x80010106);
    private const uint CoinitApartmentThreaded = 0x2;

    public bool IsSupported => true;

    public bool TryRoute(string deviceId, out string failureReason)
    {
      failureReason = null;

      IntPtr factory = IntPtr.Zero;
      IntPtr endpointPath = IntPtr.Zero;
      try
      {
        if (!TryCreateFactory(out factory, out failureReason))
          return false;

        // 빈 HSTRING(널 포인터)은 "지정 해제" 신호다. 시스템 설정으로 되돌아간다.
        if (!AudioDeviceSelectionResolver.IsSystemDefault(deviceId))
        {
          var path = AudioEndpointPath.ForWindowsEndpoint(deviceId, AudioDeviceKind.Output);
          int createHr = WindowsCreateString(path, (uint)path.Length, out endpointPath);
          if (createHr != SOk)
          {
            failureReason = $"장치 경로 문자열을 만들지 못했습니다 (hr=0x{createHr:X8}).";
            return false;
          }
        }

        var set = Invoke<SetPersistedDefaultAudioEndpointDelegate>(factory, SlotSetPersistedDefaultAudioEndpoint);
        uint processId = GetCurrentProcessId();

        // Windows는 역할별로 기본 장치를 따로 들고 있다. 게임 소리는 두 역할 모두에 걸어야
        // 재생 경로가 갈리지 않는다.
        foreach (int role in new[] { RoleConsole, RoleMultimedia })
        {
          int hr = set(factory, processId, DataFlowRender, role, endpointPath);
          if (hr != SOk)
          {
            failureReason = $"출력 장치 지정에 실패했습니다 (role={role}, hr=0x{hr:X8}).";
            return false;
          }
        }

        return true;
      }
      catch (Exception exception)
      {
        failureReason = $"출력 장치를 바꾸는 중 문제가 생겼습니다: {exception.Message}";
        return false;
      }
      finally
      {
        if (endpointPath != IntPtr.Zero)
          WindowsDeleteString(endpointPath);
        Release(ref factory);
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 구현
    // ──────────────────────────────────────────────────────────────────────────
    private static bool TryCreateFactory(out IntPtr factory, out string failureReason)
    {
      factory = IntPtr.Zero;
      failureReason = null;

      IntPtr classId = IntPtr.Zero;
      try
      {
        int hr = WindowsCreateString(ActivatableClassId, (uint)ActivatableClassId.Length, out classId);
        if (hr != SOk)
        {
          failureReason = $"활성화 클래스 이름을 만들지 못했습니다 (hr=0x{hr:X8}).";
          return false;
        }

        int lastHr = SOk;
        foreach (var candidate in OrderedInterfaceIds())
        {
          var iid = candidate;
          lastHr = RoGetActivationFactory(classId, ref iid, out factory);

          if (lastHr == CoENotInitialized)
          {
            // WinRT 활성화는 COM 초기화를 요구한다. Unity 메인 스레드는 보통 이미
            // 초기화되어 있지만 아닌 경우를 대비한다.
            int initHr = CoInitializeEx(IntPtr.Zero, CoinitApartmentThreaded);
            if (initHr >= 0 || initHr == RpcEChangedMode)
              lastHr = RoGetActivationFactory(classId, ref iid, out factory);
          }

          if (lastHr == SOk && factory != IntPtr.Zero)
            return true;

          factory = IntPtr.Zero;
        }

        failureReason = $"오디오 정책 팩터리를 얻지 못했습니다 (hr=0x{lastHr:X8}). " +
                        "이 Windows 버전에서는 앱별 출력 장치 지정을 쓸 수 없습니다.";
        return false;
      }
      finally
      {
        if (classId != IntPtr.Zero)
          WindowsDeleteString(classId);
      }
    }

    /// <summary>빌드 번호로 먼저 시도할 IID를 정합니다. 둘 다 시도하므로 순서 힌트일 뿐입니다.</summary>
    private static Guid[] OrderedInterfaceIds()
    {
      bool modernFirst = true;
      try
      {
        modernFirst = Environment.OSVersion.Version.Build >= ModernMinimumBuild;
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] Windows 빌드 번호를 읽지 못해 최신 IID부터 시도합니다: {exception.Message}");
      }

      return modernFirst
        ? new[] { IidModern, IidDownlevel }
        : new[] { IidDownlevel, IidModern };
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
        Debug.LogWarning($"[AudioDevice] 오디오 정책 팩터리 해제에 실패했습니다: {exception.Message}");
      }
      finally
      {
        comObject = IntPtr.Zero;
      }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 상호 운용 선언
    // ──────────────────────────────────────────────────────────────────────────
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReleaseDelegate(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetPersistedDefaultAudioEndpointDelegate(
      IntPtr self, uint processId, int dataFlow, int role, IntPtr deviceId);

    [DllImport("combase.dll")]
    private static extern int WindowsCreateString(
      [MarshalAs(UnmanagedType.LPWStr)] string source, uint length, out IntPtr value);

    [DllImport("combase.dll")]
    private static extern int WindowsDeleteString(IntPtr value);

    [DllImport("combase.dll")]
    private static extern int RoGetActivationFactory(IntPtr activatableClassId, ref Guid interfaceId, out IntPtr factory);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr reserved, uint flags);
  }
}
#endif
