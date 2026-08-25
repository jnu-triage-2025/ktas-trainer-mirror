using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace MultiplayerInfrastructure.Commons
{
  public sealed class UnityColorJsonConverter : JsonConverter<Color>
  {
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      var color = new Color();
      using var doc = JsonDocument.ParseValue(ref reader);
      var root = doc.RootElement;

      color.r = root.GetProperty("r").GetSingle();
      color.g = root.GetProperty("g").GetSingle();
      color.b = root.GetProperty("b").GetSingle();
      color.a = root.GetProperty("a").GetSingle();
      return color;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
      writer.WriteStartObject();
      writer.WriteNumber("r", value.r);
      writer.WriteNumber("g", value.g);
      writer.WriteNumber("b", value.b);
      writer.WriteNumber("a", value.a);
      writer.WriteEndObject();
    }
  }
}
