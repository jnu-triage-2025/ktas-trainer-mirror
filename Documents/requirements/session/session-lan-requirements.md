---
title: "Session/LAN 기능 요구사항"
domain: "module-features.multiplayer-infrastructure"
progress: "3-implemented"
flags: []
---

## 개요

Session/LAN 기능은 로컬 네트워크에서 세션을 발견하고 사용자 정보를 유지하는 기능이다. 사용자에게는 접속 가능한 세션 탐색과 참여자 식별 기반을 제공한다.

## 상세

- 세션 브로드캐스트는 일정 주기로 세션명/주소/포트를 전송해야 한다.
- 디스커버리 리스너는 수신 데이터에서 유효 세션만 파싱해 목록화해야 한다.
- 세션 목록은 TTL 기준으로 오래된 항목을 자동 제거해야 한다.
- 사용자 설명자(UserDescriptor)는 clientId/UUID/표시명 기반 조회를 지원해야 한다.

## 기술적 세부 사항

- `LanDiscoveryService`는 UdpClient 기반 BroadcastLoop/ListenLoop를 사용한다.
- `_pendingUpdate` 플래그와 `GetDiscoveredSessions()`로 UI 폴링 최적화를 지원한다.
- `UserDescriptorService`는 Identifier/ClientId 양방향 맵을 유지한다.

## 참조

- [api:multiplayer-infrastructure-overview](../../api-references/architecture/multiplayer-infrastructure-overview.md)
- [change:scene-item-authoring-visualization](../../changes/2026-03-10-scene-item-authoring-visualization.md)
