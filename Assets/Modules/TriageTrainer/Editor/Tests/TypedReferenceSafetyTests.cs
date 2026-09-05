using System;
using System.Collections.Generic;
using NUnit.Framework;
using TriageTrainer.Editor;
using TriageTrainer.Scenario;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TriageTrainer.Tests
{
  public sealed class TypedReferenceSafetyTests
  {
    [Test]
    public void WrongManagedTypeIsRejectedWithoutActivatingTheGameObject()
    {
      var wrong = new GameObject("old-gameobject-reference");
      wrong.SetActive(false);
      var errors = new List<string>();
      try
      {
        Assert.That(TypedUnityReference.SetActive<PatientA18gLeftVisualMarker>(
          wrong, true, "_patientA18gLeftVisual", errors.Add), Is.False);
        Assert.That(wrong.activeSelf, Is.False);
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Does.Contain("_patientA18gLeftVisual").And.Contain("GameObject"));
      }
      finally { Object.DestroyImmediate(wrong); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InvalidOrDestroyedReferenceRecoversOnlyTheUniqueInactiveChild(bool destroyed)
    {
      var patient = new GameObject("patient");
      var other = new GameObject("other");
      var child = new GameObject("visual");
      child.transform.SetParent(patient.transform);
      child.SetActive(false);
      var expected = child.AddComponent<PatientA18gLeftVisualMarker>();
      object stale = other;
      if (destroyed)
      {
        stale = other.AddComponent<PatientA18gLeftVisualMarker>();
        Object.DestroyImmediate((Object)stale);
      }
      try
      {
        var errors = new List<string>();
        var result = TypedUnityReference.RecoverUniqueChild<PatientA18gLeftVisualMarker>(
          stale, patient, "left", errors.Add);
        Assert.That(result, Is.SameAs(expected));
        Assert.That(errors.Count, Is.EqualTo(destroyed ? 0 : 1));
        Assert.That(TypedUnityReference.SetActive<PatientA18gLeftVisualMarker>(result, true, "left", errors.Add), Is.True);
        Assert.That(child.activeSelf, Is.True);
      }
      finally { Object.DestroyImmediate(patient); Object.DestroyImmediate(other); }
    }

    [TestCase(0)]
    [TestCase(2)]
    public void RecoveryDoesNotGuessWhenMarkersAreMissingOrAmbiguous(int count)
    {
      var patient = new GameObject("patient");
      var other = new GameObject("other-patient");
      other.AddComponent<PatientA18gLeftVisualMarker>();
      try
      {
        for (int i = 0; i < count; i++)
        {
          var child = new GameObject("visual");
          child.transform.SetParent(patient.transform);
          child.AddComponent<PatientA18gLeftVisualMarker>();
        }
        var errors = new List<string>();
        Assert.That(TypedUnityReference.RecoverUniqueChild<PatientA18gLeftVisualMarker>(
          other, patient, "left", errors.Add), Is.Null);
        Assert.That(errors, Has.Count.EqualTo(2));
      }
      finally { Object.DestroyImmediate(patient); Object.DestroyImmediate(other); }
    }

    [Test]
    public void ValidReferenceAndOptionalNullArePreservedWithoutErrors()
    {
      var go = new GameObject("visual");
      try
      {
        var marker = go.AddComponent<PatientA18gLeftVisualMarker>();
        var errors = new List<string>();
        Assert.That(TypedUnityReference.RecoverUniqueChild<PatientA18gLeftVisualMarker>(marker, null, "left", errors.Add), Is.SameAs(marker));
        Assert.That(TypedUnityReference.SetActive<PatientA18gLeftVisualMarker>(null, true, "left", errors.Add), Is.False);
        Assert.That(errors, Is.Empty);
      }
      finally { Object.DestroyImmediate(go); }
    }
  }

  public sealed class ReferenceYamlValidatorTests
  {
    private const string Owner = "--- !u!114 &1\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: owner, type: 3}\n  m_Name: Bootstrap\n  marker: {fileID: TARGET}\n";
    private const string GameObjectDoc = "--- !u!1 &2\nGameObject:\n  m_Name: Wrong\n";
    private const string MarkerDoc = "--- !u!114 &3\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: marker, type: 3}\n";
    private static Type ScriptType(string guid, string id) => guid == "owner" ? typeof(ReferenceValidationFixture) :
      guid == "marker" ? typeof(PatientA18gLeftVisualMarker) : null;

    [Test]
    public void BuildScopeExcludesUnreferencedResearchAndIncludesRuntimeDependencies()
    {
      string[] assets = { "Assets/Scenes/Researchs/TrialsScenarioGraph.unity", "Assets/Unused.prefab",
        "Assets/Resources/Runtime.prefab", "Assets/Editor/Resources/EditorOnly.prefab" };
      string[] passedRoots = null;
      var result = SerializedReferenceBuildValidator.CollectBuildAssets(
        new[] { "Assets/Scenes/Game.unity" }, assets, new[] { "Assets/Preloaded.asset" }, roots =>
        {
          passedRoots = roots;
          return new[] { "Assets/Used.prefab", "Assets/Nested.prefab", "Assets/Texture.png" };
        });
      Assert.That(passedRoots, Is.EquivalentTo(new[] { "Assets/Scenes/Game.unity", "Assets/Resources/Runtime.prefab", "Assets/Preloaded.asset" }));
      Assert.That(result, Is.EquivalentTo(new[] { "Assets/Scenes/Game.unity", "Assets/Resources/Runtime.prefab", "Assets/Used.prefab", "Assets/Nested.prefab" }));
    }

    [Test]
    public void ExplicitlyBuiltResearchSceneIsStillValidated()
    {
      const string scene = "Assets/Scenes/Researchs/TrialsScenarioGraph.unity";
      var result = SerializedReferenceBuildValidator.CollectBuildAssets(new[] { scene },
        Array.Empty<string>(), Array.Empty<string>(), roots => roots);
      Assert.That(result, Is.EqualTo(new[] { scene }));
    }

    [Test]
    public void UnityBuiltinUiDocumentScriptResolvesWithoutAProjectAssetGuid()
    {
      var method = typeof(SerializedReferenceBuildValidator).GetMethod("CreateScriptTypeResolver",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
      var resolve = (Func<string, string, Type>)method.Invoke(null, null);
      Assert.That(resolve("0000000000000000e000000000000000", "19102"),
        Is.EqualTo(typeof(UnityEngine.UIElements.UIDocument)));
    }

    [Test]
    public void BuiltinScriptsWithTheSameGuidAreResolvedByFileId()
    {
      string text = Owner.Replace("TARGET", "3") + MarkerDoc.Replace("guid: marker", "guid: builtin").Replace("fileID: 11500000, guid: builtin", "fileID: 19102, guid: builtin")
        + "--- !u!114 &4\nMonoBehaviour:\n  m_Script: {fileID: 19103, guid: builtin, type: 0}\n";
      var seen = new List<string>();
      var validator = new ReferenceYamlValidator(_ => text, _ => "", (guid, id) =>
      {
        if (guid != "builtin") return ScriptType(guid, id);
        seen.Add(id);
        return id == "19102" ? typeof(PatientA18gLeftVisualMarker) : typeof(MonoBehaviour);
      });
      Assert.That(validator.Validate(new[] { "scene.unity" }), Is.Empty);
      Assert.That(seen, Is.EquivalentTo(new[] { "19102", "19103" }));
    }

    [TestCase("2", 1)]
    [TestCase("3", 0)]
    [TestCase("0", 0)]
    [TestCase("99", 1)]
    public void RawSceneDetectsMismatchedAndMissingReferencesBeforeDeserialization(string target, int errors)
    {
      var validator = new ReferenceYamlValidator(_ => Owner.Replace("TARGET", target) + GameObjectDoc + MarkerDoc, _ => "", ScriptType);
      var result = validator.Validate(new[] { "scene.unity" });
      Assert.That(result.Count, Is.EqualTo(errors));
      if (errors > 0)
        Assert.That(result[0], Does.Contain("scene.unity").And.Contain("ReferenceValidationFixture.marker").And.Contain("PatientA18gLeftVisualMarker"));
    }

    [TestCase("2", 1)]
    [TestCase("3", 0)]
    public void PrefabOverrideIsValidatedInTheInstanceAsset(string target, int errors)
    {
      var files = new Dictionary<string, string> {
        ["source.prefab"] = Owner.Replace("TARGET", "0"),
        ["scene.unity"] = "--- !u!1001 &4\nPrefabInstance:\n    - target: {fileID: 1, guid: source, type: 3}\n      propertyPath: marker\n      value: \n      objectReference: {fileID: " + target + "}\n" + GameObjectDoc + MarkerDoc
      };
      var validator = new ReferenceYamlValidator(p => files[p], g => g + ".prefab", ScriptType);
      Assert.That(validator.Validate(new[] { "scene.unity" }).Count, Is.EqualTo(errors));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StrippedReferenceFollowsNestedPrefabToActualComponentType(bool staleLocalScript)
    {
      var files = new Dictionary<string, string> {
        ["scene.unity"] = Owner.Replace("TARGET", "5") + "--- !u!114 &5 stripped\nMonoBehaviour:\n  m_CorrespondingSourceObject: {fileID: 6, guid: middle, type: 3}\n",
        ["middle.prefab"] = "--- !u!114 &6 stripped\nMonoBehaviour:\n  m_CorrespondingSourceObject: {fileID: 3, guid: child, type: 3}\n",
        ["child.prefab"] = MarkerDoc
      };
      var validator = new ReferenceYamlValidator(p => files[p], g => g + ".prefab", ScriptType);
      Assert.That(validator.Validate(new[] { "scene.unity" }), Is.Empty);
      if (staleLocalScript)
        files["scene.unity"] += "  m_Script: {fileID: 11500000, guid: marker, type: 3}\n";
      files["child.prefab"] = GameObjectDoc.Replace("&2", "&3");
      Assert.That(validator.Validate(new[] { "scene.unity" }), Has.Count.EqualTo(1));
    }

    [TestCase(true, 0)]
    [TestCase(false, 1)]
    public void ExternalReferenceUsesTheRequestedSubassetType(bool compatible, int errors)
    {
      string scene = Owner.Replace("TARGET", "77, guid: external, type: 3");
      var validator = new ReferenceYamlValidator(_ => scene, _ => "model.fbx", ScriptType,
        (path, id) => path == "model.fbx" && id == "77" && compatible ? typeof(PatientA18gLeftVisualMarker) : typeof(GameObject));
      Assert.That(validator.Validate(new[] { "scene.unity" }).Count, Is.EqualTo(errors));
    }

    [Test]
    public void BinaryAssetOrDuplicateIdsFailClosed()
    {
      foreach (string content in new[] { "binary data", GameObjectDoc + GameObjectDoc })
      {
        var validator = new ReferenceYamlValidator(_ => content, _ => "", ScriptType);
        Assert.That(validator.Validate(new[] { "scene.unity" }), Has.Count.EqualTo(1));
      }
    }
  }

  public sealed class ReferenceValidationFixture : MonoBehaviour
  {
    public PatientA18gLeftVisualMarker marker;
  }
}
