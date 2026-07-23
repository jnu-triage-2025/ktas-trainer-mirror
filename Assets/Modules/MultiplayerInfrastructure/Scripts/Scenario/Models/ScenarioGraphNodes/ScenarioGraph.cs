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

    /// <summary>
    /// 이 시나리오에서 사용할 퀘스트 정의 include 목록.
    /// 각 항목은 Resources/Quest 하위 .quest.json(TextAsset) 파일명을 가리킨다.
    /// </summary>
    public IReadOnlyList<string> QuestDefinitionIncludes { get; set; } = Array.Empty<string>();

    /// <summary>시나리오 시작/종료 수명주기에 종속되는 NPC 등 actingNpc 정의.</summary>
    public IReadOnlyList<ScenarioActingNpcDefinition> ActingNpcs { get; set; } = Array.Empty<ScenarioActingNpcDefinition>();

    /// <summary>
    /// startNodeIdentifier 없이 시나리오를 시작할 때 사용할 기본 진입 노드 식별자.
    /// null이면 nodes의 첫 요소를 사용한다(하위호환).
    /// </summary>
    public string DefaultEntrypoint { get; set; }

    public Dictionary<string, IScenarioNode> Nodes { get; } = new Dictionary<string, IScenarioNode>();

    public void Add(IScenarioNode node)
    {
      if (node == null) throw new ArgumentNullException(nameof(node));
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
