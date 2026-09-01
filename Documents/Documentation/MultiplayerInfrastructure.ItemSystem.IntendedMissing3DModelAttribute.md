# <a id="MultiplayerInfrastructure_ItemSystem_IntendedMissing3DModelAttribute"></a> Class IntendedMissing3DModelAttribute

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

이 특성(Attribute)이 적용된 <xref href="MultiplayerInfrastructure.ItemSystem.Item" data-throw-if-not-resolved="false"></xref> 파생 클래스는
3D 모델(프리팹)이 의도적으로 누락되었음을 선언합니다.

<p>
적용 대상: Item 파생 클래스 (클래스 선언부 위)
</p>
<example>
<pre><code class="lang-csharp">[IntendedMissing3DModel]
public class Scissors : MedicalItem
{
  public const string Identifier = "scissors";
}</code></pre>
</example>

<p>
<code>Inherited = true</code> 이므로 부모 클래스에 적용하면 자식 클래스에도 자동 상속됩니다.
</p>

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class IntendedMissing3DModelAttribute : Attribute
```

#### Inheritance

object ← 
Attribute ← 
[IntendedMissing3DModelAttribute](MultiplayerInfrastructure.ItemSystem.IntendedMissing3DModelAttribute.md)

