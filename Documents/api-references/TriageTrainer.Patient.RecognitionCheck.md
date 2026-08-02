# API 레퍼런스: 환자 의식 확인 입력

> **네임스페이스:** `TriageTrainer.Entity`
> **구현:** `PatientController.RecognitionCheck.cs`, `RecognitionCheckMicrophoneInput.cs`

## 개요

환자 의식 확인 입력은 시나리오 이벤트가 활성화한 한 단계 동안 `nurse_a` 역할에만 환자 상호작용과 선택적 마이크 감지를 제공한다. 완료 판정은 서버 권위로 처리하며, 성공하면 해당 단계에 지정된 `ScenarioInteractionSignals` 신호를 한 번 발생시킨다.

## 공개 API

```csharp
public void ActivateRecognitionCheck(
  string completionSignal,
  bool allowMicrophone,
  string displayText = "말 걸기");
```

| 매개변수 | 의미 |
|---|---|
| `completionSignal` | 상호작용 또는 마이크 판정 성공 시 발신할 신호 |
| `allowMicrophone` | 해당 단계가 마이크 입력을 허용하는지 여부. 실제 활성화에는 게임룰도 참이어야 함 |
| `displayText` | 환자에게 노출할 상호작용 문구 |

호출은 서버 또는 오프라인 컨텍스트에서만 상태를 바꾼다. 이미 활성화된 단계가 완료되면 활성 상태를 먼저 해제하므로 중복 입력은 같은 신호를 다시 발생시키지 않는다.

## 게임룰

| 규칙 | 기본값 | 설명 |
|---|---:|---|
| `UseMicInRecognitionCheck` | `false` | 단계가 허용한 경우 마이크 감지를 활성화 |
| `DisableInteractionInRecognitionCheck` | `false` | 환자 상호작용 경로를 비활성화 |

두 규칙을 `(false, true)`로 만드는 변경은 입력 경로가 없어지므로 거부된다. `usability` 데이터팩은 `(true, false)`를 사용한다.

## 마이크 판정

`RecognitionCheckMicrophoneInput`은 런타임 초기화 시 생성되는 단일 영속 오브젝트다. 첫 번째 마이크 장치를 2초 루프 클립으로 녹음하고 최근 256개 샘플의 RMS를 계산한다. RMS 0.02 이상이 비연속 중단 없이 1초 유지되면 현재 감시 대상의 완료 요청을 보낸다. 감시 대상이 없으면 녹음을 종료한다.

마이크 권한은 씬 로드 직후 미리 요청한다. 권한 거부 또는 장치 부재는 오류로 시나리오를 중단하지 않으며, 상호작용 입력이 대체 경로가 된다.

## patient_b_c_ct 이벤트 매핑

| 이벤트 | 완료 신호 | 마이크 |
|---|---|---:|
| `activate_patient_b_recognition_1` | `patient_b_recognition_1` | 허용 |
| `activate_patient_b_recognition_2` | `patient_b_recognition_2` | 허용 |
| `activate_patient_b_recognition_3` | `patient_b_recognition_3` | 허용 |
| `activate_patient_b_recognition_4` | `patient_b_recognition_4` | 비허용 |
| `activate_patient_b_strength_check` | `patient_b_strength_checked` | 비허용 |
| `activate_patient_b_pupil_check` | `patient_b_pupil_checked` | 비허용 |
| `activate_patient_c_recognition_1` | `patient_c_recognition_1` | 허용 |
| `activate_patient_c_recognition_2` | `patient_c_recognition_2` | 허용 |
| `activate_patient_c_recognition_3` | `patient_c_recognition_3` | 허용 |
| `activate_patient_c_recognition_4` | `patient_c_recognition_4` | 비허용 |
| `activate_patient_c_strength_check` | `patient_c_strength_checked` | 비허용 |
| `activate_patient_c_pupil_check` | `patient_c_pupil_checked` | 비허용 |

각 이벤트는 식별자에 해당하는 환자 B 또는 C의 `PatientController`만 활성화한다. 두 환자의 단계와 완료 신호는 서로 공유하지 않는다.

## 네트워크 동작

활성 상태, 입력 허용 상태, 완료 신호, 표시 문구는 FishNet `SyncVar`로 복제된다. 원격 클라이언트의 상호작용·마이크 입력은 ownership을 요구하지 않는 `ServerRpc`로 전달되지만, 서버는 RPC 송신 연결의 세션 사용자 descriptor가 `nurse_a` 태그를 가졌는지 다시 검증한다. 호스트/오프라인 직접 호출도 동일한 역할 검사를 거치며 다른 역할의 입력은 무시한다.

## 관련 문서

- [설정 가이드](../working-guide/features/scenario/patient-b-recognition-check-setup-guide.md)
- [시나리오 정의](../requirements/content-definitions/scenario/patient_b_c_ct.md)
