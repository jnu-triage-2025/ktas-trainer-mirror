using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  /// <summary>Scene/runtime requirement와 분리된 순수 graph identity 및 target 검증.</summary>
  internal static class ScenarioGraphStructuralValidator
  {
    public static void Validate(ScenarioGraph graph, ScenarioRequirementBuilder diagnostics)
    {
      if (string.IsNullOrWhiteSpace(graph.Identifier))
      {
        diagnostics.AddStructuralDiagnostic(
          "SGR100", "MissingGraphIdentifier", null, "$graph.identifier", graph.Identifier,
          "Scenario graph identifier is null, empty, or whitespace.",
          "Provide a stable scenario identifier.");
      }

      if (graph.Nodes == null || graph.Nodes.Count == 0)
      {
        diagnostics.AddStructuralDiagnostic(
          "SGR101", "EmptyGraph", null, "$graph.nodes", string.Empty,
          "Scenario graph has no nodes.",
          "Add at least one scenario node.");
        return;
      }

      var targetIdentifiers = new HashSet<string>(StringComparer.Ordinal);
      var reportedIdentifiers = new HashSet<string>(StringComparer.Ordinal);
      var branchCompletionEdges = new HashSet<string>(StringComparer.Ordinal);

      foreach (var pair in graph.Nodes.OrderBy(value => value.Key ?? string.Empty, StringComparer.Ordinal))
      {
        if (string.IsNullOrWhiteSpace(pair.Key))
        {
          diagnostics.AddStructuralDiagnostic(
            "SGR102", "InvalidNodeKey", pair.Value, "$graph.nodes", pair.Key,
            "Scenario graph contains a null, empty, or whitespace node key.",
            "Use a non-empty node identifier as the dictionary key.");
        }
        else
        {
          targetIdentifiers.Add(pair.Key);
        }

        if (pair.Value == null)
        {
          diagnostics.AddStructuralDiagnostic(
            "SGR103", "NullNode", null, NodePath(pair.Key), pair.Key,
            $"Scenario graph node '{pair.Key}' is null.",
            "Remove the entry or provide a node instance.");
          continue;
        }

        var node = pair.Value;
        if (string.IsNullOrWhiteSpace(node.Identifier))
        {
          diagnostics.AddStructuralDiagnostic(
            "SGR104", "MissingNodeIdentifier", node, NodePath(pair.Key) + ".identifier", node.Identifier,
            $"Scenario graph node at key '{pair.Key}' has no identifier.",
            "Set the node identifier to its dictionary key.");
        }
        else
        {
          if (!reportedIdentifiers.Add(node.Identifier))
          {
            diagnostics.AddStructuralDiagnostic(
              "SGR105", "DuplicateNodeIdentifier", node, NodePath(pair.Key) + ".identifier", node.Identifier,
              $"Multiple graph entries report node identifier '{node.Identifier}'.",
              "Assign a unique identifier to every node.");
          }

          if (!string.Equals(pair.Key, node.Identifier, StringComparison.Ordinal))
          {
            diagnostics.AddStructuralDiagnostic(
              "SGR106", "NodeIdentityMismatch", node, NodePath(pair.Key) + ".identifier", node.Identifier,
              $"Node key '{pair.Key}' does not match its identifier '{node.Identifier}'.",
              "Make the dictionary key and node identifier exactly equal.");
          }
        }

      }

      CollectBranchCompletionEdges(graph, branchCompletionEdges);

      foreach (var pair in graph.Nodes.OrderBy(value => value.Key ?? string.Empty, StringComparer.Ordinal))
      {
        var node = pair.Value;
        if (node == null) continue;

        if (!(node is ScenarioChoiceNode))
        {
          ValidateTarget(node, "nextIdentifier", node.NextIdentifier, targetIdentifiers, branchCompletionEdges, diagnostics);
        }

        switch (node)
        {
          case ScenarioChoiceNode choice:
            if (choice.Options == null)
            {
              diagnostics.AddStructuralDiagnostic("SGR107", "NullChoiceOptions", node,
                "options", string.Empty, "Choice options collection is null.",
                "Provide at least one choice option.");
              break;
            }
            for (var index = 0; index < choice.Options.Count; index++)
            {
              var option = choice.Options[index];
              if (option == null)
              {
                diagnostics.AddStructuralDiagnostic("SGR107", "NullChoiceOption", node,
                  $"options[{index}]", string.Empty, "Choice contains a null option.",
                  "Remove the null entry or provide an option.");
                continue;
              }
              ValidateTarget(node, $"options[{index}].nextNodeIdentifier", option.NextNodeIdentifier,
                targetIdentifiers, branchCompletionEdges, diagnostics, required: true);
            }
            break;

          case ScenarioQuizNode quiz:
            ValidateTarget(node, "onCorrectNextIdentifier", quiz.OnCorrectNextIdentifier,
              targetIdentifiers, branchCompletionEdges, diagnostics);
            ValidateTarget(node, "onIncorrectNextIdentifier", quiz.OnIncorrectNextIdentifier,
              targetIdentifiers, branchCompletionEdges, diagnostics);
            break;

          case ScenarioValidatorNode validator:
            ValidateTarget(node, "failureNextIdentifier", validator.FailureNextIdentifier,
              targetIdentifiers, branchCompletionEdges, diagnostics);
            break;

          case ScenarioParallelNode parallel:
            if (parallel.Branches == null)
            {
              diagnostics.AddStructuralDiagnostic("SGR108", "NullParallelBranches", node,
                "branches", string.Empty, "Parallel branches collection is null.",
                "Provide the parallel branch list.");
              break;
            }
            for (var index = 0; index < parallel.Branches.Count; index++)
            {
              var branch = parallel.Branches[index];
              if (branch == null)
              {
                diagnostics.AddStructuralDiagnostic("SGR108", "NullParallelBranch", node,
                  $"branches[{index}]", string.Empty, "Parallel node contains a null branch.",
                  "Remove the null entry or provide a branch.");
                continue;
              }
              ValidateTarget(node, $"branches[{index}].identifier", branch.Identifier,
                targetIdentifiers, branchCompletionEdges, diagnostics, required: true);
            }
            break;
        }
      }
    }

    private static void ValidateTarget(
      IScenarioNode source,
      string fieldPath,
      string targetIdentifier,
      ISet<string> targets,
      ISet<string> branchCompletionEdges,
      ScenarioRequirementBuilder diagnostics,
      bool required = false)
    {
      if (string.IsNullOrWhiteSpace(targetIdentifier))
      {
        if (required)
        {
          diagnostics.AddStructuralDiagnostic(
            "SGR109", "MissingTargetIdentifier", source, fieldPath, targetIdentifier,
            $"Required graph target at '{fieldPath}' is null, empty, or whitespace.",
            "Select an existing scenario node.");
        }
        return;
      }

      if (targets.Contains(targetIdentifier)
          || branchCompletionEdges.Contains(EdgeKey(source?.Identifier, targetIdentifier))) return;

      diagnostics.AddStructuralDiagnostic(
        "SGR110", "MissingTargetNode", source, fieldPath, targetIdentifier,
        $"Graph target '{targetIdentifier}' at '{fieldPath}' does not exist.",
        "Point the edge to an existing node or remove it.");
    }

    private static string NodePath(string key)
      => "$graph.nodes[\"" + (key ?? string.Empty).Replace("\"", "\\\"") + "\"]";

    private static void CollectBranchCompletionEdges(ScenarioGraph graph, ISet<string> result)
    {
      foreach (var parallel in graph.Nodes.Values.OfType<ScenarioParallelNode>())
      {
        if (parallel.Branches == null) continue;
        foreach (var branch in parallel.Branches)
        {
          if (branch == null
              || string.IsNullOrWhiteSpace(branch.Identifier)
              || string.IsNullOrWhiteSpace(branch.CompletionConditionIdentifier)) continue;

          var visited = new HashSet<string>(StringComparer.Ordinal);
          var pending = new Queue<string>();
          pending.Enqueue(branch.Identifier);
          while (pending.Count > 0)
          {
            var cursorIdentifier = pending.Dequeue();
            if (string.IsNullOrWhiteSpace(cursorIdentifier)
                || !visited.Add(cursorIdentifier)
                || !graph.Nodes.TryGetValue(cursorIdentifier, out var cursor)
                || cursor == null) continue;

            foreach (var nextIdentifier in GetOutgoingTargets(cursor))
            {
              if (string.Equals(nextIdentifier, branch.CompletionConditionIdentifier, StringComparison.Ordinal))
              {
                result.Add(EdgeKey(cursor.Identifier, branch.CompletionConditionIdentifier));
              }
              else if (!string.Equals(nextIdentifier, parallel.NextIdentifier, StringComparison.Ordinal))
              {
                pending.Enqueue(nextIdentifier);
              }
            }
          }
        }
      }
    }

    private static string EdgeKey(string source, string target)
      => (source ?? string.Empty) + "\u001f" + (target ?? string.Empty);

    private static IEnumerable<string> GetOutgoingTargets(IScenarioNode node)
    {
      if (!string.IsNullOrWhiteSpace(node.NextIdentifier)) yield return node.NextIdentifier;
      if (node is ScenarioChoiceNode choice && choice.Options != null)
      {
        foreach (var option in choice.Options)
          if (!string.IsNullOrWhiteSpace(option?.NextNodeIdentifier)) yield return option.NextNodeIdentifier;
      }
      if (node is ScenarioQuizNode quiz)
      {
        if (!string.IsNullOrWhiteSpace(quiz.OnCorrectNextIdentifier)) yield return quiz.OnCorrectNextIdentifier;
        if (!string.IsNullOrWhiteSpace(quiz.OnIncorrectNextIdentifier)) yield return quiz.OnIncorrectNextIdentifier;
      }
      if (node is ScenarioValidatorNode validator && !string.IsNullOrWhiteSpace(validator.FailureNextIdentifier))
        yield return validator.FailureNextIdentifier;
    }
  }
}
