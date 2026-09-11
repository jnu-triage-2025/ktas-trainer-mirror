using System.Collections.Generic;
using System.Reflection;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Registry;
using NUnit.Framework;
using TriageTrainer.Entity;
using TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Tests
{
  /// <summary>
  /// 엔티티 프리셋 스폰은 <c>ServerManager.Spawn</c> <b>이전</b>에 런타임 식별자를 주입한다. 그 시점의
  /// 인스턴스는 아직 NetworkManager 를 갖지 않으므로, SyncVar 쓰기를 "서버인가" 만으로 게이트하면 값이
  /// 기록되지 않고 스폰 페이로드에도 실리지 않는다. 그러면 원격 피어는 프리팹 기본 식별자(patient,
  /// moving_patient_bed)로 엔티티를 등록하게 되고, 침대의 식별자 기반 결합(repose)이 원격 피어에서
  /// 해석되지 않아 환자가 선 자세로 남는다. 식별자에 묶인 퀘스트 표시 바인딩과 B/C 전용 처치 분기도
  /// 같은 이유로 원격 피어에서만 빠진다.
  ///
  /// <para>
  /// 이 테스트는 스폰 전 인스턴스(= NetworkManager 없음)에서 식별자 주입이 복제 대상 SyncVar 까지
  /// 도달하는지를 고정한다.
  /// </para>
  /// </summary>
  public sealed class EntityPresetSpawnIdentifierReplicationTests
  {
    private const string PatientBMalePrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeBMale.prefab";
    private const string MovingBedPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/MinecraftBoatLikes/PatientMovingBed.prefab";
    private const string PresetRegistryPath =
      "Assets/Modules/TriageTrainer/ScriptableObjects/EntityPreset Registry Requirements SO.asset";
    private const string PatientAPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/Patient/PatientTypeA.prefab";
    private const string DoctorNpcPrefabPath =
      "Assets/Modules/TriageTrainer/Prefabs/Entities/NPC/DoctorNPCHat.prefab";
    private const string DefibrillatorPadChildName = "defibrillatorpad_subclavicle_A";

    private static readonly Vector3 IsolatedSpawnPosition = new Vector3(14000f, 0f, 14000f);

    [Test]
    public void DoctorNpcIsGlobalSoEveryScenarioParticipantObservesItsSpawn()
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoctorNpcPrefabPath);
      Assert.That(prefab, Is.Not.Null);

      var networkObject = prefab.GetComponent<FishNet.Object.NetworkObject>();
      Assert.That(networkObject, Is.Not.Null);
      Assert.That(networkObject.IsGlobal, Is.True,
        "Additive 씬의 관찰자 등록 상태와 무관하게 모든 참가자에게 의사 NPC가 스폰되어야 합니다.");
    }

    [Test]
    public void PatientSpawnedIdentifierReachesTheReplicatedSyncVarBeforeSpawn()
    {
      var patientObject = InstantiateIsolated(PatientBMalePrefabPath);
      try
      {
        var patient = patientObject.GetComponent<PatientController>();
        Assert.That(patient, Is.Not.Null);

        patient.ApplySpawnedEntityIdentifier("patient_b");

        Assert.That(ReadSyncVarString(typeof(PatientController), patient, "_runtimeIdentifier"),
          Is.EqualTo("patient_b"),
          "프리셋 스폰이 주입한 식별자는 스폰 페이로드로 복제되도록 SyncVar 에 기록돼야 한다");
        Assert.That(patient.Identifier, Is.EqualTo("patient_b"));
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    [Test]
    public void MovingBedSpawnedIdentifierReachesTheReplicatedSyncVarBeforeSpawn()
    {
      var bedObject = InstantiateIsolated(MovingBedPrefabPath);
      try
      {
        var bed = bedObject.GetComponent<MovingPatientBedController>();
        Assert.That(bed, Is.Not.Null);

        bed.ApplySpawnedEntityIdentifier("bed_b");

        Assert.That(ReadSyncVarString(typeof(MovingPatientBedController), bed, "_runtimeIdentifierSync"),
          Is.EqualTo("bed_b"),
          "원격 피어가 같은 식별자로 침대를 등록하려면 스폰 전 주입값이 SyncVar 에 남아야 한다");
        Assert.That(bed.Identifier, Is.EqualTo("bed_b"));
      }
      finally
      {
        Object.DestroyImmediate(bedObject);
      }
    }

    [Test]
    public void MovingBedParentLinkReachesTheReplicatedSyncVarBeforeSpawn()
    {
      var bedObject = InstantiateIsolated(MovingBedPrefabPath);
      try
      {
        var bed = bedObject.GetComponent<MovingPatientBedController>();
        Assert.That(bed, Is.Not.Null);

        // 프리셋의 하위 참조 링크(linkChildToParent)는 스폰 전후 어느 쪽에서도 호출될 수 있다.
        // 스폰 전 호출은 ServerRpc 를 보낼 수 없으므로 권위 초깃값으로 기록돼야 한다.
        bed.ApplyParentEntityIdentifier("patient_b");

        Assert.That(bed.ReposedTargetIdentifier, Is.EqualTo("patient_b"));
        Assert.That(ReadSyncVarString(typeof(MovingPatientBedController), bed, "_reposedTargetIdentifier"),
          Is.EqualTo("patient_b"));
      }
      finally
      {
        Object.DestroyImmediate(bedObject);
      }
    }

    /// <summary>
    /// 제세동 패드처럼 이름으로 지정한 자식 표시를 켜고 끄는 경로는 서버에서 관찰자로 복제되지만,
    /// 복제 여부와 무관하게 호출한 피어에서는 즉시 반영돼야 한다. 복제를 덧붙이면서 로컬 적용이
    /// 사라지면 호스트에서도 표시가 바뀌지 않으므로 그 동작을 고정한다.
    /// </summary>
    [Test]
    public void NamedChildToggleAppliesLocallyOnTheCallingPeer()
    {
      var patientObject = InstantiateIsolated(PatientAPrefabPath);
      try
      {
        var patient = patientObject.GetComponent<PatientController>();
        Assert.That(patient, Is.Not.Null);

        var pad = FindDescendant(patientObject.transform, DefibrillatorPadChildName);
        Assert.That(pad, Is.Not.Null,
          $"'{DefibrillatorPadChildName}' 자식이 환자 A 프리팹에 있어야 이 검사가 의미를 가진다");
        Assert.That(pad.gameObject.activeSelf, Is.False,
          "제세동 패드는 초기 숨김 목록에 있으므로 Awake 직후에는 꺼져 있어야 한다");

        Assert.That(patient.SetNamedChildActive(DefibrillatorPadChildName, true), Is.True);
        Assert.That(pad.gameObject.activeSelf, Is.True);

        Assert.That(patient.SetNamedChildActive(DefibrillatorPadChildName, false), Is.True);
        Assert.That(pad.gameObject.activeSelf, Is.False);

        Assert.That(patient.SetNamedChildActive("no_such_child", true), Is.False);
      }
      finally
      {
        Object.DestroyImmediate(patientObject);
      }
    }

    private static Transform FindDescendant(Transform root, string childName)
    {
      foreach (var child in root.GetComponentsInChildren<Transform>(true))
      {
        if (child != null && string.Equals(child.name, childName, System.StringComparison.Ordinal))
          return child;
      }

      return null;
    }

    /// <summary>
    /// 등록된 모든 엔티티 프리셋을 훑어, 복제용 런타임 식별자 SyncVar 를 가진 자가 등록 컴포넌트가
    /// 스폰 전 주입을 그 SyncVar 까지 반영하는지 확인한다. 개별 클래스마다 테스트를 늘리지 않아도
    /// 새로 추가된 프리셋과 수신자가 자동으로 이 검사 대상에 들어온다.
    ///
    /// <para>
    /// 식별자용 SyncVar 가 없는 수신자(예: ObserversRpc 로 구성을 복제하는 <c>Npc</c>)는 복제 방식이
    /// 다르므로 검사 대상에서 제외한다.
    /// </para>
    /// </summary>
    [Test]
    public void EveryPresetReceiverWithAnIdentifierSyncVarReplicatesTheInjectedIdentifier()
    {
      var presetRegistry = AssetDatabase.LoadAssetAtPath<EntityPresetRegistryRequirementsSO>(PresetRegistryPath);
      Assert.That(presetRegistry, Is.Not.Null, $"preset registry '{PresetRegistryPath}' must exist");
      Assert.That(presetRegistry.entityPresetRegistryRequirements, Is.Not.Null.And.Not.Empty);

      var inspectedPrefabs = new HashSet<GameObject>();
      var coveredReceivers = new List<string>();

      foreach (var requirement in presetRegistry.entityPresetRegistryRequirements)
      {
        if (requirement.prefab == null || !inspectedPrefabs.Add(requirement.prefab))
          continue;

        // 인스턴스화 비용을 피하기 위해 프리팹 자산에서 먼저 수신자와 SyncVar 유무를 판별한다.
        if (requirement.prefab.GetComponentInChildren<ISpawnedEntityIdentifierReceiver>(true)
            is not MonoBehaviour prefabReceiver)
          continue;

        var syncVarField = FindRuntimeIdentifierSyncVarField(prefabReceiver.GetType());
        if (syncVarField == null)
          continue;

        string injected = $"{requirement.identifier}__replication_probe";
        var instance = Object.Instantiate(requirement.prefab, IsolatedSpawnPosition, Quaternion.identity);
        try
        {
          var receiver = instance.GetComponentInChildren<ISpawnedEntityIdentifierReceiver>(true);
          receiver.ApplySpawnedEntityIdentifier(injected);

          var syncVar = syncVarField.GetValue(receiver) as SyncVar<string>;
          Assert.That(syncVar, Is.Not.Null);
          Assert.That(syncVar.Value, Is.EqualTo(injected),
            $"preset '{requirement.identifier}' 의 수신자 {prefabReceiver.GetType().Name} 는 스폰 전 주입값을 "
            + $"'{syncVarField.Name}' 에 기록해야 원격 피어가 같은 식별자로 엔티티를 등록한다");
          coveredReceivers.Add(prefabReceiver.GetType().Name);
        }
        finally
        {
          Object.DestroyImmediate(instance);
          Registry.UnregisterEntity(injected);
        }
      }

      Assert.That(coveredReceivers, Is.Not.Empty,
        "식별자 SyncVar 를 가진 프리셋 수신자를 한 건도 찾지 못했다면 이 검사가 무력화된 것이다");
    }

    /// <summary>
    /// 수신자 타입에서 복제용 런타임 식별자 SyncVar 필드를 찾는다. 클래스마다 이름이
    /// <c>_runtimeIdentifier</c> 또는 <c>_runtimeIdentifierSync</c> 로 갈리므로 접두사로 판별한다.
    /// </summary>
    private static FieldInfo FindRuntimeIdentifierSyncVarField(System.Type receiverType)
    {
      for (var type = receiverType; type != null; type = type.BaseType)
      {
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
          if (field.FieldType == typeof(SyncVar<string>)
              && field.Name.StartsWith("_runtimeIdentifier", System.StringComparison.Ordinal))
          {
            return field;
          }
        }
      }

      return null;
    }

    private static GameObject InstantiateIsolated(string prefabPath)
    {
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      Assert.That(prefab, Is.Not.Null, $"prefab '{prefabPath}' must exist");
      return Object.Instantiate(prefab, IsolatedSpawnPosition, Quaternion.identity);
    }

    private static string ReadSyncVarString(System.Type declaringType, object owner, string fieldName)
    {
      var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
      Assert.That(field, Is.Not.Null, $"'{declaringType.Name}.{fieldName}' SyncVar must exist");

      var syncVar = field.GetValue(owner) as SyncVar<string>;
      Assert.That(syncVar, Is.Not.Null, $"'{fieldName}' must be a replicated SyncVar<string>");
      return syncVar.Value;
    }
  }
}
