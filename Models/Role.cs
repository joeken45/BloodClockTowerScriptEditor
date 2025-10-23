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