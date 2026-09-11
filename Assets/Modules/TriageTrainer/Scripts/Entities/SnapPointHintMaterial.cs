using UnityEngine;
using UnityEngine.Rendering;

namespace TriageTrainer.Entity
{
  /// <summary>
  /// 스냅 지점(제세동 카트, 이동식 침대) 힌트와 배치·마운트 미리보기가 쓰는 반투명 머티리얼을 만든다.
  /// </summary>
  /// <remarks>
  /// 씬이나 프리팹의 머티리얼이 참조하지 않는 셰이더는 플레이어 빌드에 포함되지 않아
  /// <see cref="Shader.Find(string)"/> 가 null 을 돌려주고 힌트가 마젠타로 보인다.
  /// 그래서 Resources 에 둔 머티리얼 에셋을 원본으로 삼아 빌드에 셰이더와 변형이 항상 포함되게 한다.
  /// 에셋을 찾지 못하는 경우에만 예전처럼 셰이더 이름으로 찾아 만든다.
  /// </remarks>
  public static class SnapPointHintMaterial
  {
    public const string MaterialResourcePath = "Materials/SnapPointHint";

    private static Material _template;
    private static bool _missingLogged;

    /// <summary>
    /// 지정한 색으로 칠한 힌트 머티리얼 인스턴스를 만든다. 원본을 구할 수 없으면 null 을 돌려준다.
    /// 돌려받은 머티리얼은 호출한 쪽이 파괴해야 한다.
    /// </summary>
    public static Material CreateInstance(Color color, string materialName)
    {
      Material template = ResolveTemplate();
      if (template == null)
        return null;

      var material = new Material(template) { name = materialName };
      if (material.HasProperty("_BaseColor"))
        material.SetColor("_BaseColor", color);
      if (material.HasProperty("_Color"))
        material.SetColor("_Color", color);
      return material;
    }

    private static Material ResolveTemplate()
    {
      if (_template != null)
        return _template;

      _template = Resources.Load<Material>(MaterialResourcePath);
      if (_template != null)
        return _template;

      Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
      if (shader == null)
      {
        if (!_missingLogged)
        {
          _missingLogged = true;
          Debug.LogWarning("[SnapPointHintMaterial] 힌트 머티리얼 에셋(" + MaterialResourcePath
            + ")과 대체 셰이더를 모두 찾지 못해 스냅 지점 힌트를 표시하지 않습니다.");
        }
        return null;
      }

      _template = new Material(shader) { name = "SnapPointHintMaterial_Fallback" };
      if (_template.HasProperty("_Surface"))
        _template.SetFloat("_Surface", 1f);
      _template.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
      _template.SetOverrideTag("RenderType", "Transparent");
      _template.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
      _template.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
      _template.SetInt("_ZWrite", 0);
      _template.renderQueue = (int)RenderQueue.Transparent;
      return _template;
    }
  }
}
