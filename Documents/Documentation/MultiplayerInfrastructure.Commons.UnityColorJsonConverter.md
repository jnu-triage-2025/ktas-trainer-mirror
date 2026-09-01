# <a id="MultiplayerInfrastructure_Commons_UnityColorJsonConverter"></a> Class UnityColorJsonConverter

Namespace: [MultiplayerInfrastructure.Commons](MultiplayerInfrastructure.Commons.md)  
Assembly: Assembly\-CSharp.dll  

```csharp
public sealed class UnityColorJsonConverter : JsonConverter<Color>
```

#### Inheritance

object ← 
JsonConverter ← 
JsonConverter<Color\> ← 
[UnityColorJsonConverter](MultiplayerInfrastructure.Commons.UnityColorJsonConverter.md)

## Methods

### <a id="MultiplayerInfrastructure_Commons_UnityColorJsonConverter_Read_System_Text_Json_Utf8JsonReader__System_Type_System_Text_Json_JsonSerializerOptions_"></a> Read\(ref Utf8JsonReader, Type, JsonSerializerOptions\)

Reads and converts the JSON to type <xref href="UnityEngine.Color" data-throw-if-not-resolved="false"></xref>.

```csharp
public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
```

#### Parameters

`reader` Utf8JsonReader

The reader.

`typeToConvert` Type

The type to convert.

`options` JsonSerializerOptions

An object that specifies serialization options to use.

#### Returns

 Color

The converted value.

### <a id="MultiplayerInfrastructure_Commons_UnityColorJsonConverter_Write_System_Text_Json_Utf8JsonWriter_UnityEngine_Color_System_Text_Json_JsonSerializerOptions_"></a> Write\(Utf8JsonWriter, Color, JsonSerializerOptions\)

Writes a specified value as JSON.

```csharp
public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
```

#### Parameters

`writer` Utf8JsonWriter

The writer to write to.

`value` Color

The value to convert to JSON.

`options` JsonSerializerOptions

An object that specifies serialization options to use.

