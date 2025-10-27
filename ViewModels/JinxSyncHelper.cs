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

        /// <summary>
        /// 從所有角色的 BOTC Jinxes 同步到集石格式和其他角色
        /// （處理使用者自訂的相剋規則）
        /// </summary>
        public static void SyncFromAllBotcJinxes(Script script)
        {
            // 1. 收集所有相剋關係（從每個角色的 Jinxes）
            var jinxPairs = new HashSet<(string id1, string name1, string id2, string name2, string reason)>();

            foreach (var role in script.Roles.Where(r => r.Team != TeamType.Jinxed))
            {
                if (role.Jinxes == null || role.Jinxes.Count == 0)
                    continue;

                foreach (var jinx in role.Jinxes)
                {
                    // 找到目標角色
                    var targetRole = script.Roles.FirstOrDefault(r => r.Id == jinx.Id && r.Team != TeamType.Jinxed);
                    if (targetRole == null) continue;

                    // 確保順序一致（字母排序，避免重複）
                    var (id1, name1, id2, name2) = string.Compare(role.Id, targetRole.Id, StringComparison.Ordinal) < 0
                        ? (role.Id, role.Name, targetRole.Id, targetRole.Name)
                        : (targetRole.Id, targetRole.Name, role.Id, role.Name);

                    jinxPairs.Add((id1, name1, id2, name2, jinx.Reason));
                }
            }

            // 2. 同步到集石格式
            var existingJinxedRoles = script.Roles.Where(r => r.Team == TeamType.Jinxed).ToList();

            // 移除多餘的集石規則
            var validJinxIds = new HashSet<string>();
            foreach (var pair in jinxPairs)
            {
                validJinxIds.Add($"{pair.id1}_{pair.id2}_meta");
            }

            foreach (var role in existingJinxedRoles)
            {
                if (!validJinxIds.Contains(role.Id))
                {
                    script.Roles.Remove(role);
                    System.Diagnostics.Debug.WriteLine($"🗑️ 移除集石相剋規則: {role.Name}");
                }
            }

            // 加入或更新集石規則
            foreach (var pair in jinxPairs)
            {
                string jinxId = $"{pair.id1}_{pair.id2}_meta";
                string jinxName = $"{pair.name1}&{pair.name2}";

                var existing = script.Roles.FirstOrDefault(r => r.Id == jinxId);
                if (existing != null)
                {
                    // 更新現有規則
                    existing.Name = jinxName;
                    existing.Ability = pair.reason;
                    System.Diagnostics.Debug.WriteLine($"✏️ 更新集石相剋規則: {jinxName}");
                }
                else
                {
                    // 建立新規則
                    var newJinxRole = new Role
                    {
                        Id = jinxId,
                        Name = jinxName,
                        Team = TeamType.Jinxed,
                        Ability = pair.reason,
                        Image = new List<string>()
                    };
                    script.Roles.Add(newJinxRole);
                    System.Diagnostics.Debug.WriteLine($"✅ 加入集石相剋規則: {jinxName}");
                }
            }

            // 3. 雙向同步所有角色的 Jinxes
            foreach (var pair in jinxPairs)
            {
                // 確保雙方都有對方的 Jinx
                var role1 = script.Roles.FirstOrDefault(r => r.Id == pair.id1 && r.Team != TeamType.Jinxed);
                var role2 = script.Roles.FirstOrDefault(r => r.Id == pair.id2 && r.Team != TeamType.Jinxed);

                if (role1 != null)
                {
                    role1.Jinxes ??= new List<Role.JinxInfo>();
                    if (!role1.Jinxes.Any(j => j.Id == pair.id2))
                    {
                        role1.Jinxes.Add(new Role.JinxInfo { Id = pair.id2, Reason = pair.reason });
                        System.Diagnostics.Debug.WriteLine($"🔗 {role1.Name} 加入與 {pair.name2} 的相剋");
                    }
                }

                if (role2 != null)
                {
                    role2.Jinxes ??= new List<Role.JinxInfo>();
                    if (!role2.Jinxes.Any(j => j.Id == pair.id1))
                    {
                        role2.Jinxes.Add(new Role.JinxInfo { Id = pair.id1, Reason = pair.reason });
                        System.Diagnostics.Debug.WriteLine($"🔗 {role2.Name} 加入與 {pair.name1} 的相剋");
                    }
                }
            }

            // 4. 清除不存在的 Jinxes
            foreach (var role in script.Roles.Where(r => r.Team != TeamType.Jinxed))
            {
                if (role.Jinxes == null) continue;

                var toRemove = role.Jinxes
                    .Where(j => !jinxPairs.Any(p =>
                        (p.id1 == role.Id && p.id2 == j.Id) ||
                        (p.id2 == role.Id && p.id1 == j.Id)))
                    .ToList();

                foreach (var jinx in toRemove)
                {
                    role.Jinxes.Remove(jinx);
                    var targetName = script.Roles.FirstOrDefault(r => r.Id == jinx.Id)?.Name ?? jinx.Id;
                    System.Diagnostics.Debug.WriteLine($"🗑️ {role.Name} 移除與 {targetName} 的相剋");
                }

                if (role.Jinxes.Count == 0)
                    role.Jinxes = null;
            }
        }

        /// <summary>
        /// 從集石格式同步到所有角色的 BOTC Jinxes
        /// （處理集石格式的編輯）
        /// </summary>
        public static void SyncFromJinxedRoles(Script script)
        {
            // 1. 收集所有集石格式的相剋規則
            var jinxedRoles = script.Roles.Where(r => r.Team == TeamType.Jinxed).ToList();

            // 2. 清空所有角色的 Jinxes（準備重建）
            foreach (var role in script.Roles.Where(r => r.Team != TeamType.Jinxed))
            {
                role.Jinxes = null;
            }

            // 3. 從集石規則重建 BOTC Jinxes
            foreach (var jinxRole in jinxedRoles)
            {
                // 解析集石規則的名稱 "角色1&角色2"
                var parts = jinxRole.Name.Split('&');
                if (parts.Length != 2) continue;

                string name1 = parts[0].Trim();
                string name2 = parts[1].Trim();

                var role1 = script.Roles.FirstOrDefault(r => r.Name == name1 && r.Team != TeamType.Jinxed);
                var role2 = script.Roles.FirstOrDefault(r => r.Name == name2 && r.Team != TeamType.Jinxed);

                if (role1 == null || role2 == null) continue;

                // 為兩個角色都加入 Jinx
                role1.Jinxes ??= new List<Role.JinxInfo>();
                role1.Jinxes.Add(new Role.JinxInfo { Id = role2.Id, Reason = jinxRole.Ability });

                role2.Jinxes ??= new List<Role.JinxInfo>();
                role2.Jinxes.Add(new Role.JinxInfo { Id = role1.Id, Reason = jinxRole.Ability });

                System.Diagnostics.Debug.WriteLine($"🔗 從集石規則建立: {name1} ↔ {name2}");
            }
        }
    }
}