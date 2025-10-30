using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Data;
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

            // 取得劇本中的所有角色名稱（排除相剋規則自己）
            var roleNames = script.Roles
                .Where(r => r.Team != TeamType.Jinxed)
                .Select(r => r.Name)
                .ToHashSet();

            foreach (var rule in allRules)
            {
                // 檢查是否兩個角色都在劇本中
                bool hasChar1 = roleNames.Contains(rule.Character1);
                bool hasChar2 = roleNames.Contains(rule.Character2);

                if (hasChar1 && hasChar2)
                {
                    // 🆕 回傳所有應該存在的相剋規則，不管是否已經加入劇本
                    detectedRules.Add(rule);
                }
            }

            return detectedRules;
        }
    }
}