# Chat Module

| 관계 모듈 | 책임 |
| :-: | :-- |
| UIOverlayStack | 전역 UI 스택. 최상위 오버레이만 입력을 받도록 하여 Esc/Enter 충돌 방지 |
| ChatUIController | UI Toolkit 기반 채팅 패널 제어 (포커스/로그 확장/축소) |
| ChatLogView | 로그 데이터 관리 및 UI 렌더링 |
| ChatMessageValidator | 메시지·명령 검증, 길이 제한·금칙어 처리 |
| ChatCommandService + IChatCommand | 명령 등록/실행, 권한 체크 |
| ChatManager | FishNet 네트워크 RPC, 쿨다운, 입력 핸들링, 스택 push/pop |

## Getting Started

프리팹 생성
1. UIDocument 컴포넌트를 가진 빈 게임 오브젝트를 생성합니다.
2. 생성한 게임 오브젝트에 다음의 컴포넌트를 추가합니다.
    - ChatManager
    - ChatManager를 추가하면 다음의 컴포넌트가 자동으로 추가됩니다:
        - ChatUIController
        - ChatUIController_ChatLogView
        - ChatCommandService
