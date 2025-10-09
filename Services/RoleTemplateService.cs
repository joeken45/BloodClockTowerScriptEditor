using BloodClockTowerScriptEditor.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BloodClockTowerScriptEditor.Services
{
    /// <summary>
    /// 角色範本服務 - Phase 2.3 將完整實作
    /// 用於載入和管理官方/自訂角色範本
    /// </summary>
    public class RoleTemplateService
    {
        private List<Role> _templates = new();

        /// <summary>
        /// 所有可用的角色範本
        /// </summary>
        public IReadOnlyList<Role> Templates => _templates.AsReadOnly();

        /// <summary>
        /// 從 JSON 檔案載入角色範本
        /// </summary>
        /// <param name="filePath">範本 JSON 檔案路徑</param>
        public void LoadTemplates(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"找不到範本檔案: {filePath}");
                }

                string json = File.ReadAllText(filePath);
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };

                var templates = JsonConvert.DeserializeObject<List<Role>>(json, settings);
                if (templates != null)
                {
                    _templates.AddRange(templates);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"載入範本失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 載入多個範本檔案
        /// </summary>
        public void LoadMultipleTemplates(params string[] filePaths)
        {
            foreach (var path in filePaths)
            {
                LoadTemplates(path);
            }
        }

        /// <summary>
        /// 清空所有範本
        /// </summary>
        public void ClearTemplates()
        {
            _templates.Clear();
        }

        /// <summary>
        /// 根據類型篩選範本
        /// </summary>
        public IEnumerable<Role> GetTemplatesByTeam(TeamType team)
        {
            return _templates.Where(r => r.Team == team);
        }

        /// <summary>
        /// 搜尋範本 (根據名稱)
        /// </summary>
        public IEnumerable<Role> SearchTemplates(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return _templates;
            }

            keyword = keyword.ToLower();
            return _templates.Where(r =>
                r.Name.ToLower().Contains(keyword) ||
                (r.NameEng?.ToLower().Contains(keyword) ?? false) ||
                r.Ability.ToLower().Contains(keyword)
            );
        }

        /// <summary>
        /// 取得範本統計
        /// </summary>
        public Dictionary<TeamType, int> GetTemplateStatistics()
        {
            return _templates
                .GroupBy(r => r.Team)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }

    /// <summary>
    /// 角色範本分類
    /// </summary>
    public class RoleTemplateCategory
    {
        public string Name { get; set; } = string.Empty;
        public TeamType Team { get; set; }
        public List<Role> Roles { get; set; } = new();
    }
}