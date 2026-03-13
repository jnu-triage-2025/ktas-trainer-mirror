# scenario 환자 B/C 지연 처치

## 기본 정보

| 항목 | 내용 |
|---|---|
|---|---|
| 제목 | 환자 B/C: 뇌손상 의심 및 상완 개방성 골절 대응 |
| 요약 | 환자를 처치 구역으로 이동시키고 의식/활력징후 사정, 산소화, 지혈, IV 확보, 동공 반응 확인 후 CT실로 이동한다. |
| 주요 등장인물 | 플레이어 A/B/C/D, 의사 NPC, 환자 B, 환자 C(동일 부상) |
| 주요 장소 | 처치 구역, CT실 |
| 리소스 식별자 - 사운드 | 없음 |
| 리소스 식별자 - 초상화 | 없음 |
| 리소스 식별자 - 웨이포인트 | wp_treatment_area, wp_ct_room |
| 리소스 식별자 - 카메라 타겟 | 없음 |
| 시작 노드 Identifier | E038 |

## 시나리오 본문

### [E038] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | triage_patientB_patientC_dummyB |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D038 |

---

### [D038] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 환자 세 명이 이송되었습니다. 간호사 A가 중증도 분류를 시행합니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | N029 |

---

### [N029] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N029 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | [플레이어 A 전용] 환자를 왼쪽부터 차례대로 클릭하여 환자의 상태를 확인하고, 중증도 분류를 실시하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q031 |

---

### [Q031] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Triage_patientB_patientC |
| **NextIdentifier** | 문자열 | V035 |

---

### [V035] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_patientB |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E039 |

---

### [E039] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_patientB_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | C036 |

---

### [C036] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [플레이어 A 전용] 해당 환자의 중증도 분류를 시행하세요. |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C036_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C036_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| KTAS 1(소생) | | #88AAFF | N029_retry_a |
| KTAS 3(응급) | | #88AAFF | N029_retry_a |
| KTAS 4(준응급) | | #88AAFF | N029_retry_a |
| KTAS 5(비응급) | | #88AAFF | N029_retry_a |
| KTAS 2(긴급) | | #88AAFF | N030 |

---

### [N029_retry_a] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N029_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 사고의 경위, 머리의 부상 등을 고려하였을 때 뇌출혈이 의심되므로, KTAS 2(긴급)이 적절합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C036 |

---

### [N030] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N030 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 해당 환자를 KTAS 2로 분류했습니다. 다음 환자를 클릭하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V036 |

---

### [V036] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_dummyB |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E040 |

---

### [E040] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_dummyB_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | C037 |

---

### [C037] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [플레이어 A 전용] 해당 환자의 중증도 분류를 시행하세요. |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C037_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C037_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| KTAS 1(소생) | | #88AAFF | N030_retry_b |
| KTAS 2(긴급) | | #88AAFF | N030_retry_b |
| KTAS 3(응급) | | #88AAFF | N030_retry_b |
| KTAS 4(준응급) | | #88AAFF | N030_retry_b |
| KTAS 5(비응급) | | #88AAFF | N031 |

---

### [N030_retry_b] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N030_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 비교적 긴급한 처치가 필요하지 않은 KTAS 5(비응급) 상태로 보입니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C037 |

---

### [N031] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N031 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 해당 환자를 KTAS 5로 분류했습니다. 다음 환자를 클릭하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V037 |

---

### [V037] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_patientC |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E041 |

---

### [E041] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | show_patientC_ui |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | C038 |

---

### [C038] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | [플레이어 A 전용] 해당 환자의 중증도 분류를 시행하세요. |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C038_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C038_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| KTAS 1(소생) | | #88AAFF | N031_retry_c |
| KTAS 3(응급) | | #88AAFF | N031_retry_c |
| KTAS 4(준응급) | | #88AAFF | N031_retry_c |
| KTAS 5(비응급) | | #88AAFF | N031_retry_c |
| KTAS 2(긴급) | | #88AAFF | N032 |

---

### [N031_retry_c] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N031_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 사고의 경위, 머리의 부상 등을 고려하였을 때 뇌출혈이 의심되므로, KTAS 2(긴급)이 적절합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C038 |

---

### [N032] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 해당 환자를 KTAS 2로 분류했습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | N033 |

---

### [N033] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 이제 입원 구역으로 이송할 긴급 환자 2명을 차례대로 클릭하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V038 |

---

### [V038] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Select_patientB, Select_patientC |
| **TargetCount** | 정수 | 2 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | Q031_1 |

---

### [Q031_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q031_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Triage_patientB_patientC |
| **NextIdentifier** | 문자열 | D039 |

---

### [D039] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | KTAS 2(긴급)으로 분류된 환자 2명을 이송하겠습니다. 간호사 B, C, D선생님 이동 도와주세요. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q032 |

---

### [Q032] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q032 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_playerB_playerC_playerD_to_triage |
| **NextIdentifier** | 문자열 | V039 |

---

### [V039] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Enter_TriageZone |
| **TargetCount** | 정수 | 3 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | Q032_1 |

---

### [Q032_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q032_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_playerB_playerC_playerD_to_triage |
| **NextIdentifier** | 문자열 | E042 |

---


### [E042] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | B_C_D_to_triage |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | P009 |

---

### [P009] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P009 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P009_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | WaitAll |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | |
| **NextIdentifier** | 문자열 | D058 |

#### [P009_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers |
|---|---|---|
| V040_A | CC_A_C_patientB_complete | NurseA, NurseC |
| V040_B | CC_B_D_patientC_complete | NurseB, NurseD |

====================================================
# [P009 병렬 브랜치 1] 환자 B 처치 그룹 (플레이어 A, C)
====================================================

### [V040_A] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_A |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Grab_Stretcher_patientB |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | V040_C |

---

### [V040_C] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_C |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Grab_Stretcher_patientB |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E043 |

---

### [E043] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | move_patientB |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N034 |

---

### [N034] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 처치 구역에 도착했습니다. 간호사 A는 의식상태를, 간호사 C는 활력징후를 사정하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | P010 |

---

### [P010] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P010 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P010_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | WaitAll |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | |
| **NextIdentifier** | 문자열 | D041 |

#### [P010_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers |
|---|---|---|
| N035 | CC_A_gcs_patientB | NurseA |
| N043 | CC_C_vital_patientB | NurseC |

====================================================
# [P010 병렬 브랜치 1] 플레이어 A (환자 B 의식 사정)
====================================================

### [N035] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 환자를 클릭하여 환자의 의식상태를 사정하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q033 |

---

### [Q033] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q033 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GCS_B |
| **NextIdentifier** | 문자열 | V041 |

---

### [V041] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Check_gcs_patientB |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N036 |

---

### [N036] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 환자의 의식 수준(AVPU)를 확인합니다. 마우스로 정답을 선택해주세요. 정답 시 계속 진행, 오답 시 재응시 합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | N037 |

---

### [N037] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | [관찰] 환자에게 질문했을 때, 무슨 일이 있었는지 기억하지 못하고, 말의 반응이 조금 느립니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C039 |

---

### [C039] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C039_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C039_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| A(Alert, 완전히 깨어 있음) | | #88AAFF | N037_retry_a |
| P(Pain response, 통증에 반응 있음) | | #88AAFF | N037_retry_a |
| U(Unconsciousness, 반응 없음) | | #88AAFF | N037_retry_a |
| V(Verbal response, 음성에 반응 있음) | | #88AAFF | N038 |

---

### [N037_retry_a] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N037_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 질문에 대답을 하지만 정확한 답변을 하지 못하므로, V(Verbal Response)가 적절합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C039 |

---

### [N038] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | [관찰] 추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C040 |

---

### [C040] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 E(Eye Opening) 점수는 몇 점입니까? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C040_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C040_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 4점(자발적) | | #88AAFF | N038_retry_b |
| 2점(통증) | | #88AAFF | N038_retry_b |
| 1점(반응 없음) | | #88AAFF | N038_retry_b |
| 3점(명령) | | #88AAFF | N039 |

---

### [N038_retry_b] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N038_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C040 |

---

### [N039] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | [관찰] 다음은 Verbal Response(V)입니다. 지금 시간대에 대해 질문하자 "어... 그... 퇴근길이었던거 같은데."라고 답했습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C041 |

---

### [C041] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 V(Verbal Response) 점수는 몇 점입니까? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C041_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C041_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(적절한 답변) | | #88AAFF | N039_retry_c |
| 3점(부적절한 답변) | | #88AAFF | N039_retry_c |
| 2점(신음소리) | | #88AAFF | N039_retry_c |
| 1점(반응 없음) | | #88AAFF | N039_retry_c |
| 4점(혼란) | | #88AAFF | N040 |

---

### [N039_retry_c] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N039_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 환자는 시간대를 인지하지 못하며 혼란스러워하는 상태입니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C041 |

---

### [N040] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 움직임에 대한 명령에 잘 수행합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C042 |

---

### [C042] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 M(Motor Response) 점수는 몇 점입니까? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C042_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C042_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(통증 원인을 치우려고 손을 뻗음) | | #88AAFF | N040_retry_d |
| 4점(통증에 회피) | | #88AAFF | N040_retry_d |
| 3점(이상 굴곡) | | #88AAFF | N040_retry_d |
| 2점(이상 신전) | | #88AAFF | N040_retry_d |
| 1점(반응 없음) | | #88AAFF | N040_retry_d |
| 6점(명령 수행) | | #88AAFF | N041 |

---

### [N040_retry_d] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N040_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 움직임에 대한 명령에 잘 수행하고 있습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C042 |

---

### [N041] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | GCS 측정 완료. E3 / V4 / M6 = 총 13점 (Drowsy/Lethargy) 입니다. 근력에 대한 추가 사정을 실시합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | N042 |

---

### [N042] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | GCS의 M(Motor Response) 사정 중 왼쪽 다리가 오른쪽 다리의 정상 근력보다 약하고, 저항에 이기지 못하고 있습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C043 |

---

### [C043] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 정상인 우측(5점)에 비해, 좌측의 근력 수준은? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C043_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C043_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(정상 근력) | | #88AAFF | N042_retry_e |
| 4점(중력+약간의 저항) | | #88AAFF | N042_retry_e |
| 2점(중력에 저항 불가, 좌우 운동) | | #88AAFF | N042_retry_e |
| 1점(약간의 근육 수축) | | #88AAFF | N042_retry_e |
| 0점(움직임 없음) | | #88AAFF | N042_retry_e |
| 3점(중력에 저항 가능) | | #88AAFF | D040 |

---

### [N042_retry_e] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N042_retry_e |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 중력에는 저항 가능하나, 간호사가 저항을 가했을 때 이겨내지 못하는 상태입니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C043 |

---

### [D040] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 현재 시나리오 B 환자의 GCS는 13점, 근력(Motor Grade)은 우측 5점, 좌측 3점입니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q033_1 |

---

### [Q033_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q033_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GCS_B |
| **NextIdentifier** | 문자열 | CC_A_gcs_patientB |

====================================================
# [P010 병렬 브랜치 2] 플레이어 C (환자 B 활력징후 사정)
====================================================

### [N043] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 환자의 활력징후를 측정합니다. 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q034 |

---

### [Q034] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q034 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_B |
| **NextIdentifier** | 문자열 | V042 |

---

### [V042] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_vital_set, Click_electrode, Click_electrode_cable |
| **TargetCount** | 정수 | 3 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N044 |

---

### [N044] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 전극을 선택하여 환자의 가슴에 부착하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V043 |

---

### [V043] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | apply_electrode |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N045 |

---

### [N045] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 전극 케이블을 클릭해 선택하고, 환자와 모니터를 각각 클릭해 연결하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V044 |

---

### [V044] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | connect_patient_and_monitor_b |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N046 |

---

### [N046] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V045 |

---

### [V045] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Check_vital_patientB |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E044 |

---

### [E044] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | activate_vital_monitor_ui_patientB |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N047 |

---

### [N047] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | V046 |

---

### [V046] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Close_vitalUI_b |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | Q034_1 |

---

### [Q034_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q034_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_B |
| **NextIdentifier** | 문자열 | CC_C_vital_patientB |

====================================================
# [P010 병렬 종료 및 P011 진입 (환자 B)]
====================================================

### [D041] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | B 환자의 의식상태는 GCS 13점, 근력 우측 5점/좌측 3점이며, 활력징후는 혈압 140/86, 맥박 120, 호흡수 24, 체온 37.3, SpO2 93% 입니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | D042 |

---

### [D042] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 간호사 A 선생님, 펜라이트로 동공반사 확인해주시고 생리식염수 1L로 IV라인 확보해주세요. 간호사 C 선생님, 산소포화도가 조금 낮으니 비강캐뉼라로 3L 주시고 지혈도 해주세요. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | P011 |

---

### [P011] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P011 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P011_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | WaitAll |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | |
| **NextIdentifier** | 문자열 | N062 |

#### [P011_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers |
|---|---|---|
| N048 | CC_A_pupil_iv_patientB | NurseA |
| N052 | CC_C_nasal_pressure_patientB | NurseC |

====================================================
# [P011 병렬 브랜치 1] 플레이어 A (동공 확인 및 IV 확보)
====================================================

### [N048] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 먼저 대광반사를 확인하겠습니다. 펜라이트를 클릭해 획득하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q035 |

---

### [Q035] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q035 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Pupil_IV_B |
| **NextIdentifier** | 문자열 | V047 |

---

### [V047] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_penlight |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N049 |

---

### [N049] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 펜라이트를 선택한 뒤, 환자의 얼굴을 클릭해 대광반사 확인을 시작합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V048 |

---

### [V048] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_patientB_face |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E045 |

---

### [E045] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | pupil_reflex_patientB |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D043 |

---

### [D043] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D043 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 좌측 동공에 비해 우측 동공이 빛에 반응하지 않습니다. 추가 평가가 필요합니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | N050 |

---

### [N050] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 다음으로 IV 라인을 확보합니다. 환자의 우측 팔에 IV 라인을 확보해야 합니다. 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | V049 |

---

### [V049] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_20g, Click_iv_set, Click_ns1 |
| **TargetCount** | 정수 | 3 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N051 |

---

### [N051] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N051 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 우측 팔을 클릭해 정맥 라인을 확보하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V050 |

---

### [V050] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Insert_iv_b_right |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E046 |

---

### [E046] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_20g_right_patientB |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediate |
| **NextIdentifier** | 문자열 | N051_1 |

---

### [N051_1] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N051_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 우측 팔에 연결된 20G 캐뉼라를 클릭해 연결하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V051 |

---

### [V051] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V051 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Connect_cannula_and_ns1_patientB |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E047 |

---

### [E047] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_ns1_right_patientB |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediate |
| **NextIdentifier** | 문자열 | D044 |

---

### [D044] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 정맥로가 확보되었습니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | D045 |

---

### [D045] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 A |
| **DialogueContent** | 문자열 | 환자의 우측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q035_1 |

---

### [Q035_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q035_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Pupil_IV_B |
| **NextIdentifier** | 문자열 | CC_A_pupil_iv_patientB |

====================================================
# [P011 병렬 브랜치 2] 플레이어 C (환자 B 산소 투여 및 지혈)
====================================================

### [N052] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N052 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 비강캐뉼라를 이용한 산소화를 먼저 실시합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | N053 |

---

### [N053] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N053 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q036 |

---

### [Q036] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q036 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Nasal_B |
| **NextIdentifier** | 문자열 | V052 |

---

### [V052] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V052 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_humidifierbottle, Click_sdw |
| **TargetCount** | 정수 | 2 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | A012 |

---

### [A012] CombineItemNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | A012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.CombineItem |
| **InputItemIdentifiers** | 문자열 목록 | humidifierbottle, sdw |
| **OutputItemIdentifier** | 문자열 | humidifierbottle_ready |
| **AutoCombine** | bool | true |
| **NextIdentifier** | 문자열 | N054 |

---

### [N054] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N054 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 유량계를 습득하여 산소 유량계를 완성하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V053 |

---

### [V053] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V053 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_flowmeter |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | A013 |

---

### [A013] CombineItemNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | A013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.CombineItem |
| **InputItemIdentifiers** | 문자열 목록 | humidifierbottle_ready, flowmeter |
| **OutputItemIdentifier** | 문자열 | oxyflowmeter_b |
| **AutoCombine** | bool | true |
| **NextIdentifier** | 문자열 | N055 |

---

### [N055] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N055 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V054 |

---

### [V054] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V054 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Connect_wall_component_2 |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N056 |

---

### [N056] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N056 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 비강캐뉼라를 클릭해 획득하고, 산소 유량계와 환자를 각각 클릭해 적용하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | V055 |

---

### [V055] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V055 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_nasal, Connect_nasal_and_o2 |
| **TargetCount** | 정수 | 2 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N057 |

---

### [N057] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N057 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C044 |

---

### [C044] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C044 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 투여될 산소의 양을 조절합니다. |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C044_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C044_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5L | | #88AAFF | N057_retry |
| 10L | | #88AAFF | N057_retry |
| 15L | | #88AAFF | N057_retry |
| 3L | | #88AAFF | D046 |

---

### [N057_retry] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N057_retry |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 처방은 3L 입니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C044 |

---

### [D046] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 산소 투여가 완료되었습니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q036_1 |

---

### [Q036_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q036_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Nasal_B |
| **NextIdentifier** | 문자열 | N058 |

---

### [N058] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N058 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q037 |

---

### [Q037] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q037 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_B |
| **NextIdentifier** | 문자열 | V056 |

---

### [V056] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V056 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_glove, Click_gauze, Click_plaster |
| **TargetCount** | 정수 | 3 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N059 |

---

### [N059] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N059 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 멸균장갑을 [우클릭]해 착용하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V057 |

---

### [V057] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V057 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | wear_glove |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N060 |

---

### [N060] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N060 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V058 |

---

### [V058] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V058 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Apply_gauze |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E048 |

---

### [E048] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_patientB |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N061 |

---

### [N061] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N061 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 압박을 가해 지혈하고 있습니다. 플라스터로 거즈를 고정합니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | V059 |

---

### [V059] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V059 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Apply_plaster_on_gauze |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E049 |

---

### [E049] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_with_plaster_patientB |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | S006 |

---

### [S006] SoundNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | S006 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | tape_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | D047 |

---

### [D047] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 지혈 중입니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | D048 |

---

### [D048] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 C |
| **DialogueContent** | 문자열 | 산소 적용 및 지혈이 완료되었습니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q037_1 |

---

### [Q037_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q037_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_B |
| **NextIdentifier** | 문자열 | CC_C_nasal_pressure_patientB |

====================================================
# [P011 병렬 종료 (환자 B 처치 완료)]
====================================================

### [N062] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N062 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 시나리오 B 환자에 대한 간호 중재가 완료되었습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | CC_A_C_patientB_complete |

====================================================
# [P009 병렬 브랜치 2] 환자 C 처치 그룹 (플레이어 B, D)
====================================================

### [V040_B] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_B |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Grab_Stretcher_patientC |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | V040_D |

---

### [V040_D] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V040_D |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Grab_Stretcher_patientC |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E050 |

---

### [E050] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | move_patientC |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N063 |

---

### [N063] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N063 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 처치 구역에 도착했습니다. 즉시 의식상태 사정 및 활력징후 사정을 시작하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | P012 |

---

### [P012] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P012 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P012_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | WaitAll |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | |
| **NextIdentifier** | 문자열 | D049 |

#### [P012_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers |
|---|---|---|
| N064 | CC_B_gcs_patientC | NurseB |
| N072 | CC_D_vital_patientC | NurseD |

====================================================
# [P012 병렬 브랜치 1] 플레이어 B (환자 C 의식 사정)
====================================================

### [N064] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N064 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 환자를 클릭하여 환자의 의식상태를 사정하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q038 |

---

### [Q038] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q038 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GCS_C |
| **NextIdentifier** | 문자열 | V060 |

---

### [V060] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V060 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Check_gcs_patientC |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N065 |

---

### [N065] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N065 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 환자의 의식 수준(AVPU)를 확인합니다. 마우스로 정답을 선택해 주시면 됩니다. 정답인 경우 계속 진행되고, 오답인 경우 재응시 합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | N066 |

---

### [N066] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N066 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | [관찰] 환자에게 질문했을 때, 무슨 일이 있었는지 기억하지 못하고, 말의 반응이 조금 느립니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C045 |

---

### [C045] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C045 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 의식 수준을 AVPU에 따라 분류할 때, 현재 환자의 의식 수준은 무엇입니까? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C045_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C045_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| A(Alert, 완전히 깨어 있음) | | #88AAFF | N066_retry_a |
| P(Pain response, 통증에 반응 있음) | | #88AAFF | N066_retry_a |
| U(Unconsciousness, 반응 없음) | | #88AAFF | N066_retry_a |
| V(Verbal response, 음성에 반응 있음) | | #88AAFF | N067 |

---

### [N066_retry_a] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N066_retry_a |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 질문에 대답을 하지만 정확한 답변을 하지 못하므로, V(Verbal Response)가 적절합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C045 |

---

### [N067] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N067 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | [관찰] 추가 사정으로 GCS를 확인합니다. 먼저 Eye Opening(E) 반응을 확인합니다. 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C046 |

---

### [C046] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C046 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 E(Eye Opening) 점수는 몇 점입니까? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C046_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C046_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 4점(자발적) | | #88AAFF | N067_retry_b |
| 2점(통증) | | #88AAFF | N067_retry_b |
| 1점(반응 없음) | | #88AAFF | N067_retry_b |
| 3점(명령) | | #88AAFF | N068 |

---

### [N067_retry_b] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N067_retry_b |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 눈을 감고 있다가, 질문을 하면 눈을 뜨고 있습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C046 |

---

### [N068] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N068 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | [관찰] 다음은 Verbal Response(V)입니다. 지금 시간대에 대해 질문하자 "어... 그... 퇴근길이었던거 같은데."라고 답했습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C047 |

---

### [C047] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C047 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 V(Verbal Response) 점수는 몇 점입니까? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C047_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C047_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(적절한 답변) | | #88AAFF | N068_retry_c |
| 3점(부적절한 답변) | | #88AAFF | N068_retry_c |
| 2점(신음소리) | | #88AAFF | N068_retry_c |
| 1점(반응 없음) | | #88AAFF | N068_retry_c |
| 4점(혼란) | | #88AAFF | N069 |

---

### [N068_retry_c] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N068_retry_c |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 환자는 시간대를 인지하지 못하며 혼란스러워하는 상태입니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C047 |

---

### [N069] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N069 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | [관찰] 마지막으로 Motor Response(M)입니다. 움직임에 대한 명령에 잘 수행합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C048 |

---

### [C048] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C048 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 관찰된 M(Motor Response) 점수는 몇 점입니까? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C048_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C048_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(통증 원인을 치우려고 손을 뻗음) | | #88AAFF | N069_retry_d |
| 4점(통증에 회피) | | #88AAFF | N069_retry_d |
| 3점(이상 굴곡) | | #88AAFF | N069_retry_d |
| 2점(이상 신전) | | #88AAFF | N069_retry_d |
| 1점(반응 없음) | | #88AAFF | N069_retry_d |
| 6점(명령 수행) | | #88AAFF | N070 |

---

### [N069_retry_d] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N069_retry_d |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 움직임에 대한 명령에 잘 수행하고 있습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C048 |

---

### [N070] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N070 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | GCS 측정 완료. E3 / V4 / M6 = 총 13점 (Drowsy/Lethargy) 입니다. 근력에 대한 추가 사정을 실시합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | N071 |

---

### [N071] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N071 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | GCS의 M(Motor Response) 사정 중 오른쪽 다리가 왼쪽 다리의 정상 근력보다 약하고, 간호사가 가하는 저항에 이기지 못하고 있습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | C049 |

---

### [C049] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 정상인 좌측(5점)에 비해, 우측의 근력 수준은? |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C049_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C049_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5점(정상 근력) | | #88AAFF | N071_retry_e |
| 4점(중력+약간의 저항) | | #88AAFF | N071_retry_e |
| 2점(중력에 저항 불가, 좌우 운동) | | #88AAFF | N071_retry_e |
| 1점(약간의 근육 수축) | | #88AAFF | N071_retry_e |
| 0점(움직임 없음) | | #88AAFF | N071_retry_e |
| 3점(중력에 저항 가능) | | #88AAFF | D050 |

---

### [N071_retry_e] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N071_retry_e |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 현재 중력에는 저항 가능하나, 간호사가 저항을 가했을 때 이겨내지 못하는 상태입니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C049 |

---

### [D050] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 현재 시나리오 C 환자의 GCS는 13점, 근력(Motor Grade)은 좌측 5점, 우측 3점입니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q038_1 |

---

### [Q038_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q038_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_GCS_C |
| **NextIdentifier** | 문자열 | CC_B_gcs_patientC |

====================================================
# [P012 병렬 브랜치 2] 플레이어 D (환자 C 활력징후 사정)
====================================================

### [N072] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N072 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 환자의 활력징후를 측정합니다. 활력징후 측정도구, 전극, 전극 케이블을 클릭해 획득하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | Q039 |

---

### [Q039] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q039 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_C |
| **NextIdentifier** | 문자열 | V061 |

---

### [V061] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V061 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_vital_set, Click_electrode, Click_electrode_cable |
| **TargetCount** | 정수 | 3 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N073 |

---

### [N073] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N073 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 전극을 선택하여 환자의 가슴에 부착하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V062 |

---

### [V062] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V062 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | apply_electrode |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N074 |

---

### [N074] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N074 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 전극 케이블을 클릭해 선택하고, 환자와 모니터를 각각 클릭해 연결하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V063 |

---

### [V063] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V063 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | connect_patient_and_monitor_patientC |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N075 |

---

### [N075] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N075 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 활력징후 측정도구를 선택한 뒤, 환자를 클릭하면 활력징후가 측정됩니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V064 |

---

### [V064] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V064 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Check_vital_patientC |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E051 |

---

### [E051] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E051 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | activate_vital_monitor_ui_patientC |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N076 |

---

### [N076] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N076 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. 확인 후 모니터 창을 닫으십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | V065 |

---

### [V065] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V065 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Close_vitalUI_c |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | Q039_1 |

---

### [Q039_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q039_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Vital_C |
| **NextIdentifier** | 문자열 | CC_D_vital_patientC |

====================================================
# [P012 병렬 종료 및 P013 진입 (환자 C)]
====================================================

### [D049] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D049 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | C 환자의 의식상태는 GCS 13점, 근력 좌측 5점/우측 3점이며, 활력징후는 혈압 140/86mmHg, 맥박 120회/분, 호흡수 24회/분, 체온 37.3도, SpO2 93% 입니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | D051 |

---

### [D051] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D051 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 간호사 B 선생님, 펜라이트로 동공반사 확인해주시고 생리식염수 1L로 IV라인 확보해주세요. 간호사 D 선생님, 산소포화도가 조금 낮으니 비강캐뉼라로 3L 주시고 지혈도 해주세요. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | P013 |

---

### [P013] ParallelNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | P013 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Parallel |
| **Branches** | ScenarioParallelBranch 목록 | **[하단 P013_Branches 표 참조]** |
| **WaitMode** | ScenarioParallelWaitMode | WaitAll |
| **AllocationType** | ScenarioParallelAllocationType | ByRole |
| **WhenBranchingPlayerNotMatched** | ScenarioParallelWhenBranchingPlayerNotMatched | |
| **NextIdentifier** | 문자열 | N091 |

#### [P013_Branches] 브랜치 목록 (ScenarioParallelBranch)

| Identifier | CompletionConditionIdentifier | RequiredRoleIdentifiers |
|---|---|---|
| N077 | CC_B_pupil_iv_patientC | NurseB |
| N081 | CC_D_nasal_pressure_patientC | NurseD |

====================================================
# [P013 병렬 브랜치 1] 플레이어 B (동공 확인 및 IV 확보)
====================================================

### [N077] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N077 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 먼저 대광반사를 확인하겠습니다. 펜라이트를 클릭해 획득하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q040 |

---

### [Q040] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q040 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Pupil_IV_C |
| **NextIdentifier** | 문자열 | V066 |

---

### [V066] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V066 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_penlight |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N078 |

---

### [N078] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N078 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 펜라이트를 선택한 뒤, 환자의 얼굴을 클릭해 대광반사 확인을 시작합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V067 |

---

### [V067] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V067 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_patientC_face |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E052 |

---

### [E052] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E052 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | pupil_reflex_patientC |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | D052 |

---

### [D052] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D052 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 우측 동공에 비해 좌측 동공이 빛에 반응하지 않습니다. 추가 평가가 필요합니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | N079 |

---

### [N079] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N079 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 다음으로 IV 라인을 확보합니다. 환자의 좌측 팔에 IV 라인을 확보해야 합니다. 20게이지 캐뉼라, 수액세트, 생리식염수 1L 수액백을 클릭해 획득하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 6.0 |
| **NextIdentifier** | 문자열 | V068 |

---

### [V068] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V068 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_20g, Click_iv_set, Click_ns1 |
| **TargetCount** | 정수 | 3 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N080 |

---

### [N080] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N080 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 20게이지 캐뉼라를 클릭해 선택한 뒤, 환자의 좌측 팔을 클릭해 정맥 라인을 확보하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V069 |

---

### [V069] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V069 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Insert_iv_c_left |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E053 |

---

### [E053] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E053 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | insert_20g_left_patientC |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediate |
| **NextIdentifier** | 문자열 | N080_1 |

---

### [N080_1] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N080_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 준비된 생리식염수 1L 수액백을 클릭해 선택한 뒤, 좌측 팔에 연결된 20G 캐뉼라를 클릭해 연결하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V070 |

---

### [V070] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V070 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Connect_cannula_and_ns1_patientC |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E054 |

---

### [E054] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E054 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | connect_ns1_left_patientC |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | Immediate |
| **NextIdentifier** | 문자열 | D053 |

---

### [D053] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D053 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 정맥로가 확보되었습니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | D054 |

---

### [D054] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D054 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 B |
| **DialogueContent** | 문자열 | 환자의 좌측 동공이 빛에 반응하지 않습니다. 추가 검사가 필요해 보입니다. IV 라인도 확보되었습니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q040_1 |

---

### [Q040_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q040_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Pupil_IV_C |
| **NextIdentifier** | 문자열 | CC_B_pupil_iv_patientC |

====================================================
# [P013 병렬 브랜치 2] 플레이어 D (환자 C 산소 투여 및 지혈)
====================================================

### [N081] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N081 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 비강캐뉼라를 이용한 산소화를 먼저 실시합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | N082 |

---

### [N082] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N082 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 산소 유량계 습윤병과 1L 멸균증류수를 클릭해 획득하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q041 |

---

### [Q041] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q041 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Nasal_C |
| **NextIdentifier** | 문자열 | V071 |

---

### [V071] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V071 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_humidifierbottle, Click_sdw |
| **TargetCount** | 정수 | 2 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | A014 |

---

### [A014] CombineItemNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | A014 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.CombineItem |
| **InputItemIdentifiers** | 문자열 목록 | humidifierbottle, sdw |
| **OutputItemIdentifier** | 문자열 | humidifierbottle_ready |
| **AutoCombine** | bool | true |
| **NextIdentifier** | 문자열 | N083 |

---

### [N083] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N083 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 유량계를 습득하여 산소 유량계를 완성하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V072 |

---

### [V072] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V072 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_flowmeter |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | A015 |

---

### [A015] CombineItemNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | A015 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.CombineItem |
| **InputItemIdentifiers** | 문자열 목록 | humidifierbottle_ready, flowmeter |
| **OutputItemIdentifier** | 문자열 | oxyflowmeter_c |
| **AutoCombine** | bool | true |
| **NextIdentifier** | 문자열 | N084 |

---

### [N084] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N084 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 완성된 유량계를 클릭한 뒤, 흡인기 옆 벽면을 클릭해 설치하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V073 |

---

### [V073] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V073 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Connect_wall_component_2 |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N085 |

---

### [N085] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N085 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 비강캐뉼라를 클릭해 획득하고, 산소 유량계와 환자를 각각 클릭해 적용하세요. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | V074 |

---

### [V074] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V074 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_nasal, Connect_nasal_and_o2 |
| **TargetCount** | 정수 | 2 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N086 |

---

### [N086] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N086 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 산소 연결이 완료되었습니다. 유량계를 클릭해 투여 산소량을 결정합니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C050 |

---

### [C050] ChoiceNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | C050 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Choice |
| **SpeakerName** | 문자열 | 시스템 |
| **DialogueContent** | 문자열 | 투여될 산소의 양을 조절합니다. |
| **PortraitSprite** | 문자열 | |
| **Options** | ScenarioChoiceOption 목록 | **[하단 C050_Options 표 참조]** |
| **NextIdentifier** | 문자열 | |

#### [C050_Options] 선택지 목록 (ScenarioChoiceOption)

| DisplayText | DisplayIconIdentifier | DisplayColor | NextNodeIdentifier |
| :--- | :--- | :--- | :--- |
| 5L | | #88AAFF | N086_retry |
| 10L | | #88AAFF | N086_retry |
| 15L | | #88AAFF | N086_retry |
| 3L | | #88AAFF | D055 |

---

### [N086_retry] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N086_retry |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 오답입니다. 처방은 3L 입니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | C050 |

---

### [D055] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D055 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 산소 투여가 완료되었습니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q041_1 |

---

### [Q041_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q041_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_Nasal_C |
| **NextIdentifier** | 문자열 | N087 |

---

### [N087] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N087 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 지혈을 실시합니다. 멸균장갑과 거즈, 플라스터를 클릭해 획득하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | Q042 |

---

### [Q042] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q042 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Add |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_C |
| **NextIdentifier** | 문자열 | V075 |

---

### [V075] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V075 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Click_glove, Click_gauze, Click_plaster |
| **TargetCount** | 정수 | 3 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N088 |

---

### [N088] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N088 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 멸균장갑을 [우클릭]해 착용하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V076 |

---

### [V076] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V076 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | wear_glove |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | N089 |

---

### [N089] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N089 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 거즈를 클릭해 선택한 뒤, 환자에게 적용하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | V077 |

---

### [V077] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V077 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Apply_gauze |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E055 |

---

### [E055] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E055 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_patientC |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N090 |

---

### [N090] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N090 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 압박을 가해 지혈하고 있습니다. 플라스터로 거즈를 고정합니다. 플라스터를 클릭해 선택한 뒤, 거즈를 클릭해 고정하십시오. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 5.0 |
| **NextIdentifier** | 문자열 | V078 |

---

### [V078] ValidatorNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | V078 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Validator |
| **Condition** | 문자열 | Apply_plaster_on_gauze |
| **TargetCount** | 정수 | 1 |
| **OnFailure** | ScenarioValidatorOnFailure | |
| **FailureNextIdentifier** | 문자열 | |
| **NextIdentifier** | 문자열 | E056 |

---

### [E056] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E056 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | apply_gauze_with_plaster_patientC |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | S007 |

---

### [S007] SoundNode

| 속성 | 타입 | 설명 |
|---|---|---|
| **Identifier** | 문자열 | S007 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Sound |
| **SoundResourceIdentifier** | 문자열 | tape_sound |
| **WaitUntilFinished** | bool | true |
| **NextIdentifier** | 문자열 | D056 |

---

### [D056] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D056 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 지혈 중입니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | D057 |

---

### [D057] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D057 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 간호사 D |
| **DialogueContent** | 문자열 | 산소 적용 및 지혈이 완료되었습니다. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | Q042_1 |

---

### [Q042_1] QuestControlNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | Q042_1 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.QuestControl |
| **Operation** | ScenarioQuestOperation | Remove |
| **FailureStrategy** | ScenarioQuestFailureStrategy | Ignore |
| **Quest** | ScenarioQuestData | Quest_BleedingControl_C |
| **NextIdentifier** | 문자열 | CC_D_nasal_pressure_patientC |

====================================================
# [P013 병렬 종료 (환자 C 처치 완료)]
====================================================

### [N091] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N091 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 시나리오 C 환자에 대한 간호 중재가 완료되었습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Toast |
| **Duration** | 실수(float) | 4.0 |
| **NextIdentifier** | 문자열 | CC_B_D_patientC_complete |

====================================================
# [P009 병렬 종료 및 최종 브리핑 / CT실 이송]
====================================================

### [D058] DialogueNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | D058 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Dialogue |
| **SpeakerName** | 문자열 | 의사 NPC |
| **DialogueContent** | 문자열 | 기전과 사정 결과를 보니 뇌손상이 의심됩니다. 활력징후는 비교적 안정되어 있으니 지금 Brain CT 찍겠습니다. 지금 환자를 CT실로 이동시켜주세요. |
| **PortraitSprite** | 문자열 | |
| **NextNodeIdentifier** | 문자열 | E057 |

---

### [E057] InvokeEventNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | E057 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.InvokeEvent |
| **EventIdentifier** | 문자열 | move_patients_to_CT |
| **MoveNextBehavior** | ScenarioInvokeEventMoveNextBehavior | WaitUntilDone |
| **NextIdentifier** | 문자열 | N092 |

---

### [N092] NotificationNode

| 속성 | 타입 | 설명 |
| :--- | :--- | :--- |
| **Identifier** | 문자열 | N092 |
| **NodeType** | ScenarioNodeType | ScenarioNodeType.Notification |
| **Message** | 문자열 | 시나리오 B, C 환자 대응 종료. 모든 시나리오를 수행하였습니다. |
| **DisplayMode** | ScenarioNotificationDisplayMode | Overlay |
| **Duration** | 실수(float) | 10.0 |
| **NextIdentifier** | 문자열 |  |











------ 백업용 ------


 시나리오 B 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 대화 가능하나 반응이 느려 약간의 기면(drowsy) 상태로 보임, - [왼쪽 팔과 다리의 근력이 비교적 약함], - 빈맥, - 빈호흡, - 상완 부위 출혈 지속 중, - 머리에 타박상 및 약간의 출혈 보임, - C/C: 두통"으로 출력한다. 

 더미 B 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 원활한 대화 가능함, - 활력징후 정상, - 사지의 약간의 타박상, - C/C: 어깨 통증"으로 출력한다. 
 
 시나리오 C 환자에 대한 정보를 UI창으로 띄우고, "- 현재 의식 상태: 대화 가능하나 반응이 느려 약간의 기면(drowsy) 상태로 보임, - 한쪽 팔 근력이 비교적 약함, - 빈맥, - 빈호흡, - 무릎 하단 부위 출혈 지속 중, - 머리에 타박상 및 약간의 출혈 보임, - C/C: 어지러움"으로 출력한다. 



## 종료 조건

| 항목 | 내용 |
|---|---|
| 종료 노드 | D063 |
| 종료 연출/설명 | 두 환자 모두 CT실 도달 시 종료된다. 검은 화면으로 fade out 되며 "시나리오 B, C 환자 대응 종료. 모든 시나리오를 수행하였습니다." 메세지를 표시하며 종료된다. |
