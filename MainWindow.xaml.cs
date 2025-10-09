using BloodClockTowerScriptEditor.ViewModels;
using System.Windows;

namespace BloodClockTowerScriptEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
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
    }
}