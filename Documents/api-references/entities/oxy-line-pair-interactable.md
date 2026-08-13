---
title: "OxyLinePairInteractable API 레퍼런스"
doc_type: api-reference
status: active
updated: 2026-08-13
---

# OxyLinePairInteractable

네임스페이스: `TriageTrainer.Entity.OxyLine`

`OxyLinePairInteractable`은 두 `OxyLineConnectionPoint`를 하나의 사용자 상호작용으로 묶는다. 두 인스턴스가 서로를 참조하며, 어느 쪽의 Collider가 감지되어도 동일한 연결을 실행한다.

상호작용 노출 조건은 다음과 같다.

- 두 포트와 두 인터랙터블이 활성화되어 있다.
- 각 인스턴스에 지정된 표시 오브젝트가 활성화되어 있다.
- 지정된 `WallAttachedOxyflowmeter`가 설치 상태다.
- 두 포트가 아직 연결되지 않았다.

네트워크 실행에서는 반대편 포트의 권위 요청으로 서버가 연결을 검증한다. 오프라인 실행에서는 `LineConnectionService.TryCreateAutomaticConnection`으로 라인을 생성한다.
