using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioGraph
  {
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// 시나리오에서 사용할 태그 사전 선언 목록.
    /// 선언되지 않은 태그가 노드/분기에서 사용되면 로딩 시 경고를 출력합니다.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>이 그래프의 권위 있는 활성 로스터를 이루는 접속 플레이어 역할들.</summary>
    public IReadOnlyList<string> ActiveRoleTags { get; set; } = Array.Empty<string>();

    /// <summary>활성 로스터에 없는 선언 역할의 ByRole 분기를 건너뛸 수 있게 한다.</summary>
    public bool SkipAbsentRoleBranches { get; set; }

    /// <summary>
    /// 플레이어 태그별 체크리스트 아이템 묶음입니다. 한 플레이어가 여러 태그를 보유하면
    /// 해당 태그의 묶음을 모두 합쳐 표시합니다.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ScenarioChecklistItemRequirement>> ChecklistItemSetsByPlayerTag { get; set; }
      = new Dictionary<string, IReadOnlyList<ScenarioChecklistItemRequirement>>(StringComparer.Ordinal);

    /// <summary>클라이언트가 이 그래프에 대해 보고할 수 있는 정확한 일반 신호들.</summary>
    public IReadOnlyList<string> ClientSignalIdentifiers { get; set; } = Array.Empty<string>();

    /// <summary>일반 클라이언트 신호 접두사. 범위가 한정된 게임플레이 소유 네임스페이스에만 사용한다.</summary>
    public IReadOnlyList<string> ClientSignalPrefixes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// 이 시나리오에서 사용할 퀘스트 정의 include 목록.
    /// 각 항목은 Resources/Quest 하위 .quest.json(TextAsset) 파일명을 가리킨다.
    /// </summary>
    public IReadOnlyList<string> QuestDefinitionIncludes { get; set; } = Array.Empty<string>();

    /// <summary>시나리오 시작/종료 수명주기에 종속되는 NPC 등 actingNpc 정의.</summary>
    public IReadOnlyList<ScenarioActingNpcDefinition> ActingNpcs { get; set; } = Array.Empty<ScenarioActingNpcDefinition>();

    /// <summary>
    /// 이 시나리오가 레지스트리에 적용할 인터렉션 정의. 시나리오 초기화 사이클에서 코드 리터럴 정의 위에 병합된다.
    /// 옛 actingNpcs[].interactions 는 로더가 이 목록으로 변환한다.
    /// </summary>
    public IReadOnlyList<InteractableEntity.InteractionDefinition> Interactions { get; set; } = Array.Empty<InteractableEntity.InteractionDefinition>();

    /// <summary>시나리오 시작 전에 생성·등록할 waypoint anchor 정의.</summary>
    public IReadOnlyList<ScenarioWaypointDefinition> Waypoints { get; set; } = Array.Empty<ScenarioWaypointDefinition>();

    /// <summary>이 시나리오에만 적용되는 사용자 정의 TTS 프로필(JSON)입니다.</summary>
    public IReadOnlyList<ScenarioTTSVoiceProfile> TtsVoiceProfiles { get; set; } = Array.Empty<ScenarioTTSVoiceProfile>();

    /// <summary>
    /// startNodeIdentifier 없이 시나리오를 시작할 때 사용할 기본 진입 노드 식별자.
    /// </summary>
    public string DefaultEntrypoint { get; set; }

    public Dictionary<string, IScenarioNode> Nodes { get; } = new Dictionary<string, IScenarioNode>();

    public void Add(IScenarioNode node)
    {
      if (node == null)
        throw new ArgumentNullException(nameof(node));
      Nodes[node.Identifier] = node;
    }

    public bool TryGetNode(string identifier, out IScenarioNode node)
      => Nodes.TryGetValue(identifier, out node);

    public bool IsTagDeclared(string tag)
      => !string.IsNullOrWhiteSpace(tag)
         && Tags != null
         && Tags.Any(each => string.Equals(each, tag, StringComparison.OrdinalIgnoreCase));
  }
}
