using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [Header("Visibility")]
    [SerializeField, Range(0f, 1f)] private float _spectatorAlpha = 0.5f;
    [SerializeField] private string _spectatorLayerName = "Spectator";

    private readonly List<Renderer> _renderers = new();
    private MaterialPropertyBlock _mpb;
    private int _originalLayer;
    private int _spectatorLayer = -1;
    private bool _visibilityInitialized;

    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    /// <summary>
    /// 머티리얼에 "_Color" 프로퍼티가 있는지 <b>셰이더 컴파일을 유발하지 않고</b> 검사한다.
    ///
    /// 주의(크래시 회피): <see cref="Material.HasProperty(string)"/>, <see cref="Material.color"/>,
    /// <see cref="Material.GetColor(int)"/> 등 <b>머티리얼 인스턴스의 프로퍼티 접근</b>은 내부적으로
    /// 머티리얼 프로퍼티 시트를 빌드(UpdateHashes)하면서 셰이더 서브프로그램 컴파일을 트리거할 수 있다.
    /// 이 컴파일이 macOS의 MultiplayerPlayMode(가상 플레이어, child domain)에서 모달 프로그레스바를
    /// 띄우다 네이티브 SIGSEGV 를 일으킨다(Editor.log: Material:HasProperty_Injected / GetColorImpl 크래시).
    /// 따라서 머티리얼 인스턴스 프로퍼티는 일절 읽지 않고, 셰이더의 <b>직렬화된 프로퍼티 메타데이터만</b>
    /// 조회하는 <see cref="Shader.FindPropertyIndex"/> 로 존재 여부만 확인한다.
    /// 색 자체는 머티리얼에서 읽지 않고 <see cref="MaterialPropertyBlock"/> 오버라이드의 적용/해제로만 처리한다.
    /// </summary>
    private static bool HasColorProperty(Material material)
    {
      if (material == null)
      {
        return false;
      }

      var shader = material.shader;
      return shader != null && shader.FindPropertyIndex("_Color") >= 0;
    }

    void Awake_Visibility()
    {
      _mpb = new MaterialPropertyBlock();
      _originalLayer = gameObject.layer;
      _spectatorLayer = LayerMask.NameToLayer(_spectatorLayerName);
      CacheRenderers();
    }

    private void CacheRenderers()
    {
      _renderers.Clear();
      var found = GetComponentsInChildren<Renderer>(includeInactive: true);
      if (found != null && found.Length > 0)
        _renderers.AddRange(found);
      // 원본 색은 머티리얼에서 읽지 않는다(프로퍼티 접근이 셰이더 컴파일→크래시 유발).
      // 복원은 MaterialPropertyBlock 의 _Color 오버라이드를 "제거"하여 머티리얼 원본으로 되돌리고,
      // spectator 투명도는 알파 채널만 오버라이드하는 방식으로 처리한다.
      _visibilityInitialized = true;
    }

    private void EnsureVisibilityInit()
    {
      if (_visibilityInitialized) return;
      _spectatorLayer = LayerMask.NameToLayer(_spectatorLayerName);
      CacheRenderers();
    }

    private void ApplyPlayerVisibility()
    {
      EnsureVisibilityInit();
      RestoreLayer();
      RestoreColors();
    }

    private void ApplySpectatorVisibility()
    {
      EnsureVisibilityInit();
      SetSpectatorLayer();
      ApplySpectatorTransparency();
    }

    private void RestoreLayer()
    {
      gameObject.layer = _originalLayer;
      for (int i = 0; i < _renderers.Count; i++)
      {
        var r = _renderers[i];
        if (r == null) continue;
        r.gameObject.layer = _originalLayer;
      }
    }

    private void SetSpectatorLayer()
    {
      if (_spectatorLayer < 0) return;
      gameObject.layer = _spectatorLayer;
      for (int i = 0; i < _renderers.Count; i++)
      {
        var r = _renderers[i];
        if (r == null) continue;
        r.gameObject.layer = _spectatorLayer;
      }
    }

    private void RestoreColors()
    {
      // 머티리얼 원본 색으로 복원: MaterialPropertyBlock 의 _Color 오버라이드를 제거한다.
      // (머티리얼 프로퍼티를 읽지 않으므로 셰이더 컴파일을 유발하지 않는다.)
      for (int i = 0; i < _renderers.Count; i++)
      {
        var r = _renderers[i];
        if (r == null) continue;

        r.GetPropertyBlock(_mpb);
        _mpb.Clear();
        r.SetPropertyBlock(_mpb);
      }
    }

    private void ApplySpectatorTransparency()
    {
      // spectator 반투명: 원본 색을 읽지 않고 알파만 _spectatorAlpha 로 오버라이드한다.
      // RGB 는 흰색(1,1,1)로 두어 셰이더의 색 틴트를 그대로 통과시킨다(표준 _Color 틴트 셰이더 기준).
      for (int i = 0; i < _renderers.Count; i++)
      {
        var r = _renderers[i];
        if (r == null) continue;
        if (!HasColorProperty(r.sharedMaterial)) continue;

        r.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorPropertyId, new Color(1f, 1f, 1f, _spectatorAlpha));
        r.SetPropertyBlock(_mpb);
      }
    }
  }
}
