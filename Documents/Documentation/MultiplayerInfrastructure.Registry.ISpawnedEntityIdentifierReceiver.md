# <a id="MultiplayerInfrastructure_Registry_ISpawnedEntityIdentifierReceiver"></a> Interface ISpawnedEntityIdentifierReceiver

Namespace: [MultiplayerInfrastructure.Registry](MultiplayerInfrastructure.Registry.md)  
Assembly: Assembly\-CSharp.dll  

엔티티 프리셋 스폰 시 인스턴스에 식별자를 주입받기 위한 인터페이스.

`Registry.TrySpawnEntityPreset(...)` 가 Instantiate 직후, 인스턴스의 자가 등록
(예: NetworkBehaviour.OnStartClient) 이 일어나기 전에 이 메서드를 호출하여,
하나의 프리셋(또는 하위 프리셋 참조)에서 인스턴스별 식별자(예: patient_a / bed_a)를
부여할 수 있게 한다.

```csharp
public interface ISpawnedEntityIdentifierReceiver
```

## Methods

### <a id="MultiplayerInfrastructure_Registry_ISpawnedEntityIdentifierReceiver_ApplySpawnedEntityIdentifier_System_String_"></a> ApplySpawnedEntityIdentifier\(string\)

스폰된 인스턴스에 부여할 엔티티 식별자를 적용한다.

```csharp
void ApplySpawnedEntityIdentifier(string identifier)
```

#### Parameters

`identifier` string

