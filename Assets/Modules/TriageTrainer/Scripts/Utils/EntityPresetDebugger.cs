using System.Collections.Generic;
using System.Text;
using FishNet;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects;
using UnityEngine;

namespace TriageTrainer.Utils
{
  /// <summary>
  /// 개발 환경(IndevScene)에서 EntityPreset 의 스폰 여부/동작 여부를 빠르게 확인하기 위한 디버그 도구.
  ///
  /// 사용 목적은 "프리셋이 등록되었는가 / 스폰이 성공하는가 / 자식 분리(ungroup)가 동작하는가 / 결과 엔티티가
  /// 레지스트리에 올라오는가" 정도의 확인이다. 정식 게임플레이 경로가 아니라 디버그 보조용이다.
  ///
  /// 사용법:
  ///  - 이 컴포넌트를 IndevScene 의 빈 GameObject 에 붙이고 인스펙터에 SO 와 (선택)스폰 기준점을 지정한다.
  ///  - 플레이모드(호스트/서버) 진입 후, 컴포넌트 우클릭 ContextMenu 또는 화면 좌상단 디버그 오버레이 버튼을 사용한다.
  ///  - 네트워크 프리셋은 서버 컨텍스트에서만 복제 스폰되므로, 호스트/서버로 실행한 상태에서 확인한다.
  /// </summary>
  [DisallowMultipleComponent]
  public class EntityPresetDebugger : MonoBehaviour
  {
    [Header("References")]
    [SerializeField] private EntityPresetRegistryRequirementsSO _requirementsSO;
    [Tooltip("스폰 기준 위치. 비우면 이 오브젝트의 위치를 사용한다.")]
    [SerializeField] private Transform _spawnOrigin;
    [Tooltip("여러 프리셋을 한꺼번에 스폰할 때 항목 간 간격(m).")]
    [SerializeField] private float _spawnSpacing = 2f;

    [Header("Single Spawn")]
    [Tooltip("'Spawn Selected Preset' 로 스폰할 프리셋 식별자.")]
    [SerializeField] private string _selectedPresetIdentifier;

    [Header("Self Registration (Debug)")]
    [Tooltip("Start 시 SO 의 프리셋을 레지스트리에 자동 등록한다. " +
             "씬에 RegisteringMultiplayerInfrastructureSupport 가 없을 때도 디버거 단독으로 스폰 테스트가 가능하게 한다.")]
    [SerializeField] private bool _autoRegisterOnStart = true;

    [Header("Debug Overlay")]
    [SerializeField] private bool _showOverlay = true;

    // 디버그로 스폰한 결과 추적(정리용 + 동작 확인용).
    private readonly List<string> _debugSpawnedIdentifiers = new();
    private string _lastResultSummary = "(아직 실행 안 함)";

    private Vector3 OriginPosition => _spawnOrigin != null ? _spawnOrigin.position : transform.position;

    private void Start()
    {
      if (_autoRegisterOnStart)
      {
        RegisterPresetsFromSO();
      }
    }

    // ── ContextMenu 액션 ─────────────────────────────────────────────────

    [ContextMenu("Register Presets From SO")]
    public void RegisterPresetsFromSO()
    {
      if (_requirementsSO?.entityPresetRegistryRequirements == null)
      {
        _lastResultSummary = "SO 미지정 또는 항목 없음";
        Debug.LogWarning("[EntityPresetDebugger] SO 가 지정되지 않았거나 항목이 없습니다.", this);
        return;
      }

      int registered = 0;
      foreach (var req in _requirementsSO.entityPresetRegistryRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.identifier) || req.prefab == null)
        {
          continue;
        }

        // 이미 등록되어 있으면(부트스트랩이 등록) 건너뛴다.
        if (Registry.TryGetEntityPreset(req.identifier, out var existing) && existing != null)
        {
          continue;
        }

        Registry.RegisterEntityPreset(
          req.identifier,
          req.fallbackEntityType,
          req.prefab,
          req.displayName,
          req.isNetworked,
          req.childReferences);
        registered++;
      }

      _lastResultSummary = $"프리셋 등록: {registered}개 신규(SO 기준)";
      Debug.Log($"[EntityPresetDebugger] {_lastResultSummary}", this);
    }

    [ContextMenu("List Registered Presets")]
    public void ListRegisteredPresets()
    {
      var presets = Registry.GetAllEntityPresets();
      var sb = new StringBuilder();
      sb.AppendLine($"[EntityPresetDebugger] 등록된 프리셋: {presets.Count}");
      foreach (var kv in presets)
      {
        var d = kv.Value;
        int childCount = d?.ChildReferences?.Count ?? 0;
        sb.AppendLine($"  - {kv.Key} (type={d?.EntityType}, networked={d?.IsNetworked}, children={childCount}, prefab={(d?.Prefab != null)})");
      }
      _lastResultSummary = $"프리셋 {presets.Count}개 등록됨";
      Debug.Log(sb.ToString(), this);
    }

    [ContextMenu("Spawn Selected Preset")]
    public void SpawnSelectedPreset()
    {
      SpawnOne(_selectedPresetIdentifier, OriginPosition);
    }

    [ContextMenu("Spawn All Presets")]
    public void SpawnAllPresets()
    {
      if (_requirementsSO?.entityPresetRegistryRequirements == null)
      {
        _lastResultSummary = "SO 미지정 또는 항목 없음";
        Debug.LogWarning("[EntityPresetDebugger] SO 가 지정되지 않았거나 항목이 없습니다.", this);
        return;
      }

      int i = 0, ok = 0;
      foreach (var req in _requirementsSO.entityPresetRegistryRequirements)
      {
        if (string.IsNullOrWhiteSpace(req.identifier))
          continue;

        Vector3 pos = OriginPosition + new Vector3(i * _spawnSpacing, 0f, 0f);
        if (SpawnOne(req.identifier, pos))
          ok++;
        i++;
      }
      _lastResultSummary = $"전체 스폰: {ok}/{i} 성공";
      Debug.Log($"[EntityPresetDebugger] {_lastResultSummary}", this);
    }

    [ContextMenu("Report Registered Entities")]
    public void ReportRegisteredEntities()
    {
      var entities = Registry.GetAllEntities();
      var sb = new StringBuilder();
      sb.AppendLine($"[EntityPresetDebugger] 현재 등록된 엔티티: {entities.Count}");
      foreach (var kv in entities)
      {
        var d = kv.Value;
        sb.AppendLine($"  - {kv.Key} (type={d?.EntityType}, networked={d?.IsNetworked}, alive={(d?.GameObject != null)})");
      }
      _lastResultSummary = $"엔티티 {entities.Count}개 등록됨";
      Debug.Log(sb.ToString(), this);
    }

    [ContextMenu("Despawn Debug Spawns")]
    public void DespawnDebugSpawns()
    {
      int removed = 0;
      foreach (var id in _debugSpawnedIdentifiers)
      {
        if (Registry.TryGetEntity(id, out var d) && d?.GameObject != null)
        {
          var go = d.GameObject;
          if (InstanceFinder.IsServerStarted && go.TryGetComponent(out FishNet.Object.NetworkObject nob) && nob.IsSpawned)
          {
            InstanceFinder.ServerManager.Despawn(go);
          }
          else
          {
            Destroy(go);
          }
          removed++;
        }
        Registry.UnregisterEntity(id);
      }
      _debugSpawnedIdentifiers.Clear();
      _lastResultSummary = $"디버그 스폰 {removed}개 정리";
      Debug.Log($"[EntityPresetDebugger] {_lastResultSummary}", this);
    }

    // ── 내부 ─────────────────────────────────────────────────────────────

    private bool SpawnOne(string presetIdentifier, Vector3 position)
    {
      if (string.IsNullOrWhiteSpace(presetIdentifier))
      {
        _lastResultSummary = "프리셋 식별자가 비어 있음";
        Debug.LogWarning("[EntityPresetDebugger] 프리셋 식별자가 비어 있습니다.", this);
        return false;
      }

      // 디버그 편의: 아직 레지스트리에 없으면 SO 기준으로 등록을 시도한 뒤 스폰한다.
      if (!Registry.TryGetEntityPreset(presetIdentifier, out _))
      {
        RegisterPresetsFromSO();
      }

      bool ok = Registry.TrySpawnEntityPreset(
        presetIdentifier, position, Quaternion.identity,
        out _, out var descriptor, out var error);

      if (ok)
      {
        string id = descriptor?.Identifier ?? "(unknown)";
        if (!string.IsNullOrWhiteSpace(id))
          _debugSpawnedIdentifiers.Add(id);
        _lastResultSummary = $"'{presetIdentifier}' 스폰 OK -> {id}";
        Debug.Log($"[EntityPresetDebugger] {_lastResultSummary} @ {position}", this);
      }
      else
      {
        _lastResultSummary = $"'{presetIdentifier}' 스폰 실패: {error}";
        Debug.LogWarning($"[EntityPresetDebugger] {_lastResultSummary}", this);
      }
      return ok;
    }

    // ── 런타임 오버레이(콘솔 없이도 상태 확인) ────────────────────────────
    private void OnGUI()
    {
      if (!_showOverlay || !Application.isPlaying)
        return;

      const float w = 360f;
      GUILayout.BeginArea(new Rect(10, 10, w, 220), GUI.skin.box);
      GUILayout.Label("<b>EntityPreset Debugger</b>");
      GUILayout.Label($"Server: {InstanceFinder.IsServerStarted}");
      int presetCount = Registry.GetAllEntityPresets().Count;
      int entityCount = Registry.GetAllEntities().Count;
      GUILayout.Label($"Presets: {presetCount} | Entities: {entityCount} | DebugSpawned: {_debugSpawnedIdentifiers.Count}");
      GUILayout.Label($"Last: {_lastResultSummary}");

      if (GUILayout.Button("Register Presets From SO")) RegisterPresetsFromSO();
      if (GUILayout.Button("Spawn All Presets")) SpawnAllPresets();
      if (GUILayout.Button($"Spawn Selected ('{_selectedPresetIdentifier}')")) SpawnSelectedPreset();
      if (GUILayout.Button("Report Registered Entities")) ReportRegisteredEntities();
      if (GUILayout.Button("Despawn Debug Spawns")) DespawnDebugSpawns();
      GUILayout.EndArea();
    }
  }
}
