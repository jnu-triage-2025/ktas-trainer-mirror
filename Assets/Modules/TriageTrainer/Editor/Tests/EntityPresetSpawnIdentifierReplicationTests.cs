using System.Reflection;
using FishNet.Object.Synchronizing;
using NUnit.Framework;
using TriageTrainer.Entity;
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

    private static readonly Vector3 IsolatedSpawnPosition = new Vector3(14000f, 0f, 14000f);

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
