# 시나리오 이벤트 레지스트리

이 문서는 시나리오에서 호출되는 이벤트 식별자를 추적하기 위한 목록입니다. 새로운 InvokeEvent를 추가하는 경우 반드시 여기에 기록합니다.

| EventIdentifier | 설명 | 호출 시점 | 구현 위치 | 상태 |
|---|---|---|---|---|
| (예) move_patient_a_to_treatment | 환자 A를 처치실로 이동 | 환자 A 시나리오 시작 | Assets/Modules/TriageTrainer/... | planned |

상태 값은 `planned`, `implemented`, `deprecated` 중 하나로 기록합니다.
