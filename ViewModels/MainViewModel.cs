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
    /// 主視窗視圖模型 - Phase 2 版本
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
        /// 新增自訂角色
        /// </summary>
        [RelayCommand]
        private void AddRole()
        {
            try
            {
                // TODO: Phase 2.3 - 改為顯示範本選擇對話框
                AddCustomRole();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增角色失敗:\n{ex.Message}", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 新增自訂空白角色
        /// </summary>
        private void AddCustomRole()
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

        /// <summary>
        /// 從範本新增角色 (Phase 2.3 將實作)
        /// </summary>
        /// <param name="template">角色範本</param>
        private void AddRoleFromTemplate(Role template)
        {
            // 生成新的唯一 ID
            string newId = GenerateUniqueId();

            // 建立角色副本
            var newRole = new Role
            {
                Id = newId,
                Name = template.Name,
                Team = template.Team,
                Ability = template.Ability,
                Image = template.Image,
                Edition = template.Edition,
                FirstNight = template.FirstNight,
                OtherNight = template.OtherNight,
                Setup = template.Setup,
                Reminders = new System.Collections.Generic.List<string>(template.Reminders),
                RemindersGlobal = new System.Collections.Generic.List<string>(template.RemindersGlobal),
                NameEng = template.NameEng,
                Flavor = template.Flavor,
                FirstNightReminder = template.FirstNightReminder,
                OtherNightReminder = template.OtherNightReminder
            };

            // 加入劇本
            CurrentScript.Roles.Add(newRole);
            UpdateFilteredRoles();

            // 自動選中新角色
            SelectedRole = newRole;

            StatusMessage = $"已從範本新增角色: {newRole.Name}";
        }

        /// <summary>
        /// 刪除角色
        /// </summary>
        [RelayCommand]
        private void DeleteRole()
        {
            if (SelectedRole == null)
            {
                MessageBox.Show("請先選擇要刪除的角色", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    $"確定要刪除角色「{SelectedRole.Name}」嗎？\n此操作無法復原。",
                    "確認刪除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    string deletedName = SelectedRole.Name;
                    CurrentScript.Roles.Remove(SelectedRole);
                    UpdateFilteredRoles();
                    SelectedRole = null;

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
        private void DuplicateRole()
        {
            if (SelectedRole == null)
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
                    Name = SelectedRole.Name + " (副本)",
                    Team = SelectedRole.Team,
                    Ability = SelectedRole.Ability,
                    Image = SelectedRole.Image,
                    Edition = SelectedRole.Edition,
                    FirstNight = SelectedRole.FirstNight,
                    OtherNight = SelectedRole.OtherNight,
                    Setup = SelectedRole.Setup,
                    Reminders = new System.Collections.Generic.List<string>(SelectedRole.Reminders),
                    RemindersGlobal = new System.Collections.Generic.List<string>(SelectedRole.RemindersGlobal),
                    NameEng = SelectedRole.NameEng,
                    Flavor = SelectedRole.Flavor,
                    FirstNightReminder = SelectedRole.FirstNightReminder,
                    OtherNightReminder = SelectedRole.OtherNightReminder
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