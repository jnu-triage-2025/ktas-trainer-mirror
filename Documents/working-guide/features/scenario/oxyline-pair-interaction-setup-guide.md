---
title: "산소 유량계-환자 산소 라인 쌍방향 연결 설정 가이드"
doc_type: guide
status: active
updated: 2026-08-13
---

# 산소 유량계-환자 산소 라인 쌍방향 연결 설정 가이드

`OxyLinePairInteractable`은 연결 쌍의 어느 쪽을 바라봐도 같은 산소 연결 상호작용을 노출한다.

## 적용 정책

- 환자 A: T-piece 표시가 활성화되고 벽면 산소 유량계가 설치된 경우, `T피스에 산소 연결`을 표시한다. 이는 현재 권장 기획으로 구현한다.
- 환자 B/C: 비강 캐뉼라 표시가 활성화되고 벽면 산소 유량계가 설치된 경우, `비강 캐뉼라에 산소 연결`을 표시한다. 이는 채택된 기획이다.

## 프리팹 설정

1. 유량계와 환자 측 장치에 각각 `OxyLineConnectionPoint` 및 Collider를 둔다.
2. 두 포트에 각각 `OxyLinePairInteractable`을 추가한다.
3. 두 컴포넌트의 `_counterpart`를 서로 연결하고 `_localEndpoint`에 같은 GameObject의 포트를 지정한다.
4. 양쪽 `_oxyflowmeter`에 해당 `WallAttachedOxyflowmeter`를 지정한다.
5. A의 `_requiredActiveDisplay`에는 T-piece 표시 오브젝트를, B/C에는 `NasalCannulaApplied` 표시 오브젝트를 지정한다.
6. B/C 쪽 `_displayText`는 `비강 캐뉼라에 산소 연결`로 바꾼다.

연결 작업은 서버 권위 경로를 사용하며, 오프라인 실행에서는 `LineConnectionService`가 즉시 산소 라인을 만든다.

## 현재 선행 작업

환자 B/C의 비강 캐뉼라에는 `OxyLineConnectionPoint`가 배치되어 있으며, `PatientController`의 Reset은 해당 컴포넌트를 다시 찾아 직렬화 필드를 복구한다. 유량계 프리팹 포트는 아직 `IntravenousLineConnectionPoint`이므로 `OxyLineConnectionPoint`로 정정하고, 각 쌍의 `OxyLinePairInteractable` 상호 참조를 프리팹/씬에 직렬화해야 실제 상호작용이 노출된다.

`PatientCareDescriptionZone`은 산소 라인을 자동 생성하지 않는다. 반드시 위 상호작용을 실행해 연결해야 한다.
