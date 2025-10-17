using BloodClockTowerScriptEditor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace BloodClockTowerScriptEditor.Services
{
    /// <summary>
    /// JSON 檔案讀寫服務
    /// </summary>
    public class JsonService
    {
        private readonly JsonSerializerSettings _settings;

        public JsonService()
        {
            _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
            };
        }

        /// <summary>
        /// 從檔案載入劇本
        /// </summary>
        public Script LoadScript(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var jArray = JArray.Parse(json);

                var script = new Script();
                var serializer = JsonSerializer.Create(_settings);

                // 解析每個物件
                foreach (var item in jArray)
                {
                    string? id = item["id"]?.ToString();

                    // 檢查是否為元數據
                    if (id == "_meta")
                    {
                        script.Meta = item.ToObject<ScriptMeta>(serializer) 
                                      ?? new ScriptMeta();
                    }
                    else
                    {
                        // 解析為角色
                        var role = item.ToObject<Role>(serializer);
                        if (role != null)
                        {
                            script.Roles.Add(role);
                        }
                    }
                }

                return script;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"載入劇本失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 儲存劇本到檔案
        /// </summary>
        public void SaveScript(Script script, string filePath)
        {
            try
            {
                var jArray = new JArray();

                // 先加入元數據
                jArray.Add(JObject.FromObject(script.Meta, JsonSerializer.Create(_settings)));

                // 再加入所有角色
                foreach (var role in script.Roles)
                {
                    jArray.Add(JObject.FromObject(role, JsonSerializer.Create(_settings)));
                }

                string json = jArray.ToString(Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"儲存劇本失敗: {ex.Message}", ex);
            }
        }
    }
}