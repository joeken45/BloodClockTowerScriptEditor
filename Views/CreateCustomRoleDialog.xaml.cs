using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BloodClockTowerScriptEditor.Views
{
    public partial class CreateCustomRoleDialog : Window
    {
        private RoleTemplate? _editingRole; // 如果是編輯模式，這裡會有值

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
        }

        /// <summary>
        /// 編輯模式
        /// </summary>
        public CreateCustomRoleDialog(RoleTemplate roleToEdit)
        {
            InitializeComponent();
            Title = "編輯自訂角色";
            _editingRole = roleToEdit;

            // 載入現有資料
            LoadRoleData(roleToEdit);
        }

        /// <summary>
        /// 載入角色資料到表單（編輯模式）
        /// </summary>
        private void LoadRoleData(RoleTemplate role)
        {
            txtName.Text = role.Name;
            txtId.Text = role.Id;
            txtId.IsEnabled = false; // 編輯時不允許修改 ID
            txtNameEng.Text = role.NameEng ?? "";
            txtAbility.Text = role.Ability ?? "";
            txtImage.Text = role.Image ?? "";
            txtEdition.Text = role.Edition ?? "custom";
            txtFirstNight.Text = role.FirstNight.ToString();
            txtOtherNight.Text = role.OtherNight.ToString();
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

            ValidateForm(null, null);
        }

        /// <summary>
        /// 驗證表單
        /// </summary>
        private void ValidateForm(object? sender, EventArgs? e)
        {
            // 防止在初始化時 btnSave 還沒建立
            if (btnSave == null)
                return;

            bool isValid = !string.IsNullOrWhiteSpace(txtName.Text) &&
                          !string.IsNullOrWhiteSpace(txtId.Text) &&
                          !string.IsNullOrWhiteSpace(txtAbility.Text) &&
                          cmbTeam.SelectedItem != null;

            btnSave.IsEnabled = isValid;
        }

        /// <summary>
        /// 儲存按鈕
        /// </summary>
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 取得表單資料
                string name = txtName.Text.Trim();
                string id = txtId.Text.Trim();
                string nameEng = txtNameEng.Text.Trim();
                string ability = txtAbility.Text.Trim();
                string image = txtImage.Text.Trim();
                string edition = txtEdition.Text.Trim();
                string team = (cmbTeam.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "townsfolk";

                // 解析數字
                if (!int.TryParse(txtFirstNight.Text, out int firstNight))
                    firstNight = 0;
                if (!int.TryParse(txtOtherNight.Text, out int otherNight))
                    otherNight = 0;

                bool setup = chkSetup.IsChecked ?? false;

                using var context = new RoleTemplateContext();

                if (_editingRole == null)
                {
                    // 新增模式：檢查 ID 是否重複
                    bool idExists = await context.RoleTemplates.AnyAsync(r => r.Id == id);
                    if (idExists)
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
                        Setup = setup,
                        FirstNight = firstNight,
                        OtherNight = otherNight,
                        IsOfficial = false, // 自訂角色
                        Category = "custom",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

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
                    roleToUpdate.Setup = setup;
                    roleToUpdate.FirstNight = firstNight;
                    roleToUpdate.OtherNight = otherNight;
                    roleToUpdate.UpdatedDate = DateTime.Now;

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