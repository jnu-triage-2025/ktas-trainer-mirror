# 설계 명세 — TODO-SPEC-4: 서브그래프 재사용(Subgraph)

## 문제
`ScenarioNodeType`(`ScenarioNodeType.cs`)에 서브그래프 호출/포함 노드가 없다.
그 결과 환자 C 처치 흐름이 환자 B 흐름을 `_patient_c` 접미사로 **통째 복제**되어
(B/C/더미 동일 흐름) 노드 수가 늘고 유지보수·검증 부담이 커진다.

## 제안 (optional, 하위호환)

### A. 신규 노드
```
enum ScenarioNodeType { ... , Subgraph }   // 신규

class ScenarioSubgraphNode : IScenarioNode {
  string Identifier;
  ScenarioNodeType NodeType => Subgraph;
  string NextIdentifier;
  string SubgraphGraphIdentifier;                  // 재사용할 ScenarioGraph id
  IReadOnlyDictionary<string,string> ParameterBindings; // 예: { "patient": "patientC" }
}
```

### B. 파라미터 치환
- 재사용 대상 그래프는 토큰 표기 사용: 노드 식별자/이벤트 식별자/대상 식별자에 `${patient}` 등.
- 실행 시 `ParameterBindings`로 토큰 치환 후 인라인 실행.
- 예: 공통 그래프 `patient_treatment_common`의 `move_${patient}` →
  - C 호출: `bind patient=patientC` → `move_patientC`
  - B 호출: `bind patient=patientB` → `move_patientB`

### C. 실행 의미
- `ScenarioController`가 Subgraph 노드 진입 시: 대상 그래프를 현재 owner/태그 컨텍스트로 로드,
  파라미터 치환, 시작 노드부터 실행. 서브그래프 종료(터미널 도달) 시 호출 노드의 `NextIdentifier`로 복귀.
- **범위 한정(본 제안)**: 1회 인라인 호출 + 단순 토큰 치환만. **재귀/순환 금지** — 로드 시 호출 그래프 의존성 그래프를 만들어 순환 검출 시 로드 에러.
- 식별자 충돌 방지: 인라인 시 서브그래프 노드 식별자에 호출 인스턴스 prefix(예: `C__move_patientC`) 부여.

## 예시 (환자 B/C 통합)
```json
// patient_b_c_ct.json (개선안)
"BC_branch_B": { "nodeType": "Subgraph", "subgraphGraphIdentifier": "patient_treatment_common",
                 "parameterBindings": { "patient": "patientB", "side": "right" },
                 "nextIdentifier": "..." },
"BC_branch_C": { "nodeType": "Subgraph", "subgraphGraphIdentifier": "patient_treatment_common",
                 "parameterBindings": { "patient": "patientC", "side": "left" },
                 "nextIdentifier": "..." }
```
- `patient_treatment_common`은 의식/활력/산소/지혈/IV/동공/이송 흐름을 토큰으로 1회 정의.

## 직렬화/검증
- DTO: `ScenarioSubgraphNodeDTO`(`subgraphGraphIdentifier`, `parameterBindings`).
- `ScenarioNodeDTOConverter`: `nodeType="Subgraph"` 분기 추가.
- `scenario.schema.json`: `Subgraph` 노드 정의 추가, 루트 enum/oneOf에 포함.
- 로드 검증: 참조 그래프 존재 여부, 순환 의존, 미바인딩 토큰 검출.

## 테스트
- 인라인 실행/복귀, 파라미터 치환 정확성.
- 순환 참조 로드 에러.
- 미바인딩 토큰 경고/에러.
- 기존 그래프(서브그래프 미사용) 회귀 없음.

## 우선순위 / 위험
- 4개 TODO-SPEC 중 **영향 범위·구현 난이도 최상** → 1·2·3로 환자 A/B/C 플레이 가능화 후 **마지막**에 도입 권장.
- 미도입 시: 현재 복제 방식으로도 플레이 가능(중복만 감수). 즉 본 항목은 "플레이 차단"이 아니라 "유지보수 개선".
