using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Services;
using BloodClockTowerScriptEditor.ViewModels;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BloodClockTowerScriptEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            // 註冊載入事件
            Loaded += MainWindow_Loaded;
            // 🆕 註冊關閉事件
            Closing += MainWindow_Closing;
            // 🆕 監聽 SelectedRole 變更，用於切換 UI
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        /// <summary>
        /// 視窗載入完成後執行
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeDefaultRolesAsync();
            await InitializeJinxRulesAsync();  

            // 🆕 為初始空白劇本加入爪牙/惡魔訊息
            if (DataContext is MainViewModel viewModel)
            {
                await viewModel.LoadMinionDemonInfoAsync();
            }
        }

        /// <summary>
        /// 視窗關閉前檢查未儲存的變更
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                // 檢查未儲存的變更
                if (!viewModel.CheckUnsavedChanges())
                {
                    // 使用者選擇取消,阻止視窗關閉
                    e.Cancel = true;
                }
            }
        }
        /// <summary>
        /// 同步 JSON 資源到程式資料夾並匯入資料庫
        /// </summary>
        private async Task<int> SyncResourceToFolderAsync(
            string resourceFileName,
            string embeddedResourceName,
            Func<string, Task<int>> importAction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🚀 開始同步 {resourceFileName}...");

                string appFolder = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(appFolder, resourceFileName);

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"📥 從內嵌資源建立 {resourceFileName}...");

                    string embeddedContent = LoadEmbeddedResource(embeddedResourceName);

                    if (string.IsNullOrEmpty(embeddedContent))
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ 無法載入內嵌資源：{embeddedResourceName}");
                        return 0;
                    }

                    await File.WriteAllTextAsync(filePath, embeddedContent);
                    System.Diagnostics.Debug.WriteLine($"✅ 已建立 {resourceFileName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✅ 使用現有 {resourceFileName}");
                }

                // 執行匯入動作
                int count = await importAction(filePath);
                System.Diagnostics.Debug.WriteLine($"✅ {resourceFileName} 同步完成！處理了 {count} 筆");

                return count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 同步 {resourceFileName} 失敗：{ex.Message}");
                return 0;
            }
        }

        // 然後原本的方法改成：
        private async Task InitializeDefaultRolesAsync()
        {
            var importService = new RoleImportService();
            await SyncResourceToFolderAsync(
                "角色總表.json",
                "BloodClockTowerScriptEditor.Resources.角色總表.json",
                path => importService.ImportFromJsonAsync(path, "官方", true)
            );
        }

        private async Task InitializeJinxRulesAsync()
        {
            var importService = new RoleImportService();
            await SyncResourceToFolderAsync(
                "相剋規則.json",
                "BloodClockTowerScriptEditor.Resources.相剋規則.json",
                importService.ImportJinxRulesFromJsonAsync
            );
        }

        /// <summary>
        /// 載入內嵌資源
        /// </summary>
        private string LoadEmbeddedResource(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ 找不到內嵌資源: {resourceName}");
                    System.Diagnostics.Debug.WriteLine("可用的資源:");
                    foreach (var name in assembly.GetManifestResourceNames())
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {name}");
                    }
                    return string.Empty;
                }

                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 讀取內嵌資源失敗: {ex.Message}");
                return string.Empty;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Blood on the Clocktower 劇本編輯器\n\n" +
                "版本: 1.0.0 \n",
                "關於",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void MoveNightOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag &&
                button.DataContext is Role role)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    bool isFirstNight = tag.Contains("First");
                    bool isUp = tag.Contains("Up");

                    if (isUp) viewModel.MoveRoleUp(role, isFirstNight);
                    else viewModel.MoveRoleDown(role, isFirstNight);

                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// 統一處理提示標記的新增/刪除
        /// Tag 格式: "Add|Normal" / "Add|Global" / "Remove|Normal" / "Remove|Global"
        /// </summary>
        private void ManageReminder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel || viewModel.SelectedRole == null)
                return;

            if (sender is not Button button || button.Tag is not string tag)
                return;

            var parts = tag.Split('|');
            if (parts.Length != 2) return;

            string action = parts[0];  // "Add" 或 "Remove"
            string type = parts[1];    // "Normal" 或 "Global"

            var collection = type == "Global"
                ? viewModel.SelectedRole.RemindersGlobal
                : viewModel.SelectedRole.Reminders;

            if (action == "Add")
            {
                if (type == "Global")
                    ReminderItem.AddGlobalReminder(collection);
                else
                    ReminderItem.AddReminder(collection);
            }
            else if (action == "Remove")
            {
                ReminderItem.RemoveSelected(collection);
            }
        }

        /// <summary>
        /// 監聽 ViewModel 屬性變更
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 當 SelectedRole 變更時，檢查是否需要切換 UI
            if (e.PropertyName == nameof(MainViewModel.SelectedRole))
            {
                UpdateRoleEditorVisibility();

                // 如果選擇的是相剋角色，監聽 Team 變更
                if (sender is MainViewModel vm && vm.SelectedRole != null)
                {
                    vm.SelectedRole.PropertyChanged -= SelectedRole_PropertyChanged;
                    vm.SelectedRole.PropertyChanged += SelectedRole_PropertyChanged;

                    // 初始化 UI 狀態
                    UpdateRoleEditorVisibility();
                }
            }
        }

        /// <summary>
        /// 監聽角色屬性變更（主要監聽 Team 變更）
        /// </summary>
        private void SelectedRole_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 當 Team 變更時，切換 UI
            if (e.PropertyName == nameof(Role.Team))
            {
                UpdateRoleEditorVisibility();

                // 如果切換到相剋規則，清空不必要的欄位
                if (sender is Role role && role.Team == TeamType.Jinxed)
                {
                    ClearNonJinxedFields(role);
                }
            }
        }

        /// <summary>
        /// 更新角色編輯器的顯示/隱藏
        /// </summary>
        private void UpdateRoleEditorVisibility()
        {
            if (DataContext is not MainViewModel viewModel || viewModel.SelectedRole == null)
            {
                // 沒有選擇角色時，兩個編輯器都隱藏
                NormalRoleEditor.Visibility = Visibility.Collapsed;
                JinxedRoleEditor.Visibility = Visibility.Collapsed;
                return;
            }

            bool isJinxedRole = viewModel.SelectedRole.Team == TeamType.Jinxed;

            // 根據角色類型切換 UI
            NormalRoleEditor.Visibility = isJinxedRole
                ? Visibility.Collapsed
                : Visibility.Visible;

            JinxedRoleEditor.Visibility = isJinxedRole
                ? Visibility.Visible
                : Visibility.Collapsed;

            System.Diagnostics.Debug.WriteLine($"UI 切換: {(isJinxedRole ? "相剋規則編輯器" : "一般角色編輯器")}");
        }

        /// <summary>
        /// 清空非相剋規則需要的欄位
        /// </summary>
        private void ClearNonJinxedFields(Role role)
        {
            // 清空不必要的欄位，但保留基本資訊
            role.Edition = null;
            role.Setup = false;
            role.Flavor = null;
            role.FirstNight = 0;
            role.FirstNightReminder = null;
            role.OtherNight = 0;
            role.OtherNightReminder = null;

            // 清空提示標記
            role.Reminders.Clear();
            role.RemindersGlobal.Clear();

            // 清空 BOTC Jinxes
            role.Jinxes = null;

            System.Diagnostics.Debug.WriteLine($"已清空角色 {role.Name} 的非相剋欄位");
        }

        // ==================== 私有欄位 ====================
        private Role? _draggedRole = null;
        private TeamType _draggedFromTeam;
        private System.Windows.Shapes.Line? _dropIndicatorLine = null;  // ✅ 改為指示線

        // ==================== 圖片管理方法 ====================

        /// <summary>
        /// 新增圖片
        /// </summary>
        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel || viewModel.SelectedRole == null)
                return;

            // 限制最多 3 張
            if (viewModel.SelectedRole.Image.Count >= 3)
            {
                MessageBox.Show("最多只能新增 3 張圖片", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ✅ Bug Fix: 只新增到 ImageItems，讓事件自動同步到 Image
            // 移除原本的 viewModel.SelectedRole.Image.Add("")
            var newItem = new ImageItem("");
            viewModel.SelectedRole.ImageItems.Add(newItem);
            viewModel.IsDirty = true;
        }

        /// <summary>
        /// 刪除圖片
        /// </summary>
        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel || viewModel.SelectedRole == null)
                return;

            // 至少保留 1 張
            if (viewModel.SelectedRole.Image.Count <= 1)
            {
                MessageBox.Show("至少需要保留 1 張圖片", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var button = sender as Button;
            if (button?.DataContext is ImageItem item)
            {
                var displayUrl = string.IsNullOrWhiteSpace(item.Url) ? "(空白)" : item.Url;
                var result = MessageBox.Show($"確定要刪除此圖片嗎？\n\n{displayUrl}",
                    "確認刪除", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // ✅ Bug Fix: 只從 ImageItems 移除，讓事件自動同步到 Image
                    // 移除原本的 viewModel.SelectedRole.Image.RemoveAt(index)
                    viewModel.SelectedRole.ImageItems.Remove(item);
                    viewModel.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// 圖片 URL 變更時觸發
        /// </summary>
        private void ImageUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.IsDirty = true;
            }
        }

        // ==================== Jinx 管理方法 ====================

        /// <summary>
        /// 新增 Jinx
        /// </summary>
        private void AddJinx_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel || viewModel.SelectedRole == null)
                return;

            var newItem = new JinxItem("", "");
            viewModel.SelectedRole.JinxItems.Add(newItem);
            viewModel.IsDirty = true;
        }

        /// <summary>
        /// 刪除 Jinx
        /// </summary>
        private void DeleteJinx_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel || viewModel.SelectedRole == null)
                return;

            var button = sender as Button;
            if (button?.DataContext is JinxItem item)
            {
                var displayRole = string.IsNullOrEmpty(item.TargetRoleName)
                    ? "(未選擇)"
                    : item.TargetRoleName;

                var displayReason = string.IsNullOrEmpty(item.Reason)
                    ? "(無說明)"
                    : (item.Reason.Length > 30 ? item.Reason.Substring(0, 30) + "..." : item.Reason);

                var result = MessageBox.Show(
                    $"確定要刪除此相剋規則嗎？\n\n角色: {displayRole}\n規則: {displayReason}",
                    "確認刪除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    viewModel.SelectedRole.JinxItems.Remove(item);
                    viewModel.IsDirty = true;
                }
            }
        }

        /// <summary>
        /// 相剋角色 1 選擇變更時，通知角色 2 的選項更新
        /// </summary>
        private void JinxRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && viewModel.SelectedRole?.Team == TeamType.Jinxed)
            {
                viewModel.IsDirty = true;

                // ✅ 集石格式編輯後，同步到 BOTC 格式
                JinxSyncHelper.SyncFromJinxedRoles(viewModel.CurrentScript);
                viewModel.UpdateFilteredRoles();
            }
        }

        /// <summary>
        /// Jinx 目標角色 ComboBox 載入時，將 ID 轉換為名稱顯示
        /// </summary>
        private void JinxTargetRole_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox &&
                comboBox.DataContext is JinxItem item &&
                DataContext is MainViewModel viewModel)
            {
                // ID → 名稱
                var role = viewModel.CurrentScript.Roles
                    .FirstOrDefault(r => r.Id == item.TargetRoleName && r.Team != TeamType.Jinxed);

                if (role != null)
                {
                    comboBox.SelectedItem = role.Name;
                }
            }
        }

        /// <summary>
        /// Jinx 目標角色選擇變更時，將名稱轉換為 ID 存入
        /// </summary>
        private void JinxTargetRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox &&
                comboBox.DataContext is JinxItem item &&
                comboBox.SelectedItem is string selectedName &&
                DataContext is MainViewModel viewModel &&
                viewModel.SelectedRole != null)
            {
                var targetRole = viewModel.CurrentScript.Roles
                    .FirstOrDefault(r => r.Name == selectedName && r.Team != TeamType.Jinxed);

                if (targetRole != null)
                {
                    // 🔴 關鍵：先記錄舊的目標角色 ID
                    string oldTargetId = item.TargetRoleName;

                    // 🔴 更新新的目標角色 ID
                    item.TargetRoleName = targetRole.Id;

                    // 🔴 立即同步整個 JinxItems 到 Jinxes
                    viewModel.SelectedRole.SyncJinxItemsToJinxes();

                    // 🔴 移除舊目標角色的反向 Jinx
                    if (!string.IsNullOrEmpty(oldTargetId))
                    {
                        var oldTargetRole = viewModel.CurrentScript.Roles
                            .FirstOrDefault(r => r.Id == oldTargetId && r.Team != TeamType.Jinxed);

                        if (oldTargetRole != null && oldTargetRole.Jinxes != null)
                        {
                            var toRemove = oldTargetRole.Jinxes
                                .FirstOrDefault(j => j.Id == viewModel.SelectedRole.Id);
                            if (toRemove != null)
                            {
                                oldTargetRole.Jinxes.Remove(toRemove);
                                if (oldTargetRole.Jinxes.Count == 0)
                                    oldTargetRole.Jinxes = null;
                            }
                        }
                    }

                    viewModel.IsDirty = true;

                    // 🔴 現在才呼叫同步
                    SyncJinxesAfterEdit();
                }
            }
        }

        private void JinxReason_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.IsDirty = true;

                // ✅ 規則說明變更也要同步
                SyncJinxesAfterEdit();
            }
        }

        private void SyncJinxesAfterEdit()
        {
            if (DataContext is MainViewModel viewModel)
            {
                // 使用新的同步方法
                JinxSyncHelper.SyncFromAllBotcJinxes(viewModel.CurrentScript);
                viewModel.UpdateFilteredRoles();
            }
        }

        // ==================== 展開/收合事件 ====================

        /// <summary>
        /// 展開所有類別
        /// </summary>
        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            SetAllTeamVisibility(Visibility.Visible);
        }

        /// <summary>
        /// 收合所有類別
        /// </summary>
        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            SetAllTeamVisibility(Visibility.Collapsed);
        }

        /// <summary>
        /// 設定所有類別的可見性
        /// </summary>
        private void SetAllTeamVisibility(Visibility visibility)
        {
            listTownsfolk.Visibility = visibility;
            listOutsider.Visibility = visibility;
            listMinion.Visibility = visibility;
            listDemon.Visibility = visibility;
            listTraveler.Visibility = visibility;
            listFabled.Visibility = visibility;
            listJinxed.Visibility = visibility;

            // 更新圖示
            string icon = visibility == Visibility.Visible ? "▼" : "▶";
            iconTownsfolk.Text = icon;
            iconOutsider.Text = icon;
            iconMinion.Text = icon;
            iconDemon.Text = icon;
            iconTraveler.Text = icon;
            iconFabled.Text = icon;
            iconJinxed.Text = icon;
        }

        /// <summary>
        /// 點擊類別標題展開/收合
        /// </summary>
        private void TeamHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border header && header.Tag is string teamName)
            {
                // 找到對應的列表和圖示
                var (list, icon) = teamName switch
                {
                    "Townsfolk" => (listTownsfolk, iconTownsfolk),
                    "Outsider" => (listOutsider, iconOutsider),
                    "Minion" => (listMinion, iconMinion),
                    "Demon" => (listDemon, iconDemon),
                    "Traveler" => (listTraveler, iconTraveler),
                    "Fabled" => (listFabled, iconFabled),
                    "Jinxed" => (listJinxed, iconJinxed),
                    _ => (null, null)
                };

                if (list != null && icon != null)
                {
                    // 切換可見性
                    bool isVisible = list.Visibility == Visibility.Visible;
                    list.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
                    icon.Text = isVisible ? "▶" : "▼";
                }
            }
        }

        // ==================== 拖曳排序事件 ====================

        /// <summary>
        /// 開始拖曳角色
        /// </summary>
        private void RoleItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Role role)
            {
                // ✅ 新增：設定選中的角色
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.SelectedRole = role;
                }

                _draggedRole = role;
                _draggedFromTeam = role.Team;
                //border.Opacity = 0.5;
            }
        }

        /// <summary>
        /// 拖曳移動
        /// </summary>
        private void RoleItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedRole != null)
            {
                if (sender is Border border)
                {
                    // ✅ 加在這裡：開始拖曳時才變透明
                    border.Opacity = 0.5;

                    // 執行拖放操作
                    DragDrop.DoDragDrop(border, _draggedRole, DragDropEffects.Move);

                    // 恢復透明度
                    border.Opacity = 1.0;
                    _draggedRole = null;
                }
            }
        }

        /// <summary>
        /// 放下角色到新位置
        /// </summary>
        private void TeamList_Drop(object sender, DragEventArgs e)
        {
            // ✅ 放下時清除指示線
            if (sender is ItemsControl dropControl)
            {
                RemoveDropIndicator(dropControl);
            }
            if (e.Data.GetDataPresent(typeof(Role)))
            {
                var droppedRole = e.Data.GetData(typeof(Role)) as Role;

                if (droppedRole == null || DataContext is not MainViewModel viewModel)
                    return;

                if (sender is ItemsControl itemsControl && itemsControl.Tag is string targetTeamStr)
                {
                    if (!Enum.TryParse<TeamType>(targetTeamStr, out var targetTeam))
                        return;

                    if (droppedRole.Team != targetTeam)
                    {
                        MessageBox.Show(
                            "不能將角色移動到不同類型的分組中",
                            "提示",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                        return;
                    }

                    var mousePosition = e.GetPosition(itemsControl);

                    // ✅ 新增：取得目標角色和是否插入到上方
                    var (targetRole, insertAbove) = GetDropTarget(itemsControl, mousePosition);

                    if (targetRole != null && targetRole != droppedRole)
                    {
                        ReorderRolesInTeam(viewModel, targetTeam, droppedRole, targetRole, insertAbove);
                        viewModel.IsDirty = true;
                    }
                }
            }
        }

        /// <summary>
        /// 取得滑鼠位置下的角色
        /// </summary>
        private Role? GetRoleUnderMouse(ItemsControl itemsControl, Point mousePosition)
        {
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null) continue;

                // 取得容器相對於 ItemsControl 的位置
                var containerPos = container.TransformToAncestor(itemsControl).Transform(new Point(0, 0));
                var containerBounds = new Rect(containerPos, container.RenderSize);

                // 檢查滑鼠是否在此容器內
                if (containerBounds.Contains(mousePosition))
                {
                    return itemsControl.Items[i] as Role;
                }
            }
            return null;
        }
        /// <summary>
        /// 取得放置目標（包含插入位置判斷）
        /// </summary>
        private (Role? targetRole, bool insertAbove) GetDropTarget(ItemsControl itemsControl, Point mousePosition)
        {
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null) continue;

                var containerPos = container.TransformToAncestor(itemsControl).Transform(new Point(0, 0));
                var containerBounds = new Rect(containerPos, container.RenderSize);

                if (containerBounds.Contains(mousePosition))
                {
                    var role = itemsControl.Items[i] as Role;

                    // 計算滑鼠在容器中的相對位置
                    double relativeY = mousePosition.Y - containerBounds.Top;
                    bool insertAbove = relativeY < containerBounds.Height / 2;

                    return (role, insertAbove);
                }
            }
            return (null, false);
        }

        /// <summary>
        /// 重新排序同類型內的角色
        /// </summary>
        private void ReorderRolesInTeam(MainViewModel viewModel, TeamType team, Role movedRole, Role targetRole, bool insertAbove)
        {
            var teamRoles = viewModel.CurrentScript.Roles
                .Where(r => r.Team == team)
                .OrderBy(r => r.DisplayOrder)
                .ToList();

            teamRoles.Remove(movedRole);

            int targetIndex = teamRoles.IndexOf(targetRole);

            // ✅ 根據 insertAbove 決定插入位置
            if (!insertAbove)
            {
                targetIndex++;
            }

            teamRoles.Insert(targetIndex, movedRole);

            for (int i = 0; i < teamRoles.Count; i++)
            {
                teamRoles[i].DisplayOrder = i;
            }

            viewModel.UpdateFilteredRoles();
        }

        /// <summary>
        /// 在同類型內上移/下移角色
        /// </summary>
        private void MoveRoleInTeam_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Role role && DataContext is MainViewModel viewModel)
            {
                bool isUp = button.Tag?.ToString() == "Up";

                var teamRoles = viewModel.CurrentScript.Roles
                    .Where(r => r.Team == role.Team)
                    .OrderBy(r => r.DisplayOrder)
                    .ToList();

                int index = teamRoles.IndexOf(role);

                // 檢查是否可以移動
                if ((isUp && index <= 0) || (!isUp && index >= teamRoles.Count - 1))
                {
                    return; // 已在頂部/底部,無法移動
                }

                // 移除當前角色
                teamRoles.RemoveAt(index);

                // 插入到新位置
                int newIndex = isUp ? index - 1 : index + 1;
                teamRoles.Insert(newIndex, role);

                // ✅ 重新編號所有 DisplayOrder
                for (int i = 0; i < teamRoles.Count; i++)
                {
                    teamRoles[i].DisplayOrder = i;
                }

                // 刷新顯示
                viewModel.UpdateFilteredRoles();
                viewModel.IsDirty = true;
            }
        }
        /// <summary>
        /// 拖曳經過時顯示插入位置指示線
        /// </summary>
        private void TeamList_DragOver(object sender, DragEventArgs e)
        {
            if (_draggedRole == null) return;

            if (sender is ItemsControl itemsControl)
            {
                var mousePosition = e.GetPosition(itemsControl);
                var (targetRole, insertAbove) = GetDropTarget(itemsControl, mousePosition);

                // 移除舊的指示線
                RemoveDropIndicator(itemsControl);

                if (targetRole != null && targetRole != _draggedRole)
                {
                    // 找到目標容器並繪製指示線
                    for (int i = 0; i < itemsControl.Items.Count; i++)
                    {
                        if (itemsControl.Items[i] == targetRole)
                        {
                            var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;

                            if (container != null)
                            {
                                DrawDropIndicator(itemsControl, container, insertAbove);
                            }
                            break;
                        }
                    }
                }
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        /// <summary>
        /// 拖曳離開時清除指示線
        /// </summary>
        private void TeamList_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is ItemsControl itemsControl)
            {
                RemoveDropIndicator(itemsControl);
            }
        }

        /// <summary>
        /// 繪製插入位置指示線
        /// </summary>
        private void DrawDropIndicator(ItemsControl itemsControl, FrameworkElement targetContainer, bool insertAbove)
        {
            // 建立指示線
            _dropIndicatorLine = new System.Windows.Shapes.Line
            {
                Stroke = System.Windows.Media.Brushes.DodgerBlue,
                StrokeThickness = 3,
                X1 = 10,
                X2 = itemsControl.ActualWidth - 10
            };

            // 計算指示線的 Y 位置
            var containerPos = targetContainer.TransformToAncestor(itemsControl).Transform(new Point(0, 0));
            double yPosition = insertAbove ? containerPos.Y : containerPos.Y + targetContainer.ActualHeight;

            _dropIndicatorLine.Y1 = yPosition;
            _dropIndicatorLine.Y2 = yPosition;

            // 加入到 ItemsControl（需要用 Grid 包裝）
            if (itemsControl.Parent is Grid parentGrid)
            {
                _dropIndicatorLine.SetValue(Grid.RowProperty, itemsControl.GetValue(Grid.RowProperty));
                _dropIndicatorLine.IsHitTestVisible = false;
                parentGrid.Children.Add(_dropIndicatorLine);
            }
        }

        /// <summary>
        /// 移除插入位置指示線
        /// </summary>
        private void RemoveDropIndicator(ItemsControl itemsControl)
        {
            if (_dropIndicatorLine != null)
            {
                if (itemsControl.Parent is Grid parentGrid)
                {
                    parentGrid.Children.Remove(_dropIndicatorLine);
                }
                _dropIndicatorLine = null;
            }
        }

        /// <summary>
        /// 夜晚順序列表選擇變更
        /// </summary>
        private void NightOrderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is Role role)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    // 只在選中新角色時才設定（避免清空時觸發）
                    if (role != null)
                    {
                        viewModel.SelectedRole = role;
                    }
                }
            }
        }
    }
}