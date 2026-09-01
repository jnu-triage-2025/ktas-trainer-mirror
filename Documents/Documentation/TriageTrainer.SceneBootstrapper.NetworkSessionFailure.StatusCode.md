# <a id="TriageTrainer_SceneBootstrapper_NetworkSessionFailure_StatusCode"></a> Enum NetworkSessionFailure.StatusCode

Namespace: [TriageTrainer.SceneBootstrapper](TriageTrainer.SceneBootstrapper.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public enum NetworkSessionFailure.StatusCode
```

## Fields

`ConnectionLost = 2003` 

연결이 수립된 후 예기치 않게 끊겼습니다.



`ConnectionRefused = 2001` 

서버가 연결을 거부했습니다.



`ConnectionTimeout = 2002` 

서버에 연결 시도 중 타임아웃이 발생했습니다.



`ServerShutdown = 1001` 

서버가 정상적으로 세션을 종료했습니다 (정당한 사유).



`Unknown = 9999` 

분류할 수 없는 연결 오류입니다.



