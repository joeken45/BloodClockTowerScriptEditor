using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 角色模型 - 完全手動實作避免序列化衝突
    /// </summary>
    public class Role : ObservableObject
    {
        // ==================== 私有欄位 ====================
        private string _id = string.Empty;
        private string _name = string.Empty;
        private TeamType _team;
        private string _ability = string.Empty;
        private string _image = string.Empty;
        private string _edition = "custom";
        private double _firstNight = 0;
        private double _otherNight = 0;
        private bool _setup = false;
        private List<string> _reminders = new();
        private List<string> _remindersGlobal = new();
        private string? _nameEng;
        private string? _flavor;
        private string? _firstNightReminder;
        private string? _otherNightReminder;

        // ==================== 必填屬性 ====================

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
                    // 當陣營變更時，通知 UI 輔助屬性更新
                    OnPropertyChanged(nameof(TeamDisplayName));
                }
            }
        }

        [JsonProperty("ability")]
        public string Ability
        {
            get => _ability;
            set => SetProperty(ref _ability, value);
        }

        [JsonProperty("image")]
        public string Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        [JsonProperty("edition")]
        public string Edition
        {
            get => _edition;
            set => SetProperty(ref _edition, value);
        }

        [JsonProperty("firstNight")]
        public double FirstNight
        {
            get => _firstNight;
            set
            {
                if (SetProperty(ref _firstNight, value))
                {
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
                    OnPropertyChanged(nameof(NightOrderDisplay));
                }
            }
        }

        [JsonProperty("setup")]
        public bool Setup
        {
            get => _setup;
            set => SetProperty(ref _setup, value);
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

        // ==================== 可選屬性 ====================

        [JsonProperty("name_eng")]
        public string? NameEng
        {
            get => _nameEng;
            set => SetProperty(ref _nameEng, value);
        }

        [JsonProperty("flavor")]
        public string? Flavor
        {
            get => _flavor;
            set => SetProperty(ref _flavor, value);
        }

        [JsonProperty("firstNightReminder")]
        public string? FirstNightReminder
        {
            get => _firstNightReminder;
            set => SetProperty(ref _firstNightReminder, value);
        }

        [JsonProperty("otherNightReminder")]
        public string? OtherNightReminder
        {
            get => _otherNightReminder;
            set => SetProperty(ref _otherNightReminder, value);
        }

        // ==================== UI 輔助屬性 (不序列化) ====================

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