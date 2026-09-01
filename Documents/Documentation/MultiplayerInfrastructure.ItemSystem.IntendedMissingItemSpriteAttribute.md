# <a id="MultiplayerInfrastructure_ItemSystem_IntendedMissingItemSpriteAttribute"></a> Class IntendedMissingItemSpriteAttribute

Namespace: [MultiplayerInfrastructure.ItemSystem](MultiplayerInfrastructure.ItemSystem.md)  
Assembly: Assembly\-CSharp.dll  

이 특성(Attribute)이 적용된 <xref href="MultiplayerInfrastructure.ItemSystem.Item" data-throw-if-not-resolved="false"></xref> 파생 클래스는
아이콘 스프라이트가 의도적으로 누락되었음을 선언합니다.

<p>
적용 대상: Item 파생 클래스 (클래스 선언부 위)
</p>
<example>
<pre><code class="lang-csharp">[IntendedMissingItemSprite]
public class Gauze : MedicalItem
{
  public const string Identifier = "gauze";
}</code></pre>
</example>

<p>
<code>Inherited = true</code> 이므로 부모 클래스에 적용하면 자식 클래스에도 자동 상속됩니다.
</p>

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class IntendedMissingItemSpriteAttribute : Attribute
```

#### Inheritance

object ← 
Attribute ← 
[IntendedMissingItemSpriteAttribute](MultiplayerInfrastructure.ItemSystem.IntendedMissingItemSpriteAttribute.md)

