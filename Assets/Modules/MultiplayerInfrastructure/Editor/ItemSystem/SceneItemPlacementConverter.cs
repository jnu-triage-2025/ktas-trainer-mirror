using System.Collections.Generic;
using System.Text;
using MultiplayerInfrastructure.ItemSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Editor.ItemSystem
{
  /// <summary>
  /// 씬에 배치된 <see cref="SceneItemPlacement"/>(런타임에 물리 스폰되어 흩어지는 grounded item)을
  /// <see cref="StaticPlacedItem"/>(맵의 일부처럼 제자리에 고정되는 정적 아이템)으로 일괄 변환하는 에디터 도구입니다.
  ///
  /// <para>씬 파일을 직접 텍스트 편집하지 않고 Unity 오브젝트 API + Undo 를 사용하므로 안전하며 되돌릴 수 있습니다.</para>
  ///
  /// 변환 내용(각 SceneItemPlacement GameObject 마다):
  /// 1. <see cref="StaticPlacedItem"/> 컴포넌트 추가 및 필드 이관
  ///    - entityIdentifier 유지, itemIdentifier → PickupReward.ItemIdentifier, stackCount → PickupReward.Amount
  ///    - VanishMode/VanishBehavior/Remains/DecreaseRemains 는 기본값(전역/Invisible/1/1)
  /// 2. Collider 가 없으면 자식 Renderer bounds 에 맞춘 <see cref="BoxCollider"/> 추가(상호작용 감지용)
  /// 3. 원본 <see cref="SceneItemPlacement"/> 컴포넌트 제거
  /// </summary>
  internal static class SceneItemPlacementConverter
  {
    private const string MenuRoot = "Tools/Multiplayer Infrastructure/Static Placed Item/";
    private const string ConvertSelectionMenuPath = MenuRoot + "Convert Selected SceneItemPlacements";
    private const string ConvertSceneMenuPath = MenuRoot + "Convert All In Active Scene";

    // ── 선택 오브젝트 변환 ─────────────────────────────────────────────────

    [MenuItem(ConvertSelectionMenuPath)]
    private static void ConvertSelection()
    {
      var placements = CollectFromSelection();
      if (placements.Count == 0)
      {
        EditorUtility.DisplayDialog(
          "Static Placed Item",
          "선택된 오브젝트(및 그 자식)에서 SceneItemPlacement 를 찾지 못했습니다.",
          "확인");
        return;
      }

      RunConversion(placements, "선택된 SceneItemPlacement");
    }

    // ── 활성 씬 전체 변환 ──────────────────────────────────────────────────

    [MenuItem(ConvertSceneMenuPath)]
    private static void ConvertActiveScene()
    {
      var placements = CollectFromActiveScene();
      if (placements.Count == 0)
      {
        EditorUtility.DisplayDialog(
          "Static Placed Item",
          "활성 씬에서 SceneItemPlacement 를 찾지 못했습니다.",
          "확인");
        return;
      }

      bool proceed = EditorUtility.DisplayDialog(
        "Static Placed Item",
        $"활성 씬의 SceneItemPlacement {placements.Count}개를 StaticPlacedItem 으로 변환합니다.\n\n" +
        "이 작업은 Undo 로 되돌릴 수 있으나, 변환 전 씬을 백업/커밋하는 것을 권장합니다.\n계속하시겠습니까?",
        "변환",
        "취소");
      if (!proceed)
        return;

      RunConversion(placements, "활성 씬 SceneItemPlacement");
    }

    // ── 공통 변환 실행 ─────────────────────────────────────────────────────

    private static void RunConversion(List<SceneItemPlacement> placements, string label)
    {
      int group = Undo.GetCurrentGroup();
      Undo.SetCurrentGroupName($"Convert {label} to StaticPlacedItem");

      int converted = 0;
      int skipped = 0;
      int failed = 0;
      var report = new StringBuilder();

      foreach (var placement in placements)
      {
        if (placement == null)
          continue;

        var go = placement.gameObject;

        // 이미 StaticPlacedItem 이 있으면 건너뛴다(중복 변환 방지).
        if (go.GetComponent<StaticPlacedItem>() != null)
        {
          skipped++;
          report.AppendLine($"  [skip] {go.name}: 이미 StaticPlacedItem 이 있습니다.");
          continue;
        }

        // 개별 실패가 전체 변환을 중단시키지 않도록 각 항목을 보호한다.
        try
        {
          if (ConvertOne(placement, report))
            converted++;
          else
            skipped++;
        }
        catch (System.Exception ex)
        {
          failed++;
          report.AppendLine($"  [FAIL] {(go != null ? go.name : "<null>")}: {ex.GetType().Name} - {ex.Message}");
        }
      }

      Undo.CollapseUndoOperations(group);

      // 씬을 더티로 표시해 저장 필요를 알린다.
      var scene = SceneManager.GetActiveScene();
      if (scene.IsValid())
        EditorSceneManager.MarkSceneDirty(scene);

      Debug.Log(
        $"[SceneItemPlacementConverter] 변환 완료: 성공 {converted}개, 건너뜀 {skipped}개, 실패 {failed}개.\n{report}");
      EditorUtility.DisplayDialog(
        "Static Placed Item",
        $"변환 완료\n\n성공: {converted}개\n건너뜀: {skipped}개\n실패: {failed}개\n\n자세한 내역은 콘솔 로그를 확인하세요.\n씬을 저장해야 반영됩니다.",
        "확인");
    }

    private static bool ConvertOne(SceneItemPlacement placement, StringBuilder report)
    {
      var go = placement.gameObject;

      // 원본 필드 읽기 (entityIdentifier 는 private 이므로 SerializedObject 사용).
      string itemIdentifier = placement.ItemIdentifier;
      int stackCount = placement.StackCount;
      string entityIdentifier = ReadSerializedString(placement, "_entityIdentifier");

      if (string.IsNullOrWhiteSpace(itemIdentifier))
      {
        report.AppendLine($"  [skip] {go.name}: itemIdentifier 가 비어 있습니다.");
        return false;
      }

      // 1. Collider 를 먼저 보장한다.
      //    StaticPlacedItem 은 [RequireComponent(typeof(Collider))] 이므로, 컴포넌트 추가 시
      //    Unity 가 Collider 를 함께 추가하려다 예외/실패가 나면 AddComponent 가 null 을 반환할 수 있다.
      //    Collider 를 먼저 명시적으로 붙여두면 RequireComponent 가 이미 만족되어 안전하다.
      EnsureBoxCollider(go, report);

      // 2. StaticPlacedItem 추가 및 필드 설정.
      var staticItem = Undo.AddComponent<StaticPlacedItem>(go);
      if (staticItem == null)
      {
        // 프리팹 인스턴스 제약 등으로 실패할 수 있다. 일반 AddComponent 로 폴백.
        staticItem = go.AddComponent<StaticPlacedItem>();
        if (staticItem != null)
          Undo.RegisterCreatedObjectUndo(staticItem, "Add StaticPlacedItem");
      }

      if (staticItem == null)
      {
        report.AppendLine($"  [FAIL] {go.name}: StaticPlacedItem 컴포넌트를 추가하지 못했습니다.");
        return false;
      }

      var so = new SerializedObject(staticItem);

      if (!string.IsNullOrWhiteSpace(entityIdentifier))
        so.FindProperty("_entityIdentifier").stringValue = entityIdentifier;

      var reward = so.FindProperty("_pickupReward");
      reward.FindPropertyRelative("_itemIdentifier").stringValue = itemIdentifier;
      reward.FindPropertyRelative("_amount").intValue = Mathf.Max(1, stackCount);
      reward.FindPropertyRelative("_decreaseRemains").intValue = 1;

      so.FindProperty("_initialState").FindPropertyRelative("_remains").intValue = 1;
      so.FindProperty("_vanishMode").enumValueIndex =
        (int)StaticPlacedItemVanishMode.VanishedGlobalOnPickup;
      so.FindProperty("_vanishBehavior").enumValueIndex =
        (int)StaticPlacedItemVanishBehavior.Invisible;
      so.FindProperty("_autoLoadModel").boolValue = false;

      so.ApplyModifiedProperties();

      // 3. 원본 SceneItemPlacement 제거.
      Undo.DestroyObjectImmediate(placement);

      report.AppendLine($"  [ok]   {go.name}: item='{itemIdentifier}' x{stackCount}, entity='{entityIdentifier}'");
      return true;
    }

    private static void EnsureBoxCollider(GameObject go, StringBuilder report)
    {
      if (go.GetComponent<Collider>() != null)
        return; // 이미 콜라이더가 있으면 그대로 사용.

      var box = Undo.AddComponent<BoxCollider>(go);
      if (box == null)
      {
        // 프리팹 인스턴스 제약 등으로 실패 시 일반 AddComponent 폴백.
        box = go.AddComponent<BoxCollider>();
        if (box != null)
          Undo.RegisterCreatedObjectUndo(box, "Add BoxCollider");
      }

      if (box == null)
      {
        report.AppendLine($"  [warn] {go.name}: BoxCollider 추가 실패(상호작용 감지 불가 가능). 수동 확인 필요.");
        return;
      }

      FitBoxColliderToRenderers(go, box);
    }

    private static void FitBoxColliderToRenderers(GameObject go, BoxCollider box)
    {
      var renderers = go.GetComponentsInChildren<Renderer>(includeInactive: true);
      bool hasBounds = false;
      Bounds worldBounds = default;

      foreach (var r in renderers)
      {
        if (r == null)
          continue;

        if (!hasBounds)
        {
          worldBounds = r.bounds;
          hasBounds = true;
        }
        else
        {
          worldBounds.Encapsulate(r.bounds);
        }
      }

      var t = go.transform;
      if (!hasBounds)
      {
        box.center = Vector3.zero;
        box.size = Vector3.one * 0.25f;
        return;
      }

      box.center = t.InverseTransformPoint(worldBounds.center);
      var lossy = t.lossyScale;
      box.size = new Vector3(
        SafeDivide(worldBounds.size.x, lossy.x),
        SafeDivide(worldBounds.size.y, lossy.y),
        SafeDivide(worldBounds.size.z, lossy.z));
    }

    private static float SafeDivide(float value, float divisor)
      => Mathf.Approximately(divisor, 0f) ? value : value / divisor;

    private static string ReadSerializedString(Object target, string propertyPath)
    {
      var so = new SerializedObject(target);
      var prop = so.FindProperty(propertyPath);
      return prop != null ? prop.stringValue : string.Empty;
    }

    // ── 수집 헬퍼 ─────────────────────────────────────────────────────────

    private static List<SceneItemPlacement> CollectFromSelection()
    {
      var result = new List<SceneItemPlacement>();
      var seen = new HashSet<SceneItemPlacement>();

      foreach (var obj in Selection.gameObjects)
      {
        if (obj == null)
          continue;

        foreach (var placement in obj.GetComponentsInChildren<SceneItemPlacement>(includeInactive: true))
        {
          if (placement != null && seen.Add(placement))
            result.Add(placement);
        }
      }

      return result;
    }

    private static List<SceneItemPlacement> CollectFromActiveScene()
    {
      var result = new List<SceneItemPlacement>();

#if UNITY_2023_1_OR_NEWER
      var all = Object.FindObjectsByType<SceneItemPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
      var all = Object.FindObjectsOfType<SceneItemPlacement>(includeInactive: true);
#endif
      foreach (var placement in all)
      {
        if (placement != null && placement.gameObject.scene == SceneManager.GetActiveScene())
          result.Add(placement);
      }

      return result;
    }
  }
}
