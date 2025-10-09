using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 角色模型
    /// </summary>
    public partial class Role : ObservableObject
    {
        [ObservableProperty]
        [JsonProperty("id")]
        private string id = string.Empty;

        [ObservableProperty]
        [JsonProperty("name")]
        private string name = string.Empty;

        [ObservableProperty]
        [JsonProperty("team")]
        [JsonConverter(typeof(StringEnumConverter))]
        private TeamType team;

        [ObservableProperty]
        [JsonProperty("ability")]
        private string ability = string.Empty;

        [ObservableProperty]
        [JsonProperty("image")]
        private string image = string.Empty;

        [ObservableProperty]
        [JsonProperty("edition")]
        private string edition = "custom";

        [ObservableProperty]
        [JsonProperty("firstNight")]
        private double firstNight = 0;

        [ObservableProperty]
        [JsonProperty("otherNight")]
        private double otherNight = 0;

        [ObservableProperty]
        [JsonProperty("setup")]
        private bool setup = false;

        [ObservableProperty]
        [JsonProperty("reminders")]
        private List<string> reminders = new();

        [ObservableProperty]
        [JsonProperty("remindersGlobal")]
        private List<string> remindersGlobal = new();

        // 可選欄位
        [ObservableProperty]
        [JsonProperty("name_eng")]
        private string? nameEng;

        [ObservableProperty]
        [JsonProperty("flavor")]
        private string? flavor;

        [ObservableProperty]
        [JsonProperty("firstNightReminder")]
        private string? firstNightReminder;

        [ObservableProperty]
        [JsonProperty("otherNightReminder")]
        private string? otherNightReminder;

        // UI 輔助屬性 (不序列化)
        [JsonIgnore]
        public string TeamDisplayName => team switch
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
                if (firstNight > 0 && otherNight > 0)
                    return $"首夜:{firstNight} | 其他:{otherNight}";
                if (firstNight > 0)
                    return $"首夜:{firstNight}";
                if (otherNight > 0)
                    return $"其他夜晚:{otherNight}";
                return "不行動";
            }
        }
    }
}