using BloodClockTowerScriptEditor.Models;
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
        /// <param name="category">分類標籤（如：官方、社群等）</param>
        /// <param name="isOfficial">是否為官方角色</param>
        /// <returns>匯入的角色數量</returns>
        public async Task<int> ImportFromJsonAsync(string jsonFilePath, string category = "官方", bool isOfficial = true)
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
                        string? id = item["id"]?.ToString();
                        string? name = item["name"]?.ToString();
                        string? team = item["team"]?.ToString();

                        // 跳過範例資料
                        if (name != null && (name.Contains("範例") || name.Contains("名稱1")))
                        {
                            continue;
                        }

                        // 基本驗證
                        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(team))
                        {
                            continue; // 跳過無效資料
                        }

                        // 🆕 檢查是否已存在 (用 Id 和 Name 雙重判斷)
                        var existing = await context.RoleTemplates
                            .Include(r => r.Reminders)
                            .FirstOrDefaultAsync(r => r.Id == id && r.Name == name);

                        if (existing != null)
                        {
                            // 🔄 更新現有角色
                            existing.OriginalOrder = orderIndex++;
                            UpdateRoleTemplate(existing, item, category, isOfficial);
                            updatedCount++;
                            System.Diagnostics.Debug.WriteLine($"✏️ 更新角色: {name} ({id})");
                        }
                        else
                        {
                            // ➕ 建立新角色
                            var roleTemplate = CreateRoleTemplate(item, category, isOfficial);
                            roleTemplate.OriginalOrder = orderIndex++;
                            context.RoleTemplates.Add(roleTemplate);
                            addedCount++;
                            System.Diagnostics.Debug.WriteLine($"➕ 新增角色: {name} ({id})");
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
        private string PreprocessJsonContent(string jsonContent)
        {
            // 移除 BOM (Byte Order Mark)
            jsonContent = jsonContent.Trim('\uFEFF', '\u200B');

            // 移除開頭的說明文字行
            var lines = jsonContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
                    trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                // 找到 JSON 陣列的開始
                if (!foundStart && trimmedLine.StartsWith("["))
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
        /// 處理提示標記 (Reminders) - 統一處理邏輯
        /// </summary>
        /// <param name="roleTemplate">要處理的角色範本</param>
        /// <param name="item">JSON 項目</param>
        /// <param name="clearExisting">是否清除現有標記（更新時使用）</param>
        private void ProcessReminders(RoleTemplate roleTemplate, JToken item, bool clearExisting = false)
        {
            if (clearExisting)
            {
                roleTemplate.Reminders.Clear();
            }

            // 處理一般提示標記
            var reminders = item["reminders"]?.ToObject<List<string>>();
            if (reminders != null)
            {
                foreach (var reminder in reminders)
                {
                    roleTemplate.Reminders.Add(new RoleReminder
                    {
                        RoleId = roleTemplate.Id,
                        ReminderText = reminder,
                        IsGlobal = false
                    });
                }
            }

            // 處理全局提示標記
            var remindersGlobal = item["remindersGlobal"]?.ToObject<List<string>>();
            if (remindersGlobal != null)
            {
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
        }

        /// <summary>
        /// 建立新的 RoleTemplate
        /// </summary>
        private RoleTemplate CreateRoleTemplate(JToken item, string category, bool isOfficial)
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
                FirstNight = item["firstNight"]?.ToObject<int>() ?? 0,
                OtherNight = item["otherNight"]?.ToObject<int>() ?? 0,
                FirstNightReminder = item["firstNightReminder"]?.ToString(),
                OtherNightReminder = item["otherNightReminder"]?.ToString(),
                Category = category,
                IsOfficial = isOfficial,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            // ✅ 原本這裡有 40 行 Reminders 處理邏輯
            // 🔄 現在改用統一的 ProcessReminders() 方法
            ProcessReminders(roleTemplate, item);

            return roleTemplate;
        }

        /// <summary>
        /// 更新現有 RoleTemplate (覆蓋所有欄位)
        /// </summary>
        private void UpdateRoleTemplate(RoleTemplate existing, JToken item, string category, bool isOfficial)
        {
            existing.Name = item["name"]?.ToString() ?? existing.Name;
            existing.Team = item["team"]?.ToString() ?? existing.Team;
            existing.Ability = item["ability"]?.ToString();
            existing.Image = item["image"]?.ToString();
            existing.Edition = item["edition"]?.ToString() ?? "custom";
            existing.Flavor = item["flavor"]?.ToString();
            existing.Setup = item["setup"]?.ToObject<bool>() ?? false;
            existing.FirstNight = item["firstNight"]?.ToObject<int>() ?? 0;
            existing.OtherNight = item["otherNight"]?.ToObject<int>() ?? 0;
            existing.FirstNightReminder = item["firstNightReminder"]?.ToString();
            existing.OtherNightReminder = item["otherNightReminder"]?.ToString();
            existing.Category = category;
            existing.IsOfficial = isOfficial;
            existing.UpdatedDate = DateTime.Now;

            // ✅ 原本這裡有 40 行 Reminders 處理邏輯（含 Clear() + 兩個 foreach）
            // 🔄 現在改用統一的 ProcessReminders() 方法，並傳入 clearExisting: true
            ProcessReminders(existing, item, clearExisting: true);
        }

        /// <summary>
        /// 從 JSON 檔案匯入相剋規則到資料庫
        /// </summary>
        /// <param name="jsonFilePath">JSON 檔案路徑</param>
        /// <returns>匯入的相剋規則數量</returns>
        public async Task<int> ImportJinxRulesFromJsonAsync(string jsonFilePath)
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

                foreach (var item in jArray)
                {
                    try
                    {
                        // 解析 JSON 物件
                        string? id = item["id"]?.ToString();
                        string? name = item["name"]?.ToString();
                        string? team = item["team"]?.ToString();

                        // 基本驗證
                        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                        {
                            continue; // 跳過無效資料
                        }

                        // 驗證是否為相剋規則 (team 必須是 "a jinxed")
                        if (team?.ToLower() != "a jinxed")
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ 跳過非相剋規則: {name} (team: {team})");
                            continue;
                        }

                        // 檢查是否已存在 (用 Id 判斷)
                        var existing = await context.JinxRules
                            .FirstOrDefaultAsync(j => j.Id == id);

                        if (existing != null)
                        {
                            // 更新現有規則
                            UpdateJinxRule(existing, item);
                            updatedCount++;
                            System.Diagnostics.Debug.WriteLine($"✏️ 更新相剋規則: {name} ({id})");
                        }
                        else
                        {
                            // 建立新規則
                            var jinxRule = CreateJinxRule(item);
                            context.JinxRules.Add(jinxRule);
                            addedCount++;
                            System.Diagnostics.Debug.WriteLine($"➕ 新增相剋規則: {name} ({id})");
                        }

                        importCount++;
                    }
                    catch (Exception ex)
                    {
                        // 記錄錯誤但繼續處理其他規則
                        System.Diagnostics.Debug.WriteLine($"❌ 匯入相剋規則時發生錯誤：{ex.Message}");
                    }
                }

                // 儲存變更
                await context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"📊 相剋規則匯入統計: 總計 {importCount} 個 (新增 {addedCount} / 更新 {updatedCount})");

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
        private JinxRule CreateJinxRule(JToken item)
        {
            var jinxRule = new JinxRule
            {
                Id = item["id"]?.ToString() ?? string.Empty,
                Name = item["name"]?.ToString() ?? string.Empty,
                Team = "a jinxed",
                Ability = item["ability"]?.ToString() ?? string.Empty,
                Image = item["image"]?.ToString(),
                Setup = item["setup"]?.ToObject<bool>() ?? false,
                FirstNight = item["firstNight"]?.ToObject<double>() ?? 0.0,
                OtherNight = item["otherNight"]?.ToObject<double>() ?? 0.0,
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
        private void UpdateJinxRule(JinxRule existing, JToken item)
        {
            existing.Name = item["name"]?.ToString() ?? existing.Name;
            existing.Ability = item["ability"]?.ToString() ?? existing.Ability;
            existing.Image = item["image"]?.ToString();
            existing.Setup = item["setup"]?.ToObject<bool>() ?? false;
            existing.FirstNight = item["firstNight"]?.ToObject<double>() ?? 0.0;
            existing.OtherNight = item["otherNight"]?.ToObject<double>() ?? 0.0;
            existing.UpdatedDate = DateTime.Now;

            // 重新解析角色名稱
            existing.ParseCharacterNames();
        }
    }
}