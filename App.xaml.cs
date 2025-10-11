using BloodClockTowerScriptEditor.Services;
using System.Windows;

namespace BloodClockTowerScriptEditor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化資料庫
            try
            {
                RoleTemplateContext.EnsureDatabaseCreated();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"資料庫初始化失敗：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}