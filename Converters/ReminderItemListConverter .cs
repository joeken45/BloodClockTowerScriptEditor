using BloodClockTowerScriptEditor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;

namespace BloodClockTowerScriptEditor.Converters
{
    /// <summary>
    /// JSON 轉換器：處理 string[] 與 ObservableCollection&lt;ReminderItem&gt; 之間的轉換
    /// 用於讀取 JSON 時將 string[] 轉為 ReminderItem，儲存時將 ReminderItem 轉回 string[]
    /// </summary>
    public class ReminderItemListConverter : JsonConverter<ObservableCollection<ReminderItem>>
    {
        /// <summary>
        /// 從 JSON 讀取（string[] → ObservableCollection&lt;ReminderItem&gt;）
        /// </summary>
        public override ObservableCollection<ReminderItem> ReadJson(
            JsonReader reader,
            Type objectType,
            ObservableCollection<ReminderItem>? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var collection = new ObservableCollection<ReminderItem>();

            if (reader.TokenType == JsonToken.Null)
            {
                return collection;
            }

            // 讀取 JSON 陣列
            var array = JArray.Load(reader);

            foreach (var item in array)
            {
                var text = item.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    collection.Add(new ReminderItem(text));
                }
            }

            return collection;
        }

        /// <summary>
        /// 寫入 JSON（ObservableCollection&lt;ReminderItem&gt; → string[]）
        /// </summary>
        public override void WriteJson(
            JsonWriter writer,
            ObservableCollection<ReminderItem>? value,
            JsonSerializer serializer)
        {
            if (value == null || value.Count == 0)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
                return;
            }

            writer.WriteStartArray();

            foreach (var item in value)
            {
                if (!string.IsNullOrWhiteSpace(item.Text))
                {
                    writer.WriteValue(item.Text);
                }
            }

            writer.WriteEndArray();
        }
    }
}