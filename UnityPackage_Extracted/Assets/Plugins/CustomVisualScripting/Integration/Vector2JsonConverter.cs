using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CustomVisualScripting.Integration
{
    /// <summary>
    /// Сериализует Vector2 только как x/y, без обхода свойств вроде normalized (циклическая ссылка в Json.NET).
    /// </summary>
    public sealed class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(
            JsonReader reader,
            Type objectType,
            Vector2 existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Vector2.zero;

            var obj = JObject.Load(reader);
            var x = obj["x"]?.Value<float>() ?? 0f;
            var y = obj["y"]?.Value<float>() ?? 0f;
            return new Vector2(x, y);
        }
    }
}
