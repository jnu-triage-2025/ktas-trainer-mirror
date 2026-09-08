#if UNITY_E2E || UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
namespace MultiplayerInfrastructure.Automation
{
  internal static class AutomationFixtures
  {
    internal static TextAsset Asset { get; private set; }
    internal static readonly List<TextAsset> Assets = new();
    internal static void Register()
    {
      Assets.Clear();
      Asset = new TextAsset(@"{
  ""identifier"": ""e2e_authoritative_dialogue"",
  ""defaultEntrypoint"": ""intro"",
  ""tags"": [
    ""e2e_fixture""
  ],
  ""nodes"": {
    ""intro"": {
      ""identifier"": ""intro"",
      ""nodeType"": ""Dialogue"",
      ""speakerName"": ""E2E"",
      ""dialogueContent"": ""Choose the second option."",
      ""nextIdentifier"": ""choice""
    },
    ""choice"": {
      ""identifier"": ""choice"",
      ""nodeType"": ""Choice"",
      ""speakerName"": ""E2E"",
      ""dialogueContent"": ""Select a route."",
      ""options"": [
        {
          ""displayText"": ""Reject"",
          ""nextNodeIdentifier"": ""rejected""
        },
        {
          ""displayText"": ""Accept"",
          ""nextNodeIdentifier"": ""accepted""
        }
      ]
    },
    ""accepted"": {
      ""identifier"": ""accepted"",
      ""nodeType"": ""Dialogue"",
      ""speakerName"": ""E2E"",
      ""dialogueContent"": ""Accepted."",
      ""nextIdentifier"": null
    },
    ""rejected"": {
      ""identifier"": ""rejected"",
      ""nodeType"": ""Dialogue"",
      ""speakerName"": ""E2E"",
      ""dialogueContent"": ""Rejected."",
      ""nextIdentifier"": null
    }
  }
}") { name = "e2e_authoritative_dialogue.scenario" };
      Registry.Registry.Register(Registry.RegistryType.ScenarioGraph, "e2e_authoritative_dialogue", Asset);
      Assets.Add(Asset);
      var protocol = new TextAsset(@"{
        ""identifier"": ""e2e_signal_protocol"",
        ""defaultEntrypoint"": ""waiting"",
        ""tags"": [""e2e_fixture""],
        ""clientSignalIdentifiers"": [""sig.e2e.protocol.exact""],
        ""clientSignalPrefixes"": [""sig.e2e.protocol.prefix.""],
        ""nodes"": { ""waiting"": {
          ""identifier"": ""waiting"", ""nodeType"": ""Dialogue"",
          ""speakerName"": ""E2E"", ""dialogueContent"": ""Protocol test in progress."",
          ""playTTS"": false, ""nextIdentifier"": null
        }}
      }") { name = "e2e_signal_protocol.scenario" };
      Assets.Add(protocol);
      Registry.Registry.Register(Registry.RegistryType.ScenarioGraph, "e2e_signal_protocol", protocol);
      var roles = new TextAsset(@"{
  ""identifier"": ""e2e_role_branches"",
  ""defaultEntrypoint"": ""parallel"",
  ""tags"": [
    ""e2e_fixture"",
    ""e2e_a"",
    ""e2e_b"",
    ""e2e_c"",
    ""e2e_d""
  ],
  ""activeRoleTags"": [
    ""e2e_a"",
    ""e2e_b"",
    ""e2e_c"",
    ""e2e_d""
  ],
  ""skipAbsentRoleBranches"": false,
  ""checklistItemSetsByPlayerTag"": {
    ""e2e_a"": [
      {
        ""identifier"": ""vital_set"",
        ""count"": 1
      }
    ],
    ""e2e_b"": [
      {
        ""identifier"": ""blood_bag"",
        ""count"": 1
      }
    ],
    ""e2e_c"": [
      {
        ""identifier"": ""normal_saline_1000ml"",
        ""count"": 1
      }
    ],
    ""e2e_d"": [
      {
        ""identifier"": ""intravenous_set"",
        ""count"": 1
      }
    ]
  },
  ""nodes"": {
    ""branch_e2e_a"": {
      ""identifier"": ""branch_e2e_a"",
      ""nodeType"": ""Choice"",
      ""speakerName"": ""E2E"",
      ""dialogueContent"": ""Only e2e_a should see this choice."",
      ""playTTS"": false,
      ""options"": [
        {
          ""displayText"": ""Complete e2e_a"",
          ""nextNodeIdentifier"": ""done_e2e_a""
        }
      ]
    },
    ""done_e2e_a"": {
      ""identifier"": ""done_e2e_a"",
      ""nodeType"": ""StateUpdate"",
      ""stateKey"": ""branch.completion.done_e2e_a"",
      ""stateValue"": ""true"",
      ""nextIdentifier"": null,
      ""targetEntityIdentifier"": null
    },
    ""branch_e2e_b"": {
      ""identifier"": ""branch_e2e_b"",
      ""nodeType"": ""Choice"",
      ""speakerName"": ""E2E"",
      ""dialogueContent"": ""Only e2e_b should see this choice."",
      ""playTTS"": false,
      ""options"": [
        {
          ""displayText"": ""Complete e2e_b"",
          ""nextNodeIdentifier"": ""done_e2e_b""
        }
      ]
    },
    ""done_e2e_b"": {
      ""identifier"": ""done_e2e_b"",
      ""nodeType"": ""StateUpdate"",
      ""stateKey"": ""branch.completion.done_e2e_b"",
      ""stateValue"": ""true"",
      ""nextIdentifier"": null,
      ""targetEntityIdentifier"": null
    },
    ""branch_e2e_c"": {
      ""identifier"": ""branch_e2e_c"",
      ""nodeType"": ""Choice"",
      ""speakerName"": ""E2E"",
      ""dialogueContent"": ""Only e2e_c should see this choice."",
      ""playTTS"": false,
      ""options"": [
        {
          ""displayText"": ""Complete e2e_c"",
          ""nextNodeIdentifier"": ""done_e2e_c""
        }
      ]
    },
    ""done_e2e_c"": {
      ""identifier"": ""done_e2e_c"",
      ""nodeType"": ""StateUpdate"",
      ""stateKey"": ""branch.completion.done_e2e_c"",
      ""stateValue"": ""true"",
      ""nextIdentifier"": null,
      ""targetEntityIdentifier"": null
    },
    ""branch_e2e_d"": {
      ""identifier"": ""branch_e2e_d"",
      ""nodeType"": ""Choice"",
      ""speakerName"": ""E2E"",
      ""dialogueContent"": ""Only e2e_d should see this choice."",
      ""playTTS"": false,
      ""options"": [
        {
          ""displayText"": ""Complete e2e_d"",
          ""nextNodeIdentifier"": ""done_e2e_d""
        }
      ]
    },
    ""done_e2e_d"": {
      ""identifier"": ""done_e2e_d"",
      ""nodeType"": ""StateUpdate"",
      ""stateKey"": ""branch.completion.done_e2e_d"",
      ""stateValue"": ""true"",
      ""nextIdentifier"": null,
      ""targetEntityIdentifier"": null
    },
    ""parallel"": {
      ""identifier"": ""parallel"",
      ""nodeType"": ""Parallel"",
      ""waitMode"": ""All"",
      ""allocationType"": ""ByRole"",
      ""whenBranchingPlayerNotMatched"": ""Panic"",
      ""branches"": [
        {
          ""identifier"": ""branch_e2e_a"",
          ""completionConditionIdentifier"": ""complete_e2e_a"",
          ""requiredPlayerTags"": [
            ""e2e_a""
          ],
          ""requiredPlayerTagsMatchMode"": ""All"",
          ""forbiddenPlayerTags"": []
        },
        {
          ""identifier"": ""branch_e2e_b"",
          ""completionConditionIdentifier"": ""complete_e2e_b"",
          ""requiredPlayerTags"": [
            ""e2e_b""
          ],
          ""requiredPlayerTagsMatchMode"": ""All"",
          ""forbiddenPlayerTags"": []
        },
        {
          ""identifier"": ""branch_e2e_c"",
          ""completionConditionIdentifier"": ""complete_e2e_c"",
          ""requiredPlayerTags"": [
            ""e2e_c""
          ],
          ""requiredPlayerTagsMatchMode"": ""All"",
          ""forbiddenPlayerTags"": []
        },
        {
          ""identifier"": ""branch_e2e_d"",
          ""completionConditionIdentifier"": ""complete_e2e_d"",
          ""requiredPlayerTags"": [
            ""e2e_d""
          ],
          ""requiredPlayerTagsMatchMode"": ""All"",
          ""forbiddenPlayerTags"": []
        }
      ],
      ""nextIdentifier"": ""joined""
    },
    ""joined"": {
      ""identifier"": ""joined"",
      ""nodeType"": ""Dialogue"",
      ""speakerName"": ""E2E"",
      ""dialogueContent"": ""Every assigned role completed."",
      ""playTTS"": false,
      ""nextIdentifier"": null
    }
  }
}") { name = "e2e_role_branches.scenario" };
      Assets.Add(roles);
      Registry.Registry.Register(Registry.RegistryType.ScenarioGraph, "e2e_role_branches", roles);
      var absentRoles = new TextAsset(roles.text
        .Replace("e2e_role_branches", "e2e_role_absent")
        .Replace("\"skipAbsentRoleBranches\": false", "\"skipAbsentRoleBranches\": true"))
        { name = "e2e_role_absent.scenario" };
      Assets.Add(absentRoles);
      Registry.Registry.Register(Registry.RegistryType.ScenarioGraph, "e2e_role_absent", absentRoles);
    }
    internal static void Reset() { Asset = null; Assets.Clear(); }
  }
}
#endif
