# 채팅 전송 결과 표시 예제 구현

## 변경 위치

`Assets/Modules/MultiplayerInfrastructure/Scripts/UI/Controllers/ChatUIController.cs`의 `HandleSubmitKey()`를 변경한다.

```csharp
_chatPanel?.SetOpen(false);
_chatPanel?.ClearInput();
SetDocumentRootPickingEnabled(_uiDocument, false);
```

## 동작 흐름

1. 일반 텍스트를 제출하면 `ChatService.SendChatServerRpc()`가 서버로 전송한다.
2. 서버가 메시지를 검증한 뒤 `ReceiveChatObserversRpc()`로 모든 접속자에게 전달한다.
3. 수신한 클라이언트는 `ChatPanelElement.AppendMessage()`로 로그에 메시지를 추가하고, 닫힌 패널이면 토스트를 생성한다.
4. 채팅 UIDocument는 보이지만 비상호작용 상태이므로 발신자를 포함한 수신자는 토스트를 확인하고 게임 조작을 계속할 수 있다.

## 검증 절차

1. Unity Editor에서 Host와 Client를 실행한다.
2. Client에서 `T`를 누르고 일반 메시지를 입력한 뒤 `Enter`를 누른다.
3. Client의 채팅 패널이 닫히고, 서버 반향 뒤 새 메시지 토스트가 표시되는지 확인한다.
4. Host에도 같은 메시지 토스트가 한 번만 표시되는지 확인한다.
5. Client에서 `/help`를 입력하고 `Enter`를 눌러 패널이 닫히는지 확인한다.
6. 일반 메시지 입력 중 `Esc`를 눌러 패널이 닫히는지 확인한다.
