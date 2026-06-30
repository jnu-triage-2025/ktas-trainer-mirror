using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MultiplayerInfrastructure.Scenario;

namespace MultiplayerInfrastructure.Editor
{
  public readonly struct ScenarioRuntimeHistoryEntry
  {
    public readonly int Sequence;
    public readonly string NodeIdentifier;
    public readonly string DisplayLabel;
    public readonly IScenarioNode NodeData;

    public ScenarioRuntimeHistoryEntry(int sequence, string nodeIdentifier, string displayLabel, IScenarioNode nodeData = null)
    {
      Sequence = sequence;
      NodeIdentifier = nodeIdentifier;
      DisplayLabel = displayLabel;
      NodeData = nodeData;
    }
  }

  public sealed class ScenarioRuntimeHistoryView : VisualElement
  {
    private readonly Button headerButton;
    private readonly Label headerLabel;
    private readonly VisualElement boxContainer;
    private readonly VisualElement resizeHandle;
    private readonly Label emptyLabel;
    private readonly ScrollView scrollView;
    private readonly Button dumpButton;
    private readonly float minHeight = 120f;
    private readonly float maxHeight = 520f;
    private readonly float collapsedHeight = 28f;
    private float historyHeight = 240f;
    private bool isExpanded = true;
    private bool isDragging;
    private int activePointerId = -1;
    private float dragStartPointerY;
    private float dragStartHeight;
    private IReadOnlyList<ScenarioRuntimeHistoryEntry> currentEntries;

    public Action<string> OnNodeSelected;

    public ScenarioRuntimeHistoryView()
    {
      style.flexDirection = FlexDirection.Column;
      style.flexGrow = 0f;
      style.flexShrink = 0f;
      style.minHeight = collapsedHeight;
      style.height = StyleKeyword.Auto;

      headerButton = new Button(ToggleExpanded)
      {
        text = string.Empty
      };
      headerButton.style.height = StyleKeyword.Auto;
      headerButton.style.unityTextAlign = TextAnchor.MiddleLeft;
      headerButton.style.paddingLeft = 4f;
      headerButton.style.paddingRight = 4f;
      headerButton.style.paddingTop = 0f;
      headerButton.style.paddingBottom = 0f;
      headerButton.style.marginBottom = 4f;
      headerButton.style.backgroundColor = StyleKeyword.None;
      headerButton.style.borderTopWidth = 0f;
      headerButton.style.borderBottomWidth = 0f;
      headerButton.style.borderLeftWidth = 0f;
      headerButton.style.borderRightWidth = 0f;
      headerButton.style.flexShrink = 0f;

      headerLabel = new Label();
      headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      headerLabel.style.fontSize = 13;
      headerLabel.style.color = Color.white;

      headerLabel = new Label("Visited Nodes")
      {
        style =
        {
          unityFontStyleAndWeight = FontStyle.Bold,
          marginBottom = 0,
          marginTop = 0,
          fontSize = 11
        }
      };
      
      headerButton.Add(headerLabel);
      Add(headerButton);

      boxContainer = new VisualElement();
      boxContainer.style.flexDirection = FlexDirection.Column;
      boxContainer.style.flexGrow = 0f;
      boxContainer.style.flexShrink = 0f;
      boxContainer.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f, 1f));
      boxContainer.style.borderTopWidth = 1f;
      boxContainer.style.borderBottomWidth = 1f;
      boxContainer.style.borderLeftWidth = 1f;
      boxContainer.style.borderRightWidth = 1f;
      boxContainer.style.borderTopColor = new Color(0.34f, 0.34f, 0.34f, 1f);
      boxContainer.style.borderBottomColor = new Color(0.34f, 0.34f, 0.34f, 1f);
      boxContainer.style.borderLeftColor = new Color(0.34f, 0.34f, 0.34f, 1f);
      boxContainer.style.borderRightColor = new Color(0.34f, 0.34f, 0.34f, 1f);
      boxContainer.style.paddingLeft = 6f;
      boxContainer.style.paddingRight = 6f;
      boxContainer.style.paddingTop = 6f;
      boxContainer.style.paddingBottom = 6f;
      boxContainer.style.minHeight = 0f;
      Add(boxContainer);

      emptyLabel = new Label("시나리오를 재생하면 방문 순서가 여기에 표시됩니다.")
      {
        style =
        {
          whiteSpace = WhiteSpace.Normal,
          fontSize = 10,
          color = new Color(0.82f, 0.82f, 0.82f, 1f),
          marginBottom = 4
        }
      };
      boxContainer.Add(emptyLabel);

      dumpButton = new Button(DumpHistoryToFile)
      {
        text = "Dump History to File"
      };
      dumpButton.style.height = 20f;
      dumpButton.style.fontSize = 10;
      dumpButton.style.marginBottom = 4f;
      dumpButton.style.display = DisplayStyle.None;
      boxContainer.Add(dumpButton);

      scrollView = new ScrollView(ScrollViewMode.Vertical)
      {
        style =
        {
          flexGrow = 1f,
          minHeight = 0f,
          height = historyHeight,
          marginTop = 0f
        }
      };
      boxContainer.Add(scrollView);

      UpdateTitle();
      UpdateExpandedState(true);
      SetHistoryHeight(historyHeight);
    }

    public void SetEntries(IReadOnlyList<ScenarioRuntimeHistoryEntry> entries)
    {
      currentEntries = entries;
      scrollView.Clear();

      if (entries == null || entries.Count == 0)
      {
        UpdateTitle();
        emptyLabel.style.display = DisplayStyle.Flex;
        dumpButton.style.display = DisplayStyle.None;
        return;
      }

      UpdateTitle(entries.Count);
      emptyLabel.style.display = DisplayStyle.None;
      dumpButton.style.display = DisplayStyle.Flex;

      for (var index = 0; index < entries.Count; index++)
      {
        var entry = entries[index];
        var button = new Button(() => OnNodeSelected?.Invoke(entry.NodeIdentifier))
        {
          text = $"{entry.Sequence}. {entry.DisplayLabel}",
          tooltip = entry.NodeIdentifier
        };
        button.style.unityTextAlign = TextAnchor.MiddleLeft;
        button.style.whiteSpace = WhiteSpace.Normal;
        button.style.marginBottom = 0f;
        button.style.paddingLeft = 6f;
        button.style.paddingRight = 6f;
        button.style.paddingTop = 3f;
        button.style.paddingBottom = 3f;
        button.SetEnabled(!string.IsNullOrWhiteSpace(entry.NodeIdentifier));
        scrollView.Add(button);
      }
    }

    private void ToggleExpanded()
    {
      UpdateExpandedState(!isExpanded);
    }

    private void UpdateExpandedState(bool expanded)
    {
      isExpanded = expanded;
      boxContainer.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
      UpdateTitle();
      if (isExpanded)
      {
        style.marginBottom = 10f;
      } else {
        style.marginBottom = 0f;
      }
    }

    private void SetHistoryHeight(float value)
    {
      historyHeight = Mathf.Clamp(value, minHeight, maxHeight);
      if (isExpanded)
      {
        style.marginBottom = 10f;
      } else {
        style.marginBottom = 0f;
      }
      scrollView.style.height = historyHeight;
    }

    private void UpdateTitle(int? entryCount = null)
    {
      var prefix = isExpanded ? "▼" : "▶";
      headerLabel.text = entryCount.HasValue
        ? $"{prefix} Visit History ({entryCount.Value})"
        : $"{prefix} Visit History";
    }

    private void DumpHistoryToFile()
    {
      if (currentEntries == null || currentEntries.Count == 0)
      {
        return;
      }

      var path = EditorUtility.SaveFilePanel("Save Runtime History Dump", "", "scenario_runtime_history.history.log", "history.log");
      if (string.IsNullOrEmpty(path))
      {
        return;
      }

      try
      {
        using (var writer = new StreamWriter(path))
        {
          writer.WriteLine($"Scenario Runtime History Dump - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
          writer.WriteLine(new string('=', 60));
          writer.WriteLine();

          for (var i = 0; i < currentEntries.Count; i++)
          {
            var entry = currentEntries[i];
            writer.WriteLine($"[{entry.Sequence:00}] ID: {entry.NodeIdentifier}");
            writer.WriteLine($"     Type: {entry.NodeData?.NodeType.ToString() ?? "Unknown"}");
            writer.WriteLine($"     Label: {entry.DisplayLabel}");

            // 다음 노드로 넘어가는 흐름 및 조건(브랜칭, 선택, 퀴즈, 게이트 등) 서술
            if (i < currentEntries.Count - 1)
            {
              var nextEntry = currentEntries[i + 1];
              var node = entry.NodeData;
              writer.WriteLine();
              writer.WriteLine("     >>> Next Transition:");
              writer.WriteLine($"         To Node ID: {nextEntry.NodeIdentifier}");

              if (node != null)
              {
                switch (node)
                {
                  case ScenarioChoiceNode choiceNode:
                    writer.WriteLine("         Reason: Choice selected by user");
                    if (choiceNode.Options != null)
                    {
                      foreach (var option in choiceNode.Options)
                      {
                        if (option.NextNodeIdentifier == nextEntry.NodeIdentifier)
                        {
                          writer.WriteLine($"         Selected Option: \"{option.DisplayText}\"");
                          break;
                        }
                      }
                    }
                    break;

                  case ScenarioQuizNode quizNode:
                    if (quizNode.OnCorrectNextIdentifier == nextEntry.NodeIdentifier)
                    {
                      writer.WriteLine("         Reason: Quiz answered CORRECTLY");
                      writer.WriteLine($"         On Correct Path: {quizNode.OnCorrectNextIdentifier}");
                    }
                    else if (quizNode.OnIncorrectNextIdentifier == nextEntry.NodeIdentifier)
                    {
                      writer.WriteLine("         Reason: Quiz answered INCORRECTLY (OnIncorrectNextIdentifier branch)");
                      writer.WriteLine($"         On Incorrect Path: {quizNode.OnIncorrectNextIdentifier}");
                    }
                    else if (quizNode.NextIdentifier == nextEntry.NodeIdentifier)
                    {
                      writer.WriteLine("         Reason: Quiz answered INCORRECTLY (Fallback to NextIdentifier)");
                      writer.WriteLine($"         On Fallback Path: {quizNode.NextIdentifier}");
                    }
                    else
                    {
                      writer.WriteLine("         Reason: Quiz answer transition");
                    }
                    break;

                  case ScenarioValidatorNode validatorNode:
                    if (validatorNode.FailureNextIdentifier == nextEntry.NodeIdentifier)
                    {
                      writer.WriteLine("         Reason: Validator conditions FAILED (Branched/Timed out to FailureNextIdentifier)");
                      writer.WriteLine($"         On Failure Branch: {validatorNode.FailureNextIdentifier}");
                    }
                    else if (validatorNode.NextIdentifier == nextEntry.NodeIdentifier)
                    {
                      writer.WriteLine("         Reason: Validator conditions PASSED (Proceed to NextIdentifier)");
                      writer.WriteLine($"         On Success Path: {validatorNode.NextIdentifier}");
                    }
                    else
                    {
                      writer.WriteLine("         Reason: Validator check transition");
                    }
                    break;

                  default:
                    if (!string.IsNullOrEmpty(node.NextIdentifier) && node.NextIdentifier == nextEntry.NodeIdentifier)
                    {
                      writer.WriteLine("         Reason: Sequential flow (NextIdentifier)");
                    }
                    else
                    {
                      writer.WriteLine("         Reason: Custom/Direct jump transition");
                    }
                    break;
                }
              }
              else
              {
                writer.WriteLine("         Reason: Unknown transition (Node data unavailable)");
              }
            }
            else
            {
              writer.WriteLine();
              writer.WriteLine("     >>> End of History (Last Executed Node)");
            }

            writer.WriteLine();
            writer.WriteLine(new string('-', 60));
            writer.WriteLine();
          }
        }
        Debug.Log($"[ScenarioRuntimeHistoryView] History dumped to: {path}");
      }
      catch (Exception ex)
      {
        Debug.LogError($"[ScenarioRuntimeHistoryView] Failed to dump history: {ex.Message}");
      }
    }
  }
}
