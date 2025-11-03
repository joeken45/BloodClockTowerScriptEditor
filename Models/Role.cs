using BloodClockTowerScriptEditor.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 角色資料模型
    /// </summary>
    public class Role : ObservableObject
    {
        // ==================== 第一部分：私有欄位 ====================
        // 按照 JSON 屬性的順序排列
        private string _id = string.Empty;
        private string _name = string.Empty;
        private TeamType _team = TeamType.Townsfolk;
        private string _ability = string.Empty;
        private List<string> _image = new();
        private string? _edition;
        private bool _setup;
        private double _firstNight;
        private double _otherNight;
        private ObservableCollection<ReminderItem> _reminders = new();
        private ObservableCollection<ReminderItem> _remindersGlobal = new();
        private string? _flavor;
        private string? _firstNightReminder;
        private string? _otherNightReminder;
        private List<JinxInfo>? _jinxes;
        private ObservableCollection<SpecialAbility>? _special;
        private string? _officialId;
        private bool _useOfficialId;

        // UI 相關私有欄位
        private int _displayOrder;
        private ObservableCollection<ImageItem>? _imageItems;
        private ObservableCollection<JinxItem>? _jinxItems;

        // ==================== 第二部分：事件 ====================

        /// <summary>
        /// 🆕 類型變更事件（用於通知 ViewModel 重新排序）
        /// </summary>
        public event System.EventHandler? TeamChanged;

        /// <summary>
        /// 🆕 夜晚順序變更事件（用於通知 ViewModel 重新排序）
        /// </summary>
        public event System.EventHandler? NightOrderChanged;

        // ==================== 第三部分：JSON 序列化屬性（BOTC 官方格式）====================
        // 按照 BOTC 官方 JSON schema 的順序排列

        [JsonProperty("id")]
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [JsonProperty("team")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TeamType Team
        {
            get => _team;
            set
            {
                if (SetProperty(ref _team, value))
                {
                    OnPropertyChanged(nameof(TeamDisplayName));
                    TeamChanged?.Invoke(this, EventArgs.Empty);

                    // 🆕 如果切換到相剋角色，通知 JinxRole1Name/2Name 更新
                    if (value == TeamType.Jinxed)
                    {
                        OnPropertyChanged(nameof(JinxRole1Name));
                        OnPropertyChanged(nameof(JinxRole2Name));
                    }
                }
            }
        }

        [JsonProperty("ability")]
        public string Ability
        {
            get => _ability;
            set => SetProperty(ref _ability, value);
        }

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ImageConverter))]
        public List<string> Image
        {
            get => _image;
            set => SetProperty(ref _image, value ?? new());
        }

        [JsonProperty("edition", NullValueHandling = NullValueHandling.Ignore)]
        public string? Edition
        {
            get => _edition;
            set => SetProperty(ref _edition, value);
        }

        [JsonProperty("setup")]
        public bool Setup
        {
            get => _setup;
            set => SetProperty(ref _setup, value);
        }

        [JsonProperty("firstNight")]
        public double FirstNight
        {
            get => _firstNight;
            set
            {
                if (SetProperty(ref _firstNight, value))
                {
                    // 🆕 夜晚順序變更時通知 ViewModel
                    NightOrderChanged?.Invoke(this, System.EventArgs.Empty);
                    OnPropertyChanged(nameof(NightOrderDisplay));
                }
            }
        }

        [JsonProperty("otherNight")]
        public double OtherNight
        {
            get => _otherNight;
            set
            {
                if (SetProperty(ref _otherNight, value))
                {
                    // 🆕 夜晚順序變更時通知 ViewModel
                    NightOrderChanged?.Invoke(this, System.EventArgs.Empty);
                    OnPropertyChanged(nameof(NightOrderDisplay));
                }
            }
        }

        [JsonProperty("reminders")]
        [JsonConverter(typeof(ReminderItemListConverter))]
        public ObservableCollection<ReminderItem> Reminders
        {
            get => _reminders;
            set => SetProperty(ref _reminders, value);
        }

        [JsonProperty("remindersGlobal")]
        [JsonConverter(typeof(ReminderItemListConverter))]
        public ObservableCollection<ReminderItem> RemindersGlobal
        {
            get => _remindersGlobal;
            set => SetProperty(ref _remindersGlobal, value);
        }

        [JsonProperty("flavor", NullValueHandling = NullValueHandling.Ignore)]
        public string? Flavor
        {
            get => _flavor;
            set => SetProperty(ref _flavor, value);
        }

        [JsonProperty("firstNightReminder", NullValueHandling = NullValueHandling.Ignore)]
        public string? FirstNightReminder
        {
            get => _firstNightReminder;
            set => SetProperty(ref _firstNightReminder, value);
        }

        [JsonProperty("otherNightReminder", NullValueHandling = NullValueHandling.Ignore)]
        public string? OtherNightReminder
        {
            get => _otherNightReminder;
            set => SetProperty(ref _otherNightReminder, value);
        }

        [JsonProperty("jinxes", NullValueHandling = NullValueHandling.Ignore)]
        public List<JinxInfo>? Jinxes
        {
            get => _jinxes;
            set => SetProperty(ref _jinxes, value);
        }

        [JsonProperty("special")]
        public ObservableCollection<SpecialAbility>? Special
        {
            get => _special;
            set => SetProperty(ref _special, value);
        }

        // ==================== 第四部分：UI 顯示屬性（不序列化）====================

        /// <summary>
        /// 顯示順序（用於同類型內的自訂排序）
        /// 不序列化到 JSON，只在記憶體中使用
        /// </summary>
        [JsonIgnore]
        public int DisplayOrder
        {
            get => _displayOrder;
            set => SetProperty(ref _displayOrder, value);
        }

        [JsonIgnore]
        public string TeamDisplayName => Team switch
        {
            TeamType.Townsfolk => "鎮民",
            TeamType.Outsider => "外來者",
            TeamType.Minion => "爪牙",
            TeamType.Demon => "惡魔",
            TeamType.Traveler => "旅行者",
            TeamType.Fabled => "傳奇",
            TeamType.Jinxed => "相剋",
            _ => "未知"
        };

        [JsonIgnore]
        public string NightOrderDisplay
        {
            get
            {
                if (FirstNight > 0 && OtherNight > 0)
                    return $"首夜:{FirstNight} | 其他:{OtherNight}";
                if (FirstNight > 0)
                    return $"首夜:{FirstNight}";
                if (OtherNight > 0)
                    return $"其他夜晚:{OtherNight}";
                return "不行動";
            }
        }

        [JsonIgnore]
        public string? ImageUrl => Image.Count > 0 ? Image[0] : null;

        /// <summary>
        /// 官方角色 ID（如 "washerwoman"）
        /// </summary>
        [JsonIgnore]
        public string? OfficialId
        {
            get => _officialId;
            set
            {
                if (SetProperty(ref _officialId, value))
                {
                    OnPropertyChanged(nameof(ShowOfficialIdCheckBox));
                }
            }
        }

        /// <summary>
        /// 是否使用官方 ID（勾選後簡化輸出）
        /// </summary>
        [JsonIgnore]
        public bool UseOfficialId
        {
            get => _useOfficialId;
            set
            {
                if (SetProperty(ref _useOfficialId, value))
                {
                    OnPropertyChanged(nameof(ShowDetailFields));
                    OnPropertyChanged(nameof(IsNameReadOnly));
                }
            }
        }

        /// <summary>
        /// 是否顯示「使用官方 ID」CheckBox
        /// </summary>
        [JsonIgnore]
        public bool ShowOfficialIdCheckBox => !string.IsNullOrEmpty(OfficialId);

        /// <summary>
        /// 是否顯示詳細編輯欄位
        /// </summary>
        [JsonIgnore]
        public bool ShowDetailFields => !UseOfficialId;

        /// <summary>
        /// 角色名稱是否唯讀
        /// </summary>
        [JsonIgnore]
        public bool IsNameReadOnly => UseOfficialId;

        // ==================== 第五部分：圖片管理（Image ↔ ImageItems 雙向同步）====================

        /// <summary>
        /// UI 綁定用的圖片列表（支援雙向更新）
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<ImageItem> ImageItems
        {
            get
            {
                if (_imageItems == null)
                {
                    _imageItems = new ObservableCollection<ImageItem>();

                    // 從 Image 初始化
                    foreach (var url in Image)
                    {
                        var item = new ImageItem(url);
                        item.PropertyChanged += OnImageItemChanged;
                        _imageItems.Add(item);
                    }

                    // 監聽集合變更
                    _imageItems.CollectionChanged += OnImageItemsCollectionChanged;
                }
                return _imageItems;
            }
        }

        /// <summary>
        /// ImageItem 的 Url 屬性變更時同步回 Image
        /// </summary>
        private void OnImageItemChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ImageItem item && e.PropertyName == nameof(ImageItem.Url))
            {
                int index = _imageItems!.IndexOf(item);
                if (index >= 0 && index < Image.Count)
                {
                    Image[index] = item.Url;

                    // ✅ Bug Fix: 當修改第一張圖片時，通知 ImageUrl 屬性變更
                    if (index == 0)
                    {
                        OnPropertyChanged(nameof(ImageUrl));
                    }
                }
            }
        }

        /// <summary>
        /// ImageItems 集合變更時同步回 Image
        /// </summary>
        private void OnImageItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 新增項目
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (ImageItem item in e.NewItems)
                {
                    item.PropertyChanged += OnImageItemChanged;
                    Image.Add(item.Url);
                }
            }
            // 移除項目
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (ImageItem item in e.OldItems)
                {
                    item.PropertyChanged -= OnImageItemChanged;
                    Image.Remove(item.Url);
                }
            }
        }

        // ==================== 第六部分：相剋規則管理（Jinxes ↔ JinxItems 雙向同步）====================

        /// <summary>
        /// UI 綁定用的 Jinx 列表（支援雙向更新）
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<JinxItem> JinxItems
        {
            get
            {
                if (_jinxItems == null)
                {
                    _jinxItems = new ObservableCollection<JinxItem>();

                    // 從 Jinxes 初始化
                    if (Jinxes != null)
                    {
                        foreach (var jinx in Jinxes)
                        {
                            // 透過 ID 查找角色名稱
                            // 注意: 這裡需要從 Script 取得所有角色來查找名稱
                            // 暫時先用 ID 作為名稱，之後再補完
                            var item = new JinxItem(jinx.Id, jinx.Reason);
                            item.PropertyChanged += OnJinxItemChanged;
                            _jinxItems.Add(item);
                        }
                    }

                    // 監聽集合變更
                    _jinxItems.CollectionChanged += OnJinxItemsCollectionChanged;
                }
                return _jinxItems;
            }
        }

        /// <summary>
        /// JinxItem 的屬性變更時同步回 Jinxes
        /// </summary>
        private void OnJinxItemChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is JinxItem item)
            {
                int index = _jinxItems!.IndexOf(item);
                if (Jinxes != null && index >= 0 && index < Jinxes.Count)
                {
                    // 🔴 修正：處理所有屬性變更
                    if (e.PropertyName == nameof(JinxItem.TargetRoleId))
                    {
                        Jinxes[index].Id = item.TargetRoleId;
                    }
                    else if (e.PropertyName == nameof(JinxItem.Reason))
                    {
                        Jinxes[index].Reason = item.Reason;
                    }
                }
            }
        }

        /// <summary>
        /// JinxItems 集合變更時同步回 Jinxes
        /// </summary>
        private void OnJinxItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 同步回 Jinxes 列表
            Jinxes = _jinxItems?.Count > 0
                ? _jinxItems.Select(item => new JinxInfo
                {
                    Id = item.TargetRoleId,  // 暫時使用名稱，之後需轉換為 ID
                    Reason = item.Reason
                }).ToList()
                : null;

            OnPropertyChanged(nameof(Jinxes));
        }

        /// <summary>
        /// 強制將 JinxItems 同步到 Jinxes（用於手動觸發同步）
        /// </summary>
        public void SyncJinxItemsToJinxes()
        {
            if (_jinxItems == null || _jinxItems.Count == 0)
            {
                Jinxes = null;
                return;
            }

            Jinxes = _jinxItems.Select(item => new JinxInfo
            {
                Id = item.TargetRoleId,
                Reason = item.Reason
            }).ToList();
        }

        /// <summary>
        /// 更新 JinxItems 中指定目標的 Reason（如果 JinxItems 已初始化）
        /// </summary>
        public void UpdateJinxItemReason(string targetRoleId, string newReason)
        {
            System.Diagnostics.Debug.WriteLine($"🔍 UpdateJinxItemReason 被呼叫: Role={this.Name}, TargetId={targetRoleId}, NewReason={newReason}");
            System.Diagnostics.Debug.WriteLine($"🔍 _jinxItems 是否為 null: {_jinxItems == null}");

            if (_jinxItems != null)
            {
                System.Diagnostics.Debug.WriteLine($"🔍 _jinxItems 數量: {_jinxItems.Count}");

                var item = _jinxItems.FirstOrDefault(ji => ji.TargetRoleId == targetRoleId);

                if (item != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ 找到 JinxItem，舊值={item.Reason}");
                    item.Reason = newReason;
                    System.Diagnostics.Debug.WriteLine($"✅ JinxItem 已更新，新值={item.Reason}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ 找不到 JinxItem (TargetRoleId={targetRoleId})");

                    // 列出所有 JinxItems 的 TargetRoleId
                    foreach (var ji in _jinxItems)
                    {
                        System.Diagnostics.Debug.WriteLine($"   - JinxItem: TargetRoleId={ji.TargetRoleId}");
                    }
                }
            }
        }

        /// <summary>
        /// 移除 JinxItems 中指定目標的項目（如果 JinxItems 已初始化）
        /// </summary>
        public void RemoveJinxItem(string targetRoleId)
        {
            if (_jinxItems != null)
            {
                var item = _jinxItems.FirstOrDefault(ji => ji.TargetRoleId == targetRoleId);
                if (item != null)
                {
                    _jinxItems.Remove(item);
                    System.Diagnostics.Debug.WriteLine($"🗑️ RemoveJinxItem: 從 {this.Name} 移除目標 {targetRoleId}");
                }
            }
        }

        /// <summary>
        /// 移除目標角色為空的 Jinx 項目
        /// </summary>
        public void RemoveEmptyJinxItems()
        {
            if (_jinxItems != null)
            {
                // 找出所有空值項目
                var emptyItems = _jinxItems
                    .Where(ji => string.IsNullOrEmpty(ji.TargetRoleId))
                    .ToList();

                if (emptyItems.Any())
                {
                    foreach (var item in emptyItems)
                    {
                        _jinxItems.Remove(item);
                    }

                    // 同步到 Jinxes
                    SyncJinxItemsToJinxes();

                    System.Diagnostics.Debug.WriteLine($"✅ 已移除 {emptyItems.Count} 個空值 Jinx");
                }
            }
        }

        /// <summary>
        /// 新增或更新 JinxItem（如果 JinxItems 已初始化）
        /// </summary>
        public void AddOrUpdateJinxItem(string targetRoleId, string reason)
        {
            if (_jinxItems != null)
            {
                var existing = _jinxItems.FirstOrDefault(ji => ji.TargetRoleId == targetRoleId);
                if (existing != null)
                {
                    existing.Reason = reason;
                    System.Diagnostics.Debug.WriteLine($"🔄 UpdateJinxItem: {this.Name} 更新目標 {targetRoleId}");
                }
                else
                {
                    var newItem = new JinxItem(targetRoleId, reason);
                    _jinxItems.Add(newItem);
                    System.Diagnostics.Debug.WriteLine($"➕ AddJinxItem: {this.Name} 新增目標 {targetRoleId}");
                }
            }
        }

        /// <summary>
        /// 檢查 JinxItems 是否已初始化
        /// </summary>
        [JsonIgnore]
        public bool IsJinxItemsInitialized => _jinxItems != null;

        // ==================== 第七部分：集石格式專用屬性 ====================

        /// <summary>
        /// 相剋角色1的名稱 (UI 綁定用)
        /// </summary>
        [JsonIgnore]
        public string JinxRole1Name
        {
            get
            {
                if (Team != TeamType.Jinxed || string.IsNullOrEmpty(Name))
                    return string.Empty;

                // 從 Name 解析: "方古&紅唇女郎" → "方古"
                var parts = Name.Split('&');
                return parts.Length > 0 ? parts[0] : string.Empty;
            }
            set
            {
                UpdateJinxRoleNames(value, JinxRole2Name);
            }
        }

        /// <summary>
        /// 相剋角色2的名稱 (UI 綁定用)
        /// </summary>
        [JsonIgnore]
        public string JinxRole2Name
        {
            get
            {
                if (Team != TeamType.Jinxed || string.IsNullOrEmpty(Name))
                    return string.Empty;

                // 從 Name 解析: "方古&紅唇女郎" → "紅唇女郎"
                var parts = Name.Split('&');
                return parts.Length > 1 ? parts[1] : string.Empty;
            }
            set
            {
                UpdateJinxRoleNames(JinxRole1Name, value);
            }
        }

        /// <summary>
        /// 更新相剋角色名稱並自動生成 Name
        /// </summary>
        private void UpdateJinxRoleNames(string role1Name, string role2Name)
        {
            // 🔴 加入防護：只有相剋角色才處理
            if (Team != TeamType.Jinxed)
                return;

            // 組合成完整的 Name
            if (string.IsNullOrEmpty(role1Name) && string.IsNullOrEmpty(role2Name))
            {
                Name = string.Empty;
            }
            else
            {
                Name = $"{role1Name}&{role2Name}";
            }

            OnPropertyChanged(nameof(JinxRole1Name));
            OnPropertyChanged(nameof(JinxRole2Name));
            OnPropertyChanged(nameof(Name));
        }

        // ==================== 第八部分：內部類別定義 ====================

        /// <summary>
        /// 相剋規則 (BOTC 專用)
        /// </summary>
        public class JinxInfo
        {
            [JsonProperty("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("reason")]
            public string Reason { get; set; } = string.Empty;
        }

        /// <summary>
        /// 特殊能力 (BOTC 專用)
        /// </summary>

        /// <summary>
        /// 特殊功能定義
        /// </summary>
        public class SpecialAbility
        {
            [JsonProperty("type")]
            public string Type { get; set; } = string.Empty;

            [JsonProperty("name")]
            public string Name { get; set; } = string.Empty;

            [JsonProperty("value")]
            public object? Value { get; set; }

            [JsonProperty("time")]
            public string? Time { get; set; }

            [JsonProperty("global")]
            public string? Global { get; set; }
        }

    }
}