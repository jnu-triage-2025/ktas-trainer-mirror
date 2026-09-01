# <a id="MultiplayerInfrastructure_Session_UserDescriptor"></a> Class UserDescriptor

Namespace: [MultiplayerInfrastructure.Session](MultiplayerInfrastructure.Session.md)  
Assembly: Assembly\-CSharp.dll  

서버에 접속한 단일 플레이어의 설명자.

- Identifier : 서버가 발급한 UUID. 코드 내 엔티티 쿼리, PlayerTag 레지스트리 키 등에 사용.
               재접속 시 새로 발급됩니다.
- DisplayName: 사람이 읽을 수 있는 표시 이름. /tag, 시나리오, UI 플레이어 목록 등에 사용.
               중복이 가능하므로 명확한 식별이 필요하면 Identifier를 사용하세요.

개발용 씬에서 직접 실행할 경우, 랜덤 UUID가 발급되고 앞 8자리가 DisplayName으로 사용됩니다.
빌드 런타임에서는 IntroScene에서 사용자가 입력한 이름이 DisplayName이 됩니다.

```csharp
public sealed class UserDescriptor
```

#### Inheritance

object ← 
[UserDescriptor](MultiplayerInfrastructure.Session.UserDescriptor.md)

## Constructors

### <a id="MultiplayerInfrastructure_Session_UserDescriptor__ctor_System_String_System_String_"></a> UserDescriptor\(string, string\)

```csharp
public UserDescriptor(string identifier, string displayName)
```

#### Parameters

`identifier` string

`displayName` string

## Properties

### <a id="MultiplayerInfrastructure_Session_UserDescriptor_DisplayName"></a> DisplayName

플레이어가 설정한 표시 이름. IntroScene에서 입력하거나, 개발용 씬에서는 UUID 앞 8자리.
변경 시 UserDescriptorService.UpdateDisplayName()을 통해 서비스에도 반영됩니다.

```csharp
public string DisplayName { get; set; }
```

#### Property Value

 string

### <a id="MultiplayerInfrastructure_Session_UserDescriptor_Identifier"></a> Identifier

서버가 발급한 UUID. Registry 키, 코드 내 엔티티 쿼리에 사용.

```csharp
public string Identifier { get; }
```

#### Property Value

 string

## Methods

### <a id="MultiplayerInfrastructure_Session_UserDescriptor_CreateDefault"></a> CreateDefault\(\)

개발용 기본 설명자. 랜덤 UUID를 생성하고 앞 8자리를 DisplayName으로 사용.

```csharp
public static UserDescriptor CreateDefault()
```

#### Returns

 [UserDescriptor](MultiplayerInfrastructure.Session.UserDescriptor.md)

### <a id="MultiplayerInfrastructure_Session_UserDescriptor_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 string

