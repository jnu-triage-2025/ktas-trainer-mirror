---
title: Requirements API Alignment Matrix
doc_type: requirement
status: active
updated: 2026-04-14
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

## 3. 불일치/개선 필요 사항

1. 일부 API 레퍼런스가 이동 전 문서 경로를 참조하고 있었음(이번 정리에서 링크 갱신).
2. `PlayerController` 문서는 상호작용 루프의 입력 우선순위/업데이트 순서가 축약되어 있어 상세 요구 검증에는 추가 근거가 필요함.
3. TTS 관련 요구사항은 scenario 문서와 API 문서 간 용어(`TranscriptIdentifier`, `identifier`)가 혼용되므로 용어 통일이 필요함.

## 4. 결론

- 핵심 기능(크로스헤어, 레이캐스트, 상호작용 인터페이스, 서버 승인 픽업)은 요구사항과 API 문서가 대체로 정합하다.
- 시나리오/TTS 용어와 일부 실행 순서 기술은 후속 문서 개선 대상으로 남긴다.
