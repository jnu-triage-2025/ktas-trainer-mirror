#if UNITY_INCLUDE_TESTS
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace MultiplayerInfrastructure.Automation.Editor
{
  public static class AutomationTestMenu
  {
    private static TestRunnerApi _api;
    [MenuItem("Tools/E2E/Run input adapter tests")]
    public static void Run()
    {
      _api = ScriptableObject.CreateInstance<TestRunnerApi>();
      _api.RegisterCallbacks(new Results());
      _api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
        testNames = new[] { "MultiplayerInfrastructure.Automation.Editor.AutomationInputTests" } }));
    }
    private sealed class Results : ICallbacks
    {
      public void RunStarted(ITestAdaptor testsToRun) { }
      public void TestStarted(ITestAdaptor test) { }
      public void TestFinished(ITestResultAdaptor result) { }
      public void RunFinished(ITestResultAdaptor result)
      {
        Directory.CreateDirectory("artifacts/unity-e2e");
        TestRunnerApi.SaveResultToFile(result, "artifacts/unity-e2e/input-tests.xml");
        Debug.Log($"E2E input tests: passed={result.PassCount}, failed={result.FailCount}");
        Object.DestroyImmediate(_api); _api = null;
      }
    }
  }
}
#endif
