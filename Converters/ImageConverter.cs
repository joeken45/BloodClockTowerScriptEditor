using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace BloodClockTowerScriptEditor.Converters
{
    /// <summary>
    /// Image 欄位轉換器 - 相容字串和陣列兩種格式
    /// </summary>
    public class ImageConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<string>);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                // 讀取單一字串 (集石格式)
                string? url = (string?)reader.Value;
                return string.IsNullOrEmpty(url) ? new List<string>() : new List<string> { url };
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                // 讀取陣列 (BOTC 格式)
                return serializer.Deserialize<List<string>>(reader) ?? new List<string>();
            }

            return new List<string>();
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // 直接序列化為陣列，JsonService 存檔時會處理格式
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var list = (List<string>)value;
            serializer.Serialize(writer, list);
        }
    }
}