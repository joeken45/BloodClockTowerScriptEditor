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
        /// <summary>
        /// 從 JSON 檔案匯入角色到資料庫
        /// </summary>
        public async Task<int> ImportFromJsonAsync(string jsonFilePath, string category = "官方", bool isOfficial = true)
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

                // 🆕 預處理 JSON 內容
                jsonContent = PreprocessJsonContent(jsonContent);

                JArray jArray;

                try
                {
                    // 嘗試解析為 JSON 陣列
                    jArray = JArray.Parse(jsonContent);
                }
                catch (JsonReaderException)
                {
                    // 如果解析失敗，可能是因為格式問題
                    throw new InvalidOperationException(
                        "JSON 格式錯誤。請確認檔案是有效的 JSON 陣列格式 [...]"
                    );
                }

                using var context = new RoleTemplateContext();

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

                        // 跳過特殊標記（爪牙訊息、惡魔訊息等）
                        if (name != null && (name.Contains("訊息") || id == "M" || id == "D"))
                        {
                            continue;
                        }

                        // 基本驗證
                        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(team))
                        {
                            continue; // 跳過無效資料
                        }

                        // 檢查是否已存在
                        var existing = await context.RoleTemplates
                            .Include(r => r.Reminders)
                            .FirstOrDefaultAsync(r => r.Id == id);

                        if (existing != null)
                        {
                            // 更新現有角色
                            UpdateRoleTemplate(existing, item, category, isOfficial);
                        }
                        else
                        {
                            // 建立新角色
                            var roleTemplate = CreateRoleTemplate(item, category, isOfficial);
                            context.RoleTemplates.Add(roleTemplate);
                        }

                        importCount++;
                    }
                    catch (Exception ex)
                    {
                        // 記錄錯誤但繼續處理其他角色
                        System.Diagnostics.Debug.WriteLine($"匯入角色時發生錯誤：{ex.Message}");
                    }
                }

                // 儲存變更
                await context.SaveChangesAsync();

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
                    trimmedLine.StartsWith("//") ||
                    trimmedLine.StartsWith("/*"))
                {
                    continue;
                }

                // 如果這行開始有 JSON 資料
                if (!foundStart && (trimmedLine.StartsWith("[") || trimmedLine.StartsWith(",")))
                {
                    foundStart = true;
                }

                if (foundStart)
                {
                    validLines.AppendLine(line);
                }
            }

            jsonContent = validLines.ToString().Trim();

            // 如果內容以逗號開頭，去掉第一個逗號並加上 [
            if (jsonContent.StartsWith(","))
            {
                jsonContent = "[" + jsonContent.Substring(1);
            }

            // 如果內容沒有以 [ 開頭，加上它
            if (!jsonContent.StartsWith("["))
            {
                jsonContent = "[" + jsonContent;
            }

            // 如果內容沒有以 ] 結尾，加上它
            if (!jsonContent.EndsWith("]"))
            {
                jsonContent = jsonContent + "]";
            }

            return jsonContent;
        }

        /// <summary>
        /// 從 JSON 物件建立 RoleTemplate
        /// </summary>
        private RoleTemplate CreateRoleTemplate(JToken item, string category, bool isOfficial)
        {
            var roleTemplate = new RoleTemplate
            {
                Id = item["id"]?.ToString() ?? string.Empty,
                Name = item["name"]?.ToString() ?? string.Empty,
                NameEng = item["name_eng"]?.ToString(),
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

            // 解析一般提示標記
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

            // 解析全局提示標記
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

            return roleTemplate;
        }

        /// <summary>
        /// 更新現有 RoleTemplate
        /// </summary>
        private void UpdateRoleTemplate(RoleTemplate existing, JToken item, string category, bool isOfficial)
        {
            existing.Name = item["name"]?.ToString() ?? existing.Name;
            existing.NameEng = item["name_eng"]?.ToString();
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

            // 清除舊的提示標記（會由 Cascade Delete 處理）
            existing.Reminders.Clear();

            // 重新加入提示標記
            var reminders = item["reminders"]?.ToObject<List<string>>();
            if (reminders != null)
            {
                foreach (var reminder in reminders)
                {
                    existing.Reminders.Add(new RoleReminder
                    {
                        RoleId = existing.Id,
                        ReminderText = reminder,
                        IsGlobal = false
                    });
                }
            }

            var remindersGlobal = item["remindersGlobal"]?.ToObject<List<string>>();
            if (remindersGlobal != null)
            {
                foreach (var reminder in remindersGlobal)
                {
                    existing.Reminders.Add(new RoleReminder
                    {
                        RoleId = existing.Id,
                        ReminderText = reminder,
                        IsGlobal = true
                    });
                }
            }
        }

        /// <summary>
        /// 取得資料庫中的角色總數
        /// </summary>
        public async Task<int> GetRoleCountAsync()
        {
            using var context = new RoleTemplateContext();
            return await context.RoleTemplates.CountAsync();
        }

        /// <summary>
        /// 清空資料庫（小心使用！）
        /// </summary>
        public async Task ClearDatabaseAsync()
        {
            using var context = new RoleTemplateContext();
            context.RoleTemplates.RemoveRange(context.RoleTemplates);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// 取得各類型角色統計
        /// </summary>
        public async Task<Dictionary<string, int>> GetRoleStatisticsAsync()
        {
            using var context = new RoleTemplateContext();

            var stats = await context.RoleTemplates
                .GroupBy(r => r.Team)
                .Select(g => new { Team = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Team, x => x.Count);

            return stats;
        }
    }
}