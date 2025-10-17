using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BloodClockTowerScriptEditor.Views
{
    public partial class CreateCustomRoleDialog : Window
    {
        private RoleTemplate? _editingRole;
        private ObservableCollection<ReminderItem> _reminders = new();
        private ObservableCollection<ReminderItem> _remindersGlobal = new();

        /// <summary>
        /// 建立的角色（供外部取用）
        /// </summary>
        public RoleTemplate? CreatedRole { get; private set; }

        /// <summary>
        /// 新增模式
        /// </summary>
        public CreateCustomRoleDialog()
        {
            InitializeComponent();
            Title = "新增自訂角色";
            _editingRole = null;

            // 綁定標記列表
            remindersList.ItemsSource = _reminders;
            globalRemindersList.ItemsSource = _remindersGlobal;
        }

        /// <summary>
        /// 編輯模式
        /// </summary>
        public CreateCustomRoleDialog(RoleTemplate roleToEdit)
        {
            InitializeComponent();
            Title = "編輯自訂角色";
            _editingRole = roleToEdit;

            // 綁定標記列表
            remindersList.ItemsSource = _reminders;
            globalRemindersList.ItemsSource = _remindersGlobal;

            // 載入現有資料
            LoadRoleData(roleToEdit);
        }

        /// <summary>
        /// 載入角色資料到表單（編輯模式）
        /// </summary>
        private void LoadRoleData(RoleTemplate role)
        {
            txtId.Text = role.Id;
            txtId.IsEnabled = false; // 編輯時不允許修改 ID
            txtName.Text = role.Name;
            txtNameEng.Text = role.NameEng ?? "";
            txtImage.Text = role.Image ?? "";
            txtAbility.Text = role.Ability ?? "";
            txtEdition.Text = role.Edition ?? "custom";
            txtFirstNight.Text = role.FirstNight.ToString();
            txtOtherNight.Text = role.OtherNight.ToString();
            txtFirstNightReminder.Text = role.FirstNightReminder ?? "";
            txtOtherNightReminder.Text = role.OtherNightReminder ?? "";
            txtFlavor.Text = role.Flavor ?? "";
            chkSetup.IsChecked = role.Setup;

            // 設定類型
            string teamLower = role.Team?.ToLower() ?? "townsfolk";
            foreach (ComboBoxItem item in cmbTeam.Items)
            {
                if (item.Tag?.ToString() == teamLower)
                {
                    cmbTeam.SelectedItem = item;
                    break;
                }
            }

            // 載入標記
            _reminders.Clear();
            _remindersGlobal.Clear();

            foreach (var reminder in role.Reminders)
            {
                if (reminder.IsGlobal)
                {
                    _remindersGlobal.Add(new ReminderItem(reminder.ReminderText));
                }
                else
                {
                    _reminders.Add(new ReminderItem(reminder.ReminderText));
                }
            }

            ValidateForm(null, null);
        }

        /// <summary>
        /// 圖片 URL 變更時更新預覽
        /// </summary>
        private void ImageUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            imgPreview.Source = null;

            if (!string.IsNullOrWhiteSpace(txtImage.Text))
            {
                try
                {
                    imgPreview.Source = new BitmapImage(new Uri(txtImage.Text));
                }
                catch
                {
                    // 圖片載入失敗，保持空白
                }
            }
        }

        /// <summary>
        /// 驗證表單
        /// </summary>
        private void ValidateForm(object? sender, EventArgs? e)
        {
            // 檢查控制項是否已初始化
            if (txtId == null || txtName == null || txtAbility == null || btnSave == null)
                return;

            bool isValid = true;

            // 驗證角色 ID
            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                isValid = false;
            }

            // 驗證角色名稱
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                isValid = false;
            }

            // 驗證能力
            if (string.IsNullOrWhiteSpace(txtAbility.Text))
            {
                isValid = false;
            }

            btnSave.IsEnabled = isValid;
        }

        /// <summary>
        /// 統一處理提示標記的新增/刪除
        /// Tag 格式: "Add|Normal" / "Add|Global" / "Remove|Normal" / "Remove|Global"
        /// </summary>
        private void ManageReminder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string tag)
                return;

            var parts = tag.Split('|');
            if (parts.Length != 2) return;

            string action = parts[0];  // "Add" 或 "Remove"
            string type = parts[1];    // "Normal" 或 "Global"

            var collection = type == "Global" ? _remindersGlobal : _reminders;

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

        /// <summary>
        /// 儲存按鈕
        /// </summary>
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string id = txtId.Text.Trim();
                string name = txtName.Text.Trim();
                string nameEng = txtNameEng.Text.Trim();
                string ability = txtAbility.Text.Trim();
                string image = txtImage.Text.Trim();
                string edition = txtEdition.Text.Trim();
                bool setup = chkSetup.IsChecked ?? false;

                // 取得類型
                string team = ((ComboBoxItem)cmbTeam.SelectedItem).Tag?.ToString() ?? "townsfolk";

                // 解析夜晚順序
                if (!int.TryParse(txtFirstNight.Text, out int firstNight))
                {
                    firstNight = 0;
                }

                if (!int.TryParse(txtOtherNight.Text, out int otherNight))
                {
                    otherNight = 0;
                }

                string firstNightReminder = txtFirstNightReminder.Text.Trim();
                string otherNightReminder = txtOtherNightReminder.Text.Trim();
                string flavor = txtFlavor.Text.Trim();

                using var context = new RoleTemplateContext();

                if (_editingRole == null)
                {
                    // 新增模式：檢查 ID 是否重複
                    var existingRole = await context.RoleTemplates
                        .FirstOrDefaultAsync(r => r.Id == id);

                    if (existingRole != null)
                    {
                        MessageBox.Show(
                            $"角色 ID「{id}」已存在，請使用其他 ID。",
                            "ID 重複",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    // 建立新角色
                    CreatedRole = new RoleTemplate
                    {
                        Id = id,
                        Name = name,
                        NameEng = string.IsNullOrEmpty(nameEng) ? null : nameEng,
                        Team = team,
                        Ability = ability,
                        Image = string.IsNullOrEmpty(image) ? null : image,
                        Edition = edition,
                        Flavor = string.IsNullOrEmpty(flavor) ? null : flavor,
                        Setup = setup,
                        FirstNight = firstNight,
                        OtherNight = otherNight,
                        FirstNightReminder = string.IsNullOrEmpty(firstNightReminder) ? null : firstNightReminder,
                        OtherNightReminder = string.IsNullOrEmpty(otherNightReminder) ? null : otherNightReminder,
                        IsOfficial = false,
                        Category = "custom",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    // 加入標記
                    foreach (var reminder in _reminders)
                    {
                        CreatedRole.Reminders.Add(new RoleReminder
                        {
                            RoleId = id,
                            ReminderText = reminder.Text,
                            IsGlobal = false
                        });
                    }

                    foreach (var reminder in _remindersGlobal)
                    {
                        CreatedRole.Reminders.Add(new RoleReminder
                        {
                            RoleId = id,
                            ReminderText = reminder.Text,
                            IsGlobal = true
                        });
                    }

                    context.RoleTemplates.Add(CreatedRole);
                    await context.SaveChangesAsync();

                    MessageBox.Show(
                        $"成功建立自訂角色「{name}」！",
                        "建立成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    // 編輯模式：更新現有角色
                    var roleToUpdate = await context.RoleTemplates
                        .Include(r => r.Reminders)
                        .FirstOrDefaultAsync(r => r.Id == _editingRole.Id);

                    if (roleToUpdate == null)
                    {
                        MessageBox.Show("找不到要編輯的角色", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    roleToUpdate.Name = name;
                    roleToUpdate.NameEng = string.IsNullOrEmpty(nameEng) ? null : nameEng;
                    roleToUpdate.Team = team;
                    roleToUpdate.Ability = ability;
                    roleToUpdate.Image = string.IsNullOrEmpty(image) ? null : image;
                    roleToUpdate.Edition = edition;
                    roleToUpdate.Flavor = string.IsNullOrEmpty(flavor) ? null : flavor;
                    roleToUpdate.Setup = setup;
                    roleToUpdate.FirstNight = firstNight;
                    roleToUpdate.OtherNight = otherNight;
                    roleToUpdate.FirstNightReminder = string.IsNullOrEmpty(firstNightReminder) ? null : firstNightReminder;
                    roleToUpdate.OtherNightReminder = string.IsNullOrEmpty(otherNightReminder) ? null : otherNightReminder;
                    roleToUpdate.UpdatedDate = DateTime.Now;

                    // 更新標記
                    roleToUpdate.Reminders.Clear();

                    foreach (var reminder in _reminders)
                    {
                        roleToUpdate.Reminders.Add(new RoleReminder
                        {
                            RoleId = roleToUpdate.Id,
                            ReminderText = reminder.Text,
                            IsGlobal = false
                        });
                    }

                    foreach (var reminder in _remindersGlobal)
                    {
                        roleToUpdate.Reminders.Add(new RoleReminder
                        {
                            RoleId = roleToUpdate.Id,
                            ReminderText = reminder.Text,
                            IsGlobal = true
                        });
                    }

                    await context.SaveChangesAsync();

                    CreatedRole = roleToUpdate;

                    MessageBox.Show(
                        $"成功更新自訂角色「{name}」！",
                        "更新成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"儲存失敗：\n{ex.Message}",
                    "錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 取消按鈕
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}