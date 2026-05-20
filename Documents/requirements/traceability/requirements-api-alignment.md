---
title: Requirements API Alignment Matrix
doc_type: requirement
status: active
updated: 2026-05-17
owner: docs
---

# Requirements-API 정합성 점검

## 1. 점검 범위

- 요구사항 문서
  - `requirements/gameplay/interaction/crosshair-raycast-spec.md`
  - `requirements/gameplay/interaction/interaction-feature-spec.md`
  - `requirements/ui/miui_crosshair.md`
- API 레퍼런스 문서
  - `api-references/MultiplayerInfrastructure.UI.Crosshair.md`
  - `api-references/MultiplayerInfrastructure.InteractableEntity.md`
  - `api-references/MultiplayerInfrastructure.Player.PlayerController.md`
  - `api-references/TextToSpeechService.TTSService.md`

## 2. 정합성 매트릭스

| 요구사항 항목 | API 근거 | 정합성 |
|---|---|---|
| 중앙 뷰포트 레이캐스트 수행 | `MultiplayerInfrastructure.UI.Crosshair`의 `PerformCrosshairRaycast` 설명 | 높음 |
| 레이어 마스크/최대 거리 설정 | Crosshair API의 `_raycastLayerMask`, `_raycastMaxDistance` | 높음 |
| 크로스헤어 표시 제어 | `CrosshairUIController.SetCrosshairVisible` | 높음 |
| 상호작용 인터페이스 모델 | `IInteractable`, `IInteract`, `Interactable` 문서화 | 높음 |
| 플레이어 상호작용 루프 연결 | `PlayerController.Interactables` 설명 | 중간 |
| 월드 아이템 픽업 서버 승인 흐름 | `LootableItemInteractHandler` 설명 | 높음 |
| TTS transcript와 PlayTTS 연동 | `TTSService` 문서의 transcript 참조, scenario graph node 정의 | 중간 |

## 2.1 2026-05 문서화 반영 매트릭스

| 구현 항목 | API 문서 | 요구사항 문서 | 정합성 |
|---|---|---|---|
| MovingPatientBedController 협동 이동/눕힘 | `api-references/entities/moving-patient-bed-and-patient.md` | `requirements/interaction/triage-moving-patient-bed-requirements.md` | 높음 |
| PatientController 상호작용/의료 상태 | `api-references/entities/patient-controller-reference.md` | `requirements/patient/triage-patient-models-requirements.md` | 높음 |
| IntravenousLine 연결 포인트/서비스 | `api-references/entities/intravenous-line-reference.md` | `requirements/interaction/triage-intravenous-line-requirements.md` | 높음 |
| UIDocumentWorldSurfaceBinder 월드 표면 바인딩 | `api-references/TriageTrainer.Entity.PatientMonitor.md` | `requirements/patient/patient-monitor-requirements.md` | 높음 |
| PlayerCharacterModel 어댑터 구현체 | `api-references/TriageTrainer.MultiplayerInfrastructureSupports.RegisteringMultiplayerInfrastructureSupport.PlayerModel.md`, `api-references/MultiplayerInfrastructure.Player.PlayerModel.md` | `requirements/player/player-model-requirements.md` | 중간 |

## 3. 불일치/개선 필요 사항

1. 일부 API 레퍼런스가 이동 전 문서 경로를 참조하고 있었음(이번 정리에서 링크 갱신).
2. `PlayerController` 문서는 상호작용 루프의 입력 우선순위/업데이트 순서가 축약되어 있어 상세 요구 검증에는 추가 근거가 필요함.
3. TTS 관련 요구사항은 scenario 문서와 API 문서 간 용어(`TranscriptIdentifier`, `identifier`)가 혼용되므로 용어 통일이 필요함.

## 4. 결론

- 핵심 기능(크로스헤어, 레이캐스트, 상호작용 인터페이스, 서버 승인 픽업)은 요구사항과 API 문서가 대체로 정합하다.
- 시나리오/TTS 용어와 일부 실행 순서 기술은 후속 문서 개선 대상으로 남긴다.
- 2026-05 반영 대상으로 선정된 환자/침대/수액 라인/플레이어 모델/모니터 바인더는 요구사항과 API 간 상호 참조를 확보했다.
