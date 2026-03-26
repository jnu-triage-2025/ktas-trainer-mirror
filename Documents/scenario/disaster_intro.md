# scenario 재난 초기 대응 및 중증도 분류

## 기본 정보

| 항목 | 내용 |
|---|---|
| 제목 | 재난 발생 초기 대응과 중증도 분류 |
| 요약 | 병원 내 재난 상황 인지, 역할 선택, 물품 준비, 초기 중증도 분류를 수행한다. |
| 주요 등장인물 | 플레이어 A/B/C/D, 원내 방송 시스템, 환자 A, 골절 환자(더미) |
| 주요 장소 | 응급실 트리아지 구역, 처치 준비 구역 |
| 리소스 식별자 - 사운드 | 없음 |
| 리소스 식별자 - 초상화 | 없음 |
| 리소스 식별자 - 웨이포인트 | wp_triage, wp_preproom, wp_treatmentroom |
| 리소스 식별자 - 카메라 타겟 | 없음 |
| 시작 노드 Identifier | D001 |

## 시나리오 본문

### [D001] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | D001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 원내 방송 |
| **DialogueContent** | 문자열 | 병원 인근 지하철역에서 폭발 사고 발생. 재난 상황 발령되었습니다. 응급실 대비 바랍니다. |
| **PortraitSprite** | 문자열 |  |
| **NextNodeIdentifier** | 문자열 | R001 |

---

### [R001] RoleAssignmentNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | R001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.RoleAssignment |
| **RoleOptions** | ScenarioRoleOption 목록 | 간호사 A: KTAS 분류 담당<br>간호사 B: 환자 처치 담당<br>간호사 C: 환자 처치 담당<br>간호사 D: 약물/수액 담당 |
| **AssignmentMode** | ScenarioRoleAssignmentMode | ScenarioRoleAssignmentMode.Select |
| **NextIdentifier** | 문자열 | D002 |

---

### [D002] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | D002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 의료진 간 재난 상황을 공유하고 초기 대응을 시작합니다. |
| **PortraitSprite** | 문자열 |  |
| **NextNodeIdentifier** | 문자열 | P001 |

---

### [P001] ParallelNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | "P001" |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P001_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | WaitAll |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched |  |
| **NextIdentifier** | 문자열 | "D004" |

#### [P001_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers | RequiredPlayerTags | ForbiddenPlayerTags | RequiredPlayerTagsMatchMode |
|---|---|---|---|---|---|
| N001 | CC_A_Triage | NurseA | triage_lead | - | All |
| N002 | CC_BC_Ready | NurseB, NurseC | airway_team, support_team | - | All |
| N003 | CC_D_Ready | NurseD | iv_team | - | All |

====================================================
# [병렬 브랜치 1] 플레이어 A (중증도 분류 담당) 흐름
====================================================

### [N001] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 중증도 분류 구역으로 이동하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q001 |

---

### [Q001] QuestControlNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | Q001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_MoveToTriage |
| **NextIdentifier** | 문자열 | I001 |

---

### [I001] InteractionNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | I001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Interaction |
| **ActorScope** | ScenarioActorScope | Role |
| **TargetIdentifier** | 문자열 | Triage_zone_Trigger |
| **InteractionType** | ScenarioInteractionType | Use |
| **CompletionConditionIdentifier** | 문자열 |  |
| **NextIdentifier** | 문자열 | Q001_1 |

---

### [Q001_1] QuestControlNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | Q001_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_MoveToTriage |
| **NextIdentifier** | 문자열 | N001_1 |

---

### [N001_1] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N001_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 카트 위 활력징후 측정도구를 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q002 |

---

### [Q002] QuestControlNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | Q002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GetVitalSet |
| **NextIdentifier** | 문자열 | V001 |

---

### [V001] ValidatorNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | V001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_vital_set |
| **TargetCount** | 정수 | 1 |
| **OnSuccessNextIdentifier** | 문자열 | Q002_2 |
| **OnFailureNextIdentifier** | 문자열 | |

---

### [Q002_2] QuestControlNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | Q002_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GetVitalSet |
| **NextIdentifier** | 문자열 | D003 |

---

### [D003] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | D003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 잠시 후 환자가 이송됩니다. 중증도 분류 후 환자 처치가 시작됩니다. 각자의 역할에 대비하세요. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | E001 |

---

### [E001] InvokeEventNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | E001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | triage_patientA_dummyA |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D003_1 |

---

### [D003_1] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | D003_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자 두 명이 이송되었습니다. 간호사 A가 중증도 분류를 시행합니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | N001_2 |

---

### [N001_2] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N001_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 환자를 차례대로 클릭하여 환자의 상태를 확인하고, 중증도 분류를 실시하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | V002 |

---

### [V002] ValidatorNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | V002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_patientA |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열 |  |
| **NextIdentifier** | 문자열 | E002 |

---

### [E002] InvokeEventNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | E002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_patientA_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | C001 |

---

### [C001] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 해당 환자의 중증도 분류를 시행하세요. |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C001_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C001_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| KTAS 2(긴급) | | #88AAFF | N001_retry_a |
| KTAS 3(응급) | | #88AAFF | N001_retry_a |
| KTAS 4(준응급) | | #88AAFF | N001_retry_a |
| KTAS 5(비응급) | | #88AAFF | N001_retry_a |
| KTAS 1(소생) | | #88AAFF | N001_3 |

---

### [N001_retry_a] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N001_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 오답입니다. 현재 흉부의 외상 및 다량의 출혈, 환자의 전반적인 외견을 고려하였을 때, KTAS 1(소생)이 적절합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C001 |

---

### [N001_3] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N001_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 해당 환자를 KTAS 1으로 분류했습니다. 다음 환자를 클릭하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | V003 |

---

### [V003] ValidatorNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | V003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_dummyA |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열 |  |
| **NextIdentifier** | 문자열 | E003 |

---

### [E003] InvokeEventNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | E003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_dummyA_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | C002 |

---

### [C002] ChoiceNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | C002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **QuestionText** | 문자열 | 해당 환자의 중증도 분류를 시행하세요. |
| **NextIdentifiers** | 문자열 목록 | C002-Wrong, C002-Correct |

---

### [C002-Wrong] ChoiceOptionNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | C002-Wrong |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.ChoiceOption |
| **OptionText** | 문자열 | KTAS 1(소생), KTAS 2(긴급), KTAS 3(응급), KTAS 4(준응급) |
| **DisplayColor** | 문자열 | #88AAFF |
| **NextIdentifier** | 문자열 | N001_retry_b |

---

### [C002-Correct] ChoiceOptionNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | C002-Correct |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.ChoiceOption |
| **OptionText** | 문자열 | KTAS 5(비응급) |
| **DisplayColor** | 문자열 | #88AAFF |
| **NextIdentifier** | 문자열 | N001_4 |

---

### [N001_retry_b] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N001_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 오답입니다. 비교적 긴급한 처치가 필요하지 않은 KTAS 5(비응급) 상태로 보입니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C002 |

---

### [N001_4] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N001_4 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 해당 환자를 KTAS 5로 분류했습니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | N001_5 |

---

### [N001_5] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N001_5 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 이제 치료를 위해 이송할 긴급 환자를 클릭하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | V004 |

---

### [V004] ValidatorNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | V004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Move_patientA |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | Ignore |
| **FailureNextIdentifier** | 문자열 |  |
| **NextIdentifier** | 문자열 | CC_A_Triage |

*(💡참고: CC_A_Triage로 넘어가면서 A 브랜치 완료 조건 달성)*

====================================================
# [병렬 브랜치 2] 플레이어 B/C (환자 처치 담당) 흐름
====================================================

### [N002] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | KTAS 1(소생) 환자에 대비하기 위해 처치실로 이동하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q003 |

---

### [Q003] QuestControlNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | Q003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_MoveToTreatmentroom |
| **NextIdentifier** | 문자열 | V005 |

---

### [V005] ValidatorNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | V005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Enter_Treatmentroom |
| **TargetCount** | 정수 | 2 |
| **NextIdentifier** | 문자열 | Q003_1 |

---

### [Q003_1] QuestControlNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | Q003_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_MoveToTreatmentroom |
| **NextIdentifier** | 문자열 | N002_1 |

---

### [N002_1] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N002_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 처치실에 있는 물품의 위치를 확인하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 3.0 |
| **NextIdentifier** | 문자열 | CC_BC_Ready |

*(💡참고: CC_BC_Ready로 넘어가면서 BC 브랜치 완료 조건 달성)*

====================================================
# [병렬 브랜치 3] 플레이어 D (약물/수액 담당) 흐름
====================================================

### [N003] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N003 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 준비실로 이동하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q004 |

---

### [Q004] QuestControlNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | Q004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_MoveToPrepRoom |
| **NextIdentifier** | 문자열 | I002 |

---

### [I002] InteractionNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | I002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Interaction |
| **ActorScope** | ScenarioActorScope | Role |
| **TargetIdentifier** | 문자열 | Preproom_Trigger |
| **InteractionType** | ScenarioInteractionType | Use |
| **CompletionConditionIdentifier** | 문자열 |  |
| **NextIdentifier** | 문자열 | Q004_1 |

---

### [Q004_1] QuestControlNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | Q004_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_MoveToPrepRoom |
| **NextIdentifier** | 문자열 | N003_1 |

---

### [N003_1] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N003_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 생리식염수 1L 수액백과 수액세트를 각각 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V006 |

---

### [V006] ValidatorNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | V006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_ns1, Click_iv_set |
| **TargetCount** | 정수 | 2 |
| **NextIdentifier** | 문자열 | A001 |

---

### [A001] CombineItemNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | A001 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.CombineItem |
| **InputItemIdentifiers** | 문자열 목록 | ns1, iv_set |
| **OutputItemIdentifier** | 문자열 | ns1_ready |
| **AutoCombine** | bool | true |
| **NextIdentifier** | 문자열 | N003_2 |


---

### [N003_2] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N003_2 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 플라즈마 솔루션 1L 수액백과 수액세트를 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V007 |

---

### [V007] ValidatorNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | V007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_ps1, Click_iv_set |
| **TargetCount** | 정수 | 2 |
| **NextIdentifier** | 문자열 | A002 |

---

### [A002] CombineItemNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | A002 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.CombineItem |
| **InputItemIdentifiers** | 문자열 목록 | ps1, iv_set |
| **OutputItemIdentifier** | 문자열 | ps1_ready |
| **AutoCombine** | bool | true |
| **NextIdentifier** | 문자열 | N003_3 |

---

### [N003_3] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | N003_3 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 수액 준비가 완료되었습니다. 혈액백을 클릭해 획득하세요. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V008 |

---

### [V008] ValidatorNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | V008 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_blood |
| **TargetCount** | 정수 | 1 |
| **NextIdentifier** | 문자열 | CC_D_Ready |

*(💡참고: CC_D_Ready로 넘어가면서 D 브랜치 완료 조건 달성)*

====================================================
# [병렬 브랜치 종료]
====================================================


### [D004] DialogueNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | D004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | KTAS 1(소생)으로 분류된 환자를 이송하겠습니다. 간호사 B, C, D선생님, 해당 환자 처치실로 이동하도록 도와주세요. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q005 |

---

### [Q005] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q005 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_playerB_playerC_playerD_to_triage |
| **NextIdentifier** | 문자열 | V009 |

---

### [V009] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Enter_TriageZone |
| **TargetCount** | 정수 | 3 |
| **OnSuccessNextIdentifier** | 문자열 | Q005_1 |
| **OnFailureNextIdentifier** | 문자열 | |

---

### [Q005_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q005_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_playerB_playerC_playerD_to_triage |
| **NextIdentifier** | 문자열 | E004 |

---

### [E004] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | B_C_D_to_triage |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N004 |

---

### [N004] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N004 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | System |
| **DialogueContent** | 문자열 | 초기 대응과 중증도 분류를 완료했습니다. 다음 처치 시나리오를 진행합니다. |
| **PortraitSpriteIdentifier** | 문자열/null | null |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | (end) |


## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | N004 |
| 종료 연출/설명 | "초기 대응과 중증도 분류를 완료했습니다. 다음 처치 시나리오를 진행합니다."라는 안내 메세지를 출력하고 다음 시나리오로 진행한다. |
