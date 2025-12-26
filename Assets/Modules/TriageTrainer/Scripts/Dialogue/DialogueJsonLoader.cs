using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriageTrainer.Dialogue
{
  /// <summary>
  /// JSON 파일에서 DialogueSession을 로드합니다.
  /// </summary>
  public static class DialogueJsonLoader
  {
    #region JSON Serializable Classes

    [Serializable]
    private class JsonDialogueSession
    {
      public string dialogueId;
      public JsonMetadata metadata;
      public List<JsonDialogueNodeData> dialogues;
    }

    [Serializable]
    private class JsonMetadata
    {
      public string author;
      public string version;
      public string description;
    }

    [Serializable]
    private class JsonDialogueNodeData
    {
      public string identifier;
      public string speakerName;
      public string dialogueText;
      public string nextDialogueIdentifier;
      public float customTypingSpeed = 0.05f;
      public List<JsonSelection> selections;
    }

    [Serializable]
    private class JsonSelection
    {
      public string identifier;
      public string displayText;
      public string nextDialogueIdentifier;
    }

    #endregion

    #region Public API

    public static DialogueSession Load(TextAsset textAsset)
    {
      if (textAsset == null)
      {
        Debug.LogError("[DialogueJsonLoader] TextAsset is null");
        return null;
      }

      return Parse(textAsset.text);
    }

    public static DialogueSession LoadFromResources(string resourcePath)
    {
      var textAsset = Resources.Load<TextAsset>(resourcePath);

      if (textAsset == null)
      {
        Debug.LogError($"[DialogueJsonLoader] Resource not found: {resourcePath}");
        return null;
      }

      return Load(textAsset);
    }

    public static DialogueSession Parse(string jsonContent)
    {
      if (string.IsNullOrEmpty(jsonContent))
      {
        Debug.LogError("[DialogueJsonLoader] JSON content is empty");
        return null;
      }

      try
      {
        var jsonSession = JsonUtility.FromJson<JsonDialogueSession>(jsonContent);
        return ConvertToDialogueSession(jsonSession);
      }
      catch (Exception e)
      {
        Debug.LogError($"[DialogueJsonLoader] Parse error: {e.Message}");
        return null;
      }
    }

    #endregion

    #region Conversion

    private static DialogueSession ConvertToDialogueSession(JsonDialogueSession json)
    {
      if (json == null)
        return null;

      var metadata = ConvertMetadata(json.metadata);
      var nodes = ConvertNodes(json.dialogues);

      return new DialogueSession(json.dialogueId, metadata, nodes);
    }

    private static DialogueMetadata ConvertMetadata(JsonMetadata json)
    {
      if (json == null)
        return new DialogueMetadata();

      return new DialogueMetadata(
          json.author ?? "",
          json.version ?? "1.0.0",
          json.description ?? ""
      );
    }

    private static List<DialogueNodeData> ConvertNodes(List<JsonDialogueNodeData> jsonNodes)
    {
      var nodes = new List<DialogueNodeData>();

      if (jsonNodes == null)
        return nodes;

      foreach (var jsonNode in jsonNodes)
      {
        var selections = ConvertSelections(jsonNode.selections);

        var node = new DialogueNodeData(
            jsonNode.identifier,
            jsonNode.speakerName ?? "",
            jsonNode.dialogueText ?? "",
            jsonNode.nextDialogueIdentifier,
            jsonNode.customTypingSpeed,
            selections
        );

        nodes.Add(node);
      }

      return nodes;
    }

    private static List<DialogueSelectionData> ConvertSelections(List<JsonSelection> jsonSelections)
    {
      var selections = new List<DialogueSelectionData>();

      if (jsonSelections == null)
        return selections;

      foreach (var jsonSelection in jsonSelections)
      {
        // 빌더 패턴 사용
        var selection = new DialogueSelectionData()
            .WithIdentifier(jsonSelection.identifier)
            .WithNextDialogue(jsonSelection.nextDialogueIdentifier);

        selection.SelectionText = jsonSelection.displayText ?? "";

        selections.Add(selection);
      }

      return selections;
    }

    #endregion
  }
}
