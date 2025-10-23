using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using BloodClockTowerScriptEditor.Models;

namespace BloodClockTowerScriptEditor.Views
{
    public partial class EditScriptMetaWindow : Window
    {
        private ScriptMeta _originalMeta;
        private ObservableCollection<StatusInfoEx> _tempStatusList;

        public EditScriptMetaWindow(ScriptMeta meta)
        {
            InitializeComponent();
            _originalMeta = meta;

            // 複製資料到臨時變數(不直接修改原始資料)
            txtName.Text = meta.Name;
            txtAuthor.Text = meta.Author;
            txtLogo.Text = meta.Logo;

            // 🆕 載入 BOTC 欄位
            chkHideTitle.IsChecked = meta.HideTitle ?? false;
            txtBackground.Text = meta.Background ?? string.Empty;
            txtAlmanac.Text = meta.Almanac ?? string.Empty;

            // 複製狀態列表 (使用擴充類別支援勾選)
            _tempStatusList = new ObservableCollection<StatusInfoEx>();
            foreach (var status in meta.Status)
            {
                _tempStatusList.Add(new StatusInfoEx
                {
                    Name = status.Name,
                    Skill = status.Skill,
                    IsSelected = false
                });
            }
            statusList.ItemsSource = _tempStatusList;

            // 監聽 Logo 網址變化以更新預覽
            txtLogo.TextChanged += (s, e) =>
            {
                imgLogo.Source = null;
                if (!string.IsNullOrWhiteSpace(txtLogo.Text))
                {
                    try
                    {
                        imgLogo.Source = new System.Windows.Media.Imaging.BitmapImage(
                            new System.Uri(txtLogo.Text));
                    }
                    catch { }
                }
            };

            // 初始載入 Logo
            if (!string.IsNullOrWhiteSpace(meta.Logo))
            {
                try
                {
                    imgLogo.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new System.Uri(meta.Logo));
                }
                catch { }
            }
        }

        private void AddStatus_Click(object sender, RoutedEventArgs e)
        {
            _tempStatusList.Add(new StatusInfoEx
            {
                Name = "新狀態",
                Skill = "請輸入說明",
                IsSelected = false
            });
        }

        private void DeleteSelectedStatus_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _tempStatusList.Where(s => s.IsSelected).ToList();

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("請先勾選要刪除的狀態", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"確定要刪除 {selectedItems.Count} 個已勾選的狀態嗎？",
                "確認刪除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var item in selectedItems)
                {
                    _tempStatusList.Remove(item);
                }
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // 確定後才將修改寫回原始資料
            _originalMeta.Name = txtName.Text;
            _originalMeta.Author = txtAuthor.Text;
            _originalMeta.Logo = txtLogo.Text;

            // 🆕 更新 BOTC 欄位
            _originalMeta.HideTitle = chkHideTitle.IsChecked == true ? true : null;
            _originalMeta.Background = string.IsNullOrWhiteSpace(txtBackground.Text) ? null : txtBackground.Text;
            _originalMeta.Almanac = string.IsNullOrWhiteSpace(txtAlmanac.Text) ? null : txtAlmanac.Text;

            // 更新狀態列表
            _originalMeta.Status.Clear();
            foreach (var status in _tempStatusList)
            {
                _originalMeta.Status.Add(new StatusInfo
                {
                    Name = status.Name,
                    Skill = status.Skill
                });
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // 取消就不修改,直接關閉
            DialogResult = false;
            Close();
        }
    }

    // 擴充的 StatusInfo 類別,支援勾選功能
    public class StatusInfoEx
    {
        public string Name { get; set; } = string.Empty;
        public string Skill { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = false;
    }
}