---
title: Module Documentation Gap Inventory
doc_type: requirement
status: active
updated: 2026-05-17
owner: docs
---

# 모듈 문서화 갭 인벤토리

## 1. 점검 기준

- 코드 인벤토리:
  - Assets/Modules/MultiplayerInfrastructure/**/*.cs
  - Assets/Modules/TriageTrainer/**/*.cs
- 기존 문서:
  - Documents/api-references/**/*.md
  - Documents/requirements/**/*.md

## 2. 이번 반영 대상(우선순위)

| 영역 | 구현 파일(대표) | 이전 상태 | 현재 상태 |
|---|---|---|---|
| PlayerTag 서비스 | `Scripts/Tag/PlayerTagService.cs` | 미문서 | 문서화 완료 |
| Scenario SerializeSupport | `Scenario/SerializeSupport/*.cs` | 미문서 | 문서화 완료 |
| Triage 이벤트 부트스트랩 | `TriageScenarioEventBootstrap*.cs` | 부분 문서(요구사항 중심) | API 문서화 완료 |
| Patient 모니터 | `Entity/PatientMonitor/*.cs` | 미문서 | 문서화 완료 |
| 이동 침대/환자 상호작용 | `Scripts/Entities/MovingPatientBed/*.cs`, `Scripts/Patient/PatientController*.cs` | 부분 문서 | API/요구사항 보강 완료 |
| 수액 라인 연결 | `Scripts/Entities/IntravenousLine/*.cs` | 미문서 | API/요구사항 문서화 완료 |
| 플레이어 캐릭터 모델 어댑터 | `Scripts/MultiplayerInfrastructureSupports/PlayerCharacterModels/*.cs` | 간접 문서만 존재 | API 문서 보강 완료 |
| 문서화 절차 | `working-guide/ai-workflow.md` | 미정의 | 절차 추가 완료 |

## 3. 잔여 권장 항목

| 영역 | 권장 문서 유형 | 우선순위 |
|---|---|---|
| RegistryPreloader Editor 툴 | api-references | 중간 |
| UI VisualElements 상세 | api-references | 중간 |
| TriageTrainer Item Definitions 자동 생성 규칙 | requirements + workflow | 중간 |
| Session/LAN Discovery 서비스 | api-references | 낮음 |

## 4. 비고

이번 반영은 운영 리스크가 높은 런타임 핵심 경로를 우선 대상으로 선정했습니다. 잔여 항목은 기능 변경 시점에 순차 문서화를 권장합니다.
