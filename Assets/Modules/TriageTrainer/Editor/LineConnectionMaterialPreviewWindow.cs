using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using TriageTrainer.Entity.AEDLine;
using TriageTrainer.Entity.IntravenousLine;
using TriageTrainer.Entity.OxyLine;
using TriageTrainer.Entity.SuctionLine;
using TriageTrainer.Entity.LineConnection;

namespace TriageTrainer.Editor
{
  /// <summary>
  /// Interactive preview for the materials used by LineConnectionService.
  /// </summary>
  public sealed class LineConnectionMaterialPreviewWindow : EditorWindow
  {
    private const string RotateGizmoPrefabPath = "Assets/Modules/TriageTrainer/Editor/Prefabs/RotateGizmo.prefab";
    private const string BakedRotateGizmoPrefabPath = "Assets/Modules/TriageTrainer/Editor/Prefabs/RotateGizmoBaked.prefab";
    private const string BakedRotateGizmoMeshDirectory = "Assets/Modules/TriageTrainer/Editor/Prefabs/RotateGizmoBakedMeshes";
    private const string PreviewSkyboxMaterialPath = "Assets/Modules/TriageTrainer/Editor/Materials/LineMaterialPreview/Skybox.mat";
    private const string IntravenousLineMaterialPath = "Assets/Modules/TriageTrainer/Materials/LineConnectionService/IntravenousLine.mat";
    private const string AEDLineMaterialPath = "Assets/Modules/TriageTrainer/Materials/LineConnectionService/AEDLine.mat";
    private const string OxyLineMaterialPath = "Assets/Modules/TriageTrainer/Materials/LineConnectionService/OxyLine.mat";
    private const string SuctionLineMaterialPath = "Assets/Modules/TriageTrainer/Materials/LineConnectionService/SuctionLine.mat";
    private enum LineType
    {
      Intravenous,
      AED,
      Oxy,
      Suction,
    }

    private LineType _lineType;
    private MonoScript _serviceImplementation;
    private MonoScript _implementation;
    private Material _material;
    private Material _intravenousMaterial;
    private Material _aedMaterial;
    private Material _oxyMaterial;
    private Material _suctionMaterial;
    private float _lineWidth = 0.08f;
    private float _elasticity = 0.15f;
    private Vector3 _previewPivot = new Vector3(0.45f, -0.05f, 0f);
    private float _previewYaw = 0f;
    private float _previewPitch = 8f;
    private float _previewDistance = 4.2f;
    private Vector3 _startPoint = new Vector3(-0.9f, 0.15f, 0f);
    private Vector3 _endPoint = new Vector3(1.8f, 0.15f, 0f);
    private PreviewRenderUtility _preview;
    private GameObject _previewLineObject;
    private GameObject _bakedRotateGizmoObject;
    private readonly System.Collections.Generic.List<GameObject> _rotateGizmoParts = new System.Collections.Generic.List<GameObject>();
    private readonly System.Collections.Generic.List<Vector3> _rotateGizmoOffsets = new System.Collections.Generic.List<Vector3>();
    private readonly System.Collections.Generic.List<Quaternion> _rotateGizmoRotations = new System.Collections.Generic.List<Quaternion>();
    private readonly System.Collections.Generic.List<Vector3> _rotateGizmoScales = new System.Collections.Generic.List<Vector3>();
    private readonly System.Collections.Generic.List<Material> _rotateGizmoMaterials = new System.Collections.Generic.List<Material>();
    private Vector3 _gizmoPosition;
    private float _gizmoScale;
    private LineRenderer _lineRenderer;
    private Material _fallbackMaterial;
    private Material _previewLineMaterial;
    private Material _previewLineMaterialSource;
    private Material _previewSkyboxMaterial;

    private static readonly IReadOnlyDictionary<LineType, Type> LineTypeImplementations =
      new Dictionary<LineType, Type>
      {
        { LineType.Intravenous, typeof(IntravenousLineConnectionPoint) },
        { LineType.AED, typeof(AEDLineConnectionPoint) },
        { LineType.Oxy, typeof(OxyLineConnectionPoint) },
        { LineType.Suction, typeof(SuctionLineConnectionPoint) },
      };

    [MenuItem("Tools/Triage Trainer/Line Connection Material Preview")]
    private static void Open()
    {
      var window = GetWindow<LineConnectionMaterialPreviewWindow>();
      window.titleContent = new GUIContent("Line Material Preview");
      window.minSize = new Vector2(420f, 360f);
      window.Show();
    }

    /*
    // Used for /Assets/Modules/TriageTrainer/Editor/Prefabs/RotateGizmoBaked.prefab to convert the ProBuilder meshes into baked Mesh assets that PreviewRenderUtility can render without ProBuilder lifecycle callbacks.
    private static void BakeRotateGizmo()
    {
      var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RotateGizmoPrefabPath);
      if (sourcePrefab == null)
      {
        Debug.LogError($"Rotate Gizmo prefab was not found at '{RotateGizmoPrefabPath}'.");
        return;
      }

      // ProBuilder meshes do not serialize a usable MeshFilter mesh. Compile
      // them once into project assets so PreviewRenderUtility needs no
      // ProBuilder lifecycle callbacks when it renders the prefab.
      AssetDatabase.DeleteAsset(BakedRotateGizmoPrefabPath);
      AssetDatabase.DeleteAsset(BakedRotateGizmoMeshDirectory);
      AssetDatabase.CreateFolder("Assets/Modules/TriageTrainer/Editor/Prefabs", "RotateGizmoBakedMeshes");

      var instance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
      try
      {
        foreach (var proBuilderMesh in instance.GetComponentsInChildren<ProBuilderMesh>(true))
        {
          var bakedMesh = new Mesh { name = $"{proBuilderMesh.name}_Baked" };
          UnityEngine.ProBuilder.MeshUtility.Compile(proBuilderMesh, bakedMesh);
          AssetDatabase.CreateAsset(bakedMesh, $"{BakedRotateGizmoMeshDirectory}/{proBuilderMesh.name}.asset");

          proBuilderMesh.GetComponent<MeshFilter>().sharedMesh = bakedMesh;
          DestroyImmediate(proBuilderMesh);
        }

        PrefabUtility.SaveAsPrefabAsset(instance, BakedRotateGizmoPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Baked line preview Gizmo: {BakedRotateGizmoPrefabPath}");
      }
      finally
      {
        DestroyImmediate(instance);
      }
    }
    */

    private void OnEnable()
    {
      _preview = new PreviewRenderUtility();
      _preview.cameraFieldOfView = 30f;
      ConfigurePreviewSkybox();
      LoadDefaultLineMaterials();

      _previewLineObject = new GameObject("Line Material Preview Line");
      _previewLineObject.hideFlags = HideFlags.HideAndDontSave;
      _lineRenderer = _previewLineObject.AddComponent<LineRenderer>();
      _lineRenderer.positionCount = 2;
      _lineRenderer.useWorldSpace = true;
      _lineRenderer.alignment = LineAlignment.View;
      _lineRenderer.textureMode = LineTextureMode.Stretch;
      _lineRenderer.numCornerVertices = 8;
      _lineRenderer.numCapVertices = 8;
      _lineRenderer.startWidth = 0.08f;
      _lineRenderer.endWidth = 0.08f;
      _lineRenderer.startColor = Color.white;
      _lineRenderer.endColor = Color.white;
      _preview.AddSingleGO(_previewLineObject);

      var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
      if (shader != null)
        _fallbackMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

      if (!TryCreateBakedGizmo())
        CreateGuaranteedGizmo();

      UpdateServiceImplementationScript();
      UpdateImplementationScript();
    }

    private void LoadDefaultLineMaterials()
    {
      _intravenousMaterial = AssetDatabase.LoadAssetAtPath<Material>(IntravenousLineMaterialPath);
      _aedMaterial = AssetDatabase.LoadAssetAtPath<Material>(AEDLineMaterialPath);
      _oxyMaterial = AssetDatabase.LoadAssetAtPath<Material>(OxyLineMaterialPath);
      _suctionMaterial = AssetDatabase.LoadAssetAtPath<Material>(SuctionLineMaterialPath);
      _material = GetSelectedLineMaterial();
    }

    private bool TryCreateBakedGizmo()
    {
      var bakedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BakedRotateGizmoPrefabPath);
      if (bakedPrefab == null)
        return false;

      _bakedRotateGizmoObject = Instantiate(bakedPrefab);
      _bakedRotateGizmoObject.name = "Line Material Preview Baked Gizmo";
      _bakedRotateGizmoObject.hideFlags = HideFlags.HideAndDontSave;
      ApplyPreviewGizmoMaterials(_bakedRotateGizmoObject);
      _preview.AddSingleGO(_bakedRotateGizmoObject);
      return true;
    }

    private void ApplyPreviewGizmoMaterials(GameObject gizmo)
    {
      // The source materials use a project shader that PreviewRenderUtility
      // cannot render, which produces Unity's magenta error material. Keep
      // each source color but use a preview-safe unlit shader instead.
      var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
      if (shader == null)
        return;

      foreach (var renderer in gizmo.GetComponentsInChildren<Renderer>(true))
      {
        var sourceMaterial = renderer.sharedMaterial;
        var color = Color.white;
        if (sourceMaterial != null)
        {
          if (sourceMaterial.HasProperty("_BaseColor"))
            color = sourceMaterial.GetColor("_BaseColor");
          else if (sourceMaterial.HasProperty("_Color"))
            color = sourceMaterial.GetColor("_Color");
        }

        var previewMaterial = new Material(shader)
        {
          hideFlags = HideFlags.HideAndDontSave,
          color = color,
        };
        renderer.sharedMaterial = previewMaterial;
        _rotateGizmoMaterials.Add(previewMaterial);
      }
    }

    private void CreateGuaranteedGizmo()
    {
      var axisMaterialShader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
      if (axisMaterialShader == null)
        return;

      CreateAxisPart("X", Color.red, Vector3.right);
      CreateAxisPart("Y", Color.green, Vector3.up);
      CreateAxisPart("Z", Color.blue, Vector3.forward);
    }

    private void CreateAxisPart(string axis, Color color, Vector3 direction)
    {
      var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
      part.name = $"Line Material Preview Gizmo {axis}";
      part.hideFlags = HideFlags.HideAndDontSave;
      part.layer = 0;
      part.transform.position = Vector3.zero;
      // A cube's long side is its local X axis. Align that axis with the
      // intended world axis instead of using LookRotation (which aligns Z).
      var rotation = Quaternion.FromToRotation(Vector3.right, direction);
      part.transform.rotation = rotation;
      part.transform.localScale = new Vector3(0.7f, 0.035f, 0.035f);
      var collider = part.GetComponent<Collider>();
      if (collider != null)
        DestroyImmediate(collider);
      var material = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"))
      { hideFlags = HideFlags.HideAndDontSave, color = color };
      part.GetComponent<Renderer>().sharedMaterial = material;
      _rotateGizmoMaterials.Add(material);
      _rotateGizmoParts.Add(part);
      _rotateGizmoOffsets.Add(direction * 0.35f);
      _rotateGizmoRotations.Add(rotation);
      _rotateGizmoScales.Add(Vector3.one);
      _preview.AddSingleGO(part);
    }

    private void OnDisable()
    {
      if (_fallbackMaterial != null)
        DestroyImmediate(_fallbackMaterial);
      if (_previewLineMaterial != null)
        DestroyImmediate(_previewLineMaterial);
      if (_previewSkyboxMaterial != null &&
          (_previewSkyboxMaterial.hideFlags & HideFlags.DontSave) != 0)
        DestroyImmediate(_previewSkyboxMaterial);

      for (int i = 0; i < _rotateGizmoMaterials.Count; i++)
      {
        if (_rotateGizmoMaterials[i] != null)
          DestroyImmediate(_rotateGizmoMaterials[i]);
      }
      _rotateGizmoMaterials.Clear();
      _rotateGizmoParts.Clear();
      _rotateGizmoOffsets.Clear();
      _rotateGizmoRotations.Clear();
      _rotateGizmoScales.Clear();

      if (_preview != null)
        _preview.Cleanup();

      _preview = null;
      _previewLineObject = null;
      _bakedRotateGizmoObject = null;
      _lineRenderer = null;
      _previewLineMaterial = null;
      _previewLineMaterialSource = null;
      _previewSkyboxMaterial = null;
    }

    private void OnGUI()
    {
      // Reference the implementation of service
      using (new EditorGUI.DisabledScope(true))
        EditorGUILayout.ObjectField("", _serviceImplementation, typeof(MonoScript), false);
      EditorGUILayout.Space(6f);

      EditorGUILayout.HelpBox("LineType을 지정하면 각 라인 타입에 대해 사전 설정된 기본값을 로드합니다. 아래의 Rendering Settings 값은 이 화면에서 일시적으로 시뮬레이션 되는 것으로, 실제로는 Implementation 항목의 코드를 직접 수정하여야 합니다.", MessageType.Info);
      EditorGUILayout.Space(6f);

      EditorGUILayout.LabelField("Line Rendering Settings", EditorStyles.boldLabel);
      EditorGUI.BeginChangeCheck();
      var selectedLineType = (LineType)EditorGUILayout.EnumPopup("LineType", _lineType);
      if (selectedLineType != _lineType)
      {
        StoreSelectedLineMaterial();
        _lineType = selectedLineType;
        UpdateImplementationScript();
        _material = GetSelectedLineMaterial();
      }
      using (new EditorGUI.DisabledScope(true))
        EditorGUILayout.ObjectField("Implementation", _implementation, typeof(MonoScript), false);
      _material = (Material)EditorGUILayout.ObjectField("Material", _material, typeof(Material), false);
      _lineWidth = EditorGUILayout.Slider("Line Width", _lineWidth, 0.01f, 0.25f);
      _elasticity = EditorGUILayout.Slider("Elasticity", _elasticity, 0f, 1f);
      EditorGUILayout.Space(6f);

      EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox("두 점은 아래의 미리보기 화면 속 점을 드래그해서도 움직일 수 있습니다.", MessageType.Info);
      DrawVector3Row("Start Point", ref _startPoint);
      DrawVector3Row("End Point", ref _endPoint);
      EditorGUILayout.Space(6f);
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.Space(4f);

      if (GUILayout.Button("Reset Values", GUILayout.Width(120f)))
      {
        LoadDefaultLineMaterials();
        _lineWidth = 0.08f;
        _elasticity = 0.15f;
        _startPoint = new Vector3(-0.9f, 0.15f, 0f);
        _endPoint = new Vector3(1.8f, 0.15f, 0f);
      }
      
      // Camera reset button
      if (GUILayout.Button("Reset Camera", GUILayout.Width(120f)))
      {
        _previewPivot = new Vector3(0.45f, -0.05f, 0f);
        _previewYaw = 0f;
        _previewPitch = 8f;
        _previewDistance = 4.2f;
      }

      EditorGUILayout.EndHorizontal();
      if (EditorGUI.EndChangeCheck())
        Repaint();

      EditorGUILayout.Space(6f);
      float previewHeight = Mathf.Max(220f, position.height - 150f);
      var previewRect = GUILayoutUtility.GetRect(0f, previewHeight, GUILayout.ExpandWidth(true));
      ConfigurePreviewCamera();
      _preview.camera.aspect = Mathf.Max(0.1f, previewRect.width / previewRect.height);
      HandlePreviewInput(previewRect);
      DrawPreview(previewRect, _lineWidth);
      Repaint();
    }

    private void StoreSelectedLineMaterial()
    {
      switch (_lineType)
      {
        case LineType.Intravenous: _intravenousMaterial = _material; break;
        case LineType.AED: _aedMaterial = _material; break;
        case LineType.Oxy: _oxyMaterial = _material; break;
        case LineType.Suction: _suctionMaterial = _material; break;
      }
    }

    private Material GetSelectedLineMaterial()
    {
      return _lineType switch
      {
        LineType.Intravenous => _intravenousMaterial,
        LineType.AED => _aedMaterial,
        LineType.Oxy => _oxyMaterial,
        LineType.Suction => _suctionMaterial,
        _ => null,
      };
    }

    private void UpdateImplementationScript()
    {
      _implementation = null;
      if (!LineTypeImplementations.TryGetValue(_lineType, out var implementationType))
        return;

      var guids = AssetDatabase.FindAssets($"t:MonoScript {implementationType.Name}");
      for (int i = 0; i < guids.Length; i++)
      {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guids[i]));
        if (script != null && script.GetClass() == implementationType)
        {
          _implementation = script;
          return;
        }
      }
    }

    private void UpdateServiceImplementationScript()
    {
      _serviceImplementation = null;
      var guids = AssetDatabase.FindAssets($"t:MonoScript {nameof(LineConnectionService)}");
      for (int i = 0; i < guids.Length; i++)
      {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guids[i]));
        if (script != null && script.GetClass() == typeof(LineConnectionService))
        {
          _serviceImplementation = script;
          return;
        }
      }
    }

    private void ConfigurePreviewSkybox()
    {
      var camera = _preview.camera;
      _previewSkyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(PreviewSkyboxMaterialPath);
      if (_previewSkyboxMaterial == null)
      {
        var shader = Shader.Find("Skybox/Procedural");
        if (shader == null)
        {
          camera.clearFlags = CameraClearFlags.Color;
          camera.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
          return;
        }

        _previewSkyboxMaterial = new Material(shader)
        {
          hideFlags = HideFlags.HideAndDontSave,
        };
        _previewSkyboxMaterial.SetColor("_SkyTint", new Color(0.18f, 0.28f, 0.42f));
        _previewSkyboxMaterial.SetColor("_GroundColor", new Color(0.055f, 0.065f, 0.08f));
        _previewSkyboxMaterial.SetFloat("_Exposure", 0.8f);
      }

      // Unity can return a managed reference to a missing component here.
      // Use Unity's overloaded null comparison rather than C#'s ?? operator.
      var skybox = camera.GetComponent<Skybox>();
      if (skybox == null)
        skybox = camera.gameObject.AddComponent<Skybox>();
      skybox.material = _previewSkyboxMaterial;
      camera.clearFlags = CameraClearFlags.Skybox;
    }

    private void HandlePreviewInput(Rect rect)
    {
      var current = Event.current;
      if (!rect.Contains(current.mousePosition))
        return;

      if (current.type == EventType.ScrollWheel)
      {
        _previewDistance = Mathf.Clamp(_previewDistance + current.delta.y * 0.015f, 1.5f, 12f);
        current.Use();
        Repaint();
        return;
      }

      if (current.type == EventType.MouseDown && current.button == 0)
      {
        int point = ClosestEndpoint(rect, current.mousePosition);
        if (point >= 0)
        {
          GUIUtility.hotControl = GetInstanceID();
          _draggedPoint = point;
          current.Use();
        }
      }

      if (current.type == EventType.MouseDrag && current.button == 0 && _draggedPoint >= 0)
      {
        var ray = PreviewRay(rect, current.mousePosition);
        var plane = new Plane(_previewCamera.transform.forward, _draggedPoint == 0 ? _startPoint : _endPoint);
        if (plane.Raycast(ray, out float distance))
        {
          if (_draggedPoint == 0)
            _startPoint = ray.GetPoint(distance);
          else
            _endPoint = ray.GetPoint(distance);
        }
        current.Use();
        Repaint();
        return;
      }

      if (current.type == EventType.MouseUp && current.button == 0 && _draggedPoint >= 0)
      {
        _draggedPoint = -1;
        GUIUtility.hotControl = 0;
        current.Use();
      }

      if (current.type != EventType.MouseDrag)
        return;

      if (current.button == 1 && !current.alt)
      {
        _previewYaw += current.delta.x * 0.5f;
        _previewPitch = Mathf.Clamp(_previewPitch - current.delta.y * 0.5f, -80f, 80f);
        current.Use();
      }
      else if (current.button == 1 && current.alt)
      {
        _previewDistance = Mathf.Clamp(_previewDistance + current.delta.y * 0.02f, 1.5f, 12f);
        current.Use();
      }
      else if (current.button == 2)
      {
        var cameraRotation = Quaternion.Euler(_previewPitch, _previewYaw, 0f);
        Vector3 right = cameraRotation * Vector3.right;
        Vector3 up = cameraRotation * Vector3.up;
        _previewPivot -= right * current.delta.x * 0.005f;
        _previewPivot += up * current.delta.y * 0.005f;
        current.Use();
      }

      Repaint();
    }

    private int _draggedPoint = -1;
    private Camera _previewCamera => _preview != null ? _preview.camera : null;

    private void DrawVector3Row(string label, ref Vector3 value)
    {
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
      value = EditorGUILayout.Vector3Field("", value);
      EditorGUILayout.EndHorizontal();
    }

    private int ClosestEndpoint(Rect rect, Vector2 mousePosition)
    {
      Vector2 start = WorldToPreview(_startPoint, rect);
      Vector2 end = WorldToPreview(_endPoint, rect);
      float startDistance = Vector2.Distance(mousePosition, start);
      float endDistance = Vector2.Distance(mousePosition, end);
      if (startDistance > 18f && endDistance > 18f)
        return -1;
      return startDistance <= endDistance ? 0 : 1;
    }

    private Vector2 WorldToPreview(Vector3 point, Rect rect)
    {
      Vector3 viewport = _previewCamera.WorldToViewportPoint(point);
      return new Vector2(rect.x + viewport.x * rect.width, rect.y + (1f - viewport.y) * rect.height);
    }

    private Ray PreviewRay(Rect rect, Vector2 mousePosition)
    {
      float x = Mathf.Clamp01((mousePosition.x - rect.x) / rect.width);
      float y = Mathf.Clamp01(1f - (mousePosition.y - rect.y) / rect.height);
      return _previewCamera.ViewportPointToRay(new Vector3(x, y, 0f));
    }

    private void DrawPreview(Rect rect, float lineWidth)
    {
      if (_preview == null || _lineRenderer == null || rect.width <= 1f || rect.height <= 1f)
        return;

      var start = _startPoint;
      var end = _endPoint;
      const int segmentCount = 32;
      _lineRenderer.positionCount = segmentCount;
      for (int i = 0; i < segmentCount; i++)
      {
        float t = i / (segmentCount - 1f);
        var point = Vector3.Lerp(start, end, t);
        // Approximate the resting shape produced by the runtime's gravity and slack.
        point += Vector3.down * (Mathf.Sin(t * Mathf.PI) * (0.18f + _elasticity * 0.42f));
        point += Vector3.forward * (Mathf.Sin(t * Mathf.PI) * 0.08f);
        _lineRenderer.SetPosition(i, point);
      }
      _lineRenderer.startWidth = lineWidth;
      _lineRenderer.endWidth = lineWidth;
      _lineRenderer.sharedMaterial = GetPreviewLineMaterial();

      ConfigurePreviewCamera();
      var camera = _preview.camera;
      PositionRotateGizmo(camera);

      _preview.BeginPreview(rect, GUIStyle.none);
      _preview.Render();
      var texture = _preview.EndPreview();
      GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
      DrawGizmoAxisLabels(rect);
      DrawEndpointMarkers(rect);
    }

    private Material GetPreviewLineMaterial()
    {
      if (_material == null)
        return _fallbackMaterial;

      if (_previewLineMaterialSource == _material && _previewLineMaterial != null)
        return _previewLineMaterial;

      if (_previewLineMaterial != null)
        DestroyImmediate(_previewLineMaterial);

      // PreviewRenderUtility does not reliably run the project's SRP shaders.
      // Convert the selected material into a built-in transparent sprite
      // material while retaining its visible color and line texture.
      var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
      if (shader == null)
        return _fallbackMaterial;

      var color = Color.white;
      if (_material.HasProperty("_BaseColor"))
        color = _material.GetColor("_BaseColor");
      else if (_material.HasProperty("_Color"))
        color = _material.GetColor("_Color");

      Texture texture = null;
      if (_material.HasProperty("_BaseMap"))
        texture = _material.GetTexture("_BaseMap");
      else if (_material.HasProperty("_MainTex"))
        texture = _material.GetTexture("_MainTex");

      _previewLineMaterial = new Material(shader)
      {
        hideFlags = HideFlags.HideAndDontSave,
        color = color,
        mainTexture = texture,
      };
      _previewLineMaterialSource = _material;
      return _previewLineMaterial;
    }

    private void ConfigurePreviewCamera()
    {
      if (_preview == null)
        return;
      var camera = _preview.camera;
      camera.orthographic = false;
      camera.fieldOfView = 35f;
      camera.nearClipPlane = 0.01f;
      camera.farClipPlane = 100f;
      camera.cullingMask = ~0;
      var cameraRotation = Quaternion.Euler(_previewPitch, _previewYaw, 0f);
      camera.transform.rotation = cameraRotation;
      camera.transform.position = _previewPivot - cameraRotation * Vector3.forward * _previewDistance;
    }

    private void PositionRotateGizmo(Camera camera)
    {
      if (_bakedRotateGizmoObject == null && _rotateGizmoParts.Count == 0)
        return;

      // This is camera-local on purpose. The old placement used a lateral
      // offset larger than the distance in front of the camera, so the Gizmo
      // was always outside the perspective frustum.
      float depth = _previewDistance * 0.72f;
      float verticalExtent = depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
      float horizontalExtent = verticalExtent * camera.aspect;
      _gizmoPosition = camera.transform.position
        + camera.transform.right * (horizontalExtent * 0.86f)
        + camera.transform.up * (verticalExtent * 0.76f)
        + camera.transform.forward * depth;
      // The baked prefab's authored bounds are much larger than the fallback
      // bars. Reduce its displayed size to one fifth of the previous value.
      _gizmoScale = depth * 0.015f;
      if (_bakedRotateGizmoObject != null)
      {
        _bakedRotateGizmoObject.transform.SetPositionAndRotation(_gizmoPosition, Quaternion.identity);
        _bakedRotateGizmoObject.transform.localScale = Vector3.one * _gizmoScale;
        return;
      }

      for (int i = 0; i < _rotateGizmoParts.Count; i++)
      {
        _rotateGizmoParts[i].transform.position = _gizmoPosition + _rotateGizmoOffsets[i] * _gizmoScale;
        _rotateGizmoParts[i].transform.rotation = _rotateGizmoRotations[i];
        _rotateGizmoParts[i].transform.localScale = _rotateGizmoScales[i] * _gizmoScale;
      }
    }

    private void DrawGizmoAxisLabels(Rect rect)
    {
      DrawGizmoAxisLabel(rect, Vector3.right, Color.white, "X");
      DrawGizmoAxisLabel(rect, Vector3.up, Color.white, "Y");
      DrawGizmoAxisLabel(rect, Vector3.forward, Color.white, "Z");
    }

    private void DrawGizmoAxisLabel(Rect rect, Vector3 direction, Color color, string label)
    {
      // Keep labels attached to the gizmo in screen space. Projecting a long
      // world-space offset makes the labels drift to the edge when the window
      // becomes narrow because the preview camera's aspect ratio changes.
      Vector2 center = WorldToPreview(_gizmoPosition, rect);
      Vector2 axisEnd = WorldToPreview(_gizmoPosition + direction * _gizmoScale, rect);
      Vector2 screenDirection = axisEnd - center;
      if (screenDirection.sqrMagnitude < 0.01f)
        screenDirection = Vector2.up;
      else
        screenDirection.Normalize();

      const float labelDistance = 34f;
      Vector2 position = center + screenDirection * labelDistance;
      var previousColor = GUI.color;
      GUI.color = color;
      GUI.Label(new Rect(position.x - 5f, position.y - 10f, 18f, 20f), label, EditorStyles.boldLabel);
      GUI.color = previousColor;
    }

    private void DrawEndpointMarkers(Rect rect)
    {
      DrawMarker(WorldToPreview(_startPoint, rect), Color.yellow, "S");
      DrawMarker(WorldToPreview(_endPoint, rect), Color.cyan, "E");
    }

    private static void DrawMarker(Vector2 position, Color color, string label)
    {
      Handles.color = color;
      Handles.DrawSolidDisc(position, Vector3.forward, 6f);
      GUI.Label(new Rect(position.x + 8f, position.y - 10f, 24f, 20f), label);
    }

  }
}
