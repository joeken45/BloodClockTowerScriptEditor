using BloodClockTowerScriptEditor.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace BloodClockTowerScriptEditor.Views
{
    /// <summary>
    /// 偵測相剋規則選擇視窗
    /// </summary>
    public partial class SelectJinxRulesDialog : Window
    {
        /// <summary>
        /// 所有偵測到的相剋規則
        /// </summary>
        public ObservableCollection<JinxRuleItem> JinxRules { get; private set; }

        /// <summary>
        /// 使用者選擇的相剋規則
        /// </summary>
        public List<JinxRuleItem> SelectedRules { get; private set; }

        public SelectJinxRulesDialog(ObservableCollection<JinxRuleItem> jinxRules)
        {
            InitializeComponent();

            JinxRules = jinxRules;
            SelectedRules = new List<JinxRuleItem>();

            // 綁定資料
            icJinxRules.ItemsSource = JinxRules;

            // 更新統計資訊
            UpdateStatistics();
        }

        /// <summary>
        /// 更新統計資訊
        /// </summary>
        private void UpdateStatistics()
        {
            int total = JinxRules.Count;
            int existing = JinxRules.Count(r => !r.IsEnabled);
            int available = total - existing;

            txtStatistics.Text = $"偵測到 {total} 個規則，已存在 {existing} 個，可加入 {available} 個";
        }

        /// <summary>
        /// 全選可用的規則
        /// </summary>
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var rule in JinxRules.Where(r => r.IsEnabled))
            {
                rule.IsSelected = true;
            }
        }

        /// <summary>
        /// 取消全選
        /// </summary>
        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var rule in JinxRules)
            {
                rule.IsSelected = false;
            }
        }

        /// <summary>
        /// 確定按鈕
        /// </summary>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // 收集使用者選擇的規則
            SelectedRules = JinxRules.Where(r => r.IsSelected && r.IsEnabled).ToList();

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