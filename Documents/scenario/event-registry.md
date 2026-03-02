# 시나리오 이벤트 레지스트리

이 문서는 시나리오에서 호출되는 이벤트 식별자를 추적하기 위한 목록입니다. 새로운 InvokeEvent를 추가하는 경우 반드시 여기에 기록합니다.

| EventIdentifier | 설명 | 호출 시점 | 구현 위치 | 상태 |
|---|---|---|---|---|
| (예) move_patient_a_to_treatment | 환자 A를 처치실로 이동 | 환자 A 시나리오 시작 | Assets/Modules/TriageTrainer/... | planned |

상태 값은 `planned`, `implemented`, `deprecated` 중 하나로 기록합니다.

약자 규칙: 영문 3자 + 숫자 3자 + ( _  + 영문 2자 + 숫자 2자)
| Pt (환자) - 환자의 상태 및 상태 변화 등 환자 관련 | Nr (간호사) - 간호사의 행위 등 간호사 관련 | Dr (의사) - 의사(NPC)의 행위 등 의사 NPC 관련|
| slt (select - 선택)

# 이벤트 본문
| slt_001 | 간호사 A 역할을 선택 | 인트로 | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
| slt_002 | 간호사 B 역할을 선택 | 인트로 | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
| slt_003 | 간호사 C 역할을 선택 | 인트로 | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
| slt_004 | 간호사 D 역할을 선택 | 인트로 | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
|  |  |  | Assets/Modules/TriageTrainer/Resources/Scenario/ | planned |
