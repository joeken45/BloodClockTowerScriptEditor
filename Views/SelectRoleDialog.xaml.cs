using BloodClockTowerScriptEditor.Data;
using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BloodClockTowerScriptEditor.Views
{
    public partial class SelectRoleDialog : Window
    {
        private List<RoleTemplate> _allRoles = [];
        private List<RoleTemplate> _filteredRoles = [];
        private DispatcherTimer? _searchTimer;

        /// <summary>
        /// 使用者選擇的角色列表（多選）
        /// </summary>
        public List<Role> SelectedRoles { get; private set; } = [];

        public SelectRoleDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 視窗載入時執行
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtResultCount.Text = "載入中...";
                await LoadRolesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"初始化對話框失敗：\n{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                this.Close();
            }
        }

        /// <summary>
        /// 從資料庫載入角色
        /// </summary>
        private async Task LoadRolesAsync()
        {
            try
            {
                using var context = new RoleTemplateContext();

                var count = await context.RoleTemplates.CountAsync();

                if (count == 0)
                {
                    MessageBox.Show(
                        "資料庫中沒有角色資料，請先匯入角色範本。",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    this.Close();
                    return;
                }

                // 載入所有角色
                var roles = await context.RoleTemplates
                    .Include(r => r.Reminders)
                    .ToListAsync();

                _allRoles = [.. roles
                    .OrderBy(r => r.IsOfficial ? 0 : 1)
                    .ThenBy(r => GetTeamOrder(r.Team))
                    .ThenBy(r => r.OriginalOrder)
                    .ThenBy(r => r.CreatedDate)];

                // 初始化 IsSelected 屬性
                foreach (var role in _allRoles)
                {
                    role.IsSelected = false;
                }

                // 套用初始篩選
                ApplyFilter();
            }
            catch (Exception ex)
            {
                throw new Exception($"載入角色失敗: {ex.Message}", ex);
            }
        }
        private static int GetTeamOrder(string team)
        {
            return team?.ToLower() switch
            {
                "townsfolk" => 0,
                "outsider" => 1,
                "minion" => 2,
                "demon" => 3,
                "traveler" => 4,
                "fabled" => 5,
                _ => 6
            };
        }

        /// <summary>
        /// 套用篩選條件
        /// </summary>
        private void ApplyFilter()
        {
            try
            {
                if (_allRoles == null || _allRoles.Count == 0)
                    return;

                // 取得篩選條件
                bool showTownsfolk = chkTownsfolk?.IsChecked ?? true;
                bool showOutsider = chkOutsider?.IsChecked ?? true;
                bool showMinion = chkMinion?.IsChecked ?? true;
                bool showDemon = chkDemon?.IsChecked ?? true;
                bool showTraveler = chkTraveler?.IsChecked ?? true;
                bool showFabled = chkFabled?.IsChecked ?? true;

                // 來源篩選
                bool? showOfficial = null;
                if (rbOfficial?.IsChecked == true)
                    showOfficial = true;
                else if (rbCustom?.IsChecked == true)
                    showOfficial = false;

                string searchText = txtSearch?.Text?.ToLower()?.Trim() ?? "";

                // 篩選角色
                _filteredRoles = [.. _allRoles.Where(r =>
                {
                    // 來源篩選
                    if (showOfficial.HasValue && r.IsOfficial != showOfficial.Value)
                        return false;

                    // 類型篩選
                    bool teamMatch = r.Team?.ToLower() switch
                    {
                        "townsfolk" => showTownsfolk,
                        "outsider" => showOutsider,
                        "minion" => showMinion,
                        "demon" => showDemon,
                        "traveler" => showTraveler,
                        "fabled" => showFabled,
                        _ => true
                    };

                    if (!teamMatch) return false;

                    // 搜尋文字篩選
                    if (string.IsNullOrEmpty(searchText)) return true;

                    return (r.Name?.ToLower().Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                           (r.Ability?.ToLower().Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ?? false);

                })];

                // 更新顯示
                rolesList.ItemsSource = null;
                rolesList.ItemsSource = _filteredRoles;

                // 更新統計
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"套用篩選失敗：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 更新統計資訊
        /// </summary>
        private void UpdateStatistics()
        {
            int selectedCount = _filteredRoles.Count(r => r.IsSelected);
            txtSelectedCount.Text = $"已選擇: {selectedCount} 個";
            txtResultCount.Text = $"共 {_filteredRoles.Count} 個角色";
        }

        /// <summary>
        /// 搜尋文字變更（延遲搜尋）
        /// </summary>
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allRoles == null || _allRoles.Count == 0)
                return;

            // 停止現有的計時器
            _searchTimer?.Stop();

            // 建立新的計時器（延遲 300ms）
            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };

            _searchTimer.Tick += (s, args) =>
            {
                _searchTimer.Stop();
                ApplyFilter();
            };

            _searchTimer.Start();
        }

        /// <summary>
        /// 篩選條件變更
        /// </summary>
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (_allRoles != null && _allRoles.Count > 0)
            {
                ApplyFilter();
            }
        }

        /// <summary>
        /// 全選
        /// </summary>
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var role in _filteredRoles)
            {
                role.IsSelected = true;
            }
            rolesList.Items.Refresh();
            UpdateStatistics();
        }

        /// <summary>
        /// 取消全選
        /// </summary>
        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var role in _filteredRoles)
            {
                role.IsSelected = false;
            }
            rolesList.Items.Refresh();
            UpdateStatistics();
        }

        /// <summary>
        /// 點擊整個角色項目區塊時切換勾選狀態
        /// </summary>
        private void RoleItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 取得點擊的 Border 對應的資料
            if (sender is Border border && border.DataContext is RoleTemplate role)
            {
                // 如果點擊的是 CheckBox 本身，不處理（讓 CheckBox 自己處理）
                if (e.OriginalSource is CheckBox)
                    return;

                // 切換勾選狀態
                role.IsSelected = !role.IsSelected;
                rolesList.Items.Refresh();
                UpdateStatistics();
            }
        }

        /// <summary>
        /// 確認新增
        /// </summary>
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 取得所有選中的角色
                var selectedTemplates = _allRoles.Where(r => r.IsSelected).ToList();

                if (selectedTemplates.Count == 0)
                {
                    MessageBox.Show(
                        "請至少選擇一個角色",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                // 轉換為 Role 物件
                SelectedRoles.Clear();
                foreach (var template in selectedTemplates)
                {
                    var role = template.ToRole();
                    if (role != null)
                    {
                        SelectedRoles.Add(role);
                    }
                }

                if (SelectedRoles.Count > 0)
                {
                    DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show(
                        "角色轉換失敗",
                        "錯誤",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"新增角色失敗：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// 來源篩選變更
        /// </summary>
        private void Source_Changed(object sender, RoutedEventArgs e)
        {
            if (_allRoles != null && _allRoles.Count > 0)
            {
                ApplyFilter();
            }
        }

        /// <summary>
        /// 新增自訂角色
        /// </summary>
        private async void CreateCustomRole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CreateCustomRoleDialog
                {
                    Owner = this
                };

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    // 重新載入角色列表
                    await LoadRolesAsync();

                    // 切換到「自訂角色」篩選
                    if (rbCustom != null)
                        rbCustom.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"新增自訂角色失敗：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 編輯自訂角色
        /// </summary>
        private async void EditRole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is RoleTemplate role)
                {
                    var dialog = new CreateCustomRoleDialog(role)
                    {
                        Owner = this
                    };

                    bool? result = dialog.ShowDialog();

                    if (result == true)
                    {
                        // 重新載入角色列表
                        await LoadRolesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"編輯角色失敗：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 刪除自訂角色
        /// </summary>
        private async void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is RoleTemplate role)
                {
                    var result = MessageBox.Show(
                        $"確定要刪除自訂角色「{role.Name}」嗎？\n\n" +
                        "此操作無法復原，角色將從資料庫中永久刪除。",
                        "確認刪除",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        using var context = new RoleTemplateContext();
                        var roleToDelete = await context.RoleTemplates
                            .Include(r => r.Reminders)
                            .FirstOrDefaultAsync(r => r.Id == role.Id);

                        if (roleToDelete != null)
                        {
                            context.RoleTemplates.Remove(roleToDelete);
                            await context.SaveChangesAsync();

                            MessageBox.Show(
                                $"已刪除自訂角色「{role.Name}」",
                                "刪除成功",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );

                            // 重新載入角色列表
                            await LoadRolesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"刪除角色失敗：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}