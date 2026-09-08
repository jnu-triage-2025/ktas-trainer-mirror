#if UNITY_E2E || UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MultiplayerInfrastructure.Automation
{
  internal static class AutomationContent
  {
    private static JArray _graphs;
    internal static void Reset() => _graphs = null;
    internal static JObject Catalogue()
    {
      var graphs = _graphs;
      if (graphs == null) {
      graphs = new JArray();
      foreach (var asset in Resources.LoadAll<TextAsset>("Scenario").Concat(AutomationFixtures.Assets).OrderBy(asset => asset.name, StringComparer.Ordinal))
      {
        if (!asset.name.EndsWith(".scenario", StringComparison.Ordinal)) continue;
        var graph = JObject.Parse(asset.text);
        graphs.Add(new JObject { ["graphId"] = graph["identifier"], ["syntheticFixture"] = AutomationFixtures.Assets.Contains(asset), ["authoritative"] = Scenario.ScenarioNetworkRelay.AutomationCompatibility((string)graph["identifier"]), ["hash"] = Hash(asset.text),
          ["activeRoleTags"] = graph["activeRoleTags"] ?? new JArray(),
          ["checklistItemSetsByPlayerTag"] = graph["checklistItemSetsByPlayerTag"] ?? new JObject(),
          ["nodes"] = new JArray((graph["nodes"] as JObject ?? new JObject()).Properties().Where(node => node.Value is JObject).Select(node => new JObject {
            ["nodeId"] = node.Name, ["nodeType"] = node.Value["nodeType"],
            ["eventIdentifier"] = node.Value["eventIdentifier"],
            ["choices"] = new JArray((node.Value["options"] as JArray ?? new JArray()).OfType<JObject>().SelectMany((choice,index) => new JToken[] { node.Name + "#" + index, choice["nextNodeIdentifier"] }))
          })) });
      }
      _graphs = graphs;
      }
      var packs = new JArray();
      var directory = Datapack.DatapackRuntimeService.DatapackRootPath;
      if (!Directory.Exists(directory) || Directory.GetFiles(directory,"*.datapack.json",SearchOption.AllDirectories).Length == 0)
        directory = Path.Combine(Application.streamingAssetsPath, "DataPacks");
      if (Directory.Exists(directory)) foreach (var path in Directory.GetFiles(directory, "*.datapack.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
      {
        string packText = File.ReadAllText(path);
        packs.Add(new JObject { ["packId"] = JObject.Parse(packText)["packId"], ["hash"] = Hash(packText) });
      }
      var content = new JObject { ["graphs"] = graphs, ["dataPacks"] = packs };
      content["contentHash"] = Hash(content.ToString(Newtonsoft.Json.Formatting.None));
      content["dataPackSource"] = directory;
      // Handler registration depends on the loaded scene; keep it outside the immutable content hash.
      content["eventHandlerDiagnostics"] = new JArray(graphs.OfType<JObject>().SelectMany(graph =>
        ((JArray)graph["nodes"]).OfType<JObject>()
          .Where(node => (string)node["nodeType"] == "InvokeEvent" && !string.IsNullOrWhiteSpace((string)node["eventIdentifier"]))
          .Select(node => new JObject {
            ["graphId"] = graph["graphId"], ["nodeId"] = node["nodeId"], ["eventIdentifier"] = node["eventIdentifier"],
            ["handlerRegistered"] = Scenario.ScenarioEventIdentifierRegistry.TryGetHandler((string)node["eventIdentifier"], out _)
          })));
      return (JObject)content.DeepClone();
    }
    internal static JObject MemoryResources()
    {
      var meshUsers = Resources.FindObjectsOfTypeAll<MeshFilter>()
        .Where(value => value.sharedMesh != null).Select(value => new { Mesh = value.sharedMesh, Transform = value.transform })
        .Concat(Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>().Where(value => value.sharedMesh != null)
          .Select(value => new { Mesh = value.sharedMesh, Transform = value.transform }))
        .GroupBy(value => value.Mesh.GetInstanceID()).ToDictionary(group => group.Key, group => group.ToArray());
      var resources = Resources.FindObjectsOfTypeAll<Texture>().Cast<UnityEngine.Object>()
        .Concat(Resources.FindObjectsOfTypeAll<Mesh>()).Select(value => new {
          Value = value, Bytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(value)
        }).OrderByDescending(entry => entry.Bytes).ToArray();
      return new JObject {
        ["measurement"] = "Unity runtime object memory estimate; not graphics driver allocation",
        ["frame"] = Time.frameCount, ["count"] = resources.Length,
        ["estimatedBytes"] = resources.Sum(entry => entry.Bytes),
        ["largest"] = new JArray(resources.Take(32).Select(entry => {
          var value = entry.Value;
          var item = new JObject { ["name"] = value.name, ["type"] = value.GetType().Name,
            ["instanceId"] = value.GetInstanceID(), ["estimatedBytes"] = entry.Bytes };
          if (value is Texture texture) { item["width"] = texture.width; item["height"] = texture.height; }
          if (value is Texture2D texture2D) item["mipmapCount"] = texture2D.mipmapCount;
          if (value is Mesh mesh) {
            item["vertexCount"] = mesh.vertexCount;
            if (meshUsers.TryGetValue(mesh.GetInstanceID(), out var users)) {
              item["userCount"] = users.Length;
              item["users"] = new JArray(users.Take(16).Select(user => {
                var path = user.Transform.name;
                for (var parent = user.Transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
                return new JObject { ["path"] = path, ["scene"] = user.Transform.gameObject.scene.name,
                  ["active"] = user.Transform.gameObject.activeInHierarchy };
              }));
            }
          }
          return item;
        }))
      };
    }
    private static string Hash(string content)
    {
      using var sha = SHA256.Create();
      return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(content))).Replace("-", "").ToLowerInvariant();
    }
  }
}
#endif
