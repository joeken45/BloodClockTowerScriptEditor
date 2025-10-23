using BloodClockTowerScriptEditor.Models;
using System.Windows;

namespace BloodClockTowerScriptEditor.Views
{
    public partial class SelectExportFormatDialog : Window
    {
        public ExportFormat SelectedFormat { get; private set; }

        public SelectExportFormatDialog()
        {
            InitializeComponent();
            SelectedFormat = ExportFormat.JiShi; // 預設集石格式
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            SelectedFormat = rbBOTC.IsChecked == true
                ? ExportFormat.BOTC
                : ExportFormat.JiShi;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}