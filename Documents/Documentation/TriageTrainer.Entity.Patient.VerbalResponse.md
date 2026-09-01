# <a id="TriageTrainer_Entity_Patient_VerbalResponse"></a> Enum VerbalResponse

Namespace: [TriageTrainer.Entity.Patient](TriageTrainer.Entity.Patient.md)  
Assembly: Assembly\-CSharp.dll  

GCS(Glasgow Coma Scale)의 V(Verbal Response, 언어 반응) 세부 항목을 표현합니다.
값은 해당 GCS 점수(1~5점)와 동일하게 매핑되어 있습니다.

```csharp
public enum VerbalResponse
```

## Fields

`Confused = 4` 

4점(Confused): 대화는 가능하지만, 시간이나 장소를 헷갈려 하며 횡설수설하는 상태.(기면 상태에서 흔히 나타남)



`None = 1` 

1점(None): 소리를 전혀 내지 않는 상태.



`Oriented = 5` 

5점(Oriented): 현재 시간, 장소, 자신이 누구인지 정확히 알고 정상적인 대화가 가능한 상태.



`Sounds = 2` 

2점(Sounds): 의미 있는 단어 없이 "으으...", "아아..." 등 신음소리만 내는 상태.



`Words = 3` 

3점(Words): 문장이 되지 않고 "아파", "집", "엄마" 등 단편적인 단어만 내뱉는 상태.



