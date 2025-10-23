using BloodClockTowerScriptEditor.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BloodClockTowerScriptEditor.Services
{
    /// <summary>
    /// 角色範本服務 - 從 SQLite 資料庫讀取
    /// </summary>
    public class RoleTemplateService
    {
        /// <summary>
        /// 取得所有角色範本
        /// </summary>
        public async Task<List<RoleTemplate>> GetAllTemplatesAsync()
        {
            using var context = new RoleTemplateContext();
            return await context.RoleTemplates
                .Include(r => r.Reminders)
                .OrderBy(r => r.Team)
                .ThenBy(r => r.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根據類型取得角色範本
        /// </summary>
        public async Task<List<RoleTemplate>> GetTemplatesByTeamAsync(string team)
        {
            using var context = new RoleTemplateContext();
            return await context.RoleTemplates
                .Include(r => r.Reminders)
                .Where(r => r.Team == team)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 搜尋角色範本
        /// </summary>
        public async Task<List<RoleTemplate>> SearchTemplatesAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return await GetAllTemplatesAsync();
            }

            keyword = keyword.ToLower();

            using var context = new RoleTemplateContext();
            return await context.RoleTemplates
                .Include(r => r.Reminders)
                .Where(r =>
                    r.Name.ToLower().Contains(keyword) ||
                    (r.Ability != null && r.Ability.ToLower().Contains(keyword)))
                .OrderBy(r => r.Team)
                .ThenBy(r => r.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根據 ID 取得角色範本
        /// </summary>
        public async Task<RoleTemplate?> GetTemplateByIdAsync(string id)
        {
            using var context = new RoleTemplateContext();
            return await context.RoleTemplates
                .Include(r => r.Reminders)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        /// <summary>
        /// 取得角色統計資訊
        /// </summary>
        public async Task<Dictionary<string, int>> GetStatisticsAsync()
        {
            using var context = new RoleTemplateContext();
            return await context.RoleTemplates
                .GroupBy(r => r.Team)
                .Select(g => new { Team = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Team, x => x.Count);
        }
    }
}