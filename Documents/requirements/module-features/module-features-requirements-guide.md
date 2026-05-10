---
title: "모듈 기능 요구사항 색인"
domain: "module-features"
progress: "3-implemented"
flags: []
---

## 개요

MultiplayerInfrastructure와 TriageTrainer 구현체를 기능 단위 하위 디렉토리로 문서화한 요구사항 모음입니다.

## 상세

- 구현 코드와 API 레퍼런스를 함께 참고하여 작성합니다.
- 비개발직군 독자를 우선 대상으로 설명합니다.
- 모듈 구분은 디렉토리가 아니라 각 문서 front matter의 `domain` 필드로 표현합니다.
- 세부 구현은 기술적 세부 사항 섹션과 API 참조로 연결합니다.

## 기술적 세부 사항

- 하위 분류:
  - `registry`: 등록/조회/프리로드/웨이포인트
  - `player`: 플레이어 코어/인벤토리 커맨드/태그
  - `interaction`: 인터랙터블 체계
  - `item`: 공통 아이템 시스템 + 트리아지 아이템 정의
  - `scenario`: 공통 시나리오 엔진 + 트리아지 시나리오 확장
  - `quest`: 퀘스트 관리
  - `chat-command`: 채팅 서비스 및 커맨드 확장
  - `datapack`: 데이터팩 런타임
  - `session`: LAN 세션/탐색
  - `ui`: UI 컨트롤러/VisualElements
  - `patient`: 환자 모니터/환자 모델
- `domain` 규칙:
  - `module-features.multiplayer-infrastructure`
  - `module-features.triage-trainer`

## 참조

- [api:architecture overview](../../api-references/architecture/multiplayer-infrastructure-overview.md)
- [change:registry-preloader-validation-tooling](../../changes/2026-03-26-registry-preloader-validation-tooling.md)
