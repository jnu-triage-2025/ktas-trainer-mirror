using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using NUnit.Framework;

namespace MultiplayerInfrastructure.Tests.Scenario
{
  /// <summary>
  /// "여는 데 성공한 그래프를 다시 저장하면 검증에 실패하는" 문제 상황의 회귀 테스트.
  ///
  /// 저장 경로(<see cref="ScenarioGraphLoader.SaveToJson"/>)는 파일 원문이 아니라
  /// DTO 에서 재생성된 JSON 을 스키마로 검증한다. 따라서 에디터에서 만들 수 있는
  /// 모든 합법적 그래프 상태(링크 해제 포함)가 스키마 유효한 JSON 을 산출해야 한다.
  /// </summary>
  public sealed class ScenarioGraphSaveRoundTripTests
  {
    /// <summary>
    /// 선택지 대상이 끊긴(엣지 해제/대상 노드 삭제) 상태 — option.NextNodeIdentifier == null.
    /// 런타임은 null 을 "이 선택지는 시나리오 종료"로 취급하므로 저장도 유효해야 한다.
    /// </summary>
    [Test]
    public void DisconnectedChoiceOptionLinkSavesAndRoundTrips()
    {
      var graph = new ScenarioGraph { Identifier = "choice-null-link" };
      graph.Add(new ScenarioChoiceNode
      {
        Identifier = "choice",
        DialogueContent = "선택하세요",
        Options = new List<ScenarioChoiceOption>
        {
          new ScenarioChoiceOption { DisplayText = "A", NextNodeIdentifier = null },
          new ScenarioChoiceOption { DisplayText = "B", NextNodeIdentifier = "end" }
        }
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "end", DialogueContent = "종료" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedChoice = (ScenarioChoiceNode)reloaded.Nodes["choice"];
      Assert.That(reloadedChoice.Options[0].NextNodeIdentifier, Is.Null);
      Assert.That(reloadedChoice.Options[1].NextNodeIdentifier, Is.EqualTo("end"));
    }

    /// <summary>
    /// 퀴즈 정답 링크가 끊긴 상태 — OnCorrectNextIdentifier == null.
    /// 런타임은 null 이면 NextIdentifier 로 폴백하므로 저장도 유효해야 한다.
    /// </summary>
    [Test]
    public void DisconnectedQuizCorrectLinkSavesAndRoundTrips()
    {
      var graph = new ScenarioGraph { Identifier = "quiz-null-link" };
      graph.Add(new ScenarioQuizNode
      {
        Identifier = "quiz",
        Question = "질문",
        Options = new List<string> { "X", "Y" },
        CorrectIndex = 0,
        OnCorrectNextIdentifier = null,
        OnIncorrectNextIdentifier = null,
        NextIdentifier = "fallback"
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "fallback", DialogueContent = "폴백" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedQuiz = (ScenarioQuizNode)reloaded.Nodes["quiz"];
      Assert.That(reloadedQuiz.OnCorrectNextIdentifier, Is.Null);
      Assert.That(reloadedQuiz.NextIdentifier, Is.EqualTo("fallback"));
    }

    /// <summary>
    /// 병렬 브랜치가 끊기면 에디터는 identifier 를 null 대신 branch_N placeholder 로 채운다.
    /// 그 상태가 스키마 유효하게 저장되어야 한다.
    /// </summary>
    [Test]
    public void DisconnectedParallelBranchPlaceholderSavesAndRoundTrips()
    {
      var graph = new ScenarioGraph { Identifier = "parallel-placeholder" };
      graph.Add(new ScenarioParallelNode
      {
        Identifier = "parallel",
        Branches = new List<ScenarioParallelBranch>
        {
          new ScenarioParallelBranch { Identifier = "stage_a" },
          new ScenarioParallelBranch { Identifier = "branch_2" } // placeholder(미연결) 상태
        },
        NextIdentifier = "merge"
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "stage_a", DialogueContent = "A" });
      graph.Add(new ScenarioDialogueNode { Identifier = "merge", DialogueContent = "합류" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedParallel = (ScenarioParallelNode)reloaded.Nodes["parallel"];
      Assert.That(reloadedParallel.Branches[1].Identifier, Is.EqualTo("branch_2"));
    }

    /// <summary>
    /// 병렬 노드의 nextIdentifier 는 저장 후에도 보존되어야 한다.
    /// (과거 Parallel 전용 수동 Writer 가 nextIdentifier 를 생략해 저장마다
    /// 다음 링크가 조용히 사라지던 데이터 유실 버그의 회귀 테스트.)
    /// </summary>
    [Test]
    public void ParallelNextIdentifierSurvivesSave()
    {
      var graph = new ScenarioGraph { Identifier = "parallel-next" };
      graph.Add(new ScenarioParallelNode
      {
        Identifier = "parallel",
        Branches = new List<ScenarioParallelBranch>
        {
          new ScenarioParallelBranch { Identifier = "stage_a" },
          new ScenarioParallelBranch { Identifier = "stage_b" }
        },
        NextIdentifier = "after_parallel"
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "stage_a", DialogueContent = "A" });
      graph.Add(new ScenarioDialogueNode { Identifier = "stage_b", DialogueContent = "B" });
      graph.Add(new ScenarioDialogueNode { Identifier = "after_parallel", DialogueContent = "다음" });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedParallel = (ScenarioParallelNode)reloaded.Nodes["parallel"];
      Assert.That(reloadedParallel.NextIdentifier, Is.EqualTo("after_parallel"),
          "병렬 노드의 nextIdentifier 가 저장 라운드트립에서 유실되면 안 된다.");
    }

    /// <summary>
    /// Validator 의 기본 matchMode(All) 는 저장 시 생략되어야 한다.
    /// (과거 "기본값 → null" 매핑이 null 생략 없이 그대로 기록되어
    /// 여는 데 성공한 모든 Validator 그래프의 재저장이 실패하던 버그의 회귀 테스트.)
    /// </summary>
    [Test]
    public void ValidatorDefaultMatchModeSavesCleanly()
    {
      var graph = new ScenarioGraph { Identifier = "validator-default" };
      graph.Add(new ScenarioValidatorNode
      {
        Identifier = "validator",
        RootConditions = new List<ScenarioValidatorRootCondition>
        {
          new ScenarioValidatorRootCondition
          {
            Condition = ScenarioValidatorCondition.PlayerCountGreaterThanOrEqual,
            TargetCount = 1
            // MatchMode 미지정 → 기본 All → 저장 시 생략되어야 함
          }
        }
      });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedValidator = (ScenarioValidatorNode)reloaded.Nodes["validator"];
      Assert.That(reloadedValidator.RootConditions[0].MatchMode, Is.EqualTo(ScenarioValidatorMatchMode.All));
    }

    /// <summary>
    /// 외부 quest definition을 사용하는 QuestControl은 inline quest가 없어도 유효하다.
    /// 에디터에서 이 노드를 선택한 뒤에도 quest:null이 유지되어야 한다.
    /// </summary>
    [Test]
    public void QuestControlWithDefinitionKeepsNullInlineQuestAndSaves()
    {
      var graph = new ScenarioGraph { Identifier = "quest-definition-only" };
      graph.Add(new ScenarioQuestControlNode
      {
        Identifier = "quest",
        Operation = ScenarioQuestOperationType.Add,
        FailureStrategy = ScenarioQuestFailureStrategy.Overwrite,
        QuestDefinitionIdentifier = "tutorial-quest-crafting",
        Quest = null
      });

      var json = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var reloaded = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      var reloadedQuest = (ScenarioQuestControlNode)reloaded.Nodes["quest"];

      Assert.That(reloadedQuest.QuestDefinitionIdentifier, Is.EqualTo("tutorial-quest-crafting"));
      Assert.That(reloadedQuest.Quest, Is.Null);
    }

    /// <summary>
    /// 저장→로드→저장 멱등성: 한 번 저장한 결과는 다시 저장해도 검증이 통과해야 한다.
    /// </summary>
    [Test]
    public void SaveLoadSaveIsIdempotentAndValid()
    {
      var graph = new ScenarioGraph { Identifier = "round-trip" };
      graph.Add(new ScenarioDialogueNode { Identifier = "start", DialogueContent = "시작", NextIdentifier = "choice" });
      graph.Add(new ScenarioChoiceNode
      {
        Identifier = "choice",
        DialogueContent = "선택",
        Options = new List<ScenarioChoiceOption>
        {
          new ScenarioChoiceOption { DisplayText = "A", NextNodeIdentifier = "end" },
          new ScenarioChoiceOption { DisplayText = "B", NextNodeIdentifier = null }
        }
      });
      graph.Add(new ScenarioDialogueNode { Identifier = "end", DialogueContent = "끝" });

      var firstSave = ScenarioGraphLoader.SaveToJson(graph, validateWithSchema: true);
      var loaded = ScenarioGraphLoader.LoadFromJson(firstSave, validateWithSchema: true);
      var secondSave = ScenarioGraphLoader.SaveToJson(loaded, validateWithSchema: true);

      var reloaded = ScenarioGraphLoader.LoadFromJson(secondSave, validateWithSchema: true);
      Assert.That(reloaded.Nodes.Count, Is.EqualTo(3));
    }
  }
}
