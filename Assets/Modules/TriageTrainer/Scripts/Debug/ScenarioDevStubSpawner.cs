using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Scenario.Preflight;
using UnityEngine;

namespace TriageTrainer.DebugTools
{
  /// <summary>
  /// IndevScene 등 개발 씬에서, 대상 시나리오가 요구하는 인터랙션 대상/트리거존을 간이 오브젝트로
  /// 즉석 생성해 흐름을 끝까지 시연할 수 있게 하는 디버그 스포너.
  ///
  /// <para>생성 규칙</para>
  /// <list type="bullet">
  /// <item>일반 인터랙션 대상 → 원기둥(Cylinder) + <see cref="ScenarioDevStub"/>(Interactable).</item>
  /// <item>식별자가 트리거존처럼 보이는 대상(이름에 trigger/zone 포함) → 넓고 납작한 통과형 큐브
  ///   (Cube, BoxCollider.isTrigger) + <see cref="ScenarioDevStub"/>(TriggerZone).</item>
  /// </list>
  ///
  /// 생성물은 식별자 그대로 레지스트리에 등록되어 사전 검증(Preflight)을 충족시키고,
  /// 인터랙션/통과 시 시나리오 게이트 신호(sig.*)를 올린다.
  ///
  /// 디버그 전용이며 빌드/프로덕션 씬에는 배치하지 않는 것을 전제로 한다.
  /// </summary>
  public sealed class ScenarioDevStubSpawner : MonoBehaviour
  {
    [Header("대상 시나리오")]
    [Tooltip("요구사항을 추출할 시나리오 JSON(.scenario.json TextAsset).")]
    [SerializeField] private TextAsset _scenarioJson;

    [Header("배치")]
    [Tooltip("생성 오브젝트들의 기준 위치. 비우면 이 컴포넌트의 Transform 을 사용한다.")]
    [SerializeField] private Transform _origin;
    [SerializeField] private float _spacing = 3f;
    [SerializeField] private int _columns = 5;

    [Header("간이 오브젝트 크기")]
    [SerializeField] private Vector3 _interactableScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 _triggerZoneScale = new Vector3(4f, 0.2f, 4f);

    [Header("옵션")]
    [Tooltip("이미 레지스트리에 존재하는(=실제 오브젝트가 있는) 대상은 건너뛴다.")]
    [SerializeField] private bool _skipExisting = true;
    [SerializeField] private bool _logToConsole = true;

    private readonly List<GameObject> _spawned = new();

    [ContextMenu("Spawn Stubs For Scenario")]
    public void SpawnStubs()
    {
      if (_scenarioJson == null)
      {
        Debug.LogWarning("[ScenarioDevStubSpawner] No scenario JSON assigned.", this);
        return;
      }

      ScenarioGraph graph;
      try
      {
        graph = ScenarioGraphLoader.LoadFromJson(_scenarioJson.text);
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"[ScenarioDevStubSpawner] Failed to load scenario: {ex.Message}", this);
        return;
      }

      ClearStubs();

      var requirements = ScenarioRequirementsCollector.Collect(graph);
      var basePosition = (_origin != null ? _origin : transform).position;
      int index = 0;

      foreach (var requirement in requirements)
      {
        if (requirement.Kind != ScenarioRequirementKind.InteractionTarget)
        {
          continue;
        }

        if (_skipExisting && MultiplayerInfrastructure.Registry.Registry.TryGetEntity(requirement.Identifier, out _))
        {
          continue;
        }

        SpawnStub(requirement.Identifier, basePosition, index);
        index++;
      }

      if (_logToConsole)
      {
        Debug.Log($"[ScenarioDevStubSpawner] Spawned {_spawned.Count} stub(s) for scenario '{graph.Identifier}'.", this);
      }
    }

    [ContextMenu("Clear Spawned Stubs")]
    public void ClearStubs()
    {
      foreach (var go in _spawned)
      {
        if (go != null)
        {
          if (Application.isPlaying)
          {
            Destroy(go);
          }
          else
          {
            DestroyImmediate(go);
          }
        }
      }
      _spawned.Clear();
    }

    private void SpawnStub(string identifier, Vector3 basePosition, int index)
    {
      bool isZone = LooksLikeTriggerZone(identifier);

      var primitive = isZone ? PrimitiveType.Cube : PrimitiveType.Cylinder;
      var go = GameObject.CreatePrimitive(primitive);
      go.name = $"[DEV] {(isZone ? "Zone" : "Interactable")}_{identifier}";

      int row = index / Mathf.Max(1, _columns);
      int col = index % Mathf.Max(1, _columns);
      go.transform.SetParent(transform, worldPositionStays: true);
      go.transform.position = basePosition + new Vector3(col * _spacing, 0f, row * _spacing);
      go.transform.localScale = isZone ? _triggerZoneScale : _interactableScale;

      var collider = go.GetComponent<Collider>();
      if (isZone && collider != null)
      {
        collider.isTrigger = true;
      }

      var stub = go.AddComponent<ScenarioDevStub>();
      stub.Configure(
        isZone ? ScenarioDevStub.StubMode.TriggerZone : ScenarioDevStub.StubMode.Interactable,
        identifier,
        signals: null);

      _spawned.Add(go);
    }

    private static bool LooksLikeTriggerZone(string identifier)
    {
      if (string.IsNullOrEmpty(identifier))
      {
        return false;
      }

      var lower = identifier.ToLowerInvariant();
      return lower.Contains("trigger") || lower.Contains("zone") || lower.Contains("room");
    }
  }
}
