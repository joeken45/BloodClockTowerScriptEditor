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
        /// <param name="script">劇本物件</param>
        /// <param name="filePath">檔案路徑</param>
        /// <param name="format">匯出格式（預設：集石）</param>
        public void SaveScript(Script script, string filePath, ExportFormat format = ExportFormat.JiShi)
        {
            try
            {
                var jArray = new JArray();
                var serializer = JsonSerializer.Create(_settings);

                // === 處理元數據 ===
                var metaObj = JObject.FromObject(script.Meta, serializer);

                // BOTC 格式：移除集石專用欄位
                if (format == ExportFormat.BOTC)
                {
                    metaObj.Remove("status");
                    metaObj.Remove("townsfolk");
                    metaObj.Remove("outsider");
                    metaObj.Remove("minion");
                    metaObj.Remove("demon");
                    metaObj.Remove("traveler");
                    metaObj.Remove("a jinxed");
                }
                // 集石格式：保留所有欄位

                jArray.Add(metaObj);

                // === 處理角色 ===
                foreach (var role in script.Roles)
                {
                    var roleObj = JObject.FromObject(role, serializer);

                    // 處理 Image 欄位
                    if (roleObj["image"] != null)
                    {
                        if (format == ExportFormat.JiShi)
                        {
                            // 集石格式：取第一個值，輸出為字串
                            var imageArray = roleObj["image"] as JArray;
                            if (imageArray != null && imageArray.Count > 0)
                            {
                                roleObj["image"] = imageArray[0];
                            }
                            else
                            {
                                roleObj.Remove("image");
                            }
                        }
                        // BOTC 格式：保持陣列原樣
                    }

                    jArray.Add(roleObj);
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