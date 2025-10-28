using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Services;
using BloodClockTowerScriptEditor.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace BloodClockTowerScriptEditor.ViewModels
{
    /// <summary>
    /// 主視窗視圖模型 - Phase 3.2 夜晚順序編輯器
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        // ==================== 私有欄位 ====================
        private bool _isDirty; // 檔案是否有未儲存的變更
        private readonly JsonService _jsonService; 
        private readonly JinxRuleService _jinxRuleService;
        private Script _currentScript;
        private Role? _selectedRole;
        private string _statusMessage;
        private string _currentFilePath;

        // ==================== 建構函式 ====================
        public MainViewModel()
        {
            _jsonService = new JsonService();
            _jinxRuleService = new JinxRuleService();
            _currentScript = new Script();
            _statusMessage = "就緒";
            _currentFilePath = string.Empty;

            FilteredRoles = new ObservableCollection<Role>();

            // 🆕 初始化夜晚順序集合
            FirstNightRoles = new ObservableCollection<Role>();
            OtherNightRoles = new ObservableCollection<Role>();
        }

        // ==================== 公開屬性 ====================

        /// <summary>
        /// 檔案是否有未儲存的變更
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }

        public Script CurrentScript
        {
            get => _currentScript;
            set
            {
                // 取消訂閱舊腳本的事件
                if (_currentScript != null)
                {
                    _currentScript.Roles.CollectionChanged -= OnRolesCollectionChanged;
                }

                if (SetProperty(ref _currentScript, value))
                {
                    // 訂閱新腳本的事件
                    if (_currentScript != null)
                    {
                        _currentScript.Roles.CollectionChanged += OnRolesCollectionChanged;
                    }

                    UpdateFilteredRoles();
                    UpdateNightOrderLists();
                }
            }
        }

        private void OnRolesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsDirty = true;
        }

        public Role? SelectedRole
        {
            get => _selectedRole;
            set
            {
                // 取消訂閱舊角色的事件
                if (_selectedRole != null)
                {
                    _selectedRole.RemoveEmptyJinxItems();  // ✅ 加入這行

                    _selectedRole.PropertyChanged -= OnRolePropertyChanged;
                    _selectedRole.TeamChanged -= OnRoleTeamChanged;
                    _selectedRole.NightOrderChanged -= OnRoleNightOrderChanged;
                }

                if (SetProperty(ref _selectedRole, value))
                {
                    // 訂閱新角色的事件
                    if (_selectedRole != null)
                    {
                        _selectedRole.PropertyChanged += OnRolePropertyChanged;
                        _selectedRole.TeamChanged += OnRoleTeamChanged;
                        _selectedRole.NightOrderChanged += OnRoleNightOrderChanged;

                        _lastRoleId = _selectedRole.Id;
                    }

                    // 🆕 通知相剋角色選項更新
                    OnPropertyChanged(nameof(AvailableRolesForJinx1));
                    OnPropertyChanged(nameof(AvailableRolesForJinx2));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string CurrentFilePath
        {
            get => _currentFilePath;
            set => SetProperty(ref _currentFilePath, value);
        }

        public ObservableCollection<Role> FilteredRoles { get; }

        /// <summary>
        /// 鎮民角色集合
        /// </summary>
        public ObservableCollection<Role> TownsfolkRoles { get; } = new();

        /// <summary>
        /// 外來者角色集合
        /// </summary>
        public ObservableCollection<Role> OutsiderRoles { get; } = new();

        /// <summary>
        /// 爪牙角色集合
        /// </summary>
        public ObservableCollection<Role> MinionRoles { get; } = new();

        /// <summary>
        /// 惡魔角色集合
        /// </summary>
        public ObservableCollection<Role> DemonRoles { get; } = new();

        /// <summary>
        /// 旅行者角色集合
        /// </summary>
        public ObservableCollection<Role> TravelerRoles { get; } = new();

        /// <summary>
        /// 傳奇角色集合
        /// </summary>
        public ObservableCollection<Role> FabledRoles { get; } = new();

        /// <summary>
        /// 相剋角色集合
        /// </summary>
        public ObservableCollection<Role> JinxedRoles { get; } = new();

        // ==================== 各類型數量屬性 ====================

        /// <summary>
        /// 鎮民數量
        /// </summary>
        public int TownsfolkCount => TownsfolkRoles.Count;

        /// <summary>
        /// 外來者數量
        /// </summary>
        public int OutsidersCount => OutsiderRoles.Count;

        /// <summary>
        /// 爪牙數量
        /// </summary>
        public int MinionsCount => MinionRoles.Count;

        /// <summary>
        /// 惡魔數量
        /// </summary>
        public int DemonsCount => DemonRoles.Count;

        /// <summary>
        /// 旅行者數量
        /// </summary>
        public int TravelersCount => TravelerRoles.Count;

        /// <summary>
        /// 傳奇數量
        /// </summary>
        public int FabledCount => FabledRoles.Count;

        /// <summary>
        /// 相剋數量
        /// </summary>
        public int JinxedCount => JinxedRoles.Count;

        // 🆕 夜晚順序集合
        public ObservableCollection<Role> FirstNightRoles { get; }
        public ObservableCollection<Role> OtherNightRoles { get; }

        // 篩選條件
        public bool ShowTownsfolk
        {
            get => GetTeamFilter(TeamType.Townsfolk);
            set => SetTeamFilter(TeamType.Townsfolk, value);
        }

        public bool ShowOutsiders
        {
            get => GetTeamFilter(TeamType.Outsider);
            set => SetTeamFilter(TeamType.Outsider, value);
        }

        public bool ShowMinions
        {
            get => GetTeamFilter(TeamType.Minion);
            set => SetTeamFilter(TeamType.Minion, value);
        }

        public bool ShowDemons
        {
            get => GetTeamFilter(TeamType.Demon);
            set => SetTeamFilter(TeamType.Demon, value);
        }

        public bool ShowTravelers
        {
            get => GetTeamFilter(TeamType.Traveler);
            set => SetTeamFilter(TeamType.Traveler, value);
        }

        public bool ShowFabled
        {
            get => GetTeamFilter(TeamType.Fabled);
            set => SetTeamFilter(TeamType.Fabled, value);
        }

        public bool ShowJinxed
        {
            get => GetTeamFilter(TeamType.Jinxed);
            set => SetTeamFilter(TeamType.Jinxed, value);
        }

        // ==================== 命令 ====================

        [RelayCommand]
        private async Task NewFile()
        {
            // 檢查未儲存的變更
            if (!CheckUnsavedChanges())
                return;

            // 建立新劇本
            CurrentScript = new Script();

            // 🆕 自動加入爪牙/惡魔訊息
            await LoadMinionDemonInfoAsync();

            CurrentFilePath = string.Empty;
            SelectedRole = null;
            IsDirty = false;
            StatusMessage = "已建立新檔案";
        }

        [RelayCommand]
        private void LoadJson()
        {
            try
            {
                // 檢查未儲存的變更
                if (!CheckUnsavedChanges())
                    return;

                var dialog = new OpenFileDialog
                {
                    Filter = "JSON 檔案 (*.json)|*.json|所有檔案 (*.*)|*.*",
                    Title = "開啟劇本檔案"
                };

                if (dialog.ShowDialog() == true)
                {
                    CurrentScript = _jsonService.LoadScript(dialog.FileName);
                    CurrentFilePath = dialog.FileName;
                    StatusMessage = $"已載入: {dialog.FileName}";
                    SelectedRole = null;
                    IsDirty = false; // 載入後重置標記
                }
            }
            catch (Exception ex)
            {
                ShowError($"載入失敗: {ex.Message}", "載入失敗");
            }
        }

        [RelayCommand]
        private void SaveJson()
        {
            try
            {
                // ✅ 儲存前驗證
                if (!ValidateScript())
                    return;

                if (string.IsNullOrEmpty(CurrentFilePath))
                {
                    SaveAsJson();
                    return;
                }

                // 🆕 彈出格式選擇對話框
                var formatDialog = new SelectExportFormatDialog
                {
                    Owner = Application.Current.MainWindow
                };

                if (formatDialog.ShowDialog() != true)
                    return;

                _jsonService.SaveScript(CurrentScript, CurrentFilePath, formatDialog.SelectedFormat);
                IsDirty = false; // 儲存後清除標記
                StatusMessage = $"已儲存: {CurrentFilePath}";
                ShowSuccess("儲存成功！");
            }
            catch (Exception ex)
            {
                ShowError($"儲存失敗: {ex.Message}", "儲存失敗");
            }
        }

        [RelayCommand]
        private void SaveAsJson()
        {
            try
            {
                // ✅ 儲存前驗證
                if (!ValidateScript())
                    return;

                // 🆕 彈出格式選擇對話框
                var formatDialog = new SelectExportFormatDialog
                {
                    Owner = Application.Current.MainWindow
                };

                if (formatDialog.ShowDialog() != true)
                    return;

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON 檔案 (*.json)|*.json|所有檔案 (*.*)|*.*",
                    Title = "另存劇本檔案",
                    FileName = "script.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    _jsonService.SaveScript(CurrentScript, dialog.FileName, formatDialog.SelectedFormat);
                    CurrentFilePath = dialog.FileName;
                    IsDirty = false; // 儲存後清除標記
                    StatusMessage = $"已儲存: {dialog.FileName}";
                    ShowSuccess("儲存成功！");
                }
            }
            catch (Exception ex)
            {
                ShowError($"儲存失敗: {ex.Message}", "儲存失敗");
            }
        }

        [RelayCommand]
        private async Task AddFromOfficialTemplate()
        {
            try
            {
                var dialog = new SelectRoleDialog
                {
                    Owner = Application.Current.MainWindow
                };

                bool? result = dialog.ShowDialog();

                if (result == true && dialog.SelectedRoles.Count > 0)
                {
                    // 🆕 檢查重複
                    var existingIds = CurrentScript.Roles
                        .Select(r => r.Id)
                        .ToHashSet();

                    var duplicates = dialog.SelectedRoles
                        .Where(r => existingIds.Contains(r.Id))
                        .Select(r => r.Name)
                        .ToList();

                    var rolesToAdd = dialog.SelectedRoles;

                    if (duplicates.Any())
                    {
                        if (!ShowWarning($"以下角色已存在於劇本中：\n\n{string.Join("\n", duplicates)}\n\n是否仍要加入重複的角色？", "重複角色"))
                        {
                            // 只加入不重複的角色
                            rolesToAdd = dialog.SelectedRoles
                                .Where(r => !existingIds.Contains(r.Id))
                                .ToList();
                        }
                    }

                    // 加入角色
                    foreach (var role in rolesToAdd)
                    {
                        CurrentScript.Roles.Add(role);
                        role.PropertyChanged += OnRolePropertyChanged;
                    }

                    if (rolesToAdd.Count > 0)
                    {
                        UpdateFilteredRoles();
                        UpdateNightOrderLists();
                        IsDirty = true;
                        StatusMessage = $"已新增 {rolesToAdd.Count} 個角色";
                    }
                    else
                    {
                        StatusMessage = "未新增任何角色";
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"新增角色失敗：{ex.Message}", "新增角色失敗");
            }

            // ✅ 加這一行消除警告
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task DetectJinx()
        {
            try
            {
                StatusMessage = "正在偵測相剋規則...";

                // 1. 從資料庫偵測所有可能的相剋規則
                var detectedRules = await _jinxRuleService.DetectJinxRulesAsync(CurrentScript);

                if (detectedRules.Count == 0)
                {
                    ShowInfo("未偵測到任何相剋規則。\n\n當前劇本中的角色沒有資料庫中定義的相剋關係。");
                    StatusMessage = "未偵測到相剋規則";
                    return;
                }

                // 2. 建立顯示用的 JinxRuleItem 列表
                var jinxItems = new ObservableCollection<JinxRuleItem>();

                foreach (var rule in detectedRules)
                {
                    // 從資料庫規則取得角色名稱
                    string name1 = rule.Character1;
                    string name2 = rule.Character2;

                    var role1 = CurrentScript.Roles.FirstOrDefault(r =>
                        r.Name == name1 && r.Team != TeamType.Jinxed);
                    var role2 = CurrentScript.Roles.FirstOrDefault(r =>
                        r.Name == name2 && r.Team != TeamType.Jinxed);

                    if (role1 == null || role2 == null) continue;

                    // 檢查是否已存在（集石格式已存在）
                    bool alreadyExists = CurrentScript.Roles.Any(r => r.Id == rule.Id);

                    jinxItems.Add(new JinxRuleItem
                    {
                        RuleId = rule.Id,
                        Role1Id = role1.Id,
                        Role1Name = role1.Name,
                        Role2Id = role2.Id,
                        Role2Name = role2.Name,
                        Reason = rule.Ability ?? "",
                        IsEnabled = !alreadyExists,
                        IsSelected = false
                    });
                }

                // 3. 顯示選擇視窗
                var dialog = new Views.SelectJinxRulesDialog(jinxItems)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    var selectedRules = dialog.SelectedRules;

                    if (selectedRules.Count > 0)
                    {
                        // 4. ✅ 將選中的 JinxRule 轉換為 Role 並加入劇本
                        foreach (var item in selectedRules)
                        {
                            // 從資料庫規則找到對應的 JinxRule
                            var rule = detectedRules.FirstOrDefault(r => r.Id == item.RuleId);
                            if (rule != null)
                            {
                                // ✅ 將 JinxRule 轉換為 Role（集石格式）
                                var jinxRole = new Role
                                {
                                    Id = rule.Id,
                                    Name = rule.Name,
                                    Team = TeamType.Jinxed,
                                    Ability = rule.Ability ?? ""
                                };

                                if (!string.IsNullOrEmpty(rule.Image))
                                {
                                    jinxRole.Image = new List<string> { rule.Image };
                                }

                                CurrentScript.Roles.Add(jinxRole);
                                System.Diagnostics.Debug.WriteLine($"✅ 加入相剋規則: {jinxRole.Name}");
                            }
                        }

                        // 5. 同步 BOTC Jinxes
                        JinxSyncHelper.SyncFromAllBotcJinxes(CurrentScript);
                        await JinxSyncHelper.SyncAllRoleJinxesAsync(CurrentScript);

                        UpdateFilteredRoles();
                        IsDirty = true;
                        StatusMessage = $"已加入 {selectedRules.Count} 個相剋規則";

                        ShowInfo($"成功加入 {selectedRules.Count} 個相剋規則");
                    }
                }
                else
                {
                    StatusMessage = "取消偵測相剋規則";
                }
            }
            catch (Exception ex)
            {
                ShowError($"偵測相剋規則失敗：{ex.Message}", "偵測失敗");
            }
        }

        [RelayCommand]
        private async Task RemoveRole()
        {
            if (SelectedRole == null)
            {
                ShowInfo("請先選擇要刪除的角色");
                return;
            }

            if (ShowConfirm($"確定要刪除角色「{SelectedRole.Name}」嗎？", "確認刪除"))
            {
                // ✅ 如果刪除的是集石格式，移除雙向 BOTC Jinxes
                if (SelectedRole.Team == TeamType.Jinxed)
                {
                    var parts = SelectedRole.Name.Split('&');
                    if (parts.Length == 2)
                    {
                        string name1 = parts[0].Trim();
                        string name2 = parts[1].Trim();

                        var role1 = CurrentScript.Roles
                            .FirstOrDefault(r => r.Name == name1 && r.Team != TeamType.Jinxed);
                        var role2 = CurrentScript.Roles
                            .FirstOrDefault(r => r.Name == name2 && r.Team != TeamType.Jinxed);

                        if (role1 != null && role2 != null)
                        {
                            // 移除雙向 Jinx
                            RemoveBidirectionalJinx(role1, role2);
                        }
                    }
                }

                CurrentScript.Roles.Remove(SelectedRole);
                SelectedRole = null;

                // ✅ 只清理失效的集石格式（角色已不存在）
                CleanupInvalidJinxRules();

                UpdateFilteredRoles();
                UpdateNightOrderLists();
                IsDirty = true;
                StatusMessage = "角色已刪除";
            }

            // ✅ 加這一行消除警告
            await Task.CompletedTask;
        }

        [RelayCommand]
        private void EditScriptMeta()
        {
            try
            {
                var dialog = new Views.EditScriptMetaWindow(CurrentScript.Meta);
                if (dialog.ShowDialog() == true)
                {
                    OnPropertyChanged(nameof(CurrentScript));
                    IsDirty = true; // 加上這行
                    StatusMessage = "劇本資訊已更新";
                }
            }
            catch (Exception ex)
            {
                ShowError($"編輯劇本資訊失敗: {ex.Message}", "編輯失敗");
            }
        }

        // ==================== 驗證方法 ====================

        /// <summary>
        /// 驗證單一角色
        /// </summary>
        private bool ValidateRole(Role role)
        {
            var errors = new System.Collections.Generic.List<string>();

            if (string.IsNullOrWhiteSpace(role.Id))
                errors.Add("• 角色 ID 為必填");

            if (string.IsNullOrWhiteSpace(role.Name))
                errors.Add("• 角色名稱為必填");

            if (errors.Any())
            {
                ShowInfo($"請先完成當前角色的必填欄位：\n\n{string.Join("\n", errors)}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 驗證整個劇本
        /// </summary>
        private bool ValidateScript()
        {
            var errors = new System.Collections.Generic.List<string>();

            // 劇本檢查
            if (string.IsNullOrWhiteSpace(CurrentScript.Meta.Name))
                errors.Add("• 劇本名稱為必填");

            // 所有角色檢查
            foreach (var role in CurrentScript.Roles)
            {
                if (string.IsNullOrWhiteSpace(role.Id))
                    errors.Add($"• 角色「{role.Name ?? "(未命名)"}」缺少 ID");

                if (string.IsNullOrWhiteSpace(role.Name))
                    errors.Add("• 發現未命名的角色");
            }

            // 唯一性檢查
            var duplicateIds = CurrentScript.Roles
                .GroupBy(r => r.Id)
                .Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key))
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Any())
            {
                errors.Add($"• 重複的角色 ID：{string.Join(", ", duplicateIds)}");
            }

            // 必填欄位和唯一性錯誤：直接阻止儲存
            if (errors.Any())
            {
                ShowError(string.Join("\n", errors), "無法儲存");
                return false;
            }

            // ✅ 夜晚順序衝突檢查：警告但允許繼續
            var conflicts = CheckNightOrderConflicts();
            if (conflicts.Any())
            {
                var message = "偵測到夜晚順序衝突：\n\n" + string.Join("\n", conflicts) + "\n\n是否仍要儲存？";
                return ShowConfirm(message, "夜晚順序衝突");
            }

            return true;
        }

        /// <summary>
        /// 檢查夜晚順序衝突
        /// </summary>
        private List<string> CheckNightOrderConflicts()
        {
            var conflicts = new List<string>();

            // 檢查首夜順序衝突
            var firstNightGroups = CurrentScript.Roles
                .Where(r => r.FirstNight > 0)
                .GroupBy(r => r.FirstNight)
                .Where(g => g.Count() > 1);

            foreach (var group in firstNightGroups)
            {
                var roleNames = string.Join("、", group.Select(r => r.Name));
                conflicts.Add($"⚠️ 首夜順序 {group.Key}：{roleNames}");
            }

            // 檢查其他夜順序衝突
            var otherNightGroups = CurrentScript.Roles
                .Where(r => r.OtherNight > 0)
                .GroupBy(r => r.OtherNight)
                .Where(g => g.Count() > 1);

            foreach (var group in otherNightGroups)
            {
                var roleNames = string.Join("、", group.Select(r => r.Name));
                conflicts.Add($"⚠️ 其他夜順序 {group.Key}：{roleNames}");
            }

            return conflicts;
        }
        // ==================== 私有方法 ====================

        /// <summary>
        /// 篩選條件字典 - 管理各陣營的顯示/隱藏狀態
        /// </summary>
        private readonly Dictionary<TeamType, bool> _teamFilters = new()
{
    { TeamType.Townsfolk, true },
    { TeamType.Outsider, true },
    { TeamType.Minion, true },
    { TeamType.Demon, true },
    { TeamType.Traveler, true },
    { TeamType.Fabled, true },
    { TeamType.Jinxed, true }
};

        /// <summary>
        /// 取得陣營篩選狀態
        /// </summary>
        private bool GetTeamFilter(TeamType team) => _teamFilters[team];

        /// <summary>
        /// 設定陣營篩選狀態
        /// </summary>
        private void SetTeamFilter(TeamType team, bool value)
        {
            if (_teamFilters[team] != value)
            {
                _teamFilters[team] = value;
                UpdateFilteredRoles();
                OnPropertyChanged($"Show{team}");
            }
        }

        public void UpdateFilteredRoles()
        {
            // 清空所有集合
            TownsfolkRoles.Clear();
            OutsiderRoles.Clear();
            MinionRoles.Clear();
            DemonRoles.Clear();
            TravelerRoles.Clear();
            FabledRoles.Clear();
            JinxedRoles.Clear();

            // 按 Team 和 DisplayOrder 排序後分類
            var sortedRoles = CurrentScript.Roles
                .OrderBy(r => r.Team)
                .ThenBy(r => r.DisplayOrder)
                .ToList();

            foreach (var role in sortedRoles)
            {
                // 訂閱角色的類型變更事件
                role.TeamChanged -= OnRoleTeamChanged;
                role.TeamChanged += OnRoleTeamChanged;

                // 訂閱角色的夜晚順序變更事件
                role.NightOrderChanged -= OnRoleNightOrderChanged;
                role.NightOrderChanged += OnRoleNightOrderChanged;

                // 訂閱角色的屬性變更事件 (追蹤 IsDirty)
                role.PropertyChanged -= OnRolePropertyChanged;
                role.PropertyChanged += OnRolePropertyChanged;

                // 根據 Team 分類
                switch (role.Team)
                {
                    case TeamType.Townsfolk:
                        TownsfolkRoles.Add(role);
                        break;
                    case TeamType.Outsider:
                        OutsiderRoles.Add(role);
                        break;
                    case TeamType.Minion:
                        MinionRoles.Add(role);
                        break;
                    case TeamType.Demon:
                        DemonRoles.Add(role);
                        break;
                    case TeamType.Traveler:
                        TravelerRoles.Add(role);
                        break;
                    case TeamType.Fabled:
                        FabledRoles.Add(role);
                        break;
                    case TeamType.Jinxed:
                        JinxedRoles.Add(role);
                        break;
                }
            }

            // 通知所有數量屬性更新
            OnPropertyChanged(nameof(TownsfolkCount));
            OnPropertyChanged(nameof(OutsidersCount));
            OnPropertyChanged(nameof(MinionsCount));
            OnPropertyChanged(nameof(DemonsCount));
            OnPropertyChanged(nameof(TravelersCount));
            OnPropertyChanged(nameof(FabledCount));
            OnPropertyChanged(nameof(JinxedCount));
        }

        // 🆕 當角色類型改變時重新篩選和排序
        private void OnRoleTeamChanged(object? sender, EventArgs e)
        {
            UpdateFilteredRoles();
            UpdateNightOrderLists(); // 同時更新夜晚順序
        }

        // 當角色夜晚順序改變時重新排序
        private void OnRoleNightOrderChanged(object? sender, EventArgs e)
        {
            UpdateNightOrderLists();
        }

        /// <summary>
        /// 更新夜晚順序列表
        /// </summary>
        private void UpdateNightOrderLists()
        {
            // 🆕 保存當前選中的角色
            var currentSelected = SelectedRole;

            // 清空現有列表
            FirstNightRoles.Clear();
            OtherNightRoles.Clear();

            // 篩選並排序首個夜晚角色
            var firstNight = CurrentScript.Roles
                .Where(r => r.FirstNight > 0)
                .OrderBy(r => r.FirstNight)
                .ThenBy(r => r.Name);

            foreach (var role in firstNight)
            {
                FirstNightRoles.Add(role);
            }

            // 篩選並排序其他夜晚角色
            var otherNight = CurrentScript.Roles
                .Where(r => r.OtherNight > 0)
                .OrderBy(r => r.OtherNight)
                .ThenBy(r => r.Name);

            foreach (var role in otherNight)
            {
                OtherNightRoles.Add(role);
            }

            // 🆕 恢復選中的角色 (如果還在列表中)
            if (currentSelected != null)
            {
                SelectedRole = currentSelected;
            }
        }

        /// <summary>
        /// 上移角色在夜晚順序中的位置
        /// </summary>
        public void MoveRoleUp(Role role, bool isFirstNight)
        {
            var list = isFirstNight ? FirstNightRoles : OtherNightRoles;
            var index = list.IndexOf(role);

            if (index <= 0) return; // 已在頂部

            double newOrder;

            if (index == 1)
            {
                // 第二個 → 第一個 - 0.001
                var first = list[0];
                var firstOrder = isFirstNight ? first.FirstNight : first.OtherNight;
                newOrder = firstOrder - 0.001;
            }
            else
            {
                // 其他 → 上上個 + 0.001
                var target = list[index - 2];
                var targetOrder = isFirstNight ? target.FirstNight : target.OtherNight;
                newOrder = targetOrder + 0.001;
            }

            if (isFirstNight)
                role.FirstNight = Math.Round(newOrder, 3); // 保留三位小數
            else
                role.OtherNight = Math.Round(newOrder, 3);
        }

        /// <summary>
        /// 下移角色在夜晚順序中的位置
        /// </summary>
        public void MoveRoleDown(Role role, bool isFirstNight)
        {
            var list = isFirstNight ? FirstNightRoles : OtherNightRoles;
            var index = list.IndexOf(role);

            if (index >= list.Count - 1) return; // 已在底部

            var below = list[index + 1];
            var belowOrder = isFirstNight ? below.FirstNight : below.OtherNight;

            // 下移 = 下一個 + 0.001
            var newOrder = belowOrder + 0.001;

            if (isFirstNight)
                role.FirstNight = Math.Round(newOrder, 3); // 保留三位小數
            else
                role.OtherNight = Math.Round(newOrder, 3);
        }

        private string _lastRoleId = string.Empty;  // 新增欄位記錄上次的 ID

        private void OnRolePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 任何角色屬性變更都標記為需要儲存
            IsDirty = true;
        }

        /// <summary>
        /// 通知相剋角色選項列表更新（供 UI 呼叫）
        /// </summary>
        public void NotifyJinxRolesListChanged()
        {
            OnPropertyChanged(nameof(AvailableRolesForJinx1));
            OnPropertyChanged(nameof(AvailableRolesForJinx2));
        }

        /// <summary>
        /// 供 Jinx ComboBox 綁定使用的角色名稱列表
        /// </summary>
        public List<string> AvailableRoleNamesForJinx
        {
            get
            {
                if (SelectedRole == null) return new List<string>();

                return CurrentScript.Roles
                    .Where(r => r.Name != SelectedRole.Name &&      // 排除自己
                               r.Team != TeamType.Jinxed)           // 排除相剋物件
                    .Select(r => r.Name)
                    .OrderBy(r => r)
                    .ToList();
            }
        }

        /// <summary>
        /// 相剋角色 1 的可選角色列表（排除相剋物件和角色 2）
        /// </summary>
        public List<string> AvailableRolesForJinx1
        {
            get
            {
                // 🔴 只有在選擇相剋角色時才計算
                if (SelectedRole == null || SelectedRole.Team != TeamType.Jinxed)
                    return new List<string>();

                // 🔴 使用 _name 直接讀取，避免觸發 PropertyChanged
                var role2Name = SelectedRole.JinxRole2Name;

                return CurrentScript.Roles
                    .Where(r => r.Team != TeamType.Jinxed &&                    // 排除相剋物件
                               !string.IsNullOrEmpty(r.Name) &&                 // 排除空名稱
                               r.Name != role2Name)                             // 排除角色2
                    .Select(r => r.Name)
                    .OrderBy(r => r)
                    .ToList();
            }
        }

        /// <summary>
        /// 相剋角色 2 的可選角色列表（排除相剋物件和角色 1）
        /// </summary>
        public List<string> AvailableRolesForJinx2
        {
            get
            {
                // 🔴 只有在選擇相剋角色時才計算
                if (SelectedRole == null || SelectedRole.Team != TeamType.Jinxed)
                    return new List<string>();

                // 🔴 使用 _name 直接讀取，避免觸發 PropertyChanged
                var role1Name = SelectedRole.JinxRole1Name;

                return CurrentScript.Roles
                    .Where(r => r.Team != TeamType.Jinxed &&                    // 排除相剋物件
                               !string.IsNullOrEmpty(r.Name) &&                 // 排除空名稱
                               r.Name != role1Name)                             // 排除角色1
                    .Select(r => r.Name)
                    .OrderBy(r => r)
                    .ToList();
            }
        }

        /// <summary>
        /// 移除雙向 Jinx 的 Helper 方法
        /// </summary>
        private void RemoveBidirectionalJinx(Role role1, Role role2)
        {
            // 移除 role1 → role2
            if (role1.Jinxes != null)
            {
                var toRemove = role1.Jinxes.FirstOrDefault(j => j.Id == role2.Id);
                if (toRemove != null)
                {
                    role1.Jinxes.Remove(toRemove);
                    if (role1.Jinxes.Count == 0) role1.Jinxes = null;
                }
            }
            if (role1.IsJinxItemsInitialized)
            {
                role1.RemoveJinxItem(role2.Id);
            }

            // 移除 role2 → role1
            if (role2.Jinxes != null)
            {
                var toRemove = role2.Jinxes.FirstOrDefault(j => j.Id == role1.Id);
                if (toRemove != null)
                {
                    role2.Jinxes.Remove(toRemove);
                    if (role2.Jinxes.Count == 0) role2.Jinxes = null;
                }
            }
            if (role2.IsJinxItemsInitialized)
            {
                role2.RemoveJinxItem(role1.Id);
            }

            System.Diagnostics.Debug.WriteLine($"✅ 已移除雙向 Jinx: {role1.Name} ↔ {role2.Name}");
        }

        /// <summary>
        /// 清理失效的集石格式
        /// </summary>
        private void CleanupInvalidJinxRules()  // ✅ 移除 async Task，改為 void
        {
            var jinxedRoles = CurrentScript.Roles.Where(r => r.Team == TeamType.Jinxed).ToList();

            foreach (var jinxRole in jinxedRoles)
            {
                var parts = jinxRole.Name.Split('&');
                if (parts.Length != 2) continue;

                bool role1Exists = CurrentScript.Roles.Any(r =>
                    r.Name == parts[0].Trim() && r.Team != TeamType.Jinxed);
                bool role2Exists = CurrentScript.Roles.Any(r =>
                    r.Name == parts[1].Trim() && r.Team != TeamType.Jinxed);

                // 如果任一角色不存在，移除集石格式
                if (!role1Exists || !role2Exists)
                {
                    CurrentScript.Roles.Remove(jinxRole);
                    System.Diagnostics.Debug.WriteLine($"🗑️ 清理失效的相剋規則: {jinxRole.Name}");
                }
            }

        }


        /// <summary>
        /// 檢查是否有未儲存的變更,詢問使用者是否儲存
        /// </summary>
        /// <returns>true: 繼續操作, false: 取消操作</returns>
        public bool CheckUnsavedChanges()
        {
            if (!IsDirty)
                return true;

            var result = MessageBox.Show(
                "檔案尚未儲存,是否要儲存變更?",
                "未儲存的變更",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question
            );

            switch (result)
            {
                case MessageBoxResult.Yes:
                    // 儲存檔案
                    if (string.IsNullOrEmpty(CurrentFilePath))
                    {
                        SaveAsJson();
                    }
                    else
                    {
                        SaveJson();
                    }
                    return true;

                case MessageBoxResult.No:
                    // 不儲存,繼續操作
                    return true;

                case MessageBoxResult.Cancel:
                    // 取消操作
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 載入爪牙/惡魔訊息（私有，內部使用）
        /// </summary>
        public async Task LoadMinionDemonInfoAsync() 
        {
            try
            {
                using var context = new RoleTemplateContext();

                var minionInfo = await context.RoleTemplates
                    .Include(r => r.Reminders)
                    .FirstOrDefaultAsync(r => r.Id == "M");

                var demonInfo = await context.RoleTemplates
                    .Include(r => r.Reminders)
                    .FirstOrDefaultAsync(r => r.Id == "D");

                if (minionInfo != null)
                {
                    CurrentScript.Roles.Add(minionInfo.ToRole());
                }

                if (demonInfo != null)
                {
                    CurrentScript.Roles.Add(demonInfo.ToRole());
                }

                UpdateFilteredRoles();
                UpdateNightOrderLists();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 載入爪牙/惡魔訊息失敗：{ex.Message}");
            }
        }

        #region MessageBox 輔助方法

        /// <summary>
        /// 顯示錯誤訊息
        /// </summary>
        private void ShowError(string message, string title = "錯誤")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = title;
        }

        /// <summary>
        /// 顯示成功訊息
        /// </summary>
        private void ShowSuccess(string message, string title = "成功")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            StatusMessage = title;
        }

        /// <summary>
        /// 顯示提示訊息
        /// </summary>
        private void ShowInfo(string message, string title = "提示")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 顯示確認對話框
        /// </summary>
        private bool ShowConfirm(string message, string title = "確認")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes;
        }

        /// <summary>
        /// 顯示警告對話框
        /// </summary>
        private bool ShowWarning(string message, string title = "警告")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
                == MessageBoxResult.Yes;
        }

        #endregion
    }
}