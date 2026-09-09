# Scenario JSON Validator CLI

Unity의 `ScenarioJsonSchemaValidator`와 같은 검증 코드를 사용하여 시나리오 JSON의 구문, 중복 속성, JSON Schema를 검사합니다.

```sh
dotnet run --project Tools/scenario-json-validator -- \
  Assets/Modules/TriageTrainer/Resources/Scenario/patient_a_critical.scenario.json
```

저장소 루트 또는 그 하위에서 실행하면 기본 스키마를 자동으로 찾습니다. 다른 스키마를 사용하려면 `--schema PATH`를 지정합니다. 자동화에서는 `--format json`을 사용하면 됩니다. 유효하면 종료 코드 `0`, 검증 오류면 `1`, 사용법 또는 파일 오류면 `2`를 반환합니다.

콘텐츠 브랜치를 병합한 뒤에는 상호작용 레지스트리 마이그레이션 회귀 검사도 실행합니다.

```sh
python3 Tools/scenario-json-validator/test_interaction_migration.py
```

이 검사는 폐기된 노드와 NPC 필드의 재유입, 상호작용 정의 누락과 주소 중복, 가시성 제어 대상, 의사에게 제출하는 물품과 완료 신호, 도입부의 이송 상호작용 개방 순서, B/C 분기 역할과 가시성 조건, 퀘스트 참조를 확인합니다. Unity 재생과 서버의 역할 승인 동작은 E2E에서 별도로 검증합니다.
