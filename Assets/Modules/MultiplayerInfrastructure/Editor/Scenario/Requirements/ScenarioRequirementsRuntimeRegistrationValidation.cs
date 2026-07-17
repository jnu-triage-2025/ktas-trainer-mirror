#if UNITY_EDITOR
using System;
using System.Linq;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario.Requirements;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  internal static class ScenarioRequirementsRuntimeRegistrationValidation
  {
    internal static void Run()
    {
      ScenarioRequirementRuntimeRegistrationRegistry.Reset();
      var first = new GameObject("runtime-owner-a");
      var second = new GameObject("runtime-owner-b");
      var firstHandle = ScenarioRequirementRuntimeRegistrationRegistry.Register(RegistryType.Npc, ScenarioRequirementKind.Npc, "shared", first.AddComponent<RuntimeOwner>(), new[] { ScenarioRequirementCapability.RegisteredNpcComponent });
      var idempotentHandle = ScenarioRequirementRuntimeRegistrationRegistry.Register(RegistryType.Npc, ScenarioRequirementKind.Npc, "shared", first.GetComponent<RuntimeOwner>(), new[] { ScenarioRequirementCapability.RegisteredNpcComponent });
      if (!firstHandle.IsValid || !idempotentHandle.IsValid || ScenarioRequirementRuntimeRegistrationRegistry.GetAll().Count != 1) throw new InvalidOperationException("Same-owner registration was not idempotent.");
      var secondHandle = ScenarioRequirementRuntimeRegistrationRegistry.Register(RegistryType.Npc, ScenarioRequirementKind.Npc, "shared", second.AddComponent<RuntimeOwner>(), new[] { ScenarioRequirementCapability.RegisteredNpcComponent });
      if (!secondHandle.IsValid || ScenarioRequirementRuntimeRegistrationRegistry.GetAll().Count != 2) throw new InvalidOperationException("Different-owner duplicate was collapsed.");
      firstHandle.Dispose();
      if (ScenarioRequirementRuntimeRegistrationRegistry.GetAll().Count != 1) throw new InvalidOperationException("Owner handle did not remove exactly one record.");
      ScenarioRequirementRuntimeRegistrationRegistry.Reset();
      secondHandle.Dispose();
      if (ScenarioRequirementRuntimeRegistrationRegistry.GetAll().Count != 0) throw new InvalidOperationException("Stale handle removed or retained records after reset.");
      UnityEngine.Object.DestroyImmediate(first);
      UnityEngine.Object.DestroyImmediate(second);
      Debug.Log("[ScenarioRequirementsRuntimeRegistrationValidation] Runtime owner registration validation passed.");
    }

    private sealed class RuntimeOwner : MonoBehaviour { }
  }
}
#endif
