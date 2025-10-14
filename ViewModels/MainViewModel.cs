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
                    // 🆕 載入後自動排序
                    SortRoles();
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
                // 🆕 儲存前先排序
                SortRoles();
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
        /// 從官方角色範本新增（支援多選）
        /// </summary>
        [RelayCommand]
        private void AddFromOfficialTemplate()
        {
            try
            {
                // 檢查當前劇本是否存在
                if (CurrentScript == null)
                {
                    MessageBox.Show(
                        "請先載入或建立一個劇本",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                System.Diagnostics.Debug.WriteLine("開啟角色選擇對話框...");

                var dialog = new Views.SelectRoleDialog
                {
                    Owner = Application.Current.MainWindow
                };

                System.Diagnostics.Debug.WriteLine("對話框已建立，準備顯示...");

                bool? result = dialog.ShowDialog();

                System.Diagnostics.Debug.WriteLine($"對話框已關閉，結果：{result}");

                if (result == true && dialog.SelectedRoles != null && dialog.SelectedRoles.Count > 0)
                {
                    // 批次加入選擇的角色
                    int addedCount = 0;
                    foreach (var role in dialog.SelectedRoles)
                    {
                        CurrentScript.Roles.Add(role);
                        addedCount++;
                    }

                    // 🆕 新增後自動排序
                    SortRoles();
                    UpdateFilteredRoles();

                    // 自動選中最後一個新增的角色
                    SelectedRole = dialog.SelectedRoles.Last();

                    StatusMessage = $"已新增 {addedCount} 個角色";

                    System.Diagnostics.Debug.WriteLine($"成功新增 {addedCount} 個角色");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"發生錯誤：{ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆疊追蹤：{ex.StackTrace}");

                MessageBox.Show(
                    $"新增角色失敗:\n{ex.Message}\n\n詳細資訊請查看輸出視窗",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
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
            catch (Exception ex)
            {
                MessageBox.Show($"刪除角色失敗:\n{ex.Message}", "錯誤",
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

        // ==================== 🆕 排序功能 (Phase 3.1) ====================

        /// <summary>
        /// 定義角色類型的排序優先級
        /// 鎮民(1) > 外來者(2) > 爪牙(3) > 惡魔(4) > 旅行者(5) > 傳奇(6) > 相剋(7)
        /// </summary>
        private int GetTeamSortOrder(TeamType team)
        {
            return team switch
            {
                TeamType.Townsfolk => 1,
                TeamType.Outsider => 2,
                TeamType.Minion => 3,
                TeamType.Demon => 4,
                TeamType.Traveler => 5,
                TeamType.Fabled => 6,
                TeamType.Jinxed => 7,
                _ => 99
            };
        }

        /// <summary>
        /// 對當前劇本的角色列表進行排序
        /// </summary>
        private void SortRoles()
        {
            if (CurrentScript?.Roles == null || CurrentScript.Roles.Count == 0)
                return;

            var sortedRoles = CurrentScript.Roles
                .OrderBy(r => GetTeamSortOrder(r.Team))
                .ToList();

            CurrentScript.Roles.Clear();
            foreach (var role in sortedRoles)
            {
                CurrentScript.Roles.Add(role);
            }

            System.Diagnostics.Debug.WriteLine($"✅ 角色已自動排序，共 {sortedRoles.Count} 個角色");
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

        // ==================== 劇本資訊編輯命令 (Phase 2.7 新增) ====================

        /// <summary>
        /// 開啟劇本資訊編輯視窗
        /// </summary>
        [RelayCommand]
        private void EditScriptMeta()
        {
            try
            {
                var dialog = new Views.EditScriptMetaWindow(CurrentScript.Meta)
                {
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    // 編輯完成後更新顯示
                    OnPropertyChanged(nameof(CurrentScript));
                    StatusMessage = "已更新劇本資訊";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"開啟編輯視窗失敗:\n{ex.Message}", "錯誤",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}