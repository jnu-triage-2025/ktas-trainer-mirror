# Scenario JSON Validator CLI

Unity의 `ScenarioJsonSchemaValidator`와 같은 검증 코드를 사용하여 시나리오 JSON의 구문, 중복 속성, JSON Schema를 검사합니다.

```sh
dotnet run --project Tools/scenario-json-validator -- \
  Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json
```

저장소 루트 또는 그 하위에서 실행하면 기본 스키마를 자동으로 찾습니다. 다른 스키마를 사용하려면 `--schema PATH`를 지정합니다. 자동화에서는 `--format json`을 사용하면 됩니다. 유효하면 종료 코드 `0`, 검증 오류면 `1`, 사용법 또는 파일 오류면 `2`를 반환합니다.
