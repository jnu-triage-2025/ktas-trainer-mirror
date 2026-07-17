using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public static class ScenarioRuntimeSceneReadiness
  {
    private static readonly HashSet<int> LoadedScenes = new HashSet<int>();
    private static readonly HashSet<int> ReadyScenes = new HashSet<int>();
    public static event Action<Scene> SceneReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration() { LoadedScenes.Clear(); ReadyScenes.Clear(); SceneReady = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() { SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneUnloaded -= OnSceneUnloaded; SceneManager.sceneLoaded += OnSceneLoaded; SceneManager.sceneUnloaded += OnSceneUnloaded; }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => LoadedScenes.Add(scene.handle);
    private static void OnSceneUnloaded(Scene scene) { LoadedScenes.Remove(scene.handle); ReadyScenes.Remove(scene.handle); }

    public static void MarkReady(Scene scene)
    {
      if (!scene.IsValid() || !scene.isLoaded) return;
      // The scene that was already active before this subsystem subscribed to
      // sceneLoaded (e.g. the process boot scene) never raised sceneLoaded, so
      // it would otherwise be missing from LoadedScenes and could never be
      // marked ready.  Register any valid loaded scene here so readiness works
      // for the startup scene as well.
      LoadedScenes.Add(scene.handle);
      if (!ReadyScenes.Add(scene.handle)) return;
      SceneReady?.Invoke(scene);
    }

    public static bool IsReady(Scene scene) => scene.IsValid() && ReadyScenes.Contains(scene.handle);
    public static bool IsLoaded(Scene scene) => scene.IsValid() && LoadedScenes.Contains(scene.handle);
    public static void Clear(Scene scene) => ReadyScenes.Remove(scene.handle);
  }
}
