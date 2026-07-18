---
title: "Scenario 인게임 요구사항 계약"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

# 시나리오 요구사항 계약 시스템

공식 영문 명칭은 **Scenario Requirements Contract System**이다. 시나리오 JSON에 적힌 식별자가 실행 가능한
씬·레지스트리·리소스 환경과 일치하는지, 실행 전에 확인할 수 있게 하는 기능이다. 기존 Preflight의 호환
동작은 유지하고, 더 상세한 계약은 추출·선언·검증의 세 단계로 다룬다.

## 사용자에게 보이는 결과

- NPC, 웨이포인트, 이벤트, 리소스처럼 시나리오가 요구하는 항목과 모든 사용 위치를 한 목록에서 본다.
- 같은 식별자를 여러 노드가 쓰면 하나의 항목으로 묶되 필요한 기능과 사용 위치를 모두 보존한다.
- 씬에 없거나 필요한 컴포넌트가 빠진 경우를 구분해 Editor, 빌드, 런타임에서 같은 키와 진단 코드로 알린다.
- 수동 보충 정보는 시나리오와 같은 폴더의 `.scenario.requirements.json` sidecar에 둔다. sidecar가 없어도 기존 시나리오는 inferred 결과로 실행된다.

## 확정된 용어

- **요구사항(Requirement)**: `(kind, identifier)`로 식별되는 외부 환경 계약이다.
- **사용 위치(Occurrence)**: requirement를 소비/생산한 scenario node와 field path다.
- **manifest**: graph에서 추론한 requirement와 승인된 sidecar 선언을 병합한 immutable 결과다.
- **canonical sidecar**: 사람이 승인한 `.scenario.requirements.json`이다.
- **candidate 문서**: AI 또는 수동 제안용 `.scenario.requirements.candidates.json`이다. 승인 전에는 canonical 계약이 아니다.
- **공급자(Provider)**: requirement capability를 실제로 제공하는 scene object, asset, static catalog 또는 runtime 등록이다.
- **구성 프로필(Composition Profile)**: scenario와 씬 path/GUID 및 scene role을 연결하는 asset이다.
- **생성 계획(Generation Plan)**: Apply 전에 확인하는 씬 object 생성/설정/삭제 작업 목록이다.

## 운영 원칙

- AI 후보와 생성 계획은 검토 전 씬이나 sidecar를 바꾸지 않는다.
- Production 빌드는 해결되지 않은 오류를 차단할 수 있으며, 검증 자체는 파일을 수정하지 않는다.
- 런타임 신호처럼 시작 전에 증명할 수 없는 값은 누락으로 단정하지 않고 `Indeterminate`로 표시한다.
- 실제 적용과 Undo 절차, 상태/진단 해석, 통합 검증 사용법은 [운영 가이드](../../working-guide/features/scenario/scenario-ingame-requirements-support-guide.md)를 따른다.

## 참조

- [구현 제안](../../../Agents/Proposals/done/2026-07-15-scenario-ingame-requirements-support/Feature Proposal - Scenario Ingame Requirements Support.md)
- [API 레퍼런스](../../api-references/MultiplayerInfrastructure.Scenario.Requirements.md)
