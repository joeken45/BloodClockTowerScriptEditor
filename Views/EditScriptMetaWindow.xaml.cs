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
            txtDescription.Text = meta.Description;

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
            try
            {
                // 將 _tempStatusList 轉換為 StatusInfo 列表傳入
                var existingStatuses = _tempStatusList.Select(s => new StatusInfo
                {
                    Name = s.Name,
                    Skill = s.Skill
                }).ToList();

                var dialog = new StatusDialog(existingStatuses)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true && dialog.SelectedStatuses.Count > 0)
                {
                    foreach (var status in dialog.SelectedStatuses)
                    {
                        _tempStatusList.Add(new StatusInfoEx
                        {
                            Name = status.Name,
                            Skill = status.Skill,
                            IsSelected = false
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"新增狀態失敗：{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void RemoveSelectedStatus_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _tempStatusList.Where(x => x.IsSelected).ToList();

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
            _originalMeta.Description = txtDescription.Text;

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