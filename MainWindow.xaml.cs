using BloodClockTowerScriptEditor.ViewModels;
using BloodClockTowerScriptEditor.Services;
using BloodClockTowerScriptEditor.Models;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System;
using System.Windows.Controls;
using System.Windows.Input;

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
    }
}