# <a id="MultiplayerInfrastructure_Commons_Josa"></a> Class Josa

Namespace: [MultiplayerInfrastructure.Commons](MultiplayerInfrastructure.Commons.md)  
Assembly: Assembly\-CSharp.dll  

한국어 조사(포스트포지션 파티클) 유틸리티.

<p>
Josa.js(https://github.com/e-/Josa.js)의 알고리즘을 참고하여 구현했습니다.
마지막 글자의 유니코드 코드 포인트에서 Hangul Syllable 기준점(0xAC00, '가')을 빼고
28로 나눈 나머지로 종성(받침) 유무를 판별합니다.
</p>

```csharp
public static class Josa
```

#### Inheritance

object ← 
[Josa](MultiplayerInfrastructure.Commons.Josa.md)

## Methods

### <a id="MultiplayerInfrastructure_Commons_Josa_ObjectParticle_System_String_"></a> ObjectParticle\(string\)

목적격 조사(을/를)를 판별합니다.

<p>
<ul><li>마지막 글자가 한글이고 받침이 있으면 "을"을 반환합니다 (예: "김민수" → "을").</li><li>마지막 글자가 한글이고 받침이 없으면 "를"을 반환합니다 (예: "김나리" → "를").</li><li>마지막 글자가 한글이 아니거나 텍스트가 비어 있으면 "을(를)"을 반환합니다.</li></ul>
</p>

```csharp
public static string ObjectParticle(string text)
```

#### Parameters

`text` string

조사를 붙일 대상 텍스트.

#### Returns

 string

"을", "를", 또는 "을(를)".

