#if (UNITY_E2E || UNITY_EDITOR) && UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace MultiplayerInfrastructure.Automation.Editor
{
  public sealed class AutomationInputTests
  {
    [Test]
    public void PointerExpiresWithoutCreatingDevicesOrDependingOnControlHeartbeat()
    {
      var type = typeof(AutomationBridge).Assembly.GetType("MultiplayerInfrastructure.Automation.AutomationUI");
      var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
      var ui = System.Activator.CreateInstance(type, true);
      var pressed = type.GetField("_pressed", flags);
      pressed.SetValue(ui, true);
      type.GetField("_pressedUntil", flags).SetValue(ui, 10d);
      type.GetMethod("ExpirePointer", flags).Invoke(ui, new object[] { 9d });
      Assert.That(pressed.GetValue(ui), Is.True);
      type.GetMethod("ExpirePointer", flags).Invoke(ui, new object[] { 10d });
      Assert.That(pressed.GetValue(ui), Is.False);
      Assert.That(type.GetField("_mouse", flags).GetValue(ui), Is.Null);
      type.GetMethod("ReleasePointer", flags).Invoke(ui, null);
      Assert.That(pressed.GetValue(ui), Is.False);
    }
    [Test]
    public void PointerExpiryQueuesActualMouseRelease()
    {
      var type = typeof(AutomationBridge).Assembly.GetType("MultiplayerInfrastructure.Automation.AutomationUI");
      var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
      var ui = System.Activator.CreateInstance(type, true);
      var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
      try {
        type.GetField("_mouse", flags).SetValue(ui, mouse);
        type.GetField("_pressed", flags).SetValue(ui, true);
        type.GetField("_pressedUntil", flags).SetValue(ui, 10d);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { buttons = 1 });
        UnityEngine.InputSystem.InputSystem.Update();
        Assert.That(mouse.leftButton.isPressed, Is.True);
        type.GetMethod("ExpirePointer", flags).Invoke(ui, new object[] { 10d });
        UnityEngine.InputSystem.InputSystem.Update();
        Assert.That(mouse.leftButton.isPressed, Is.False);
      } finally { UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse); }
    }
    [Test]
    public void InputWaitRequiresBoundedIntegerDuration()
    {
      var validate = typeof(AutomationBridge).GetMethod("ValidateInput", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
      foreach (var duration in new[] { "1", "100", "2000" })
        Assert.DoesNotThrow(() => validate.Invoke(null, new object[] { Newtonsoft.Json.Linq.JArray.Parse("[{\"operation\":\"wait\",\"durationMs\":" + duration + "}]") }));
      foreach (var duration in new[] { "null", "0", "-1", "2001", "1.5", "\"100\"" })
      {
        var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => validate.Invoke(null,
          new object[] { Newtonsoft.Json.Linq.JArray.Parse("[{\"operation\":\"wait\",\"durationMs\":" + duration + "}]") }));
        Assert.That(error.InnerException, Is.TypeOf<System.ArgumentException>());
      }
    }
    [Test]
    public void AutomationLayoutsDeclareBackgroundSupport()
    {
      var type = typeof(AutomationBridge).Assembly.GetType("MultiplayerInfrastructure.Automation.AutomationUI");
      type.GetMethod("RegisterLayouts", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
        .Invoke(null, null);
      Assert.That(UnityEngine.InputSystem.InputSystem.LoadLayout("E2EBackgroundMouse").canRunInBackground, Is.True);
      Assert.That(UnityEngine.InputSystem.InputSystem.LoadLayout("E2EBackgroundKeyboard").canRunInBackground, Is.True);
    }
    [Test]
    public void DisabledAutomationDeviceIsReenabledWithoutChangingOtherDevices()
    {
      var owned = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
      var unrelated = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
      try {
        UnityEngine.InputSystem.InputSystem.DisableDevice(owned);
        UnityEngine.InputSystem.InputSystem.DisableDevice(unrelated);
        var type = typeof(AutomationBridge).Assembly.GetType("MultiplayerInfrastructure.Automation.AutomationUI");
        var method = type.GetMethod("EnsureDeviceEnabled", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        method.Invoke(null, new object[] { owned });
        Assert.That(owned.enabled, Is.True);
        Assert.That(unrelated.enabled, Is.False);
        method.Invoke(null, new object[] { owned });
        Assert.That(owned.enabled, Is.True);
      } finally {
        UnityEngine.InputSystem.InputSystem.RemoveDevice(owned);
        UnityEngine.InputSystem.InputSystem.RemoveDevice(unrelated);
      }
    }
    [Test]
    public void UnregisterWithoutIdentifierPreservesRegisteredPlayers()
    {
      var before = Session.UserDescriptorService.GetAll().ToDictionary(pair => pair.Key, pair => pair.Value);
      Assert.DoesNotThrow(() => Session.UserDescriptorService.Unregister(null));
      Assert.DoesNotThrow(() => Session.UserDescriptorService.Unregister(string.Empty));
      Assert.That(Session.UserDescriptorService.GetAll().Count, Is.EqualTo(before.Count));
      foreach (var pair in before)
        Assert.That(Session.UserDescriptorService.GetAll()[pair.Key], Is.SameAs(pair.Value));
    }
    [Test]
    public void OrdinaryBuildGuardDetectsInputAndEnvironmentResidueWithoutBridge()
    {
      using var assembly = Mono.Cecil.AssemblyDefinition.CreateAssembly(
        new Mono.Cecil.AssemblyNameDefinition("Player", new System.Version(1, 0)), "Player", Mono.Cecil.ModuleKind.Dll);
      var module = assembly.MainModule;
      var input = new Mono.Cecil.TypeDefinition("MultiplayerInfrastructure.Automation", "PlayerInput",
        Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object);
      module.Types.Add(input);
      Assert.That(AutomationBuildGuard.Inspect(assembly, false), Is.Empty);
      input.Fields.Add(new Mono.Cecil.FieldDefinition("Override", Mono.Cecil.FieldAttributes.Static, module.TypeSystem.Object));
      Assert.That(AutomationBuildGuard.Inspect(assembly, false), Has.Some.Contains("override"));
      input.Fields.Clear();
      var method = new Mono.Cecil.MethodDefinition("ReadSetting", Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void);
      input.Methods.Add(method);
      var il = method.Body.GetILProcessor();
      il.Append(il.Create(Mono.Cecil.Cil.OpCodes.Ldstr, "UNITY_E2E_ALLOW_CHAT_COMMANDS"));
      il.Append(il.Create(Mono.Cecil.Cil.OpCodes.Pop)); il.Append(il.Create(Mono.Cecil.Cil.OpCodes.Ret));
      Assert.That(AutomationBuildGuard.Inspect(assembly, false), Has.Some.Contains("environment"));
      input.Methods.Clear();
      module.Types.Add(new Mono.Cecil.TypeDefinition("MultiplayerInfrastructure.Automation", "AutomationInputSource",
        Mono.Cecil.TypeAttributes.Public, module.TypeSystem.Object));
      Assert.That(AutomationBuildGuard.Inspect(assembly, false), Has.Some.Contains("AutomationInputSource"));
      Assert.That(AutomationBuildGuard.Inspect(assembly, true), Has.Some.Contains("AutomationBridge"));
    }
    [Test]
    public void AutomationChatCommandsRequireExplicitOptIn()
    {
      var values = new System.Collections.Generic.Dictionary<string, string> {
        ["UNITY_E2E_EDITOR"] = "1", ["UNITY_E2E_TOKEN"] = new string('t', 32),
        ["UNITY_E2E_PROFILE"] = System.IO.Path.GetTempPath(), ["UNITY_E2E_PORT"] = "17891",
        ["UNITY_E2E_ALLOW_CHAT_COMMANDS"] = null
      };
      var previous = values.Keys.ToDictionary(key => key, System.Environment.GetEnvironmentVariable);
      var type = typeof(AutomationBridge).Assembly.GetType("MultiplayerInfrastructure.Automation.AutomationConfiguration");
      var property = type.GetProperty("AllowChatCommands", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
      try {
        foreach (var pair in values) System.Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        foreach (var value in new[] { null, "0", "true", "1" }) {
          System.Environment.SetEnvironmentVariable("UNITY_E2E_ALLOW_CHAT_COMMANDS", value);
          Assert.That((bool)property.GetValue(null), Is.EqualTo(value == "1"));
        }
        System.Environment.SetEnvironmentVariable("UNITY_E2E_ALLOW_CHAT_COMMANDS", "0");
        System.Environment.SetEnvironmentVariable("UNITY_E2E_TOKEN", null);
        Assert.That((bool)property.GetValue(null), Is.True, "Ordinary editor sessions retain their chat behavior");
      } finally {
        foreach (var pair in previous) System.Environment.SetEnvironmentVariable(pair.Key, pair.Value);
      }
    }
    [Test]
    public void CatalogueHandlesDialogueChoicesAndStringQuizOptions()
    {
      var type = typeof(AutomationBridge).Assembly.GetType("MultiplayerInfrastructure.Automation.AutomationContent");
      var method = type.GetMethod("Catalogue", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
      var catalogue = (Newtonsoft.Json.Linq.JObject)method.Invoke(null, null);
      Assert.That(((string)catalogue["contentHash"]).Length, Is.EqualTo(64));
      Assert.That(catalogue["graphs"].HasValues, Is.True);
    }
    [Test]
    public void SyntheticDialogueUsesTheAuthoritativeExecutionPath()
    {
      var assembly = typeof(AutomationBridge).Assembly;
      assembly.GetType("MultiplayerInfrastructure.Automation.AutomationFixtures")
        .GetMethod("Register", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
      var result = (Newtonsoft.Json.Linq.JObject)assembly.GetType("MultiplayerInfrastructure.Scenario.ScenarioNetworkRelay")
        .GetMethod("AutomationCompatibility", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
        .Invoke(null, new object[] { "e2e_authoritative_dialogue" });
      Assert.That((bool)result["supported"], Is.True, result.ToString());
    }
    [Test]
    public void EventHandlerDiagnosticsFollowRegistrationWithoutChangingContentHash()
    {
      const string identifier = "show_tutorial_interaction_hint";
      var type = typeof(AutomationBridge).Assembly.GetType("MultiplayerInfrastructure.Automation.AutomationContent");
      var method = type.GetMethod("Catalogue", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
      Scenario.ScenarioEventIdentifierRegistry.TryGetHandler(identifier, out var original);
      int invocations = 0;
      Scenario.ScenarioEventIdentifierRegistry.ScenarioEventHandler probe = () => { invocations++; return null; };
      try {
        Scenario.ScenarioEventIdentifierRegistry.Unregister(identifier);
        var before = (Newtonsoft.Json.Linq.JObject)method.Invoke(null, null);
        var missing = before["eventHandlerDiagnostics"].Single(entry => (string)entry["eventIdentifier"] == identifier);
        Assert.That((bool)missing["handlerRegistered"], Is.False);
        Scenario.ScenarioEventIdentifierRegistry.Register(identifier, probe);
        var after = (Newtonsoft.Json.Linq.JObject)method.Invoke(null, null);
        Assert.That((bool)after["eventHandlerDiagnostics"].Single(entry => (string)entry["eventIdentifier"] == identifier)["handlerRegistered"], Is.True);
        Assert.That((string)after["contentHash"], Is.EqualTo((string)before["contentHash"]));
        Assert.That(invocations, Is.Zero);
      } finally {
        Scenario.ScenarioEventIdentifierRegistry.Unregister(identifier, probe);
        if (original != null) Scenario.ScenarioEventIdentifierRegistry.Register(identifier, original);
      }
    }
    [Test]
    public void RoleFixtureDeclaresFourDistinctBranchesAndChecklistSets()
    {
      var type = typeof(AutomationBridge).Assembly.GetType("MultiplayerInfrastructure.Automation.AutomationFixtures");
      var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
      type.GetMethod("Register", flags).Invoke(null, null);
      var assets = (System.Collections.Generic.IEnumerable<TextAsset>)type.GetField("Assets", flags).GetValue(null);
      var graph = Scenario.ScenarioGraphLoader.LoadFromJson(assets.Single(a => a.name == "e2e_role_branches.scenario").text);
      Assert.That(graph.ActiveRoleTags.Distinct().Count(), Is.EqualTo(4));
      Assert.That(graph.ChecklistItemSetsByPlayerTag.Count, Is.EqualTo(4));
      Assert.That(graph.TryGetNode("parallel", out var node), Is.True);
      var parallel = (Scenario.ScenarioParallelNode)node;
      Assert.That(parallel.AllocationType, Is.EqualTo(Scenario.ScenarioParallelAllocationType.ByRole));
      Assert.That(parallel.Branches.Count, Is.EqualTo(4));
      foreach (var branch in parallel.Branches)
      {
        Assert.That(graph.TryGetNode(branch.Identifier, out var branchNode), Is.True);
        var choice = (Scenario.ScenarioChoiceNode)branchNode;
        var nextId = choice.Options.Single().NextNodeIdentifier;
        Assert.That(nextId, Is.Not.EqualTo(branch.CompletionConditionIdentifier),
          "The completion label stops execution before the state update node.");
        Assert.That(graph.TryGetNode(nextId, out var completionNode), Is.True);
        Assert.That(completionNode, Is.InstanceOf<Scenario.ScenarioStateUpdateNode>());
      }
    }
    [Test]
    public void TapPreservesDownAndUpOnSeparateFrames()
    {
      var input = new AutomationInputSource();
      input.Enqueue(KeyCode.W, true, 2);
      input.Enqueue(KeyCode.W, false, 0);
      input.BeginFrame(0, 1f / 60);
      Assert.That(input.GetKeyDown(KeyCode.W), Is.True);
      Assert.That(input.GetKeyDown(KeyCode.W), Is.True, "Reads must not consume the transition");
      Assert.That(input.GetKeyUp(KeyCode.W), Is.False);
      input.BeginFrame(.02, 1f / 60);
      Assert.That(input.GetKeyDown(KeyCode.W), Is.False);
      Assert.That(input.GetKeyUp(KeyCode.W), Is.True);
    }
    [Test]
    public void ExpiredInputDoesNotResumeAfterMainLoopStall()
    {
      var input = new AutomationInputSource();
      input.Enqueue(KeyCode.W, true, 1);
      input.BeginFrame(2, .1f);
      Assert.That(input.GetKey(KeyCode.W), Is.False);
      Assert.That(input.GetAxis("Vertical"), Is.Zero);
    }
    [Test]
    public void ReleaseAllClearsQueuedTransitionsAndMouseDeltas()
    {
      var input = new AutomationInputSource();
      input.Enqueue(KeyCode.W, true, 2);
      input.Look(new Vector2(3, 4));
      input.ReleaseAll(); input.BeginFrame(.01, .01f);
      Assert.That(input.GetKeyDown(KeyCode.W), Is.False);
      Assert.That(input.GetAxis("Mouse X"), Is.Zero);
    }
    [Test]
    public void MovementMatchesConfiguredSensitivityAndSnap()
    {
      var input = new AutomationInputSource();
      input.Enqueue(KeyCode.D, true, 2); input.BeginFrame(0, .1f);
      Assert.That(input.GetAxis("Horizontal"), Is.EqualTo(.3f).Within(.0001));
      input.Enqueue(KeyCode.D, false, 0); input.Enqueue(KeyCode.A, true, 2); input.BeginFrame(.1, .1f);
      Assert.That(input.GetAxis("Horizontal"), Is.EqualTo(-.3f).Within(.0001));
    }
  }
}
#endif
