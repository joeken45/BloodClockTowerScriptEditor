using BloodClockTowerScriptEditor.Services;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace BloodClockTowerScriptEditor.Views
{
    public partial class ImportRolesWindow : Window
    {
        private readonly RoleImportService _importService;

        public ImportRolesWindow()
        {
            InitializeComponent();
            _importService = new RoleImportService();

            Loaded += ImportRolesWindow_Loaded;
        }

        private async void ImportRolesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDatabaseStatusAsync();
        }

        /// <summary>
        /// 載入資料庫狀態
        /// </summary>
        private async Task LoadDatabaseStatusAsync()
        {
            try
            {
                var count = await _importService.GetRoleCountAsync();
                var stats = await _importService.GetRoleStatisticsAsync();

                string statusText = $"資料庫中共有 {count} 個角色\n\n";
                statusText += "各類型統計：\n";

                foreach (var stat in stats)
                {
                    string teamName = stat.Key switch
                    {
                        "townsfolk" => "鎮民",
                        "outsider" => "外來者",
                        "minion" => "爪牙",
                        "demon" => "惡魔",
                        "traveler" => "旅行者",
                        "fabled" => "傳奇",
                        _ => stat.Key
                    };
                    statusText += $"  • {teamName}：{stat.Value} 個\n";
                }

                txtDatabaseStatus.Text = statusText;
            }
            catch (Exception ex)
            {
                txtDatabaseStatus.Text = $"無法載入資料庫狀態：{ex.Message}";
            }
        }

        /// <summary>
        /// 選擇檔案並匯入
        /// </summary>
        private async void SelectAndImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON 檔案 (*.json)|*.json|文字檔案 (*.txt)|*.txt|所有檔案 (*.*)|*.*",
                Title = "選擇角色總表檔案"
            };

            if (dialog.ShowDialog() == true)
            {
                await ImportFileAsync(dialog.FileName);
            }
        }

        /// <summary>
        /// 執行匯入
        /// </summary>
        private async Task ImportFileAsync(string filePath)
        {
            try
            {
                // 顯示進度條
                progressBar.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true;
                txtStatus.Text = "正在匯入資料，請稍候...";

                // 🆕 先檢查檔案內容
                var fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                var preview = fileContent.Length > 500
                    ? fileContent.Substring(0, 500) + "..."
                    : fileContent;

                System.Diagnostics.Debug.WriteLine("檔案內容預覽：");
                System.Diagnostics.Debug.WriteLine(preview);

                // 執行匯入
                int importedCount = await _importService.ImportFromJsonAsync(filePath, "官方", true);

                // 隱藏進度條
                progressBar.Visibility = Visibility.Collapsed;
                txtStatus.Text = $"匯入完成！成功匯入 {importedCount} 個角色。";

                // 重新載入狀態
                await LoadDatabaseStatusAsync();

                MessageBox.Show(
                    $"成功匯入 {importedCount} 個角色到資料庫！",
                    "匯入成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                progressBar.Visibility = Visibility.Collapsed;
                txtStatus.Text = "匯入失敗";

                // 🆕 顯示完整錯誤訊息
                var errorMessage = $"匯入失敗：\n\n{ex.Message}";

                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\n內部錯誤：\n{ex.InnerException.Message}";
                }

                MessageBox.Show(
                    errorMessage,
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                // 🆕 輸出到除錯視窗
                System.Diagnostics.Debug.WriteLine("完整錯誤：");
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 關閉視窗
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}