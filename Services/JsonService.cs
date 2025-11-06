using BloodClockTowerScriptEditor.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
                    // ✅ 新增：判斷是否為官方 ID 字串或集石格式物件
                    if (item.Type == JTokenType.String)
                    {
                        // 情況 1: BOTC 格式的簡化官方角色（如 "washerwoman"）
                        string officialId = item.ToString();

                        // 從資料庫查找對應的角色
                        using var context = new Data.RoleTemplateContext();
                        var template = context.RoleTemplates
                            .Include(r => r.Reminders)
                            .FirstOrDefault(r => r.OfficialId == officialId);

                        if (template != null)
                        {
                            var role = template.ToRole();
                            role.UseOfficialId = true;  // ✅ 標記為使用官方 ID
                            allRoles.Add(role);
                        }
                        else
                        {
                            // 找不到對應角色，記錄警告
                            System.Diagnostics.Debug.WriteLine($"⚠️ 找不到官方角色: {officialId}");
                        }

                        continue;
                    }

                    string? id = item["id"]?.ToString();

                    // ✅ 新增：判斷是否為集石格式的官方角色（只有 id 欄位，沒有其他欄位）
                    if (!string.IsNullOrEmpty(id) && id != "_meta")
                    {
                        // 檢查是否只有 id 欄位（集石格式的官方角色）
                        if (item is JObject jObject && jObject.Properties().Count() == 1 && jObject.Property("id") != null)
                        {
                            // 情況 2: 集石格式的簡化官方角色（如 {"id":"washerwoman"}）
                            string officialId = id;

                            // 從資料庫查找對應的角色
                            using var context = new Data.RoleTemplateContext();
                            var template = context.RoleTemplates
                                .Include(r => r.Reminders)
                                .FirstOrDefault(r => r.OfficialId == officialId);

                            if (template != null)
                            {
                                var role = template.ToRole();
                                role.UseOfficialId = true;  // ✅ 標記為使用官方 ID
                                allRoles.Add(role);
                            }
                            else
                            {
                                // 找不到對應角色，記錄警告
                                System.Diagnostics.Debug.WriteLine($"⚠️ 找不到官方角色: {officialId}");
                            }

                            continue;
                        }
                    }

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
                // ✅ 儲存前清理所有角色的空值 Jinx
                foreach (var role in script.Roles)
                {
                    role.RemoveEmptyJinxItems();
                }

                // ✅ 新增：生成夜晚順序陣列（在序列化之前）
                GenerateNightOrderArrays(script);

                var jArray = new JArray();
                var serializer = JsonSerializer.Create(_settings);

                // === 處理元數據 ===
                var metaObj = JObject.FromObject(script.Meta, serializer);
                jArray.Add(metaObj);

                // === 處理角色 ===
                // 🆕 先過濾並排序角色
                var rolesToExport = script.Roles.AsEnumerable();

                // BOTC格式：過濾掉必要階段角色
                if (format == ExportFormat.BOTC)
                {
                    var excludeIds = new[] { "minioninfo", "demoninfo", "dawn", "dusk" };
                    rolesToExport = rolesToExport.Where(r => !excludeIds.Contains(r.Id));
                }

                // 🆕 依照 Team 分組後，再依照 DisplayOrder 排序
                rolesToExport = rolesToExport
                    .OrderBy(r => r.Team)           // 先按 Team 排序
                    .ThenBy(r => r.DisplayOrder);   // 同 Team 內按 DisplayOrder 排序

                foreach (var role in rolesToExport)
                {
                    // ✅ 新增：判斷是否使用官方 ID
                    if (role.UseOfficialId && !string.IsNullOrEmpty(role.OfficialId))
                    {
                        // 情況 1: 使用官方 ID → 只輸出 ID 字串
                        jArray.Add(role.OfficialId);
                        continue;
                    }

                    JObject roleObj;

                    // 🆕 判斷是否為相剋規則
                    if (role.Team == TeamType.Jinxed)
                    {
                        // 相剋規則：只輸出必要欄位
                        roleObj = new JObject
                        {
                            ["id"] = role.Id,
                            ["name"] = role.Name,
                            ["team"] = "a jinxed",
                            ["ability"] = role.Ability
                        };

                        // 可選欄位：image（如果有的話）
                        if (role.Image != null && role.Image.Count > 0)
                        {
                            if (format == ExportFormat.JiShi)
                            {
                                // 集石格式：字串
                                roleObj["image"] = role.Image[0];
                            }
                            else
                            {
                                // BOTC 格式：陣列
                                roleObj["image"] = JArray.FromObject(role.Image);
                            }
                        }
                    }
                    // 🆕 判斷是否為私貨商人
                    else if (role.Name == "私貨商人")
                    {
                        // 私貨商人：只輸出必要欄位
                        roleObj = new JObject
                        {
                            ["id"] = role.Id,
                            ["name"] = role.Name,
                            ["team"] = role.Team.ToString().ToLower(),
                            ["ability"] = role.Ability
                        };

                        // 可選：如果有圖片才輸出
                        if (role.Image != null && role.Image.Count > 0 && !string.IsNullOrWhiteSpace(role.Image[0]))
                        {
                            roleObj["image"] = role.Image[0];
                        }
                    }
                    else
                    {
                        // 一般角色：完整序列化
                        roleObj = JObject.FromObject(role, serializer);

                        // 處理 Image 欄位
                        if (roleObj["image"] != null)
                        {
                            if (format == ExportFormat.JiShi)
                            {
                                // 集石格式：取第一個值，輸出為字串
                                if (roleObj["image"] is JArray imageArray && imageArray.Count > 0)
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
        private static (string role1, string role2, bool isValid) ParseJinxNameForLoad(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return (string.Empty, string.Empty, false);

            // 嘗試各種分隔符號
            char[] separators = ['&', '＆', '+', 'x', 'X', '-', '|', '/'];

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

       
        /// <summary>
        /// 生成 _meta 的 firstNight 和 otherNight 陣列
        /// </summary>
        private static void GenerateNightOrderArrays(Script script)
        {
            // 生成 firstNight 陣列
            var firstNightRoles = script.Roles
                .Where(r => r.FirstNight > 0)
                .OrderBy(r => r.FirstNight)
                .ThenBy(r => r.Name)
                .Select(r =>
                {
                    // ✅ 如果使用官方 ID，優先使用官方 ID
                    if (r.UseOfficialId && !string.IsNullOrEmpty(r.OfficialId))
                        return r.OfficialId;

                    // 否則使用原邏輯
                    return r.Id;
                })
                .ToList();

            // 只在有角色時才設置（避免空陣列）
            script.Meta.FirstNight = firstNightRoles.Count > 0 ? firstNightRoles : null;

            // 生成 otherNight 陣列
            var otherNightRoles = script.Roles
                .Where(r => r.OtherNight > 0)
                .OrderBy(r => r.OtherNight)
                .ThenBy(r => r.Name)
                .Select(r =>
                {
                    // ✅ 如果使用官方 ID，優先使用官方 ID
                    if (r.UseOfficialId && !string.IsNullOrEmpty(r.OfficialId))
                        return r.OfficialId;

                    // 否則使用原邏輯
                    return r.Id;
                })
                .ToList();

            // 只在有角色時才設置（避免空陣列）
            script.Meta.OtherNight = otherNightRoles.Count > 0 ? otherNightRoles : null;
        }

    }
}