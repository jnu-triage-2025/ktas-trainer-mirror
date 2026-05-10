# API 레퍼런스: MultiplayerInfrastructure.Scenario.SerializeSupport

> 네임스페이스: MultiplayerInfrastructure.Scenario  
> 파일 위치:  
> - Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/SerializeSupport/ScenarioGraphLoader.cs  
> - Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/SerializeSupport/ScenarioNodeDTOConverter.cs  
> - Assets/Modules/MultiplayerInfrastructure/Scripts/Scenario/SerializeSupport/ScenarioJsonSchemaValidator.cs

## 0. 개요

Scenario SerializeSupport는 JSON 시나리오를 런타임 ScenarioGraph로 변환하는 파이프라인입니다.

파이프라인 요약:
1. JSON Schema 검증 (선택)
2. DTO 역직렬화(nodeType 기반 분기)
3. Domain Node 변환
4. 그래프/태그 유효성 경고 출력

## 1. ScenarioGraphLoader

### LoadFromJson

```csharp
public static ScenarioGraph LoadFromJson(string json, bool validateWithSchema = true)
```

- 역할: JSON 텍스트를 ScenarioGraph로 변환
- 입력 검증:
  - 빈 문자열: ArgumentException
  - JSON 파싱 실패: JsonException
  - 스키마 위반: ScenarioSchemaValidationException
- 옵션:
  - validateWithSchema=true일 때 ScenarioJsonSchemaValidator.Validate 호출

### 내부 핵심

| 메서드 | 역할 |
|---|---|
| ToDomain | DTO -> ScenarioGraph 변환 |
| ConvertNode | DTO 타입별 Domain Node 생성 |
| NormalizeTags | 공백/중복 제거 태그 정규화 |
| WarnForUndeclaredTags | 선언되지 않은 태그 사용 경고 |

## 2. ScenarioNodeDTOConverter

### nodeType 기반 역직렬화

지원 매핑(일부):
- Dialogue -> ScenarioDialogueNodeDTO
- Choice -> ScenarioChoiceNodeDTO
- Parallel -> ScenarioParallelNodeDTO
- InvokeEvent -> ScenarioInvokeEventNodeDTO
- PlayTTS -> ScenarioPlayTTSNodeDTO
- TagModification, PlayerTag -> ScenarioPlayerTagNodeDTO

예외:
- nodeType 누락/미지원 값은 JsonException

## 3. ScenarioJsonSchemaValidator

### Validate

```csharp
public static void Validate(string json)
```

- 역할: Scenario JSON을 schema로 검증
- 실패 시: ScenarioSchemaValidationException(인스턴스 경로별 에러 메시지 포함)
- 스키마 소스: ScenarioJsonSchemaProvider.SchemaText

## 4. 작성/운영 시 주의

- nodeType 문자열은 enum 이름과 문서 표기를 일치시킵니다.
- Parallel 태그 조건 사용 시 graph.tags 선언과 동기화합니다.
- Choice는 options 배열 기반으로 작성하고, 분리된 ChoiceOptionNode 형태를 피합니다.

## 5. 관련 문서

- api-references/MultiplayerInfrastructure.Scenario.ScenarioController.md
- requirements/content-definitions/scenario/scenario-graph-spec.md
- requirements/content-definitions/scenario/json-conversion-rules.md