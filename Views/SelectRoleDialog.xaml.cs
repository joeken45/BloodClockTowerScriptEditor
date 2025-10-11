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

namespace BloodClockTowerScriptEditor.Views
{
    public partial class SelectRoleDialog : Window
    {
        private List<RoleTemplate> _allRoles = new();
        private List<RoleTemplate> _filteredRoles = new();

        /// <summary>
        /// 使用者選擇的角色
        /// </summary>
        public Role? SelectedRole { get; private set; }

        public SelectRoleDialog()
        {
            InitializeComponent();

            // 🆕 延遲載入，避免在建構函式中執行非同步操作
            this.Loaded += SelectRoleDialog_Loaded;
        }

        /// <summary>
        /// 視窗載入時執行
        /// </summary>
        private async void SelectRoleDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 顯示載入訊息
                txtResultCount.Text = "載入中...";

                await LoadRolesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"初始化對話框失敗：\n{ex.Message}\n\n堆疊追蹤：\n{ex.StackTrace}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                // 關閉對話框
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
                System.Diagnostics.Debug.WriteLine("開始載入角色...");

                using var context = new RoleTemplateContext();

                // 🆕 先檢查資料庫是否有資料
                var count = await context.RoleTemplates.CountAsync();
                System.Diagnostics.Debug.WriteLine($"資料庫中有 {count} 個角色");

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

                // 載入所有角色及其提示標記
                _allRoles = await context.RoleTemplates
                    .Include(r => r.Reminders)
                    .OrderBy(r => r.Team)
                    .ThenBy(r => r.Name)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"成功載入 {_allRoles.Count} 個角色");

                // 🆕 檢查並過濾無效資料
                var validRoles = _allRoles.Where(r =>
                    !string.IsNullOrEmpty(r.Id) &&
                    !string.IsNullOrEmpty(r.Name) &&
                    !string.IsNullOrEmpty(r.Team)
                ).ToList();

                if (validRoles.Count < _allRoles.Count)
                {
                    System.Diagnostics.Debug.WriteLine($"過濾掉 {_allRoles.Count - validRoles.Count} 個無效角色");
                    _allRoles = validRoles;
                }

                // 套用篩選
                ApplyFilter();

                System.Diagnostics.Debug.WriteLine("角色載入完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入角色失敗：{ex.Message}");
                throw; // 重新拋出例外，由上層處理
            }
        }

        /// <summary>
        /// 套用篩選條件
        /// </summary>
        private void ApplyFilter()
        {
            try
            {
                // 🆕 安全地取得搜尋文字
                var searchText = txtSearch?.Text?.Trim().ToLower() ?? string.Empty;

                _filteredRoles = _allRoles.Where(r =>
                {
                    // 類型篩選
                    bool teamMatch = r.Team switch
                    {
                        "townsfolk" => chkTownsfolk?.IsChecked == true,
                        "outsider" => chkOutsider?.IsChecked == true,
                        "minion" => chkMinion?.IsChecked == true,
                        "demon" => chkDemon?.IsChecked == true,
                        "traveler" => chkTraveler?.IsChecked == true,
                        "fabled" => chkFabled?.IsChecked == true,
                        _ => false
                    };

                    if (!teamMatch) return false;

                    // 搜尋篩選
                    if (string.IsNullOrEmpty(searchText)) return true;

                    return r.Name.ToLower().Contains(searchText) ||
                           (r.NameEng?.ToLower().Contains(searchText) ?? false) ||
                           (r.Ability?.ToLower().Contains(searchText) ?? false);

                }).ToList();

                // 更新顯示
                if (rolesList != null)
                {
                    rolesList.ItemsSource = _filteredRoles;
                }

                if (txtResultCount != null)
                {
                    txtResultCount.Text = $"共 {_filteredRoles.Count} 個角色";
                }

                System.Diagnostics.Debug.WriteLine($"篩選後顯示 {_filteredRoles.Count} 個角色");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"套用篩選時發生錯誤：{ex.Message}");
                MessageBox.Show(
                    $"套用篩選失敗：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 搜尋文字變更
        /// </summary>
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 🆕 確保已載入資料
            if (_allRoles != null && _allRoles.Count > 0)
            {
                ApplyFilter();
            }
        }

        /// <summary>
        /// 篩選條件變更
        /// </summary>
        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            // 🆕 確保已載入資料
            if (_allRoles != null && _allRoles.Count > 0)
            {
                ApplyFilter();
            }
        }

        /// <summary>
        /// 角色卡片點擊
        /// </summary>
        private void RoleCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.Tag is RoleTemplate roleTemplate)
                {
                    if (roleTemplate == null)
                    {
                        MessageBox.Show("角色資料無效", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // 確認對話框
                    var result = MessageBox.Show(
                        $"確定要新增「{roleTemplate.Name}」到劇本中嗎？",
                        "確認新增",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // 轉換為 Role 並設定結果
                            SelectedRole = roleTemplate.ToRole();

                            if (SelectedRole == null)
                            {
                                MessageBox.Show("角色轉換失敗", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            this.DialogResult = true;
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"轉換角色時發生錯誤:\n{ex.Message}",
                                "錯誤",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"處理點擊事件時發生錯誤:\n{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 取消按鈕
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}