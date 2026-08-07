# 예제 구현

```json
{
  "identifier": "assess_avpu_b",
  "nodeType": "Choice",
  "speakerName": "@s",
  "dialogueContent": "환자의 AVPU는...",
  "assessmentIdentifier": "patient_b.avpu",
  "correctOptionIndex": 3,
  "options": [
    { "displayText": "AVPU A", "nextNodeIdentifier": "feedback_wrong" },
    { "displayText": "AVPU P", "nextNodeIdentifier": "feedback_wrong" },
    { "displayText": "AVPU U", "nextNodeIdentifier": "feedback_wrong" },
    { "displayText": "AVPU V", "nextNodeIdentifier": "feedback_correct" }
  ]
}
```

```text
@t=[nurse_c, ???]선생님
@t=[nurse_c, @s]선생님
```

서버는 `Parallel(ByRole)`에서 이 노드를 nurse_a의 client id에만 표시하고, 같은 client id가 보낸 선택만 해당 브랜치 대기 상태에 적용한다.

역할 클라이언트에서 활력 UI 같은 로컬 이벤트를 실행하려면 다음과 같이 지정한다.

```json
{
  "identifier": "B_VITAL_OPEN",
  "nodeType": "InvokeEvent",
  "eventIdentifier": "activate_vital_monitor_ui_patient_b",
  "invokeOnRoleClient": true,
  "moveNextBehavior": "WaitUntilDone"
}
```
