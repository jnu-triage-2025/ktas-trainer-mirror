---
title: Module Documentation Gap Inventory
doc_type: requirement
status: active
updated: 2026-07-15
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

## 2. 반영 이력

### 2026-07-15 반영 (갭 분석 및 문서화)

| 영역 | 구현 파일(대표) | 이전 상태 | 현재 상태 |
|---|---|---|---|
| ScenarioGraph 노드 타입 전체 레퍼런스 | `Scripts/Scenario/Models/ScenarioGraphNodes/*.cs` (29종) | 미문서 | API 문서화 완료 (`MultiplayerInfrastructure.Scenario.ScenarioGraphNodes.md`) |
| Session 시스템 | `Scripts/Session/LanDiscoveryService.cs`, `UserDescriptorService.cs`, `UserDescriptor.cs`, `SessionInformationModel.cs` | 미문서 | API 문서화 완료 (`MultiplayerInfrastructure.Session.md`) |
| Permission 시스템 | `Scripts/Permission/PermissionService.cs` | 미문서 | API 문서화 완료 (`MultiplayerInfrastructure.Permission.md`) |
| Variable/Scoreboard 시스템 | `Scripts/Variable/SessionVariableService.cs` | 미문서 | API 문서화 완료 (`MultiplayerInfrastructure.Variable.md`) |
| Logging 시스템 | `Scripts/Logging/GameLogService.cs`, `GameSessionService.cs`, `GameLogEntry.cs` | 미문서 | API 문서화 완료 (`MultiplayerInfrastructure.Logging.md`) |
| MedicalItem 기반 클래스 및 전체 아이템 목록 | `Scripts/Items/MedicalItem.cs`, `Definitions/*.cs` (103개) | 1개만 문서화 | API 문서화 완료 (`TriageTrainer.ItemDefinitions.MedicalItem.md`) |

### 2026-05-20 반영

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
| UIOverlayStack | `Scripts/UI/UIOverlayStack.cs` | 간접 문서(요구사항만 언급) | API 문서화 완료 |
| Overworld 초기화 에디터 툴 | `Editor/Utils/OverworldGameObjectInitializer/*.cs`, `Scripts/Utils/OverworldSpawnPoint.cs` | 미문서 | 요구사항/API 문서화 완료 |
| Scenario Graph Editor authoring window | `Editor/Scenario/ScenarioGraphEditor/*.cs` | 미문서 | API 문서화 완료 |

## 3. 잔여 미문서화 항목

### 높은 우선순위

| 영역 | 구현 파일(대표) | 권장 문서 유형 |
|---|---|---|
| UI 컨트롤러 전체 | `Scripts/UI/Controllers/*.cs` (17개) | api-references |
| UI VisualElements 전체 | `Scripts/UI/VisualElements/*.cs` (21개) | api-references |
| Command 시스템 | `Scripts/Command/CommandService.cs`, `CommandDefinitions/*.cs` | api-references |
| WallAttachedOxyflowmeter | `Scripts/Entities/WallAttachedOxyflowmeter/WallAttachedOxyflowmeter.cs` | api-references |
| WallAttachedWallSuction | `Scripts/Entities/WallAttachedWallSuction/WallAttachedWallSuction.cs` | api-references |

### 중간 우선순위

| 영역 | 구현 파일(대표) | 권장 문서 유형 |
|---|---|---|
| RegistryPreloader Editor 툴 | `Editor/Registry/RegistryPreloaderValidationPanel.cs` | api-references |
| PatientController 상세 API | `Scripts/Patient/PatientController*.cs` (13 partial) | api-references |
| 환자 의료 상태 모델 | `Scripts/Patient/Models/*.cs` (36개) | api-references |
| Npc 엔티티 | `Scripts/Entity/Npc.cs`, `NPCBaseModelSO.cs` | api-references |
| Ridable/Reposable 엔티티 | `Scripts/Entity/Ridable.cs`, `IReposable.cs` | api-references |
| TriageScenarioEventBootstrap 이벤트 핸들러 | `.Event.*.cs` (~50 partial) | 워킹 가이드 |
| RubricModels/RubricResultStore | `Scripts/Scenario/Rubric/RubricModels.cs`, `RubricResultStore.cs` | api-references |

### 낮은 우선순위

| 영역 | 권장 문서 유형 |
|---|---|
| Camera 시스템 (`MainCameraController`, `NearbyInteractablesDetector`) | api-references |
| HumanoidAnimationController | api-references |
| FishNetSupport 래퍼 | api-references |
| PlayerInventory, PlayerGamemodeService | api-references |
| StaticObjectDisplayment/Service | api-references |

## 4. 비고

이번 반영은 시나리오 그래프 운영에 직접 필요한 노드 레퍼런스와, 코드 내 의존도가 높지만 완전히 미문서화되어 있던 핵심 시스템(Session, Permission, Variable, Logging, 아이템 정의)을 우선 대상으로 선정했습니다.

잔여 항목은 기능 변경 또는 신규 개발 시점에 순차 문서화를 권장합니다.
