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
    /// 主視窗視圖模型 - Phase 3.2 夜晚順序編輯器
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        // ==================== 私有欄位 ====================
        private bool _isDirty; // 檔案是否有未儲存的變更
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

        // 【4. 新增角色集合變更事件處理】
        private void OnRolesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsDirty = true;
        }

        public Role? SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
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

        // 🆕 夜晚順序集合
        public ObservableCollection<Role> FirstNightRoles { get; }
        public ObservableCollection<Role> OtherNightRoles { get; }

        // 篩選條件
        public bool ShowTownsfolk
        {
            get => _showTownsfolk;
            set
            {
                if (SetProperty(ref _showTownsfolk, value))
                    UpdateFilteredRoles();
            }
        }

        public bool ShowOutsiders
        {
            get => _showOutsiders;
            set
            {
                if (SetProperty(ref _showOutsiders, value))
                    UpdateFilteredRoles();
            }
        }

        public bool ShowMinions
        {
            get => _showMinions;
            set
            {
                if (SetProperty(ref _showMinions, value))
                    UpdateFilteredRoles();
            }
        }

        public bool ShowDemons
        {
            get => _showDemons;
            set
            {
                if (SetProperty(ref _showDemons, value))
                    UpdateFilteredRoles();
            }
        }

        public bool ShowTravelers
        {
            get => _showTravelers;
            set
            {
                if (SetProperty(ref _showTravelers, value))
                    UpdateFilteredRoles();
            }
        }

        public bool ShowFabled
        {
            get => _showFabled;
            set
            {
                if (SetProperty(ref _showFabled, value))
                    UpdateFilteredRoles();
            }
        }

        // ==================== 命令 ====================

        [RelayCommand]
        private void NewFile()
        {
            // 檢查未儲存的變更
            if (!CheckUnsavedChanges())
                return;

            // 建立新劇本
            CurrentScript = new Script();
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
                MessageBox.Show($"載入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                IsDirty = false; // 儲存後清除標記
                StatusMessage = $"已儲存: {CurrentFilePath}";
                MessageBox.Show("儲存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    Filter = "JSON 檔案 (*.json)|*.json|所有檔案 (*.*)|*.*",
                    Title = "另存劇本檔案",
                    FileName = "script.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    _jsonService.SaveScript(CurrentScript, dialog.FileName);
                    CurrentFilePath = dialog.FileName;
                    IsDirty = false; // 儲存後清除標記
                    StatusMessage = $"已儲存: {dialog.FileName}";
                    MessageBox.Show("儲存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "儲存失敗";
            }
        }

        [RelayCommand]
        private void AddFromOfficialTemplate()
        {
            try
            {
                var dialog = new Views.SelectRoleDialog();
                if (dialog.ShowDialog() == true && dialog.SelectedRoles != null)
                {
                    int addedCount = 0;
                    foreach (var selectedRole in dialog.SelectedRoles)
                    {
                        CurrentScript.Roles.Add(selectedRole);
                        addedCount++;
                    }

                    UpdateFilteredRoles();
                    UpdateNightOrderLists();
                    StatusMessage = $"已新增 {addedCount} 個角色";
                    // IsDirty 會自動被 OnRolesCollectionChanged 設置,所以這裡不用再加
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增角色失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "新增角色失敗";
            }
        }

        [RelayCommand]
        private void RemoveRole()
        {
            if (SelectedRole == null)
            {
                MessageBox.Show("請先選擇要刪除的角色", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"確定要刪除角色「{SelectedRole.Name}」嗎？",
                "確認刪除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                CurrentScript.Roles.Remove(SelectedRole);
                SelectedRole = null;
                UpdateFilteredRoles();
                UpdateNightOrderLists(); // 🆕 更新夜晚順序
                StatusMessage = "角色已刪除";
            }
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
                MessageBox.Show($"編輯劇本資訊失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "編輯失敗";
            }
        }

        // ==================== 私有方法 ====================

        private void UpdateFilteredRoles()
        {
            FilteredRoles.Clear();

            var filtered = CurrentScript.Roles
                .Where(r =>
                    (ShowTownsfolk && r.Team == TeamType.Townsfolk) ||
                    (ShowOutsiders && r.Team == TeamType.Outsider) ||
                    (ShowMinions && r.Team == TeamType.Minion) ||
                    (ShowDemons && r.Team == TeamType.Demon) ||
                    (ShowTravelers && r.Team == TeamType.Traveler) ||
                    (ShowFabled && r.Team == TeamType.Fabled)
                )
                .OrderBy(r => r.Team)
                .ThenBy(r => r.Name);

            foreach (var role in filtered)
            {
                // 訂閱角色的類型變更事件
                role.TeamChanged -= OnRoleTeamChanged;
                role.TeamChanged += OnRoleTeamChanged;

                // 訂閱角色的夜晚順序變更事件
                role.NightOrderChanged -= OnRoleNightOrderChanged;
                role.NightOrderChanged += OnRoleNightOrderChanged;

                // 🆕 訂閱角色的屬性變更事件 (追蹤 IsDirty)
                role.PropertyChanged -= OnRolePropertyChanged;
                role.PropertyChanged += OnRolePropertyChanged;

                FilteredRoles.Add(role);
            }
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
                // 第二個 → 第一個 - 0.1
                var first = list[0];
                var firstOrder = isFirstNight ? first.FirstNight : first.OtherNight;
                newOrder = firstOrder - 0.1;
            }
            else
            {
                // 其他 → 上上個 + 0.1
                var target = list[index - 2];
                var targetOrder = isFirstNight ? target.FirstNight : target.OtherNight;
                newOrder = targetOrder + 0.1;
            }

            if (isFirstNight)
                role.FirstNight = (int)(newOrder * 10) / 10.0; // 保留一位小數
            else
                role.OtherNight = (int)(newOrder * 10) / 10.0;
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

            // 下移 = 下一個 + 0.1
            var newOrder = belowOrder + 0.1;

            if (isFirstNight)
                role.FirstNight = (int)(newOrder * 10) / 10.0;
            else
                role.OtherNight = (int)(newOrder * 10) / 10.0;
        }

        // 【步驟14: 新增角色屬性變更處理 - 放在私有方法區塊】
        private void OnRolePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 任何角色屬性變更都標記為需要儲存
            IsDirty = true;
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
    }
}