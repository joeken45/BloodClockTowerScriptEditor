using BloodClockTowerScriptEditor.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BloodClockTowerScriptEditor.Views
{
    public partial class StatusDialog : Window
    {
        /// <summary>
        /// 選擇的狀態列表
        /// </summary>
        public List<StatusInfo> SelectedStatuses { get; private set; } = new();

        /// <summary>
        /// 現有的狀態列表（用於檢查重複）
        /// </summary>
        private List<StatusInfo> _existingStatuses;

        public StatusDialog(List<StatusInfo> existingStatuses)
        {
            InitializeComponent();
            _existingStatuses = existingStatuses ?? new List<StatusInfo>();
        }

        /// <summary>
        /// 自訂選項被勾選時啟用輸入框
        /// </summary>
        private void CustomCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (pnlCustom != null)
            {
                pnlCustom.IsEnabled = true;
            }
        }

        /// <summary>
        /// 自訂選項被取消勾選時停用輸入框
        /// </summary>
        private void CustomCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (pnlCustom != null)
            {
                pnlCustom.IsEnabled = false;
            }
        }

        /// <summary>
        /// 確定按鈕
        /// </summary>
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            SelectedStatuses.Clear();
            var duplicates = new List<string>();

            // 檢查醉酒
            if (chkDrunk.IsChecked == true)
            {
                if (_existingStatuses.Any(s => s.Name == "醉酒"))
                {
                    duplicates.Add("醉酒");
                }
                else
                {
                    SelectedStatuses.Add(new StatusInfo
                    {
                        Name = "醉酒",
                        Skill = "通常因善良角色影響而獲得。醉酒玩家會失去能力，訊息角色可能會得知錯誤的訊息，醉酒玩家不會得知自己醉酒。"
                    });
                }
            }

            // 檢查中毒
            if (chkPoisoned.IsChecked == true)
            {
                if (_existingStatuses.Any(s => s.Name == "中毒"))
                {
                    duplicates.Add("中毒");
                }
                else
                {
                    SelectedStatuses.Add(new StatusInfo
                    {
                        Name = "中毒",
                        Skill = "通常因邪惡角色影響而獲得。中毒玩家會失去能力，訊息角色可能會得知錯誤的訊息，中毒玩家不會得知自己中毒。"
                    });
                }
            }

            // 檢查瘋狂
            if (chkInsane.IsChecked == true)
            {
                if (_existingStatuses.Any(s => s.Name == "瘋狂"))
                {
                    duplicates.Add("瘋狂");
                }
                else
                {
                    SelectedStatuses.Add(new StatusInfo
                    {
                        Name = "瘋狂",
                        Skill = "玩家以合理的方式證明指定的內容。"
                    });
                }
            }

            // 檢查活屍
            if (chkZombie.IsChecked == true)
            {
                if (_existingStatuses.Any(s => s.Name == "活屍"))
                {
                    duplicates.Add("活屍");
                }
                else
                {
                    SelectedStatuses.Add(new StatusInfo
                    {
                        Name = "活屍",
                        Skill = "所有人以為你存活，但其實你已經死亡，仍然可以提名與投票並且不消耗遺言票，資訊角色可能得知錯誤的訊息。"
                    });
                }
            }

            // 檢查自訂
            if (chkCustom.IsChecked == true)
            {
                string customName = txtCustomName.Text.Trim();
                string customSkill = txtCustomSkill.Text.Trim();

                if (string.IsNullOrEmpty(customName))
                {
                    MessageBox.Show(
                        "請輸入狀態名稱",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    txtCustomName.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(customSkill))
                {
                    MessageBox.Show(
                        "請輸入狀態說明",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    txtCustomSkill.Focus();
                    return;
                }

                // 檢查自訂名稱是否重複
                if (_existingStatuses.Any(s => s.Name == customName))
                {
                    duplicates.Add(customName);
                }
                else
                {
                    SelectedStatuses.Add(new StatusInfo
                    {
                        Name = customName,
                        Skill = customSkill
                    });
                }
            }

            // 顯示重複警告
            if (duplicates.Count > 0)
            {
                string duplicateNames = string.Join("、", duplicates);
                MessageBox.Show(
                    $"以下狀態已存在，將不會重複新增：\n{duplicateNames}",
                    "重複狀態",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            // 檢查是否至少選擇一個
            if (SelectedStatuses.Count == 0)
            {
                if (duplicates.Count > 0)
                {
                    // 全部都是重複的
                    DialogResult = false;
                    Close();
                }
                else
                {
                    MessageBox.Show(
                        "請至少選擇一個狀態",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                return;
            }

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按鈕
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}