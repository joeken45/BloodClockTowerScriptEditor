using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace BloodClockTowerScriptEditor.ViewModels
{
    /// <summary>
    /// 相剋規則同步輔助類別 - 處理集石與 BOTC 雙格式的相剋規則維護
    /// </summary>
    public static class JinxSyncHelper
    {
        /// <summary>
        /// 為劇本中所有角色同步 BOTC 格式的 Jinxes 陣列
        /// </summary>
        public static async Task SyncAllRoleJinxesAsync(Script script)
        {
            var jinxService = new JinxRuleService();

            foreach (var role in script.Roles)
            {
                // 跳過集石格式的獨立相剋物件
                if (role.Team == TeamType.Jinxed)
                    continue;

                await UpdateRoleJinxesAsync(role, script.Roles, jinxService);
            }
        }

        /// <summary>
        /// 更新單一角色的 Jinxes 陣列
        /// </summary>
        private static async Task UpdateRoleJinxesAsync(Role role, ObservableCollection<Role> allRoles, JinxRuleService jinxService)
        {
            // 1. 查詢資料庫中包含此角色名稱的相剋規則
            var rules = await jinxService.GetRulesByCharacterNameAsync(role.Name);

            if (rules.Count == 0)
            {
                // 沒有相剋規則，清空 Jinxes
                role.Jinxes = null;
                return;
            }

            // 2. 建立新的 Jinxes 列表
            var jinxes = new List<Role.JinxInfo>();

            foreach (var rule in rules)
            {
                // 找出對方角色名稱
                string otherCharName = rule.Character1 == role.Name
                    ? rule.Character2
                    : rule.Character1;

                // 在劇本中找到對方角色的實際 ID
                var otherRole = allRoles.FirstOrDefault(r => r.Name == otherCharName && r.Team != TeamType.Jinxed);

                if (otherRole != null)
                {
                    jinxes.Add(new Role.JinxInfo
                    {
                        Id = otherRole.Id,
                        Reason = rule.Ability
                    });
                }
            }

            // 3. 更新角色的 Jinxes 屬性
            role.Jinxes = jinxes.Count > 0 ? jinxes : null;
        }

        /// <summary>
        /// 檢查並加入/移除集石格式的相剋規則獨立物件
        /// </summary>
        public static async Task SyncJinxedRolesAsync(Script script)
        {
            var jinxService = new JinxRuleService();

            // 1. 偵測應該存在的相剋規則
            var detectedRules = await jinxService.DetectJinxRulesAsync(script);

            // 2. 取得目前劇本中的相剋規則物件
            var existingJinxedRoles = script.Roles
                .Where(r => r.Team == TeamType.Jinxed)
                .ToList();

            // 3. 加入缺少的相剋規則
            foreach (var rule in detectedRules)
            {
                bool alreadyAdded = existingJinxedRoles.Any(r => r.Id == rule.Id);

                if (!alreadyAdded)
                {
                    var role = rule.ToRole();
                    script.Roles.Add(role);
                    System.Diagnostics.Debug.WriteLine($"✅ 加入相剋規則: {role.Name}");
                }
            }

            // 4. 移除多餘的相剋規則
            var validJinxIds = detectedRules.Select(r => r.Id).ToHashSet();
            var rolesToRemove = existingJinxedRoles
                .Where(r => !validJinxIds.Contains(r.Id))
                .ToList();

            foreach (var role in rolesToRemove)
            {
                script.Roles.Remove(role);
                System.Diagnostics.Debug.WriteLine($"🗑️ 移除相剋規則: {role.Name}");
            }
        }
    }
}