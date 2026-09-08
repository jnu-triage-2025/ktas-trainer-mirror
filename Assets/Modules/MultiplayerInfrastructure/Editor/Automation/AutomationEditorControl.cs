using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Automation.Editor
{
  [InitializeOnLoad]
  public static class AutomationEditorControl
  {
    private static HttpListener _listener;
    private static readonly ConcurrentQueue<(string type, TaskCompletionSource<JObject> result)> Pending = new();
    private const string SessionKey = "UnityE2E.EditorControl";
    private const string TokenKey = "UnityE2E.EditorControl.Token";
    static AutomationEditorControl()
    {
      AssemblyReloadEvents.beforeAssemblyReload += StopListener;
      EditorApplication.quitting += Disable;
      if (SessionState.GetBool(SessionKey, false)) EditorApplication.delayCall += Enable;
    }
    [MenuItem("Tools/E2E/Enable editor control")]
    public static void Enable()
    {
      if (_listener != null) return;
      string token = SessionState.GetString(TokenKey, "");
      if (token.Length < 32) { token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"); SessionState.SetString(TokenKey, token); }
      var profile = Path.GetFullPath("Temp/e2e-editor-profile");
      Directory.CreateDirectory(profile);
      Environment.SetEnvironmentVariable("UNITY_E2E_EDITOR", "1");
      Environment.SetEnvironmentVariable("UNITY_E2E_TOKEN", token);
      Environment.SetEnvironmentVariable("UNITY_E2E_INSTANCE", "editor");
      Environment.SetEnvironmentVariable("UNITY_E2E_RUN", Guid.NewGuid().ToString("N"));
      Environment.SetEnvironmentVariable("UNITY_E2E_PORT", "17891");
      Environment.SetEnvironmentVariable("UNITY_E2E_PROFILE", profile);
      _listener = new HttpListener(); _listener.Prefixes.Add("http://127.0.0.1:17892/");
      try { _listener.Start(); }
      catch { _listener = null; throw; }
      File.WriteAllText("Temp/e2e-editor-connection.json", new JObject { ["instanceId"] = "editor", ["runId"] = Environment.GetEnvironmentVariable("UNITY_E2E_RUN"),
        ["port"] = 17891, ["editorPort"] = 17892, ["token"] = token, ["role"] = "editor" }.ToString());
      SessionState.SetBool(SessionKey, true);
      EditorApplication.update -= Tick; EditorApplication.update += Tick;
      _ = Listen(_listener, token);
      Debug.Log("Editor E2E control enabled. Connection information: Temp/e2e-editor-connection.json");
    }
    [MenuItem("Tools/E2E/Disable editor control")]
    public static void Disable()
    {
      SessionState.SetBool(SessionKey, false); StopListener();
      Environment.SetEnvironmentVariable("UNITY_E2E_EDITOR", null);
      if (File.Exists("Temp/e2e-editor-connection.json")) File.Delete("Temp/e2e-editor-connection.json");
    }
    private static async Task Listen(HttpListener listener, string token)
    {
      while (listener.IsListening)
      {
        try { var context = await listener.GetContextAsync(); _ = Serve(context, token); }
        catch (Exception) { break; }
      }
    }
    private static async Task Serve(HttpListenerContext context, string token)
    {
      try
      {
        if (context.Request.HttpMethod != "POST" || context.Request.Url.AbsolutePath != "/command"
          || context.Request.Headers["Origin"] != null || context.Request.Headers["Authorization"] != "Bearer " + token
          || context.Request.RemoteEndPoint == null || !IPAddress.IsLoopback(context.Request.RemoteEndPoint.Address))
        { context.Response.StatusCode = 403; return; }
        if (context.Request.ContentLength64 < 0 || context.Request.ContentLength64 > 4096 || Pending.Count >= 16)
        { context.Response.StatusCode = 413; return; }
        using var reader = new StreamReader(context.Request.InputStream);
        var message = JObject.Parse(await reader.ReadToEndAsync());
        var result = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        Pending.Enqueue(((string)message["type"], result));
        if (await Task.WhenAny(result.Task, Task.Delay(10000)) != result.Task) { result.TrySetCanceled(); context.Response.StatusCode = 408; return; }
        var bytes = Encoding.UTF8.GetBytes((await result.Task).ToString());
        context.Response.ContentType = "application/json"; await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
      }
      catch { context.Response.StatusCode = 400; }
      finally { context.Response.Close(); }
    }
    private static void Tick()
    {
      if (!Pending.TryDequeue(out var request) || request.result.Task.IsCompleted) return;
      try
      {
        switch (request.type)
        {
          case "editor.play":
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) throw new InvalidOperationException("EDITOR_BUSY");
            if (!EditorApplication.isPlaying) {
              Environment.SetEnvironmentVariable("UNITY_E2E_RUN", Guid.NewGuid().ToString("N"));
              var connection = JObject.Parse(File.ReadAllText("Temp/e2e-editor-connection.json"));
              connection["runId"] = Environment.GetEnvironmentVariable("UNITY_E2E_RUN");
              File.WriteAllText("Temp/e2e-editor-connection.json", connection.ToString());
            }
            EditorApplication.isPlaying = true; break;
          case "editor.stop": EditorApplication.isPlaying = false; break;
#if UNITY_INCLUDE_TESTS
          case "editor.tests":
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("EDITOR_BUSY");
            AutomationTestMenu.Run(); break;
#endif
          case "editor.observe": break;
          default: throw new InvalidOperationException("UNSUPPORTED_CAPABILITY");
        }
        request.result.TrySetResult(new JObject { ["ok"] = true, ["playing"] = EditorApplication.isPlaying,
          ["compiling"] = EditorApplication.isCompiling, ["updating"] = EditorApplication.isUpdating,
          ["scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path });
      }
      catch (Exception ex) { request.result.TrySetResult(new JObject { ["ok"] = false, ["error"] = ex.Message }); }
    }
    private static void StopListener()
    {
      EditorApplication.update -= Tick;
      _listener?.Close(); _listener = null;
      while (Pending.TryDequeue(out var request)) request.result.TrySetResult(new JObject { ["ok"] = false, ["error"] = "EDITOR_RELOADING" });
    }
  }
}
