using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Data;
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
    }
}