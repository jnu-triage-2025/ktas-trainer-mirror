# 런타임 상호작용 상태 강화 예시 구현

## 서버 권한 탑승

`CmdToggle`은 `sender.ClientId`에 해당하는 플레이어 엔티티를 레지스트리에서 찾고, 신규 탑승일 때 장비 루트 또는 attach point와의 거리가 허용 범위 안인지 확인한다. 이미 탑승한 사용자의 하차는 거리 검사를 생략한다.

## 소유자 확인형 장비 해제

IV 슬롯 해제는 다음 계약을 따른다.

```csharp
patient.ClearIVFluidConnection(
  isLeftArm: true,
  expectedSource: disconnectingInfuser);
```

현재 슬롯이 `expectedSource`와 다르면 늦게 도착한 이전 장비의 이벤트로 간주하고 무시한다.

## 모니터 단일 연결

새 모니터가 환자를 선택할 때 환자의 기존 `MonitoringPatientMonitor`를 먼저 `SetMonitoringPatient(null)`로 해제한다. 그 후 새 모니터와 환자의 정방향·역방향 참조를 설정한다.

## UI 트리 재바인딩

Title UI 컨트롤러는 현재 제목, 부제목, Actionbar 문자열과 활성 상태를 별도 필드에 저장한다. `UIDocument.rootVisualElement`가 변경되면 새 `TitleUIElement`에 이 상태와 opacity/visibility를 다시 적용한다.

## 입력 충돌 방지

탑승형 컨트롤이 `PlayerController`에 활성 컨트롤 참조를 설정한다. 플레이어 입력 처리기는 이 상태에서 `LeftShift` 물체 드롭을 실행하지 않으며, 탑승 컨트롤만 하차 요청을 처리한다.
