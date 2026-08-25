using System;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Performance
{
  /// <summary>
  /// 3인칭 카메라 POV(거리) 사용자 설정을 관리하는 서비스입니다.
  ///
  /// 역할:
  ///   - 사용자가 설정한 3인칭 카메라 거리를 <see cref="PlayerPrefs"/>에 저장/로드합니다.
  ///   - 활성 <see cref="MainCameraController"/>에 값을 적용합니다.
  ///   - 설정 변경 시 <see cref="OnDistanceChanged"/> 이벤트를 발행합니다.
  ///   - <see cref="RegistryType.Service"/>에 등록되어 설정 UI 등 외부에서 조회 가능합니다.
  ///
  /// 저장값이 없으면(HasStoredValue == false) 카메라의 인스펙터 기본 거리를 사용합니다.
  /// 씬에 하나만 배치하세요(TexturePerformanceService와 동일한 시스템 오브젝트에 두는 것을 권장).
  /// </summary>
  public class CameraDistancePreferenceService : MonoBehaviour
  {
    private const string PlayerPrefsKey = "MultiplayerInfrastructure.CameraThirdPersonDistance";

    /// <summary>POV 거리가 변경되었을 때 새 거리 값이 전달됩니다.</summary>
    public event Action<float> OnDistanceChanged;

    private float _currentDistance;
    private bool _hasStoredValue;

    /// <summary>현재 서비스가 보유한 3인칭 카메라 거리 값입니다.</summary>
    public float CurrentDistance => _currentDistance;

    /// <summary>PlayerPrefs에 저장된 값이 존재하는지 여부입니다.</summary>
    public bool HasStoredValue => _hasStoredValue;

    private void Awake()
    {
      Registry.Registry.Register(
        RegistryType.Service,
        Registry.Registry.TypeKey<CameraDistancePreferenceService>(),
        this
      );

      LoadStoredValue();
    }

    private void OnDestroy()
    {
      Registry.Registry.Unregister(
        RegistryType.Service,
        Registry.Registry.TypeKey<CameraDistancePreferenceService>()
      );
    }

    private void Start()
    {
      // 카메라가 이미 존재하면 저장값을 초기 적용한다.
      // (카메라가 나중에 스폰되는 경우에는 ApplyToCamera가 소유자 카메라 초기화 이후에도
      //  설정 UI 열람/적용 시 재적용되므로 문제되지 않는다.)
      if (_hasStoredValue)
        ApplyToCamera(_currentDistance);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 3인칭 카메라 거리를 설정하고 즉시 카메라에 적용한 뒤 PlayerPrefs에 저장합니다.
    /// </summary>
    public void SetDistance(float distance)
    {
      PersistDistance(distance);
      ApplyToCamera(distance);
    }

    /// <summary>
    /// POV 거리만 저장합니다. 현재 카메라에는 적용하지 않으므로, 휠 조정 중 자동으로 전환된
    /// 1/3인칭 시점은 유지하면서 다음 실행에 사용할 거리만 별도로 보관할 수 있습니다.
    /// </summary>
    public void PersistDistance(float distance)
    {
      _currentDistance = distance;
      _hasStoredValue = true;

      Save(distance);
      OnDistanceChanged?.Invoke(distance);
    }

    /// <summary>
    /// 현재 활성 카메라의 거리를 기준으로 서비스 값을 동기화합니다.
    /// 저장값이 없을 때 설정 UI가 슬라이더 초기값을 카메라 현재값으로 맞추기 위해 사용합니다.
    /// </summary>
    public float ResolveEffectiveDistance()
    {
      if (_hasStoredValue)
        return _currentDistance;

      var cam = ResolveCamera();
      if (cam != null)
      {
        _currentDistance = cam.DesiredThirdPersonDistance;
        return _currentDistance;
      }

      return _currentDistance;
    }

    /// <summary>현재 카메라의 POV 거리 허용 범위를 반환합니다. 카메라가 없으면 기본 범위를 반환합니다.</summary>
    public void GetDistanceRange(out float min, out float max)
    {
      var cam = ResolveCamera();
      if (cam != null)
      {
        min = cam.MinThirdPersonDistance;
        max = cam.MaxThirdPersonDistance;
        return;
      }

      // 카메라 미존재 시 CameraHolder 인스펙터 기본값과 동일한 폴백.
      min = 1.0f;
      max = 8.0f;
    }

    /// <summary>
    /// 저장된 값을 다시 불러와 카메라에 적용합니다. 카메라가 늦게 초기화된 경우 재적용에 사용합니다.
    /// </summary>
    public void ReapplyToCamera()
    {
      if (_hasStoredValue)
        ApplyToCamera(_currentDistance);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 내부 구현
    // ──────────────────────────────────────────────────────────────────────────

    private static MainCameraController ResolveCamera()
    {
      if (MainCameraController.Instance != null)
        return MainCameraController.Instance;

      return Registry.Registry.Get<MainCameraController>(
        RegistryType.Service,
        Registry.Registry.TypeKey<MainCameraController>()
      );
    }

    private static void ApplyToCamera(float distance)
    {
      var cam = ResolveCamera();
      if (cam != null)
        cam.SetThirdPersonDistance(distance);
    }

    private void LoadStoredValue()
    {
      if (!PlayerPrefs.HasKey(PlayerPrefsKey))
      {
        _hasStoredValue = false;
        return;
      }

      _currentDistance = PlayerPrefs.GetFloat(PlayerPrefsKey);
      _hasStoredValue = true;
    }

    private static void Save(float distance)
    {
      PlayerPrefs.SetFloat(PlayerPrefsKey, distance);
      PlayerPrefs.Save();
    }
  }
}
