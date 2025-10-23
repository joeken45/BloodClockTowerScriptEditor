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