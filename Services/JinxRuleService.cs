using BloodClockTowerScriptEditor.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BloodClockTowerScriptEditor.Services
{
    /// <summary>
    /// 相剋規則服務 - 查詢和偵測相剋規則
    /// </summary>
    public class JinxRuleService
    {
        /// <summary>
        /// 偵測劇本中的相剋規則
        /// </summary>
        /// <param name="script">當前劇本</param>
        /// <returns>偵測到的相剋規則列表</returns>
        public async Task<List<JinxRule>> DetectJinxRulesAsync(Script script)
        {
            var detectedRules = new List<JinxRule>();

            using var context = new JinxRuleContext();

            // 取得所有相剋規則
            var allRules = await context.JinxRules.ToListAsync();

            // 取得劇本中的所有角色名稱
            var roleNames = script.Roles.Select(r => r.Name).ToHashSet();

            foreach (var rule in allRules)
            {
                // 檢查是否兩個角色都在劇本中
                bool hasChar1 = roleNames.Contains(rule.Character1);
                bool hasChar2 = roleNames.Contains(rule.Character2);

                if (hasChar1 && hasChar2)
                {
                    // 檢查是否已經加入過 (用 Id 判斷)
                    bool alreadyAdded = script.Roles.Any(r => r.Id == rule.Id);

                    if (!alreadyAdded)
                    {
                        detectedRules.Add(rule);
                    }
                }
            }

            return detectedRules;
        }

        /// <summary>
        /// 根據兩個角色名稱查詢相剋規則
        /// </summary>
        public async Task<JinxRule?> GetJinxRuleAsync(string character1, string character2)
        {
            using var context = new JinxRuleContext();

            return await context.JinxRules
                .FirstOrDefaultAsync(j =>
                    (j.Character1 == character1 && j.Character2 == character2) ||
                    (j.Character1 == character2 && j.Character2 == character1));
        }

        /// <summary>
        /// 取得所有相剋規則
        /// </summary>
        public async Task<List<JinxRule>> GetAllRulesAsync()
        {
            using var context = new JinxRuleContext();
            return await context.JinxRules.ToListAsync();
        }

        /// <summary>
        /// 根據 ID 取得相剋規則
        /// </summary>
        public async Task<JinxRule?> GetRuleByIdAsync(string id)
        {
            using var context = new JinxRuleContext();
            return await context.JinxRules.FirstOrDefaultAsync(j => j.Id == id);
        }

        /// <summary>
        /// 根據角色名稱查詢所有相關的相剋規則
        /// </summary>
        public async Task<List<JinxRule>> GetRulesByCharacterNameAsync(string characterName)
        {
            using var context = new JinxRuleContext();
            return await context.JinxRules
                .Where(j => j.Character1 == characterName || j.Character2 == characterName)
                .ToListAsync();
        }
    }
}