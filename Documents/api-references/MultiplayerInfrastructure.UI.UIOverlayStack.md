# API 레퍼런스: `MultiplayerInfrastructure.UI.UIOverlayStack`

> **네임스페이스:** `MultiplayerInfrastructure.UI`  
> **파일 위치:** `Assets/Modules/MultiplayerInfrastructure/Scripts/UI/UIOverlayStack.cs`

---

## 0. 문서 목적

`UIOverlayStack`은 채팅/인벤토리/퀘스트/다이얼로그처럼 "플레이어 입력을 잠시 UI에 양도해야 하는 패널"의 최상단 우선권을 관리하는 정적 스택입니다.

핵심 목적은 다음 두 가지입니다.

- 오버레이 중첩 상황에서 입력 대상(Top)을 일관되게 유지
- 오브젝트 파괴/씬 리로드 상황에서도 스택 정합성을 자동 복구

---

## 1. 구성 요소

- 자료구조: `Stack<IUIOverlay>` (정적)
- 이벤트: `StackChanged`
- 인터페이스 규약: `IUIOverlay.OnOverlayPushed()`, `IUIOverlay.OnOverlayPopped()`

---

## 2. 주요 공개 API

### `Top`

```csharp
public static IUIOverlay Top { get; }
```

- 반환 전 `PruneDeadOverlays()`를 실행해 파괴된 Unity 오브젝트를 정리합니다.
- 스택이 비어 있으면 `null`을 반환합니다.

### `IsTop(IUIOverlay overlay)`

```csharp
public static bool IsTop(IUIOverlay overlay)
```

- 전달한 오버레이가 현재 최상단인지 확인합니다.

### `Push(IUIOverlay overlay)`

```csharp
public static void Push(IUIOverlay overlay)
```

동작 순서:

1. dead overlay 정리
2. `overlay == null`이면 무시
3. 기존 Top이 있으면 먼저 `SafeOnOverlayPopped(previousTop)` 호출
4. 신규 오버레이 push
5. 신규 Top에 `SafeOnOverlayPushed(newTop)` 호출
6. 스택 상태가 실제로 바뀌었을 때만 `StackChanged` 발행

중요 규칙:

- 같은 인스턴스를 연속 push하면 중복 push를 방지하고 즉시 반환합니다.

### `Pop()`

```csharp
public static IUIOverlay Pop()
```

동작 순서:

1. dead overlay 정리
2. 비어 있으면 `null`
3. 현재 Top pop 후 `SafeOnOverlayPopped(popped)` 호출
4. dead overlay 재정리
5. 새 Top이 있으면 `SafeOnOverlayPushed(newTop)` 호출
6. 상태 변화가 있을 때만 `StackChanged` 발행

### `Clear()`

```csharp
public static void Clear()
```

- 스택을 끝까지 pop하면서 각 항목에 `OnOverlayPopped`를 호출합니다.

### `IsEmpty()`

```csharp
public static bool IsEmpty()
```

- dead overlay 정리 후 비어 있는지 확인합니다.

---

## 3. 생명주기 및 안전장치

### 플레이모드/도메인 리로드 초기화

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetOnSubsystemRegistration()
```

- Subsystem 재등록 시점에 스택을 초기화해 이전 세션 잔존 상태를 제거합니다.

### 죽은 오버레이 정리(`PruneDeadOverlays`)

- `overlay == null` 또는 UnityEngine.Object 파괴 상태(`unityObject == null`)를 제거합니다.
- 정리 후 Top/Count가 달라졌을 때만 `StackChanged`를 발행합니다.

### `SafeOnOverlayPushed` / `SafeOnOverlayPopped`

- 살아 있는(`IsAlive`) 오버레이에만 콜백을 호출합니다.
- 파괴된 오브젝트에 콜백을 보내며 발생할 수 있는 예외/경고를 방지합니다.

---

## 4. PlayerController 입력 루프와의 결합

`PlayerController.Input`은 오버레이 스택을 기준으로 월드 입력 경로를 차단합니다.

- `HandleEscape()`는 스택이 비어 있지 않으면 `UIOverlayStack.Pop()`을 우선 실행
- 스택이 비어 있지 않으면 인벤토리/채팅 외 월드 조작 처리 루프를 조기 종료
- 다이얼로그가 Top인 경우 상호작용 키 입력은 월드 `Interact`가 아니라 다이얼로그 선택으로 라우팅

이 구조로 인해 "UI 열림 상태에서 의도치 않게 이동/상호작용이 실행되는 문제"를 줄일 수 있습니다.

---

## 5. 사용 예시

```csharp
// 인벤토리 열기
if (UIOverlayStack.IsEmpty())
    UIOverlayStack.Push(inventoryUI);

// 현재 Top이 인벤토리면 닫기
if (UIOverlayStack.IsTop(inventoryUI))
    UIOverlayStack.Pop();

// 전환 감시
UIOverlayStack.StackChanged += () =>
{
    var top = UIOverlayStack.Top;
    Debug.Log(top == null ? "Overlay cleared" : $"Overlay top: {top}");
};
```
