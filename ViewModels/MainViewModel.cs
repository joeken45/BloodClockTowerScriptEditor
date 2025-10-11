using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace BloodClockTowerScriptEditor.ViewModels
{
    /// <summary>
    /// 主視窗視圖模型 - Phase 2 修正版
    /// 修正：AddCustomRoleCommand 現在可以正常運作
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        // ==================== 私有欄位 ====================
        private readonly JsonService _jsonService;
        private Script _currentScript;
        private Role? _selectedRole;
        private string _statusMessage;
        private string _currentFilePath;
        private bool _showTownsfolk;
        private bool _showOutsiders;
        private bool _showMinions;
        private bool _showDemons;
        private bool _showTravelers;
        private bool _showFabled;

        // ==================== 建構函式 ====================
        public MainViewModel()
        {
            _jsonService = new JsonService();
            _currentScript = new Script();
            _statusMessage = "就緒";
            _currentFilePath = string.Empty;

            // 預設全部顯示
            _showTownsfolk = true;
            _showOutsiders = true;
            _showMinions = true;
            _showDemons = true;
            _showTravelers = true;
            _showFabled = true;

            FilteredRoles = new ObservableCollection<Role>();
        }

        // ==================== 公開屬性 ====================

        public Script CurrentScript
        {
            get => _currentScript;
            set
            {
                if (SetProperty(ref _currentScript, value))
                {
                    UpdateFilteredRoles();
                }
            }
        }

        public Role? SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value))
                {
                    // 當選擇變更時，更新狀態列
                    if (value != null)
                    {
                        StatusMessage = $"正在編輯: {value.Name}";
                    }
                    else
                    {
                        StatusMessage = "就緒";
                    }
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

        // 篩選條件
        public bool ShowTownsfolk
        {
            get => _showTownsfolk;
            set
            {
                if (SetProperty(ref _showTownsfolk, value))
                {
                    UpdateFilteredRoles();
                }
            }
        }

        public bool ShowOutsiders
        {
            get => _showOutsiders;
            set
            {
                if (SetProperty(ref _showOutsiders, value))
                {
                    UpdateFilteredRoles();
                }
            }
        }

        public bool ShowMinions
        {
            get => _showMinions;
            set
            {
                if (SetProperty(ref _showMinions, value))
                {
                    UpdateFilteredRoles();
                }
            }
        }

        public bool ShowDemons
        {
            get => _showDemons;
            set
            {
                if (SetProperty(ref _showDemons, value))
                {
                    UpdateFilteredRoles();
                }
            }
        }

        public bool ShowTravelers
        {
            get => _showTravelers;
            set
            {
                if (SetProperty(ref _showTravelers, value))
                {
                    UpdateFilteredRoles();
                }
            }
        }

        public bool ShowFabled
        {
            get => _showFabled;
            set
            {
                if (SetProperty(ref _showFabled, value))
                {
                    UpdateFilteredRoles();
                }
            }
        }

        // 篩選後的角色列表
        public ObservableCollection<Role> FilteredRoles { get; }

        // ==================== 檔案操作命令 ====================

        [RelayCommand]
        private void LoadJson()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON 檔案 (*.json)|*.json|所有檔案 (*.*)|*.*",
                    Title = "選擇劇本檔案"
                };

                if (dialog.ShowDialog() == true)
                {
                    CurrentScript = _jsonService.LoadScript(dialog.FileName);
                    CurrentFilePath = dialog.FileName;
                    UpdateFilteredRoles();
                    StatusMessage = $"已載入: {CurrentScript.Meta.Name} (共 {CurrentScript.TotalRoleCount} 個角色)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入失敗:\n{ex.Message}", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "載入失敗";
            }
        }

        [RelayCommand]
        private void SaveJson()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilePath))
                {
                    SaveAsJson();
                    return;
                }

                _jsonService.SaveScript(CurrentScript, CurrentFilePath);
                StatusMessage = $"已儲存: {CurrentScript.Meta.Name}";
                MessageBox.Show("儲存成功!", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存失敗:\n{ex.Message}", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "儲存失敗";
            }
        }

        [RelayCommand]
        private void SaveAsJson()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON 檔案 (*.json)|*.json",
                    Title = "儲存劇本",
                    FileName = CurrentScript.Meta.Name + ".json"
                };

                if (dialog.ShowDialog() == true)
                {
                    _jsonService.SaveScript(CurrentScript, dialog.FileName);
                    CurrentFilePath = dialog.FileName;
                    StatusMessage = $"已儲存: {CurrentScript.Meta.Name}";
                    MessageBox.Show("儲存成功!", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存失敗:\n{ex.Message}", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "儲存失敗";
            }
        }

        // ==================== 角色編輯命令 (Phase 2 新增) ====================

        /// <summary>
        /// 新增自訂空白角色 (直接綁定到選單)
        /// </summary>
        [RelayCommand]
        private void AddCustomRole()
        {
            try
            {
                // 生成新的唯一 ID
                string newId = GenerateUniqueId();

                // 建立新角色
                var newRole = new Role
                {
                    Id = newId,
                    Name = "新角色",
                    Team = TeamType.Townsfolk,
                    Ability = "請輸入能力描述...",
                    Image = "https://",
                    Edition = "custom",
                    FirstNight = 0,
                    OtherNight = 0,
                    Setup = false,
                    Reminders = new System.Collections.Generic.List<string>(),
                    RemindersGlobal = new System.Collections.Generic.List<string>()
                };

                // 加入劇本
                CurrentScript.Roles.Add(newRole);
                UpdateFilteredRoles();

                // 自動選中新角色
                SelectedRole = newRole;

                StatusMessage = $"已新增自訂角色: {newRole.Name}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增角色失敗:\n{ex.Message}", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 從官方範本新增角色 (未實作)
        /// </summary>
        [RelayCommand]
        private void AddFromOfficialTemplate()
        {
            // TODO: Phase 2.4 實作官方角色範本選擇
            MessageBox.Show("官方角色範本功能開發中...", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 從自訂範本新增角色 (未實作)
        /// </summary>
        [RelayCommand]
        private void AddFromCustomTemplate()
        {
            // TODO: 未來功能 - 從自訂範本新增
            MessageBox.Show("自訂範本功能開發中...", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 刪除角色
        /// </summary>
        [RelayCommand]
        private void DeleteRole(object? parameter = null)
        {
            // 如果有傳入參數，使用參數；否則使用 SelectedRole
            var roleToDelete = parameter as Role ?? SelectedRole;

            if (roleToDelete == null)
            {
                MessageBox.Show("請先選擇要刪除的角色", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    $"確定要刪除角色「{roleToDelete.Name}」嗎？\n此操作無法復原。",
                    "確認刪除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    string deletedName = roleToDelete.Name;
                    CurrentScript.Roles.Remove(roleToDelete);
                    UpdateFilteredRoles();

                    // 如果刪除的是當前選中的角色，清空選擇
                    if (SelectedRole == roleToDelete)
                    {
                        SelectedRole = null;
                    }

                    StatusMessage = $"已刪除角色: {deletedName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刪除角色失敗:\n{ex.Message}", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 複製角色
        /// </summary>
        [RelayCommand]
        private void DuplicateRole(object? parameter = null)
        {
            // 如果有傳入參數，使用參數；否則使用 SelectedRole
            var roleToDuplicate = parameter as Role ?? SelectedRole;

            if (roleToDuplicate == null)
            {
                MessageBox.Show("請先選擇要複製的角色", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 建立副本
                var duplicatedRole = new Role
                {
                    Id = GenerateUniqueId(),
                    Name = roleToDuplicate.Name + " (副本)",
                    Team = roleToDuplicate.Team,
                    Ability = roleToDuplicate.Ability,
                    Image = roleToDuplicate.Image,
                    Edition = roleToDuplicate.Edition,
                    FirstNight = roleToDuplicate.FirstNight,
                    OtherNight = roleToDuplicate.OtherNight,
                    Setup = roleToDuplicate.Setup,
                    Reminders = new System.Collections.Generic.List<string>(roleToDuplicate.Reminders),
                    RemindersGlobal = new System.Collections.Generic.List<string>(roleToDuplicate.RemindersGlobal),
                    NameEng = roleToDuplicate.NameEng,
                    Flavor = roleToDuplicate.Flavor,
                    FirstNightReminder = roleToDuplicate.FirstNightReminder,
                    OtherNightReminder = roleToDuplicate.OtherNightReminder
                };

                // 加入劇本
                CurrentScript.Roles.Add(duplicatedRole);
                UpdateFilteredRoles();

                // 自動選中新角色
                SelectedRole = duplicatedRole;

                StatusMessage = $"已複製角色: {duplicatedRole.Name}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"複製角色失敗:\n{ex.Message}", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 更新篩選後的角色列表
        /// </summary>
        private void UpdateFilteredRoles()
        {
            FilteredRoles.Clear();

            foreach (var role in CurrentScript.Roles)
            {
                bool shouldShow = role.Team switch
                {
                    TeamType.Townsfolk => ShowTownsfolk,
                    TeamType.Outsider => ShowOutsiders,
                    TeamType.Minion => ShowMinions,
                    TeamType.Demon => ShowDemons,
                    TeamType.Traveler => ShowTravelers,
                    TeamType.Fabled => ShowFabled,
                    TeamType.Jinxed => ShowFabled, // 相剋規則也歸類到傳奇
                    _ => true
                };

                if (shouldShow)
                {
                    FilteredRoles.Add(role);
                }
            }
        }

        /// <summary>
        /// 生成唯一 ID
        /// </summary>
        private string GenerateUniqueId()
        {
            string baseId = "role_";
            int counter = 1;

            // 檢查是否已存在
            while (CurrentScript.Roles.Any(r => r.Id == $"{baseId}{counter}"))
            {
                counter++;
            }

            return $"{baseId}{counter}";
        }
    }
}