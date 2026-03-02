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
    private readonly List<Color> _originalColors = new();
    private MaterialPropertyBlock _mpb;
    private int _originalLayer;
    private int _spectatorLayer = -1;
    private bool _visibilityInitialized;

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
      _originalColors.Clear();
      var found = GetComponentsInChildren<Renderer>(includeInactive: true);
      if (found != null && found.Length > 0)
        _renderers.AddRange(found);
      for (int i = 0; i < _renderers.Count; i++)
      {
        var r = _renderers[i];
        if (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
          _originalColors.Add(r.sharedMaterial.color);
        else
          _originalColors.Add(Color.white);
      }
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
      for (int i = 0; i < _renderers.Count; i++)
      {
        var r = _renderers[i];
        if (r == null) continue;
        if (r.sharedMaterial == null || !r.sharedMaterial.HasProperty("_Color")) continue;

        r.GetPropertyBlock(_mpb);
        _mpb.SetColor("_Color", _originalColors[i]);
        r.SetPropertyBlock(_mpb);
      }
    }

    private void ApplySpectatorTransparency()
    {
      for (int i = 0; i < _renderers.Count; i++)
      {
        var r = _renderers[i];
        if (r == null) continue;
        if (r.sharedMaterial == null || !r.sharedMaterial.HasProperty("_Color")) continue;

        var original = _originalColors[i];
        r.GetPropertyBlock(_mpb);
        _mpb.SetColor("_Color", new Color(original.r, original.g, original.b, _spectatorAlpha));
        r.SetPropertyBlock(_mpb);
      }
    }
  }
}
