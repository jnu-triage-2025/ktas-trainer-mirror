#if UNITY_E2E || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FishNet;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Automation
{
  [DefaultExecutionOrder(-32000)]
  public sealed class AutomationBridge : MonoBehaviour
  {
    private sealed class Request
    {
      internal JObject Message;
      internal double Deadline;
      internal double ReceivedAt, DispatchedAt, CompletedAt;
      internal int InputGeneration;
      internal bool OwnsInput;
      internal readonly TaskCompletionSource<JObject> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private readonly ConcurrentQueue<Request> _queue = new();
    private readonly Dictionary<string, (string fingerprint, Request request)> _commands = new();
    private readonly HashSet<string> _retiredMutations = new();
    private readonly Queue<string> _completed = new();
    private readonly Queue<JObject> _events = new();
    private readonly AutomationInputSource _input = new();
    private readonly AutomationUI _ui = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private HttpListener _listener;
    private CancellationTokenSource _stop;
    private string _owner = "None";
    private long _epoch;
    private long _sequence;
    private double _lease;
    private int _pending;
    private int _queued;
    private int _inputGeneration;
    private Request _activeInput;
    private Request _activeCapture;
    private ScenarioController _scenario;
    private const int EventCapacity = 4096;
    private double _performanceAt, _lastFps, _peakFrameMs, _lastPeakFrameMs;
    private int _performanceFrames;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { PlayerInput.Override = null; ProfilePlayerPrefs.ResetCache(); AutomationContent.Reset(); AutomationFixtures.Reset(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
      if (!AutomationConfiguration.Valid) return;
      AutomationFixtures.Register();
      var go = new GameObject("Unity E2E Bridge");
      DontDestroyOnLoad(go);
      go.AddComponent<AutomationBridge>();
    }
    private void Awake()
    {
      _stop = new CancellationTokenSource();
      _listener = new HttpListener();
      _listener.Prefixes.Add($"http://127.0.0.1:{AutomationConfiguration.Port}/");
      try { _listener.Start(); }
      catch { Destroy(gameObject); throw; }
      Application.runInBackground = true;
      Application.logMessageReceived += OnLog;
      AutomationEvents.Published += Emit;
      ScenarioSignalParameterStore.OnValueRecorded += OnSignalParameterRecorded;
      ScenarioInteractionSignals.OnSignalRegistered += OnSignalRegistered;
      ScenarioInteractionSignals.OnSignalCleared += OnSignalCleared;
      Quest.PlayerQuestStateFlagService.FlagsChanged += OnQuestFlagsChanged;
      _ = Task.Run(Listen);
    }
    private async Task Listen()
    {
      while (!_stop.IsCancellationRequested)
      {
        try { var context = await _listener.GetContextAsync().ConfigureAwait(false); _ = Serve(context); }
        catch (Exception) when (_stop.IsCancellationRequested) { break; }
      }
    }
    private async Task Serve(HttpListenerContext context)
    {
      bool counted = false;
      double serveStarted = _clock.Elapsed.TotalSeconds;
      try
      {
        if (context.Request.RemoteEndPoint == null || !IPAddress.IsLoopback(context.Request.RemoteEndPoint.Address)
          || context.Request.Headers["Authorization"] != "Bearer " + AutomationConfiguration.Token
          || context.Request.Headers["Origin"] != null)
        { context.Response.StatusCode = 403; return; }
        if (context.Request.HttpMethod != "POST" || context.Request.Url.AbsolutePath != "/command")
        { context.Response.StatusCode = 404; return; }
        if (Interlocked.Increment(ref _pending) > 128) { Interlocked.Decrement(ref _pending); context.Response.StatusCode = 429; return; }
        counted = true;
        if (context.Request.ContentLength64 < 0 || context.Request.ContentLength64 > 65536)
        { context.Response.StatusCode = 413; return; }
        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
        var message = JObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(false));
        var ttl = (int?)message["ttlMs"] ?? 3000;
        if (ttl < 1 || ttl > 30000) throw new ArgumentException("INVALID_ARGUMENT");
        var request = new Request { ReceivedAt = _clock.Elapsed.TotalSeconds, Message = message, InputGeneration = Volatile.Read(ref _inputGeneration), Deadline = _clock.Elapsed.TotalSeconds + ttl / 1000d };
        if (Interlocked.Increment(ref _queued) > 128) { Interlocked.Decrement(ref _queued); context.Response.StatusCode = 429; return; }
        _queue.Enqueue(request);
        var winner = await Task.WhenAny(request.Result.Task, Task.Delay(ttl, _stop.Token)).ConfigureAwait(false);
        var result = winner == request.Result.Task ? await request.Result.Task.ConfigureAwait(false) : Error("DEADLINE_EXCEEDED");
        double serializeStarted = _clock.Elapsed.TotalSeconds;
        var bytes = Encoding.UTF8.GetBytes(result.ToString(Newtonsoft.Json.Formatting.None));
        double writeStarted = _clock.Elapsed.TotalSeconds;
        context.Response.Headers["X-E2E-Serialize-Ms"] = ((writeStarted - serializeStarted) * 1000d).ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Response.Headers["X-E2E-Serve-Ms"] = ((writeStarted - serveStarted) * 1000d).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (winner == request.Result.Task && request.CompletedAt > 0)
          context.Response.Headers["X-E2E-Resume-Ms"] = (Math.Max(0, serializeStarted - request.CompletedAt) * 1000d).ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
      }
      catch (Exception) { context.Response.StatusCode = 400; }
      finally { if (counted) Interlocked.Decrement(ref _pending); context.Response.Close(); }
    }
    private void Update()
    {
      double now = _clock.Elapsed.TotalSeconds;
      if (_owner != "None" && now >= _lease) Quiesce("None");
      _ui.ExpirePointer(Time.realtimeSinceStartupAsDouble);
      _input.BeginFrame(now, Time.unscaledDeltaTime);
      _performanceFrames++; _peakFrameMs = Math.Max(_peakFrameMs, Time.unscaledDeltaTime * 1000d);
      if (now - _performanceAt >= 1) {
        _lastFps = _performanceFrames / (now - _performanceAt); _lastPeakFrameMs = _peakFrameMs;
        _performanceAt = now; _performanceFrames = 0; _peakFrameMs = 0;
        Emit("performance.sample", "process", Performance());
      }
      for (int i = 0; i < 16 && _queue.TryDequeue(out var request); i++) { Interlocked.Decrement(ref _queued); Dispatch(request); }
      _scenario = ScenarioController.Instance;
    }
    private void LateUpdate()
    {
      if (!_input.DownKeys.Any() && !_input.UpKeys.Any()) return;
      var local = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsOwner);
      Emit("input.frame", "automation", new JObject {
        ["down"] = new JArray(_input.DownKeys.Select(key => key.ToString())),
        ["up"] = new JArray(_input.UpKeys.Select(key => key.ToString())),
        ["context"] = UIOverlayStack.Top?.GetType().Name ?? "Gameplay",
        ["dialogueBinding"] = local?.AutomationDialogueBinding
      });
    }
    private void Dispatch(Request request)
    {
      request.DispatchedAt = _clock.Elapsed.TotalSeconds;
      var m = request.Message;
      string id = (string)m["commandId"];
      if (string.IsNullOrEmpty(id) || id.Length > 128 || (string)m["protocolVersion"] != "1.0"
        || (string)m["instanceId"] != AutomationConfiguration.InstanceId || (string)m["runId"] != AutomationConfiguration.RunId)
      { request.Result.TrySetResult(Error("INVALID_ARGUMENT")); return; }
      string fingerprint = m.ToString(Newtonsoft.Json.Formatting.None);
      if (_commands.TryGetValue(id, out var cached))
      {
        if (cached.fingerprint != fingerprint) request.Result.TrySetResult(Error("STATE_CONFLICT"));
        else _ = Forward(cached.request, request);
        return;
      }
      if (_retiredMutations.Contains(id)) { request.Result.TrySetResult(Error("COMMAND_RESULT_EXPIRED")); return; }
      if (_retiredMutations.Count >= 65536) { request.Result.TrySetResult(Error("SESSION_CAPACITY_EXCEEDED")); return; }
      _commands[id] = (fingerprint, request);
      if (IsInputMutation((string)m["type"]))
      {
        if (_activeInput != null) { Finish(request, Error("INPUT_BUSY")); return; }
        _activeInput = request; request.OwnsInput = true;
      }
      if ((string)m["type"] == "game.screenshot")
      {
        if (_activeCapture != null) { Finish(request, Error("CAPTURE_BUSY")); return; }
        _activeCapture = request;
      }
      StartCoroutine(Execute(request));
    }
    private static async Task Forward(Request previous, Request next) => next.Result.TrySetResult(await previous.Result.Task.ConfigureAwait(false));
    private IEnumerator Execute(Request request)
    {
      JObject output = null;
      IEnumerator operation;
      try { operation = Apply(request, value => output = value); }
      catch (Exception ex) { Finish(request, Error(ex.Message)); yield break; }
      while (true)
      {
        bool moved;
        object current = null;
        try { moved = operation.MoveNext(); if (moved) current = operation.Current; }
        catch (Exception ex) { Finish(request, Error(ex.Message)); yield break; }
        if (!moved) break;
        yield return current;
      }
      Finish(request, new JObject { ["ok"] = true, ["status"] = "completed", ["result"] = output ?? new JObject() });
    }
    private void Finish(Request request, JObject result)
    {
      string id = (string)request.Message["commandId"];
      result["commandId"] = id;
      result["controlEpoch"] = _epoch;
      result["frame"] = Time.frameCount;
      result["bridgeTimings"] = new JObject {
        ["queueMs"] = Math.Max(0, request.DispatchedAt - request.ReceivedAt) * 1000d,
        ["processingMs"] = Math.Max(0, _clock.Elapsed.TotalSeconds - request.DispatchedAt) * 1000d
      };
      request.CompletedAt = _clock.Elapsed.TotalSeconds;
      request.Result.TrySetResult(result);
      if (request.OwnsInput && request.InputGeneration == _inputGeneration && !(bool)result["ok"] && (long?)request.Message["controlEpoch"] == _epoch
        && (string)request.Message["owner"] == _owner && IsInputMutation((string)request.Message["type"]))
      { _input.ReleaseAll(); _ui.ReleasePointer(); }
      if (_activeInput == request) _activeInput = null;
      if (_activeCapture == request) _activeCapture = null;
      _completed.Enqueue(id);
      while (_completed.Count > 2048)
      {
        var old = _completed.Dequeue();
        if (_commands.TryGetValue(old, out var entry) && !IsReadOnly((string)entry.request.Message["type"])) _retiredMutations.Add(old);
        _commands.Remove(old);
      }
    }
    private static bool IsReadOnly(string type) => type == "game.observe" || type == "game.catalogue" || type == "ui.query" || type == "events.read" || type == "game.screenshot";
    private static bool IsInputMutation(string type) => type == "input.execute" || type == "ui.activate" || type == "ui.pointer" || type == "ui.text";
    private void Guard(Request request, bool control)
    {
      if (_clock.Elapsed.TotalSeconds >= request.Deadline) throw new InvalidOperationException("DEADLINE_EXCEEDED");
      if (IsInputMutation((string)request.Message["type"]) && request.InputGeneration != _inputGeneration) throw new InvalidOperationException("CANCELLED");
      if (control && (long?)request.Message["controlEpoch"] != _epoch) throw new InvalidOperationException("STALE_CONTROL_EPOCH");
      if (control && (_owner == "None" || (string)request.Message["owner"] != _owner)) throw new InvalidOperationException("CONTROL_DENIED");
    }
    private IEnumerator Apply(Request request, Action<JObject> result)
    {
      var m = request.Message;
      string type = (string)m["type"];
      var p = m["payload"] as JObject ?? new JObject();
      bool readOnly = IsReadOnly(type);
      Guard(request, !readOnly && type != "control.acquire");
      switch (type)
      {
        case "game.observe": result(Observe(p)); break;
        case "game.catalogue":
          var catalogue = AutomationContent.Catalogue();
          if ((bool?)p["includeMemoryResources"] == true) catalogue["memoryResources"] = AutomationContent.MemoryResources();
          result(catalogue); break;
        case "protocol.signal_raise":
          if (!AutomationConfiguration.AllowProtocolTests) throw new InvalidOperationException("PROTOCOL_TESTS_DISABLED");
          if (p.Properties().Any(property => property.Name != "signalId" && property.Name != "parameterJson" && property.Name != "count")
            || p["signalId"]?.Type != JTokenType.String || ((string)p["signalId"]).Length < 1 || ((string)p["signalId"]).Length > 1024
            || (p["parameterJson"] != null && (p["parameterJson"].Type != JTokenType.String || ((string)p["parameterJson"]).Length > 16384))
            || (p["count"] != null && (p["count"].Type != JTokenType.Integer || (long)p["count"] < 1 || (long)p["count"] > 64)))
            throw new ArgumentException("INVALID_ARGUMENT");
          int signalCount = (int?)p["count"] ?? 1;
          ScenarioNetworkRelay.AutomationSendClientSignal((string)p["signalId"], (string)p["parameterJson"], signalCount);
          var sentSignal = new JObject { ["signalId"] = p["signalId"], ["sent"] = signalCount, ["mode"] = "protocol_test" };
          AutomationEvents.Publish("protocol.sent", "client", sentSignal);
          result(sentSignal);
          break;
        case "fixture.scenario_start":
          if (!AutomationConfiguration.AllowScenarioFixtures) throw new InvalidOperationException("FIXTURE_DISABLED");
          string graphId = (string)p["graphId"];
          if (p.Properties().Any(property => property.Name != "graphId" && property.Name != "ownerId")
            || p["graphId"]?.Type != JTokenType.String || string.IsNullOrEmpty(graphId) || graphId.Length > 180
            || !System.Text.RegularExpressions.Regex.IsMatch(graphId, @"\A[A-Za-z0-9_.-]+\z")
            || p["ownerId"]?.Type != JTokenType.Integer || (long)p["ownerId"] < 0 || (long)p["ownerId"] > int.MaxValue)
            throw new ArgumentException("INVALID_ARGUMENT");
          var chat = FindAnyObjectByType<Chat.ChatService>();
          if (chat == null || !chat.IsServerInitialized) throw new InvalidOperationException("NOT_SERVER");
          if (_scenario != null && _scenario.IsActive) throw new InvalidOperationException("SCENARIO_BUSY");
          int fixtureOwner = (int)p["ownerId"];
          if (!Session.UserDescriptorService.TryGetByClientId(fixtureOwner, out _)) throw new InvalidOperationException("UNKNOWN_PLAYER");
          if (!Registry.Registry.Contains(Registry.RegistryType.ScenarioGraph, graphId)
            || !Registry.Registry.TryGetScenarioGraph(graphId, out ScenarioGraph fixtureGraph, out _))
            throw new InvalidOperationException("FIXTURE_GRAPH_INVALID");
          bool accepted = chat.TryExecuteSystemCommand($"scenario execute fish:{fixtureOwner} {graphId}", out string fixtureResult);
          AutomationEvents.Publish("fixture.scenario_start", "server", new JObject {
            ["graphId"] = graphId, ["ownerId"] = fixtureOwner, ["accepted"] = accepted,
            ["mode"] = "setup_bypass", ["message"] = fixtureResult });
          if (!accepted) throw new InvalidOperationException("FIXTURE_FAILED");
          result(new JObject { ["accepted"] = true, ["mode"] = "setup_bypass", ["message"] = fixtureResult });
          break;
        case "ui.query": result(new JObject { ["elements"] = AutomationUI.Query(p), ["inputDiagnostics"] = _ui.Diagnostics() }); break;
        case "control.acquire":
          if (_owner != "None") throw new InvalidOperationException("CONTROL_DENIED");
          string owner = (string)p["owner"];
          if (owner != "Automation" && owner != "RemoteHuman") throw new ArgumentException("INVALID_ARGUMENT");
          Quiesce(owner); result(Control()); break;
        case "control.heartbeat": _lease = _clock.Elapsed.TotalSeconds + AutomationConfiguration.ControlLeaseMs / 1000d; result(Control()); break;
        case "control.handoff":
          string next = (string)p["owner"];
          if (next != "Automation" && next != "RemoteHuman" && next != "None") throw new ArgumentException("INVALID_ARGUMENT");
          Quiesce(next); result(Control()); break;
        case "input.release_all": Interlocked.Increment(ref _inputGeneration); _activeInput = null; _input.ReleaseAll(); _ui.ReleasePointer(); break;
        case "input.execute":
          if (p["expectedPresentationRevision"] != null)
          {
            var dialogue = FindAnyObjectByType<DialoguePanelUIController>();
            if (dialogue == null || (long)dialogue.AutomationSnapshot()["presentationRevision"] != (long)p["expectedPresentationRevision"])
              throw new InvalidOperationException("STATE_CONFLICT");
          }
          var steps = p["sequence"] as JArray;
          if (steps == null || steps.Count == 0 || steps.Count > 128) throw new ArgumentException("INVALID_ARGUMENT");
          ValidateInput(steps);
          foreach (JObject step in steps)
          {
            Guard(request, true);
            string op = (string)step["operation"];
            if (op == "wait")
            {
              double until = _clock.Elapsed.TotalSeconds + (int)step["durationMs"] / 1000d;
              while (_clock.Elapsed.TotalSeconds < until) { Guard(request, true); yield return null; }
              Guard(request, true);
            }
            else if (op == "lookDelta") _input.Look(new Vector2((float)step["x"], (float)step["y"]));
            else if (op == "scroll")
            {
              var notches = new Vector2((float?)step["x"] ?? 0f, (float?)step["y"] ?? 0f);
              _input.Scroll(notches);
              _ui.Scroll(notches);
            }
            else if (op == "interactionSelect")
            {
              var localPlayer = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).FirstOrDefault(player => player.IsOwner);
              if (localPlayer == null) throw new InvalidOperationException("PLAYER_NOT_READY");
              localPlayer.AutomationSelectInteraction((int)step["index"]);
            }
            else if (op == "interactionExecute")
            {
              var localPlayer = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).FirstOrDefault(player => player.IsOwner);
              if (localPlayer == null) throw new InvalidOperationException("PLAYER_NOT_READY");
              localPlayer.AutomationExecuteInteraction((int)step["index"],
                (string)step["expectedEntityId"], (string)step["expectedInteractionId"]);
            }
            else if (op == "hotbarSelect")
            {
              var localPlayer = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).FirstOrDefault(player => player.IsOwner);
              if (localPlayer == null) throw new InvalidOperationException("PLAYER_NOT_READY");
              localPlayer.AutomationSelectHotbarSlot((int)step["index"]);
            }
            else
            {
              if (!Enum.TryParse((string)step["key"], out KeyCode key) || !Enum.IsDefined(typeof(KeyCode), key)) throw new ArgumentException("INVALID_ARGUMENT");
              int duration = (int?)step["durationMs"] ?? 100;
              if (duration < 1 || duration > 2000) throw new ArgumentException("INVALID_ARGUMENT");
              if (op != "press" && op != "release" && op != "tap" && op != "hold") throw new ArgumentException("INVALID_ARGUMENT");
              _input.Enqueue(key, op != "release", Math.Min(request.Deadline, _clock.Elapsed.TotalSeconds + 2));
              if (op == "tap" || op == "hold")
              {
                // Wait for a published down frame, then retain it until the requested duration.
                yield return null;
                Guard(request, true);
                if (!_input.GetKey(key)) throw new InvalidOperationException("INPUT_NOT_APPLIED");
                double until = _clock.Elapsed.TotalSeconds + duration / 1000d;
                _input.Renew(key, Math.Min(request.Deadline, until));
                while (_clock.Elapsed.TotalSeconds < until) { Guard(request, true); yield return null; }
                Guard(request, true);
                _input.Enqueue(key, false, 0);
              }
            }
            yield return null;
          }
          break;
        case "ui.activate":
          if ((string)p["mode"] != "device_input") throw new InvalidOperationException("UNSUPPORTED_CAPABILITY");
          // EventSystem skips Process on the frame where its active input module changes.
          // Prepare the module and pointer before queuing the single button-down transition.
          _ui.Activate(p, false); yield return null; Guard(request, true);
          _ui.Activate(p); yield return null; Guard(request, true); _ui.ReleasePointer(); yield return null; break;
        case "ui.pointer":
          if ((p["screenWidth"] != null && (int)p["screenWidth"] != Screen.width) || (p["screenHeight"] != null && (int)p["screenHeight"] != Screen.height)
            || (p["frame"] != null && ((int)p["frame"] > Time.frameCount || Time.frameCount - (int)p["frame"] > 90))) throw new InvalidOperationException("STATE_CONFLICT");
          var pointer = new Vector2((float)p["x"], (float)p["y"]);
          if (!_ui.IsReady) { _ui.Move(pointer, false); yield return null; Guard(request, true); }
          var pointerTarget = AutomationUI.PointerButtonTarget(pointer);
          _input.Pointer(pointer);
          _ui.Move(pointer, (bool?)p["pressed"] ?? false); yield return null;
          result(new JObject { ["target"] = pointerTarget }); break;
        case "ui.text":
          if ((string)p["mode"] != "input_adapter") throw new InvalidOperationException("UNSUPPORTED_CAPABILITY");
          string text = (string)p["text"];
          if (text == null || text.Length > 1024) throw new ArgumentException("INVALID_ARGUMENT");
          _ui.Text(text); yield return null; result(new JObject { ["mode"] = "input_adapter", ["characters"] = text.Length }); break;
        case "game.screenshot":
          yield return new WaitForEndOfFrame(); Guard(request, false);
          var captureClock = System.Diagnostics.Stopwatch.StartNew();
          var capture = ScreenCapture.CaptureScreenshotAsTexture();
          double readbackMs = captureClock.Elapsed.TotalMilliseconds;
          try {
            var jpeg = capture.EncodeToJPG(65);
            double encodedMs = captureClock.Elapsed.TotalMilliseconds;
            var data = Convert.ToBase64String(jpeg);
            result(new JObject { ["frame"] = Time.frameCount, ["mimeType"] = "image/jpeg", ["data"] = data,
              ["width"] = capture.width, ["height"] = capture.height,
              ["captureTimings"] = new JObject { ["queueMs"] = (request.DispatchedAt - request.ReceivedAt) * 1000d,
                ["frameWaitMs"] = (_clock.Elapsed.TotalSeconds - request.DispatchedAt) * 1000d - captureClock.Elapsed.TotalMilliseconds,
                ["readbackMs"] = readbackMs,
                ["encodeMs"] = encodedMs - readbackMs, ["base64Ms"] = captureClock.Elapsed.TotalMilliseconds - encodedMs } });
          }
          finally { Destroy(capture); }
          break;
        case "events.read":
          long after = (long?)p["cursor"] ?? 0;
          if (_events.Count > 0 && after < (long)_events.Peek()["eventSequence"] - 1) throw new InvalidOperationException("OBSERVATION_GAP");
          result(new JObject { ["events"] = new JArray(_events.Where(e => (long)e["eventSequence"] > after)), ["cursor"] = _sequence }); break;
        default: throw new InvalidOperationException("UNSUPPORTED_CAPABILITY");
      }
    }
    private static void ValidateInput(JArray sequence)
    {
      foreach (var token in sequence)
      {
        if (token is not JObject step) throw new ArgumentException("INVALID_ARGUMENT");
        string operation = (string)step["operation"];
        if (operation == "wait")
        {
          var duration = step["durationMs"];
          if (duration == null || duration.Type != JTokenType.Integer || (double)duration < 1 || (double)duration > 2000)
            throw new ArgumentException("INVALID_ARGUMENT");
        }
        else if (operation == "interactionSelect" || operation == "interactionExecute" || operation == "hotbarSelect")
        {
          var index = step["index"];
          if (index == null || index.Type != JTokenType.Integer || (long)index < 0 || (long)index > 200)
            throw new ArgumentException("INVALID_ARGUMENT");
          foreach (string field in new[] { "expectedEntityId", "expectedInteractionId" })
          {
            var expected = step[field];
            if (expected != null && (operation != "interactionExecute" || expected.Type != JTokenType.String
              || string.IsNullOrWhiteSpace((string)expected) || ((string)expected).Length > 180))
              throw new ArgumentException("INVALID_ARGUMENT");
          }
        }
        else if (operation == "lookDelta" || operation == "scroll")
        {
          var coordinates = new[] { "x", "y" };
          foreach (var coordinate in coordinates)
          {
            var value = step[coordinate];
            if (operation == "scroll" && value == null) continue;
            if (value == null || (value.Type != JTokenType.Integer && value.Type != JTokenType.Float)) throw new ArgumentException("INVALID_ARGUMENT");
            double number = (double)value;
            if (double.IsNaN(number) || double.IsInfinity(number) || Math.Abs(number) > 10000) throw new ArgumentException("INVALID_ARGUMENT");
          }
          if (operation == "scroll" && step["x"] == null && step["y"] == null) throw new ArgumentException("INVALID_ARGUMENT");
        }
        else
        {
          if (operation != "press" && operation != "release" && operation != "tap" && operation != "hold") throw new ArgumentException("INVALID_ARGUMENT");
          if (!Enum.TryParse((string)step["key"], out KeyCode key) || !Enum.IsDefined(typeof(KeyCode), key) || key == KeyCode.None) throw new ArgumentException("INVALID_ARGUMENT");
          int duration = (int?)step["durationMs"] ?? 100;
          if (duration < 1 || duration > 2000) throw new ArgumentException("INVALID_ARGUMENT");
        }
      }
    }
    private JObject Control() => new() { ["owner"] = _owner, ["controlEpoch"] = _epoch, ["leaseMs"] = AutomationConfiguration.ControlLeaseMs };
    private void Quiesce(string owner)
    {
      Interlocked.Increment(ref _inputGeneration); _activeInput = null;
      _input.ReleaseAll(); _ui.Dispose(); _epoch++; _owner = owner;
      _input.Pointer(new Vector2(Screen.width / 2f, Screen.height / 2f));
      PlayerInput.Override = owner == "None" ? null : _input;
      _lease = _clock.Elapsed.TotalSeconds + AutomationConfiguration.ControlLeaseMs / 1000d;
      Emit("control.changed", "automation", Control());
    }
    private JObject Performance() => new JObject {
      ["fps"] = _lastFps, ["maxFrameMs"] = _lastPeakFrameMs,
      ["targetFrameRate"] = Application.targetFrameRate, ["vSyncCount"] = QualitySettings.vSyncCount,
      ["jobWorkerCount"] = Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount,
      ["jobWorkerMaximumCount"] = Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerMaximumCount,
      ["graphicsDriverMemoryBytes"] = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver(),
      ["currentTextureMemoryBytes"] = Texture.currentTextureMemory,
      ["textureMipmapLimit"] = QualitySettings.globalTextureMipmapLimit,
      ["allocatedMemoryBytes"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
      ["reservedMemoryBytes"] = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong()
    };
    private JObject Observe(JObject options = null)
    {
      var manager = FindAnyObjectByType<FishNet.Managing.NetworkManager>();
      var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
      var local = players.FirstOrDefault(p => p.IsOwner);
      var dialogue = FindAnyObjectByType<DialoguePanelUIController>();
      var camera = UnityEngine.Camera.main;
      var questManager = Registry.Registry.Get<Quest.QuestManager>(Registry.RegistryType.Service, Registry.Registry.TypeKey<Quest.QuestManager>());
      return new JObject {
        ["protocolVersion"] = "1.0", ["instanceId"] = AutomationConfiguration.InstanceId,
        ["runId"] = AutomationConfiguration.RunId, ["unityVersion"] = Application.unityVersion,
        ["performance"] = Performance(), ["buildId"] = Application.buildGUID, ["eventCursor"] = _sequence, ["frame"] = Time.frameCount,
        ["input"] = new JObject { ["horizontal"] = _input.GetAxis("Horizontal"), ["vertical"] = _input.GetAxis("Vertical"), ["w"] = _input.GetKey(KeyCode.W) },
        ["inputBindings"] = local?.AutomationBindings, ["interactions"] = local?.AutomationInteractions() ?? new JArray(),
        // E2E diagnosis: expose the registry decision separately from proximity
        // and input selection.  A route can then distinguish an unavailable
        // scenario definition from a valid definition whose collider was not
        // reached yet.
        ["interactionRegistry"] = new JArray(InteractionRegistry.AllEntries.Select(entry =>
        {
          string reason = local == null ? "local player unavailable" : null;
          bool visible = local != null && InteractionRegistry.IsVisible(entry, local, out reason);
          bool canInteract = entry.Handler is not IInteractorConditional conditional
            || (local != null && conditional.CanInteract(local.transform));
          return new JObject {
            ["entityId"] = entry.Address.EntityIdentifier,
            ["interactionId"] = entry.Address.InteractionIdentifier,
            ["source"] = entry.Source,
            ["scenarioId"] = entry.DataScenarioIdentifier,
            ["visible"] = visible,
            ["visibilityReason"] = reason,
            ["canInteract"] = canInteract,
            ["initialVisible"] = entry.Definition?.InitialVisible,
            ["visibilityConditions"] = entry.Definition?.VisibilityConditions == null
              ? new JArray() : JArray.FromObject(entry.Definition.VisibilityConditions)
          };
        })),
        ["scenarioInteractionDefinitions"] = _scenario?.CurrentGraph?.Interactions == null ? new JArray() :
          new JArray(_scenario.CurrentGraph.Interactions.Select(definition => new JObject {
            ["entityId"] = definition?.Entity?.Identifier,
            ["interactionId"] = definition?.InteractionIdentifier,
            ["hasVisibilityConditions"] = definition?.HasVisibilityConditions,
            ["initialVisible"] = definition?.InitialVisible
          })),
        ["pendingInteractionDefinitions"] = new JArray(InteractionRegistry.PendingDefinitions.Select(pending => new JObject {
          ["scenarioId"] = pending.ScenarioIdentifier,
          ["entityId"] = pending.Definition?.Entity?.Identifier,
          ["interactionId"] = pending.Definition?.InteractionIdentifier
        })),
        ["entities"] = new JArray(FindObjectsByType<Entity.Npc>(FindObjectsSortMode.None).Where(n => Registry.Registry.Get<GameObject>(Registry.RegistryType.Npc, n.Identifier) == n.gameObject).Select(n => new JObject { ["id"] = n.Identifier, ["kind"] = "npc", ["position"] = new JArray(n.transform.position.x,n.transform.position.y,n.transform.position.z), ["groundProbe"] = AutomationGeometry.GroundProbe(n.transform.position,n.transform) })),
        ["vehicles"] = new JArray(FindObjectsByType<Entity.MinecraftBoatLikeControl>(FindObjectsSortMode.None).Select(vehicle => vehicle.AutomationState)),
        ["scenarioEntities"] = new JArray(Registry.Registry.GetAllEntities()
          .Where(pair => pair.Value?.GameObject != null && pair.Value.GameObject.activeInHierarchy)
          .Select(pair => new { Id = pair.Key, Object = pair.Value.GameObject,
            Identified = pair.Value.GameObject.GetComponent<Entity.IScenarioIdentifiedEntity>() })
          .Where(entry => entry.Identified != null && entry.Identified.ScenarioEntityIdentifier == entry.Id)
          .Select(entry => new {
            entry.Id, entry.Object, entry.Identified,
            Patient = entry.Object.GetComponentInChildren<TriageTrainer.Entity.PatientController>(true)
          })
          .Select(entry => new JObject {
            ["id"] = entry.Id, ["componentType"] = entry.Identified.GetType().Name,
            ["position"] = new JArray(entry.Object.transform.position.x,entry.Object.transform.position.y,entry.Object.transform.position.z),
            ["recognitionSignals"] = entry.Patient == null ? new JArray() : new JArray(entry.Patient.AutomationRecognitionCheckSignals())
          })),
        ["localQuests"] = questManager == null ? null :
          new JArray(questManager.Quests.Select(quest => new JObject {
            ["id"] = quest.Id, ["definitionId"] = quest.DefinitionIdentifier,
            ["scenarioId"] = quest.SourceScenarioIdentifier, ["completed"] = quest.Completed,
            ["placeholder"] = quest.IsGroupWaitPlaceholder, ["waypointId"] = quest.WaypointIdentifier,
            ["progress"] = quest.Progress == null ? null : JObject.FromObject(quest.Progress),
            ["tasks"] = quest.Tasks == null ? new JArray() : JArray.FromObject(quest.Tasks)
          })),
        ["staticPlacedItems"] = (bool?)options?["includeStaticItems"] == false ? null : new JArray(FindObjectsByType<ItemSystem.StaticPlacedItem>(FindObjectsSortMode.None)
          .Select(item => new JObject {
            ["id"] = item.EntityIdentifier, ["scene"] = item.gameObject.scene.name,
            ["position"] = new JArray(item.transform.position.x,item.transform.position.y,item.transform.position.z),
            ["rewards"] = new JArray(item.PickupRewards.Select(reward => new JObject {
              ["itemId"] = reward.ItemIdentifier, ["count"] = reward.Amount
            }))
          })),
        ["worldItems"] = new JArray(FindObjectsByType<ItemSystem.ItemObject>(FindObjectsSortMode.None)
          .Where(item => item.Item != null && !string.IsNullOrWhiteSpace(item.Identifier))
          .Select(item => new JObject {
            ["id"] = item.Identifier, ["itemId"] = item.Item.CurrentIdentifier,
            ["position"] = new JArray(item.AuthoritativePosition.x, item.AuthoritativePosition.y, item.AuthoritativePosition.z),
            ["grounded"] = item.IsGrounded
          })),
        ["camera"] = camera == null ? null : new JObject { ["eulerAngles"] = new JArray(camera.transform.eulerAngles.x,camera.transform.eulerAngles.y,camera.transform.eulerAngles.z) },
        ["inputContext"] = UIOverlayStack.Top?.GetType().Name ?? "Gameplay",
        ["scene"] = SceneManager.GetActiveScene().name, ["control"] = Control(),
        ["activeDataPacks"] = new JArray(Session.SessionConfigurationService.DatapackIds),
        ["capabilities"] = new JArray("input.legacyAdapter", "ui.toolkit.deviceInput", "capture.screenshot", "scenario.observe"),
        ["server"] = new JObject { ["started"] = manager != null && manager.IsServerStarted,
          ["tick"] = manager == null ? 0 : manager.TimeManager.Tick,
          ["connections"] = manager == null ? 0 : manager.ServerManager.Clients.Count,
          ["transport"] = manager?.TransportManager.Transport?.GetType().FullName,
          ["localPort"] = manager?.TransportManager.Transport?.GetPort(),
          ["configuredGamePort"] = Session.SessionConfigurationService.Current.port,
          ["clientAddress"] = manager?.TransportManager.Transport?.GetClientAddress() },
        ["client"] = new JObject { ["started"] = manager != null && manager.IsClientStarted,
          ["localPlayerReady"] = local != null && local.IsClientInitialized,
          ["players"] = new JArray(players.Select(p => new JObject {
            ["ownerId"] = p.OwnerId, ["userIdentifier"] = p.UserIdentifier,
            ["tags"] = new JArray(Tag.PlayerTagService.GetTagsByIdentifier(p.UserIdentifier)),
            ["inventory"] = p.AutomationInventory(),
            ["handlingItemId"] = p.HandlingItem?.CurrentIdentifier,
            ["selectedHotbarSlot"] = p.IsOwner ? p.AutomationSelectedHotbarSlot : -1,
            ["questFlags"] = new JArray(Quest.PlayerQuestStateFlagService.GetFlags(p.UserIdentifier)), ["local"] = p.IsOwner, ["objectId"] = p.ObjectId,
            ["position"] = new JArray(p.transform.position.x, p.transform.position.y, p.transform.position.z),
            ["yaw"] = p.transform.eulerAngles.y, ["cameraPitch"] = p.IsOwner ? (float?)p.AutomationCameraPitch : null, ["canMove"] = p.canMove,
            ["walkingSpeed"] = p.WalkingSpeed, ["scriptedMovement"] = p.AutomationScriptedMovement,
            ["rotationSensitivity"] = p.AutomationRotationSensitivity,
            ["movementSuppressed"] = p.IsMovementSuppressed, ["cursorLocked"] = p.IsCursorLocked })) },
        ["dialogue"] = dialogue == null ? new JObject() : dialogue.AutomationSnapshot(),
        ["signalParameters"] = JArray.FromObject(ScenarioSignalParameterStore.GetAll()),
        ["scenario"] = _scenario == null ? new JObject() : _scenario.AutomationSnapshot(),
        ["scenarioNetwork"] = ScenarioNetworkRelay.AutomationRoleAllocations(),
        ["automaticDoors"] = new JArray(FindObjectsByType<autoDoorSlide>(FindObjectsSortMode.None).Select(door => new JObject {
          ["name"] = door.name,
          ["position"] = new JArray(door.transform.position.x, door.transform.position.y, door.transform.position.z),
          ["triggers"] = new JArray(door.GetComponents<Collider>().Where(c => c.isTrigger).Select(c => new JObject {
            ["enabled"] = c.enabled,
            ["center"] = new JArray(c.bounds.center.x, c.bounds.center.y, c.bounds.center.z),
            ["size"] = new JArray(c.bounds.size.x, c.bounds.size.y, c.bounds.size.z)
          })),
          ["panels"] = new JArray((door.doors ?? Array.Empty<GameObject>()).Where(panel => panel != null).Select(panel => new JObject {
            ["name"] = panel.name,
            ["position"] = new JArray(panel.transform.position.x, panel.transform.position.y, panel.transform.position.z),
            ["localPosition"] = new JArray(panel.transform.localPosition.x, panel.transform.localPosition.y, panel.transform.localPosition.z)
          }))
        })),
        ["waypoints"] = new JArray(FindObjectsByType<Registry.WaypointAnchor>(FindObjectsSortMode.None)
          .Select(w => new JObject { ["id"] = w.Identifier, ["position"] = new JArray(w.transform.position.x, w.transform.position.y, w.transform.position.z) }))
      };
    }
    private void OnQuestFlagsChanged(string identifier) => Emit("quest.flagsChanged", InstanceFinder.IsServerStarted ? "server" : "client",
      new JObject { ["userIdentifier"] = identifier, ["flags"] = new JArray(Quest.PlayerQuestStateFlagService.GetFlags(identifier)) });
    private void OnSignalParameterRecorded(ScenarioSignalParameter value) => Emit("signal.parameter_recorded",
      InstanceFinder.IsServerStarted ? "server" : "client", new JObject {
        ["signalId"] = value.SignalIdentifier, ["playerId"] = value.PlayerIdentifier,
        ["sequence"] = value.Sequence, ["parameterJson"] = value.ParameterJson,
        ["serverRealtime"] = InstanceFinder.IsServerStarted ? (double?)Time.realtimeSinceStartupAsDouble : null
      });
    private void OnSignalRegistered(string signal) => Emit("signal.registered", InstanceFinder.IsServerStarted ? "server" : "client", new JObject { ["signalId"] = signal });
    private void OnSignalCleared(string signal) => Emit("signal.cleared", InstanceFinder.IsServerStarted ? "server" : "client", new JObject { ["signalId"] = signal });
    private void OnLog(string message, string stack, LogType type)
    {
      if (!string.IsNullOrEmpty(AutomationConfiguration.Token)) message = message.Replace(AutomationConfiguration.Token, "[redacted]");
      Emit("log", "process", new JObject { ["message"] = message, ["severity"] = type.ToString() });
    }
    private void Emit(string type, string side, JObject payload)
    {
      _events.Enqueue(new JObject { ["eventSequence"] = ++_sequence, ["eventType"] = type,
        ["instanceId"] = AutomationConfiguration.InstanceId, ["runId"] = AutomationConfiguration.RunId,
        ["side"] = side, ["frame"] = Time.frameCount, ["wallClockTimestamp"] = DateTime.UtcNow,
        ["monotonicTimestamp"] = _clock.Elapsed.TotalSeconds, ["payload"] = payload });
      while (_events.Count > EventCapacity) _events.Dequeue();
    }
    private static JObject Error(string code) => new() { ["ok"] = false, ["error"] = new JObject { ["code"] = code } };
    private void OnDestroy()
    {
      Application.logMessageReceived -= OnLog;
      AutomationEvents.Published -= Emit;
      ScenarioSignalParameterStore.OnValueRecorded -= OnSignalParameterRecorded;
      ScenarioInteractionSignals.OnSignalRegistered -= OnSignalRegistered;
      ScenarioInteractionSignals.OnSignalCleared -= OnSignalCleared;
      Quest.PlayerQuestStateFlagService.FlagsChanged -= OnQuestFlagsChanged;
      _input.ReleaseAll(); _ui.Dispose(); PlayerInput.Override = null;
      _stop?.Cancel(); _listener?.Close();
      foreach (var cached in _commands.Values) cached.request.Result.TrySetResult(Error("INSTANCE_UNAVAILABLE"));
      while (_queue.TryDequeue(out var request)) request.Result.TrySetResult(Error("INSTANCE_UNAVAILABLE"));
    }
  }
}
#endif
