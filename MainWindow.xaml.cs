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
        }

        /// <summary>
        /// 視窗載入完成後執行
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeDefaultRolesAsync();
        }

        /// <summary>
        /// 初始化預設角色資料（每次啟動都執行）
        /// </summary>
        private async Task InitializeDefaultRolesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 開始同步角色資料...");

                // 取得程式資料夾路徑
                string appFolder = AppDomain.CurrentDomain.BaseDirectory;
                string roleJsonPath = Path.Combine(appFolder, "角色總表.json");

                System.Diagnostics.Debug.WriteLine($"📂 程式資料夾: {appFolder}");
                System.Diagnostics.Debug.WriteLine($"📄 檢查檔案: {roleJsonPath}");

                // 檢查程式資料夾中是否有 角色總表.json
                if (!File.Exists(roleJsonPath))
                {
                    System.Diagnostics.Debug.WriteLine("📥 程式資料夾中沒有角色總表.json，從內嵌資源建立...");

                    // 載入內嵌資源
                    string embeddedContent = LoadEmbeddedResource("BloodClockTowerScriptEditor.Resources.角色總表.json");

                    if (string.IsNullOrEmpty(embeddedContent))
                    {
                        System.Diagnostics.Debug.WriteLine("❌ 無法載入內嵌資源：角色總表.json");
                        return;
                    }

                    // 寫入到程式資料夾
                    await File.WriteAllTextAsync(roleJsonPath, embeddedContent);
                    System.Diagnostics.Debug.WriteLine($"✅ 已建立角色總表.json 到程式資料夾");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✅ 程式資料夾中已有角色總表.json，使用現有檔案");
                }

                // 從程式資料夾匯入資料庫
                var importService = new RoleImportService();
                int importedCount = await importService.ImportFromJsonAsync(roleJsonPath, "官方", true);

                System.Diagnostics.Debug.WriteLine($"✅ 角色資料同步完成！處理了 {importedCount} 個角色");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 同步角色資料失敗：{ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);

                // 靜默失敗，不干擾使用者操作
            }
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
                "版本: 1.0.0 (MVP)\n" +
                "技術: WPF + .NET 8.0 + MVVM\n\n" +
                "功能:\n" +
                "• 載入/儲存 JSON 劇本檔案\n" +
                "• 顯示角色詳細資訊\n" +
                "• 陣營篩選功能\n\n" +
                "開發中...",
                "關於",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        /// <summary>
        /// 🆕 TabControl 切換時清空選擇
        /// </summary>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && e.Source is TabControl)
            {
                // 切換分頁時清空選中的角色
                viewModel.SelectedRole = null;
            }
        }

        /// <summary>
        /// 🆕 夜晚順序中的角色被點擊
        /// </summary>
        private void NightRole_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Role role)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    // 設定選中的角色
                    viewModel.SelectedRole = role;

                    // 除錯輸出
                    System.Diagnostics.Debug.WriteLine($"🖱️ 點擊夜晚順序角色: {role.Name}");
                }
            }
        }

        // 首個夜晚 - 上移
        private void MoveUpFirstNight_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Role role)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    viewModel.MoveRoleUp(role, isFirstNight: true);
                    e.Handled = true; // 防止觸發 Border 的點擊事件
                }
            }
        }

        // 首個夜晚 - 下移
        private void MoveDownFirstNight_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Role role)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    viewModel.MoveRoleDown(role, isFirstNight: true);
                    e.Handled = true;
                }
            }
        }

        // 其他夜晚 - 上移
        private void MoveUpOtherNight_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Role role)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    viewModel.MoveRoleUp(role, isFirstNight: false);
                    e.Handled = true;
                }
            }
        }

        // 其他夜晚 - 下移
        private void MoveDownOtherNight_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Role role)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel != null)
                {
                    viewModel.MoveRoleDown(role, isFirstNight: false);
                    e.Handled = true;
                }
            }
        }
    }
}