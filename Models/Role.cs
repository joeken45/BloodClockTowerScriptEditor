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
        private List<SpecialAbility>? _special;

        // ==================== JSON 序列化屬性 ====================

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
                    TeamChanged?.Invoke(this, EventArgs.Empty); // ✅ 確保有這行
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

        [JsonProperty("special", NullValueHandling = NullValueHandling.Ignore)]
        public List<SpecialAbility>? Special
        {
            get => _special;
            set => SetProperty(ref _special, value);
        }

        // ==================== UI 輔助屬性 (不序列化) ====================

        /// <summary>
        /// 🆕 類型變更事件（用於通知 ViewModel 重新排序）
        /// </summary>
        public event System.EventHandler? TeamChanged;

        /// <summary>
        /// 🆕 夜晚順序變更事件（用於通知 ViewModel 重新排序）
        /// </summary>
        public event System.EventHandler? NightOrderChanged;

        private int _displayOrder;

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

        // ==================== 圖片列表 UI 綁定 ====================

        private ObservableCollection<ImageItem>? _imageItems;

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

        // ==================== Jinx 列表 UI 綁定 ====================

        // ==================== 集石相剋規則專用屬性 ====================

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


        private ObservableCollection<JinxItem>? _jinxItems;

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
                    // 這裡需要將角色名稱轉換回 ID
                    // 暫時先直接使用 TargetRoleName，之後再補完
                    Jinxes[index].Reason = item.Reason;
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
                    Id = item.TargetRoleName,  // 暫時使用名稱，之後需轉換為 ID
                    Reason = item.Reason
                }).ToList()
                : null;

            OnPropertyChanged(nameof(Jinxes));
        }

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
        public class SpecialAbility
        {
            [JsonProperty("name")]
            public string Name { get; set; } = string.Empty;

            [JsonProperty("type")]
            public string Type { get; set; } = string.Empty;

            [JsonProperty("time", NullValueHandling = NullValueHandling.Ignore)]
            public string? Time { get; set; }

            [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
            public string? Value { get; set; }

            [JsonProperty("global", NullValueHandling = NullValueHandling.Ignore)]
            public bool? Global { get; set; }
        }
    }
}