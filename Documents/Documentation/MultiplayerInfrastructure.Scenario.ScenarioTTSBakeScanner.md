# <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner"></a> Class ScenarioTTSBakeScanner

Namespace: [MultiplayerInfrastructure.Scenario](MultiplayerInfrastructure.Scenario.md)  
Assembly: Assembly\-CSharp.dll  

프로젝트 내 모든 시나리오 그래프(JSON)를 스캔하여, PlayTTS 플래그가 켜져 있고
변수를 포함하지 않는(=bake 가능한) 인라인 텍스트(Dialogue/DisinteractableDialogue/Choice/Quiz 콘텐츠)를 수집하고
사전 합성(bake)한다.

이 클래스는 에디터 전용(<code>#if UNITY_EDITOR</code>)이지만 <code>Editor</code> 폴더 밖(런타임 어셈블리)에
위치한다. 이렇게 하면 <code>Assembly-CSharp</code>(에디터 정의 포함)로 컴파일되어,
동일 어셈블리의 에디터 훅(TTSPlayModeValidator, TTSBuildPreprocessor)에서도 참조할 수 있다.

수집 결과는 다음 두 곳에서 사용된다.
  · 인라인 오디오 Baker (사전 합성)
  · 플레이 모드 진입 / 빌드 시 bake 상태(미bake/dirty) 검사

```csharp
public static class ScenarioTTSBakeScanner
```

#### Inheritance

object ← 
[ScenarioTTSBakeScanner](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.md)

## Methods

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_BakeAllNeededSynchronously_System_String_System_Int32_System_Single_System_String_TextToSpeechService_TTSVoiceProfile___"></a> BakeAllNeededSynchronously\(string, int, float, string, TTSVoiceProfile\[\]\)

스캔 후 미bake/dirty 작업을 동기적으로 모두 bake한다.
플레이 모드 진입 검사에서 사용자가 "지금 bake"를 선택했을 때 호출한다.
진행 상황은 <xref href="UnityEditor.EditorUtility.DisplayProgressBar(System.String%2cSystem.String%2cSystem.Single)" data-throw-if-not-resolved="false"></xref> 로 표시한다.

```csharp
public static void BakeAllNeededSynchronously(string language = "ko", int totalStep = 5, float speed = 1.05, string voiceStyleName = "F1", TTSVoiceProfile[] voiceProfiles = null)
```

#### Parameters

`language` string

기본 목소리에 사용할 언어 코드

`totalStep` int

기본 목소리 Diffusion 스텝 수

`speed` float

기본 목소리 발화 속도

`voiceStyleName` string

기본 목소리 스타일 파일명

`voiceProfiles` TTSVoiceProfile\[\]

추가 목소리 프로파일 목록. null이면 기본 목소리만 사용한다.
각 프로파일이 지정한 voice identifier로 bake된 job은 해당 프로파일의 스타일로 합성된다.

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ContainsVariable_System_String_"></a> ContainsVariable\(string\)

텍스트가 변수({...})를 포함하면 true (=bake 불가, 런타임 즉석 합성 대상).

```csharp
public static bool ContainsVariable(string text)
```

#### Parameters

`text` string

#### Returns

 bool

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_DeleteOrphans_System_Collections_Generic_IEnumerable_System_String__"></a> DeleteOrphans\(IEnumerable<string\>\)

사용하지 않는(orphan) baked WAV 파일과 그 .meta를 삭제한다.
삭제 후 비게 된 하위 디렉터리도 정리한다.

```csharp
public static int DeleteOrphans(IEnumerable<string> orphanedPaths)
```

#### Parameters

`orphanedPaths` IEnumerable<string\>

#### Returns

 int

실제로 삭제한 파일 수.

### <a id="MultiplayerInfrastructure_Scenario_ScenarioTTSBakeScanner_ScanAllScenarios_System_String_"></a> ScanAllScenarios\(string\)

AssetDatabase에서 *.scenario.json TextAsset을 모두 찾아 스캔한다.
(에디터 전용)

```csharp
public static ScenarioTTSBakeScanner.ScanResult ScanAllScenarios(string streamingAssetsPath)
```

#### Parameters

`streamingAssetsPath` string

#### Returns

 [ScenarioTTSBakeScanner](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.md).[ScanResult](MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.ScanResult.md)

