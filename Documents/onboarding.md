# 온보딩 가이드

![](./_static/onboarding__workflow.png)

개발 프로젝트는 일정한 사이클을 가지면서 운영됩니다. 이 사이클을 구체적으로 어떻게 할 것인지는 다양한 방법론이 있지만, 여기서는 매우 단순화되고 느슨한 형태로 운영합니다.  

대부분의 작업 시간은 위 이미지에서 &lt;3. (코딩) 구현&gt;, &lt;4. (코딩) 단위 테스트&gt;에 소비됩니다. 이 작업은 &lt;2.(Git) 새 브런치 생성&gt; 과정에서 생성한 자기 자신만의 브랜치 안에서만 수행됩니다. 이 프로젝트에서는 <u>한 사람이 작업하는 브랜치는 다른 사람이 접근하지 않도록</u> 합니다.

&lt;4. (코딩) 단위 테스트&gt;는 자신의 진행 상황이 자신의 입장에서 오류 없이 동작하는지 확인합니다. &lt;7. (코딩) 시스템 테스트&gt;는 다른 사람의 작업물과 합쳐졌을 때, 오류 없이 동작하는지 확인합니다.  

&lt;7. (코딩) 시스템 테스트&gt;가 성공하면 &lt;8. (Git) 병합&gt; 작업이 이루어집니다. 대개는 병합 요청을 생성하면, 리뷰어가 내용을 확인하고 (이 프로젝트에서는) 직접 수동으로 병합 작업을 수행합니다.  

여러분께서는 일반적으로는 &lt;1. (설계) 기능 요구사항 도출&gt;, &lt;2. (설계) 기능 요구사항 분석&gt; (이 두개 작업은 AI에게 지시문을 작성하기 위해서 꼭 필요합니다.) &lt;3. (코딩) 구현&gt;과 &lt;4. (코딩) 단위 테스트&gt; 작업을 반복하면서, 기능이 어느정도 진척이 있을 때 &lt;5. (Git) 커밋 및 푸시&gt; &lt;6. (.md 파일) 문서화&gt; 작업을 수행하는 작업을 반복하시게 됩니다.

이 과정에서
1. 목표한 기능이 잘 동작할 것
2. 변경 내용이 이미 작성된 다른 것과 문제를 일으키지 않을 것
이 주요한 목표가 됩니다.

문서화를 포함하여 모든 부가적인 작업은 이 두 가지 목표를 달성하기 위한 최소한의 수단으로 이해해주세요. 특히 AI 생성물이 더욱 복잡한 프로젝트를 대상으로 하는 경우에, 더욱이 이 목표가 중요합니다.  

## 빠른 시작

1. [Documents/README.md](README.md)를 먼저 읽고, 필요한 문서 링크를 확인합니다.
2. 수정하려는 영역이 무엇인지 결정합니다. (시나리오, 아이템, 인터랙터블, 문서)
3. 해당 영역의 가이드를 읽고, 작업 범위와 산출물을 정리합니다.
    > [!WARNING]  
    > 작업을 시작하기 전 반드시 브랜치를 배정받았거나 생성했는지 확인해주세요. 다른 사람이 작업중인 브랜치나, `main` 브랜치에서 직접 작업하지 마세요.
4. AI에게 문서와 템플릿을 제공하고, 필요한 추가 질문을 받습니다:
    - 지시문은 /Agents/Templates/에서 찾습니다.
    - 예시 프롬프트는 /Agents/Examples/에서 찾아서 참고합니다.
    - 실제 사용된 프롬프트는 /Agents/Actually Used/에서 찾아서 참고합니다.
    - 지시문 템플릿에 AI가 참고할 문서 링크가 포함되어있습니다. 그대로 사용하세요.
5. 결과물을 검토한 뒤 커밋합니다.

## 작업 범위와 경로 규칙

- 프로젝트 전용 구현은 반드시 `Assets/Modules/TriageTrainer/` 아래에 둡니다.
- 기반 시스템인 `Assets/Modules/MultiplayerInfrastructure/`는 원칙적으로 수정하지 않습니다.
- 기반 시스템 변경이 필요하면 `Agents/Proposals/`에 기능 제안서를 작성합니다.

## 작업 시 유니티 테스트

![](./_static/onboarding__indev-hierarchy.png)

- AI가 생성한 결과물이 런타임(실제 실행 상황을 지칭)에서 의도대로 동작하는지 확인해야 합니다. 테스트는 IndevScene에서 수행하는 것을 권장합니다. AI의 지시대로 인게임 씬에 오브젝트를 배치하고 컴포넌트를 추가한 뒤 플레이 모드로 실행하여 테스트합니다.

![](./_static/onboarding__ingamescene-scene.png)

- 인게임에서 테스트하기 위해서는 씬을 플레이하고, 이어서 인게임 화면 좌측 상단의 FishNet의 Start Server, Start Client 버튼을 차례로 클릭합니다.

<br />

![](./_static/onboarding__console-tab.png)

- AI가 생성한 C# 스크립트에 오류가 없는지 확인합니다. 유니티 에디터의 Console 탭에서 오류 메시지가 없는지 확인합니다.

## 무엇을 어디에 작성하나요

| 작업 유형 | 주로 수정/추가하는 문서 | 산출물 위치 |
|---|---|---|
| 시나리오 작성 | Documents/scenario-authoring.md | Documents/scenario/*.md |
| 아이템 구현 | Documents/item.md | Assets/Modules/TriageTrainer/Prefabs/Items/<identifier>/ |
| 인터랙터블 구현 | Agents/Actually Used/Implement - (구현한 대상의 이름).md | Assets/Modules/TriageTrainer/Prefabs/Interactables/<identifier>/ |
| 문서 보완 | Documents/*.md | Documents/ |

## 식별자 및 네이밍 규칙

- 식별자에는 소문자와 숫자, 밑줄만 사용합니다. 예: `patient_a`, `move_patient_a_to_treatment`.
- 노드 Identifier는 접두사 규칙을 따릅니다. 예: `D001`, `E010`, `P001-B1`.
- 프리팹/폴더 식별자는 `lower_snake_case`로 작성합니다.

## AI에게 전달할 핵심 정보

- 관련 문서 링크와 경로: 예) [Documents/scenario-graph.md](scenario-graph.md), [Documents/scenario/_template.md](scenario/_template.md)
- 기존 구현 또는 예시 파일: 예) [Documents/scenario/patient_a_critical.md](scenario/patient_a_critical.md)
- 작업 목표와 범위: 무엇을 추가/수정할지
- 금지 조건: `MultiplayerInfrastructure` 수정 금지

## 제출 전 체크리스트

- 변경된 파일이 올바른 경로에 있는가
- 새로운 식별자가 문서/레지스트리에 기록되었는가
- Scenario의 NextIdentifier가 모두 유효한가
- 문서에 누락된 기본 정보가 없는가
- 커밋 메시지가 규칙을 따르는가

## 자주 하는 실수

- 기반 시스템 폴더를 직접 수정함
- 식별자 규칙을 지키지 않아 참조가 끊김
- 템플릿을 그대로 남겨 불필요한 문구가 포함됨
- 시나리오에서 NextIdentifier가 비어 있거나 존재하지 않음
