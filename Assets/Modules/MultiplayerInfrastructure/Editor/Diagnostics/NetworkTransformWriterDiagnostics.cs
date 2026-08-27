using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FishNet.Component.Transforming;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Diagnostics
{
  /// <summary>
  /// NetworkTransform 이 "틱 구독은 살아 있는데 송신 버퍼는 반납된" 상태에 빠졌는지 찾아내는 진단.
  ///
  /// <para>
  /// 그 상태가 되면 <c>SendToClients()</c>의 <c>writer.Clear()</c>에서 매 틱 NullReferenceException 이
  /// 난다. 예외 스택에는 어떤 오브젝트인지가 남지 않아 원인 지목이 어렵다. 이 진단은 살아 있는 모든
  /// NetworkTransform 을 훑어 그 조합에 해당하는 것을 이름과 씬까지 찍는다.
  /// </para>
  ///
  /// <para>
  /// 읽기 전용이며 FishNet 을 수정하지 않는다. 필드가 private 이라 리플렉션으로 들여다보고, 필드 이름이
  /// 바뀌면 조용히 틀린 답을 내지 않도록 그 사실을 보고한다.
  /// </para>
  /// </summary>
  public static class NetworkTransformWriterDiagnostics
  {
    private const string WriterFieldName = "_toClientChangedWriter";
    private const string SubscribedFieldName = "_subscribedToTicks";

    /*[MenuItem("Tools/Diagnostics/NetworkTransform 송신 버퍼 상태 점검")]*/
    public static void Scan()
    {
      var writerField = typeof(NetworkTransform).GetField(
        WriterFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
      var subscribedField = typeof(NetworkTransform).GetField(
        SubscribedFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

      if (writerField == null || subscribedField == null)
      {
        Debug.LogWarning(
          $"[NetworkTransformDiagnostics] NetworkTransform 의 '{WriterFieldName}' 또는 "
          + $"'{SubscribedFieldName}' 필드를 찾지 못했습니다. FishNet 버전이 바뀌었을 수 있습니다.");
        return;
      }

      var all = Resources.FindObjectsOfTypeAll<NetworkTransform>();
      var suspects = new List<NetworkTransform>();
      var report = new StringBuilder(512);
      report.AppendLine($"[NetworkTransformDiagnostics] NetworkTransform {all.Length}개 점검");

      for (int i = 0; i < all.Length; i++)
      {
        var each = all[i];
        if (each == null || EditorUtility.IsPersistent(each))
          continue;

        bool writerMissing = writerField.GetValue(each) == null;
        bool subscribed = subscribedField.GetValue(each) is true;
        bool serverInitialized = SafeReadServerInitialized(each);

        // 이 셋이 동시에 성립할 때만 SendToClients 가 null 버퍼에 닿는다.
        if (!writerMissing || !subscribed || !serverInitialized)
          continue;

        suspects.Add(each);
        report.Append("  - ").Append(Describe(each)).AppendLine();
      }

      if (suspects.Count == 0)
      {
        report.AppendLine("  문제 상태인 NetworkTransform 은 없습니다.");
        Debug.Log(report.ToString());
        return;
      }

      report.AppendLine();
      report.AppendLine(
        "  위 오브젝트는 틱 구독이 남은 채 송신 버퍼가 반납된 상태입니다. 스폰된 NetworkObject 를 "
        + "Despawn 이 아니라 Destroy 로 없앴거나, NetworkManager 참조가 끊긴 뒤 정리가 돌면 이 상태가 됩니다.");
      Debug.LogWarning(report.ToString(), suspects[0]);
      Selection.objects = ToObjectArray(suspects);
    }

    private static bool SafeReadServerInitialized(NetworkTransform target)
    {
      // 파괴된 컴포넌트에서도 읽히도록 예외를 막는다. 진단이 진단 대상 때문에 죽으면 안 된다.
      try
      {
        return target.IsServerInitialized;
      }
      catch (Exception)
      {
        return false;
      }
    }

    private static string Describe(NetworkTransform target)
    {
      var go = target.gameObject;
      string scene = go.scene.IsValid() ? go.scene.name : "(no scene)";
      return $"{GetHierarchyPath(go)}  [scene={scene}, activeInHierarchy={go.activeInHierarchy}]";
    }

    private static string GetHierarchyPath(GameObject go)
    {
      var path = new StringBuilder(go.name);
      var cursor = go.transform.parent;
      while (cursor != null)
      {
        path.Insert(0, cursor.name + "/");
        cursor = cursor.parent;
      }

      return path.ToString();
    }

    private static UnityEngine.Object[] ToObjectArray(List<NetworkTransform> suspects)
    {
      var objects = new UnityEngine.Object[suspects.Count];
      for (int i = 0; i < suspects.Count; i++)
        objects[i] = suspects[i].gameObject;
      return objects;
    }
  }
}
