using System;
using System.Linq;
using MultiplayerInfrastructure.ItemSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Editor.ItemSystem
{
  /// <summary>
  /// <see cref="SceneItemPlacementConverter"/> 를 Unity 에디터 GUI 없이(-batchmode) 실행하기 위한 진입점입니다.
  ///
  /// <para>
  /// OverworldScene 등 특정 씬에 남아 있는 <see cref="SceneItemPlacement"/>(grounded item, 물리 스폰) 를
  /// <see cref="StaticPlacedItem"/>(정적 배치 아이템) 으로 일괄 전환하고 씬을 저장합니다.
  /// 이미 <see cref="StaticPlacedItem"/> 이 함께 존재하는 GameObject 는 중복된
  /// <see cref="SceneItemPlacement"/> 만 제거(dedup)합니다(<see cref="SceneItemPlacementConverter"/> 참고).
  /// </para>
  ///
  /// <para>
  /// 실행 예시(레포 루트 기준):
  /// <code>
  /// /Applications/Unity/Hub/Editor/6000.2.8f1/Unity.app/Contents/MacOS/Unity \
  ///   -batchmode -nographics -quit -projectPath "$(pwd)" \
  ///   -executeMethod MultiplayerInfrastructure.Editor.ItemSystem.SceneItemPlacementBatchConverter.ConvertOverworldScene \
  ///   -logFile -
  /// </code>
  /// </para>
  ///
  /// <para>
  /// 씬 경로를 바꿔 재사용하려면 <c>ConvertScene(string scenePath)</c> 를 다른 -executeMethod 진입점으로 감싸거나
  /// 커맨드라인 인자(<see cref="Environment.GetCommandLineArgs"/>)로 확장할 수 있습니다.
  /// </para>
  /// </summary>
  internal static class SceneItemPlacementBatchConverter
  {
    private const string DefaultOverworldScenePath = "Assets/Scenes/OverworldScene.unity";

    /// <summary>
    /// -executeMethod 진입점. OverworldScene 을 열고 전체 변환을 수행한 뒤 씬을 저장합니다.
    /// </summary>
    public static void ConvertOverworldScene()
    {
      ConvertScene(DefaultOverworldScenePath);
    }

    /// <summary>
    /// 지정된 씬 경로를 열고 <see cref="SceneItemPlacement"/> → <see cref="StaticPlacedItem"/> 전환을 실행합니다.
    /// 배치 모드에서는 대화상자를 띄우지 않고 콘솔 로그(-logFile)로만 결과를 보고합니다.
    /// </summary>
    public static void ConvertScene(string scenePath)
    {
      if (string.IsNullOrWhiteSpace(scenePath))
      {
        Debug.LogError("[SceneItemPlacementBatchConverter] scenePath 가 비어 있습니다.");
        EditorApplication.Exit(1);
        return;
      }

      Scene scene;
      try
      {
        scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[SceneItemPlacementBatchConverter] 씬을 여는 중 오류가 발생했습니다: '{scenePath}'. {ex}");
        EditorApplication.Exit(1);
        return;
      }

      if (!scene.IsValid())
      {
        Debug.LogError($"[SceneItemPlacementBatchConverter] 씬을 열지 못했습니다: '{scenePath}'.");
        EditorApplication.Exit(1);
        return;
      }

      var placements = SceneItemPlacementConverter.CollectFromActiveScene();
      Debug.Log(
        $"[SceneItemPlacementBatchConverter] '{scenePath}' 에서 SceneItemPlacement {placements.Count}개를 발견했습니다.");

      if (placements.Count == 0)
      {
        Debug.Log("[SceneItemPlacementBatchConverter] 변환 대상이 없습니다. 종료합니다.");
        return;
      }

      var result = SceneItemPlacementConverter.RunConversionCore(placements, $"'{scenePath}'의 SceneItemPlacement (batch)");

      Debug.Log(
        $"[SceneItemPlacementBatchConverter] 변환 완료: 성공 {result.Converted}개, 건너뜀(dedup) {result.Skipped}개, " +
        $"실패 {result.Failed}개.\n{result.Report}");

      if (result.Failed > 0)
      {
        Debug.LogError($"[SceneItemPlacementBatchConverter] 실패 {result.Failed}건 발생. 씬을 저장하지 않고 종료합니다.");
        EditorApplication.Exit(1);
        return;
      }

      bool saved = EditorSceneManager.SaveScene(scene);
      if (!saved)
      {
        Debug.LogError($"[SceneItemPlacementBatchConverter] 씬 저장에 실패했습니다: '{scenePath}'.");
        EditorApplication.Exit(1);
        return;
      }

      Debug.Log($"[SceneItemPlacementBatchConverter] 씬 저장 완료: '{scenePath}'.");

      // 변환 후 검증: SceneItemPlacement 가 완전히 제거되었는지 재확인.
      var remaining = SceneItemPlacementConverter.CollectFromActiveScene();
      if (remaining.Count > 0)
      {
        Debug.LogError(
          $"[SceneItemPlacementBatchConverter] 변환 후에도 SceneItemPlacement {remaining.Count}개가 남아 있습니다: " +
          $"{string.Join(", ", remaining.Where(p => p != null).Select(p => p.gameObject.name))}");
        EditorApplication.Exit(1);
      }
    }
  }
}
