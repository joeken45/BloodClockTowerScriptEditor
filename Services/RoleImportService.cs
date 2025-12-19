using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BloodClockTowerScriptEditor.Services
{
    /// <summary>
    /// 角色資料匯入服務
    /// </summary>
    public class RoleImportService
    {
        /// <summary>
        /// 從 JSON 檔案匯入角色到資料庫
        /// </summary>
        /// <param name="jsonFilePath">JSON 檔案路徑</param>
        /// <param name="isOfficial">是否為官方角色</param>
        /// <returns>匯入的角色數量</returns>
        public static async Task<int> ImportFromJsonAsync(string jsonFilePath, bool isOfficial = true)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"找不到檔案：{jsonFilePath}");
            }

            int importCount = 0;
            int updatedCount = 0;
            int addedCount = 0;

            try
            {
                // 讀取 JSON 內容
                string jsonContent = await File.ReadAllTextAsync(jsonFilePath);

                // 預處理 JSON 內容
                jsonContent = PreprocessJsonContent(jsonContent);

                JArray jArray;

                try
                {
                    // 嘗試解析為 JSON 陣列
                    jArray = JArray.Parse(jsonContent);
                }
                catch (JsonReaderException)
                {
                    throw new InvalidOperationException(
                        "JSON 格式錯誤。請確認檔案是有效的 JSON 陣列格式 [...]"
                    );
                }

                using var context = new RoleTemplateContext();

                int orderIndex = 0;
                foreach (var item in jArray)
                {
                    try
                    {
                        // 解析 JSON 物件
                        string? officialId = item["officialId"]?.ToString();
                        string? name = item["name"]?.ToString();

                        // 基本驗證
                        if (string.IsNullOrEmpty(officialId) || string.IsNullOrEmpty(name))
                        {
                            continue; // 跳過無效資料
                        }

                        // 🆕 檢查是否已存在 (用 OfficialId 和 Name 雙重判斷)
                        var existing = await context.RoleTemplates
                            .Include(r => r.Reminders)
                            .FirstOrDefaultAsync(r => r.OfficialId == officialId && r.Name == name);

                        if (existing != null)
                        {
                            string? newId = item["id"]?.ToString();

                            // 🆕 檢查 Id 是否變更
                            if (!string.IsNullOrEmpty(newId) && existing.Id != newId)
                            {
                                // Id 變更：刪除舊記錄，建立新記錄
                                context.RoleTemplates.Remove(existing);

                                var roleTemplate = CreateRoleTemplate(item, isOfficial);
                                roleTemplate.OriginalOrder = orderIndex++;
                                context.RoleTemplates.Add(roleTemplate);

                                updatedCount++;
                                System.Diagnostics.Debug.WriteLine($"🔄 重建角色 (Id變更): {name} ({existing.Id} → {newId})");
                            }
                            else
                            {
                                // Id 未變更：正常更新
                                existing.OriginalOrder = orderIndex++;
                                UpdateRoleTemplate(existing, item, isOfficial);
                                updatedCount++;
                                System.Diagnostics.Debug.WriteLine($"✏️ 更新角色: {name} ({officialId})");
                            }
                        }
                        else
                        {
                            // ➕ 建立新角色
                            var roleTemplate = CreateRoleTemplate(item, isOfficial);
                            roleTemplate.OriginalOrder = orderIndex++;
                            context.RoleTemplates.Add(roleTemplate);
                            addedCount++;
                            System.Diagnostics.Debug.WriteLine($"➕ 新增角色: {name} ({officialId})");
                        }

                        importCount++;
                    }
                    catch (Exception ex)
                    {
                        // 記錄錯誤但繼續處理其他角色
                        System.Diagnostics.Debug.WriteLine($"❌ 匯入角色時發生錯誤：{ex.Message}");
                    }
                }

                // 儲存變更
                await context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"📊 匯入統計: 總計 {importCount} 個 (新增 {addedCount} / 更新 {updatedCount})");

                return importCount;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"匯入失敗：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 預處理 JSON 內容，修正常見格式問題
        /// </summary>
        private static string PreprocessJsonContent(string jsonContent)
        {
            // 移除 BOM (Byte Order Mark)
            jsonContent = jsonContent.Trim('\uFEFF', '\u200B');

            // 移除開頭的說明文字行
            var lines = jsonContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var validLines = new System.Text.StringBuilder();
            bool foundStart = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // 跳過說明行
                if (trimmedLine.StartsWith("範例") ||
                    trimmedLine.StartsWith("說明") ||
                    trimmedLine.StartsWith("注意") ||
                    trimmedLine.StartsWith("//") ||
                    trimmedLine.StartsWith('#'))
                {
                    continue;
                }

                // 找到 JSON 陣列的開始
                if (!foundStart && trimmedLine.StartsWith('['))
                {
                    foundStart = true;
                }

                if (foundStart)
                {
                    validLines.AppendLine(line);
                }
            }

            return validLines.ToString();
        }

        /// <summary>
        /// 安全解析夜晚順序（處理空字串、小數、null）
        /// </summary>
        private static double ParseNightOrder(JToken? token)  // ✅ 返回 double
        {
            if (token == null || token.Type == JTokenType.Null)
                return 0.0;

            string? value = token.ToString();
            if (string.IsNullOrWhiteSpace(value))
                return 0.0;

            // 直接解析為 double
            if (double.TryParse(value, out double result))
                return result;

            return 0.0;
        }
        /// <summary>
        /// 安全解析提示標記（支援字串、陣列、逗號分隔）
        /// </summary>
        private static List<string> ParseReminders(JToken? token)
        {
            var result = new List<string>();

            if (token == null || token.Type == JTokenType.Null)
                return result;

            // 情況 1: 陣列格式 ["標記1", "標記2"]
            if (token.Type == JTokenType.Array)
            {
                var array = token.ToObject<List<string>>();
                if (array != null)
                {
                    foreach (var item in array)
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                            result.Add(item.Trim());
                    }
                }
            }
            // 情況 2: 字串格式 "標記1, 標記2" 或 "標記1"
            else if (token.Type == JTokenType.String)
            {
                string? value = token.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    // ✅ 排除空陣列字串表示
                    if (value.Trim() == "[]")
                        return result;

                    // 用逗號分割
                    var items = value.Split([',', '，'], StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in items)
                    {
                        string trimmed = item.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            result.Add(trimmed);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 處理提示標記 (Reminders) - 統一處理邏輯（已更新使用 ParseReminders）
        /// </summary>
        private static void ProcessReminders(RoleTemplate roleTemplate, JToken item, bool clearExisting = false)
        {
            if (clearExisting)
            {
                roleTemplate.Reminders.Clear();
            }

            // 處理一般提示標記（使用新的解析方法）
            var reminders = ParseReminders(item["reminders"]);
            foreach (var reminder in reminders)
            {
                roleTemplate.Reminders.Add(new RoleReminder
                {
                    RoleId = roleTemplate.Id,
                    ReminderText = reminder,
                    IsGlobal = false
                });
            }

            // 處理全局提示標記（使用新的解析方法）
            var remindersGlobal = ParseReminders(item["remindersGlobal"]);
            foreach (var reminder in remindersGlobal)
            {
                roleTemplate.Reminders.Add(new RoleReminder
                {
                    RoleId = roleTemplate.Id,
                    ReminderText = reminder,
                    IsGlobal = true
                });
            }
        }

        /// <summary>
        /// 建立新的 RoleTemplate（已更新使用 ParseNightOrder）
        /// </summary>
        private static RoleTemplate CreateRoleTemplate(JToken item, bool isOfficial)
        {
            var roleTemplate = new RoleTemplate
            {
                Id = item["id"]?.ToString() ?? string.Empty,
                Name = item["name"]?.ToString() ?? string.Empty,
                Team = item["team"]?.ToString() ?? "townsfolk",
                Ability = item["ability"]?.ToString(),
                Image = item["image"]?.ToString(),
                Edition = item["edition"]?.ToString() ?? "custom",
                Flavor = item["flavor"]?.ToString(),
                Setup = item["setup"]?.ToObject<bool>() ?? false,
                FirstNight = ParseNightOrder(item["firstNight"]),
                OtherNight = ParseNightOrder(item["otherNight"]),
                FirstNightReminder = item["firstNightReminder"]?.ToString(),
                OtherNightReminder = item["otherNightReminder"]?.ToString(),
                IsOfficial = isOfficial,
                OfficialId = item["officialId"]?.ToString(),
                SpecialJson = item["special"]?.ToString(Formatting.None),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            // 使用新的 ProcessReminders 方法
            ProcessReminders(roleTemplate, item);

            return roleTemplate;
        }

        /// <summary>
        /// 更新現有 RoleTemplate（已更新使用 ParseNightOrder）
        /// </summary>
        private static void UpdateRoleTemplate(RoleTemplate existing, JToken item, bool isOfficial)
        {
            existing.Name = item["name"]?.ToString() ?? existing.Name;
            existing.Team = item["team"]?.ToString() ?? existing.Team;
            existing.Ability = item["ability"]?.ToString();
            existing.Image = item["image"]?.ToString();
            existing.Edition = item["edition"]?.ToString() ?? "custom";
            existing.Flavor = item["flavor"]?.ToString();
            existing.Setup = item["setup"]?.ToObject<bool>() ?? false;
            existing.FirstNight = ParseNightOrder(item["firstNight"]);
            existing.OtherNight = ParseNightOrder(item["otherNight"]);
            existing.FirstNightReminder = item["firstNightReminder"]?.ToString();
            existing.OtherNightReminder = item["otherNightReminder"]?.ToString();
            existing.OfficialId = item["officialId"]?.ToString();
            existing.SpecialJson = item["special"]?.ToString(Formatting.None);
            existing.IsOfficial = isOfficial;
            existing.UpdatedDate = DateTime.Now;

            // 使用新的 ProcessReminders 方法（清除現有標記）
            ProcessReminders(existing, item, clearExisting: true);
        }

        /// <summary>
        /// 從 JSON 檔案匯入相剋規則到資料庫
        /// </summary>
        /// <param name="jsonFilePath">JSON 檔案路徑</param>
        /// <returns>匯入的相剋規則數量</returns>
        public static async Task<int> ImportJinxRulesFromJsonAsync(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"找不到檔案：{jsonFilePath}");
            }

            int importCount = 0;

            try
            {
                // 讀取 JSON 內容
                string jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                // 預處理 JSON 內容 (重用現有方法)
                jsonContent = PreprocessJsonContent(jsonContent);

                JArray jArray;
                try
                {
                    // 嘗試解析為 JSON 陣列
                    jArray = JArray.Parse(jsonContent);
                }
                catch (JsonReaderException)
                {
                    throw new InvalidOperationException(
                        "JSON 格式錯誤。請確認檔案是有效的 JSON 陣列格式 [...]"
                    );
                }

                using var context = new JinxRuleContext();

                // 🆕 清除所有現有相剋規則（以 JSON 為主）
                context.JinxRules.RemoveRange(context.JinxRules);

                foreach (var item in jArray)
                {
                    try
                    {
                        string? id = item["id"]?.ToString();
                        string? name = item["name"]?.ToString();

                        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ 跳過無效資料: id={id}, name={name}");
                            continue;
                        }

                        // 直接新增
                        var jinxRule = CreateJinxRule(item);
                        context.JinxRules.Add(jinxRule);
                        importCount++;
                        System.Diagnostics.Debug.WriteLine($"➕ 新增相剋規則: {name} ({id})");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ 匯入相剋規則時發生錯誤：{ex.Message}");
                    }
                }

                await context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"📊 相剋規則匯入完成: 共 {importCount} 個");

                return importCount;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"匯入相剋規則失敗：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 建立新的 JinxRule
        /// </summary>
        private static JinxRule CreateJinxRule(JToken item)
        {
            var jinxRule = new JinxRule
            {
                Id = item["id"]?.ToString() ?? string.Empty,
                Name = item["name"]?.ToString() ?? string.Empty,
                Team = "a jinxed",
                Ability = item["ability"]?.ToString() ?? string.Empty,
                Image = item["image"]?.ToString(),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            // 解析角色名稱 (從 Name 用 & 分割)
            jinxRule.ParseCharacterNames();

            return jinxRule;
        }

        /// <summary>
        /// 更新現有 JinxRule
        /// </summary>
        private static void UpdateJinxRule(JinxRule existing, JToken item)
        {
            existing.Name = item["name"]?.ToString() ?? existing.Name;
            existing.Ability = item["ability"]?.ToString() ?? existing.Ability;
            existing.Image = item["image"]?.ToString();
            existing.UpdatedDate = DateTime.Now;

            // 重新解析角色名稱
            existing.ParseCharacterNames();
        }
    }
}