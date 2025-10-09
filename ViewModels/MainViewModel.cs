using BloodClockTowerScriptEditor.Models;
using BloodClockTowerScriptEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace BloodClockTowerScriptEditor.ViewModels
{
    /// <summary>
    /// 主視窗視圖模型
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly JsonService _jsonService;

        [ObservableProperty]
        private Script currentScript = new();

        [ObservableProperty]
        private Role? selectedRole;

        [ObservableProperty]
        private string statusMessage = "就緒";

        [ObservableProperty]
        private string currentFilePath = string.Empty;

        // 篩選陣營
        [ObservableProperty]
        private bool showTownsfolk = true;

        [ObservableProperty]
        private bool showOutsiders = true;

        [ObservableProperty]
        private bool showMinions = true;

        [ObservableProperty]
        private bool showDemons = true;

        [ObservableProperty]
        private bool showTravelers = true;

        [ObservableProperty]
        private bool showFabled = true;

        // 篩選後的角色列表
        public ObservableCollection<Role> FilteredRoles { get; } = new();

        public MainViewModel()
        {
            _jsonService = new JsonService();
            
            // 監聽篩選條件變化
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName?.StartsWith("Show") == true)
                {
                    UpdateFilteredRoles();
                }
            };
        }

        // 當劇本變更時更新列表
        partial void OnCurrentScriptChanged(Script value)
        {
            UpdateFilteredRoles();
        }

        /// <summary>
        /// 載入 JSON 檔案
        /// </summary>
        [RelayCommand]
        private void LoadJson()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON 檔案 (*.json)|*.json|所有檔案 (*.*)|*.*",
                    Title = "選擇劇本檔案"
                };

                if (dialog.ShowDialog() == true)
                {
                    CurrentScript = _jsonService.LoadScript(dialog.FileName);
                    CurrentFilePath = dialog.FileName;
                    UpdateFilteredRoles();
                    StatusMessage = $"已載入: {CurrentScript.Meta.Name} (共 {CurrentScript.TotalRoleCount} 個角色)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入失敗:\n{ex.Message}", "錯誤", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "載入失敗";
            }
        }

        /// <summary>
        /// 儲存 JSON 檔案
        /// </summary>
        [RelayCommand]
        private void SaveJson()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilePath))
                {
                    SaveAsJson();
                    return;
                }

                _jsonService.SaveScript(CurrentScript, CurrentFilePath);
                StatusMessage = $"已儲存: {CurrentScript.Meta.Name}";
                MessageBox.Show("儲存成功!", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存失敗:\n{ex.Message}", "錯誤", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "儲存失敗";
            }
        }

        /// <summary>
        /// 另存新檔
        /// </summary>
        [RelayCommand]
        private void SaveAsJson()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON 檔案 (*.json)|*.json",
                    Title = "儲存劇本",
                    FileName = CurrentScript.Meta.Name + ".json"
                };

                if (dialog.ShowDialog() == true)
                {
                    _jsonService.SaveScript(CurrentScript, dialog.FileName);
                    CurrentFilePath = dialog.FileName;
                    StatusMessage = $"已儲存: {CurrentScript.Meta.Name}";
                    MessageBox.Show("儲存成功!", "提示", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"儲存失敗:\n{ex.Message}", "錯誤", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "儲存失敗";
            }
        }

        /// <summary>
        /// 更新篩選後的角色列表
        /// </summary>
        private void UpdateFilteredRoles()
        {
            FilteredRoles.Clear();

            foreach (var role in CurrentScript.Roles)
            {
                bool shouldShow = role.Team switch
                {
                    TeamType.Townsfolk => ShowTownsfolk,
                    TeamType.Outsider => ShowOutsiders,
                    TeamType.Minion => ShowMinions,
                    TeamType.Demon => ShowDemons,
                    TeamType.Traveler => ShowTravelers,
                    TeamType.Fabled => ShowFabled,
                    _ => true
                };

                if (shouldShow)
                {
                    FilteredRoles.Add(role);
                }
            }
        }
    }
}