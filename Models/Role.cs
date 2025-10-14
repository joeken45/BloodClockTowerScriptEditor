using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

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
        private string? _image;
        private string? _edition;
        private bool _setup;
        private double _firstNight;
        private double _otherNight;
        private List<string> _reminders = new();
        private List<string> _remindersGlobal = new();
        private string? _nameEng;
        private string? _flavor;
        private string? _firstNightReminder;
        private string? _otherNightReminder;

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
                    // 🆕 類型變更時通知 UI 更新
                    OnPropertyChanged(nameof(TeamDisplayName));

                    // 🆕 觸發自訂事件，通知 ViewModel 需要重新排序
                    TeamChanged?.Invoke(this, System.EventArgs.Empty);
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
        public string? Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
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
            set => SetProperty(ref _firstNight, value);
        }

        [JsonProperty("otherNight")]
        public double OtherNight
        {
            get => _otherNight;
            set => SetProperty(ref _otherNight, value);
        }

        [JsonProperty("reminders")]
        public List<string> Reminders
        {
            get => _reminders;
            set => SetProperty(ref _reminders, value);
        }

        [JsonProperty("remindersGlobal")]
        public List<string> RemindersGlobal
        {
            get => _remindersGlobal;
            set => SetProperty(ref _remindersGlobal, value);
        }

        [JsonProperty("name_eng", NullValueHandling = NullValueHandling.Ignore)]
        public string? NameEng
        {
            get => _nameEng;
            set => SetProperty(ref _nameEng, value);
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

        // ==================== UI 輔助屬性 (不序列化) ====================

        /// <summary>
        /// 🆕 類型變更事件（用於通知 ViewModel 重新排序）
        /// 注意：事件不會被序列化，不需要 JsonIgnore 屬性
        /// </summary>
        public event System.EventHandler? TeamChanged;

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
    }
}