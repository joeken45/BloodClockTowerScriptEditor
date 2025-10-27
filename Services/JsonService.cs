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

                // 🆕 第一階段：收集所有角色（包含相剋規則）
                var allRoles = new List<Role>();
                ScriptMeta? meta = null;

                foreach (var item in jArray)
                {
                    string? id = item["id"]?.ToString();

                    // 檢查是否為元數據
                    if (id == "_meta")
                    {
                        meta = item.ToObject<ScriptMeta>(serializer) ?? new ScriptMeta();
                    }
                    else
                    {
                        // 解析為角色
                        var role = item.ToObject<Role>(serializer);
                        if (role != null)
                        {
                            allRoles.Add(role);
                        }
                    }
                }

                // 🆕 第二階段：建立角色名稱集合（不含相剋規則）
                var roleNamesInScript = allRoles
                    .Where(r => r.Team != TeamType.Jinxed)
                    .Select(r => r.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToHashSet();

                // 🆕 第三階段：處理角色並驗證相剋規則
                script.Meta = meta ?? new ScriptMeta();

                foreach (var role in allRoles)
                {
                    // 如果是相剋規則，進行名稱容錯處理和驗證
                    if (role.Team == TeamType.Jinxed && !string.IsNullOrEmpty(role.Name))
                    {
                        var (role1, role2, isValid) = ParseJinxNameForLoad(role.Name);

                        if (!isValid)
                        {
                            // 無法解析，跳過
                            System.Diagnostics.Debug.WriteLine($"⚠️ 跳過無效的相剋規則: {role.Name} (ID: {role.Id}) - 無法解析名稱");
                            continue;
                        }

                        // 🆕 檢查兩個角色是否都在劇本中
                        bool role1Exists = roleNamesInScript.Contains(role1);
                        bool role2Exists = roleNamesInScript.Contains(role2);

                        if (!role1Exists || !role2Exists)
                        {
                            // 角色不存在，跳過
                            var missingRoles = new List<string>();
                            if (!role1Exists) missingRoles.Add(role1);
                            if (!role2Exists) missingRoles.Add(role2);

                            System.Diagnostics.Debug.WriteLine(
                                $"⚠️ 跳過相剋規則: {role.Name} (ID: {role.Id}) - " +
                                $"以下角色不在劇本中: {string.Join(", ", missingRoles)}"
                            );
                            continue;
                        }

                        // 修正為標準格式
                        string originalName = role.Name;
                        role.Name = $"{role1}&{role2}";

                        // 記錄修正訊息（只在有變更時才顯示）
                        if (originalName != role.Name)
                        {
                            System.Diagnostics.Debug.WriteLine($"✏️ 修正相剋規則名稱: {originalName} → {role.Name}");
                        }
                    }

                    // 加入角色到劇本
                    script.Roles.Add(role);
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
                //if (format == ExportFormat.BOTC)
                //{
                //    metaObj.Remove("status");
                //    metaObj.Remove("townsfolk");
                //    metaObj.Remove("outsider");
                //    metaObj.Remove("minion");
                //    metaObj.Remove("demon");
                //    metaObj.Remove("traveler");
                //    metaObj.Remove("a jinxed");
                //}
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
        
        /// <summary>
        /// 解析相剋規則名稱（載入劇本時使用）
        /// </summary>
        private (string role1, string role2, bool isValid) ParseJinxNameForLoad(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (string.Empty, string.Empty, false);

            // 嘗試各種分隔符號
            char[] separators = { '&', '＆', '+', 'x', 'X', '-', '|', '/' };

            foreach (var sep in separators)
            {
                if (name.Contains(sep))
                {
                    var parts = name.Split(sep, 2); // 只分兩部分
                    if (parts.Length == 2)
                    {
                        string role1 = parts[0].Trim();
                        string role2 = parts[1].Trim();

                        if (!string.IsNullOrEmpty(role1) && !string.IsNullOrEmpty(role2))
                        {
                            return (role1, role2, true);
                        }
                    }
                }
            }

            // 無法解析
            return (string.Empty, string.Empty, false);
        }
    }
}