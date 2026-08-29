---
name: scenario-json-validation
description: Validate and safely correct MultiplayerInfrastructure scenario JSON files against the repository schema. Use for .scenario.json structure or schema errors; do not use for gameplay-design changes.
---

# Scenario JSON Validation

Use this skill when a `*.scenario.json` file needs schema validation or a format-only correction.

Run the repository CLI from the repository root before and after an edit:

```sh
dotnet run --project Tools/scenario-json-validator -- PATH/TO/file.scenario.json
```

The command checks JSON syntax, duplicate properties, and `Assets/Modules/MultiplayerInfrastructure/Resources/Schema/scenario.schema.json`. Use `--format json` when structured output helps another tool. Supply `--schema PATH` only when validating against a deliberately different schema.

When correcting an error, make the smallest data-only change that satisfies the reported schema rule. Preserve node identifiers, graph flow (`defaultEntrypoint`, `nextIdentifier`, branch targets), and gameplay semantics unless the user explicitly asks to change them. Do not silence an error by removing a node or replacing a required value with a guessed identifier.

For errors involving conditional node fields, inspect the node's `nodeType` and the matching branch in the schema before editing. Validate the final file again and report both the changed fields and the final validation result.
