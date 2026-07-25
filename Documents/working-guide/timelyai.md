# 전남대학교 AI 8종 서비스 - Visual Studio Code 연결

## 인증 키 발급받기

![](./_static/timelyai/timelyai-settings.png)

[전남대학교 AI 8종 서비스의 API 키 발급 페이지](https://aioni.jnu.ac.kr/jnu/settings/api-key)에서 API 키를 발급받습니다.  

## Visual Studio Code에 코딩 에이전트 익스텐션 설치/설정

[Kilo Code 익스텐션](https://marketplace.visualstudio.com/items?itemName=kilocode.Kilo-Code)을 Visual Studio Code에 설치합니다.  

![](./_static/timelyai/kilocode-tab-btn.png)  

![](./_static/timelyai/kilocode-settings-btn.png)  

설치 후, Visual Studio Code 좌측에서 Kilo Code 탭을 열어, Kilo Code 설정에 접근합니다.  

![](./_static/timelyai/kilocode-settings-provider-custom.png)  

이어서 Kilo Code 설정 > Provider > Custom provider를 `Connect` 합니다.

| 항목 | 값 |
| :-: | :-: |
| Provider ID<sup>*</sup> | `timelyai` |
| Display Name<sup>*</sup> | `전남대학교` |
| Provider API | OpenAI Compatible |
| Base URL | `https://hello.timelygpt.co.kr/api/v2/chat/bridge/openai` |
| API key | 위에서 발급받은 키 |

\*: 자유롭게 수정 가능

![](./_static/timelyai/kilocode-models-found.png)  

위 정보를 입력하면 아래의 Models 항목에 자동으로 AI 모델 목록이 로드됩니다. Add \# model(s) 합니다. (최초 설정 시에는 수백개 모델 정보가 로드됨)

추가가 완료되면 `Submit` 합니다.  

## AI 사용

![](./_static/timelyai/kilocode-dashboard.png)  

모델을 선택하고 프롬프트를 입력합니다.  

> [!NOTE]  
> GitHub Copilot에서는 패널에 파일을 드래그하여 파일을 첨부할 수 있었는데, Kilo Code에서는 `@(파일 경로)`를 입력해 파일을 첨부할 수 있습니다.  
> 파일을 첨부하지 않아도 필요한 수준에서 AI가 파일을 확인하지만, 첨부하면 속도가 향상되고 사용량이 덜합니다.  

> [!WARNING]  
> 많은 종류의 AI 모델이 리스트에 추가되지만, 학교에서 계약한 회사에서의 설정으로 인해 주로 알려진 모델만 정상적으로 동작합니다.  

- Anthropic
  - 저가 모델
    - Anthropic Claude Haiku 4.5
  - 일반 모델
    - Anthropic Claude Sonnet 4.5
    - Anthropic Claude Sonnet 4.6<sup>1</sup>
    - Anthropic Claude Sonnet 5
  - 고급 모델
    - Anthropic Claude Opus 4.5
    - Anthropic Claude Opus 4.6
    - Anthropic Claude Opus 4.7
    - Anthropic Claude Opus 4.8<sup>1</sup>
    - Anthropic Claude Opus 5
  - 초고가 모델
    - Anthropic Claude Fable 5<sup>2</sup><sup>3</sup>
- Google
  - 저가 모델
    - Google Gemini 3.1 Flash Lite
    - Google Gemini 3.5 Flash Lite
    - Google Gemini 3.5 Flash
    - Google Gemini 3.6 Flash
  - 고급 모델
    - Google Gemini 3.1 Pro Preview
- OpenAI
  - 저가 모델
    - OpenAI GPT-5.6 Luna<sup>1</sup>
  - 일반 모델
    - OpenAI GPT-5.3-Codex<sup>1</sup>
    - OpenAI GPT-5.4
    - OpenAI GPT-5.5<sup>1</sup>
    - OpenAI GPT-5.6 Luna Pro
    - OpenAI GPT-5.6 Terra
  - 고급 모델
    - OpenAI GPT-5.4 Pro
    - OpenAI GPT-5.5 Pro
    - OpenAI GPT-5.6 Terra Pro
  - 초고가 모델
    - OpenAI GPT-5.6 Sol<sup>2</sup> <sup>3</sup>
    - OpenAI GPT-5.6 Sol Pro<sup>3</sup>
- xAI
  - xAI Grok 4.3
  - xAI Grok 4.5

<sup>1</sup>: 정확도/효율성/속도/쿼터 사용량 고려 만족도 좋았던 모델

<sup>2</sup>: 정확도가 높았지만 쿼터를 매우 많이 소모하는 모델

<sup>3</sup>: [Fable 5급](https://news.hada.io/topic?id=30446) 모델
