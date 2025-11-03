using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 劇本元數據 - 完全手動實作避免序列化衝突
    /// </summary>
    public class ScriptMeta : ObservableObject
    {
        // ==================== 私有欄位 ====================
        private string _id = "_meta";
        private string _name = "未命名劇本";
        private string _author = string.Empty;
        private string _description = string.Empty;
        private string _logo = string.Empty;
        private string _townsfolk = "鎮民";
        private string _outsider = "外來者";
        private string _minion = "爪牙";
        private string _demon = "惡魔";
        private string _traveler = "旅行者";
        private string _aJinxed = "相剋規則";
        private List<StatusInfo> _status = new();
        // === BOTC 專用 (選用) ===
        private bool? _hideTitle;
        private string? _background;
        private string? _almanac;
        private List<string>? _bootlegger;
        private List<string>? _firstNight;
        private List<string>? _otherNight;

        // ==================== 公開屬性 ====================

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

        [JsonProperty("author")]
        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        [JsonProperty("description")]
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        [JsonProperty("logo")]
        public string Logo
        {
            get => _logo;
            set => SetProperty(ref _logo, value);
        }

        [JsonProperty("townsfolk")]
        public string Townsfolk
        {
            get => _townsfolk;
            set => SetProperty(ref _townsfolk, value);
        }

        [JsonProperty("outsider")]
        public string Outsider
        {
            get => _outsider;
            set => SetProperty(ref _outsider, value);
        }

        [JsonProperty("minion")]
        public string Minion
        {
            get => _minion;
            set => SetProperty(ref _minion, value);
        }

        [JsonProperty("demon")]
        public string Demon
        {
            get => _demon;
            set => SetProperty(ref _demon, value);
        }

        [JsonProperty("traveler")]
        public string Traveler
        {
            get => _traveler;
            set => SetProperty(ref _traveler, value);
        }

        [JsonProperty("a jinxed")]
        public string AJinxed
        {
            get => _aJinxed;
            set => SetProperty(ref _aJinxed, value);
        }

        [JsonProperty("status")]
        public List<StatusInfo> Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        [JsonProperty("hideTitle", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HideTitle
        {
            get => _hideTitle;
            set => SetProperty(ref _hideTitle, value);
        }

        [JsonProperty("background", NullValueHandling = NullValueHandling.Ignore)]
        public string? Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }

        [JsonProperty("almanac", NullValueHandling = NullValueHandling.Ignore)]
        public string? Almanac
        {
            get => _almanac;
            set => SetProperty(ref _almanac, value);
        }

        [JsonProperty("bootlegger", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? Bootlegger
        {
            get => _bootlegger;
            set => SetProperty(ref _bootlegger, value);
        }

        [JsonProperty("firstNight", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? FirstNight
        {
            get => _firstNight;
            set => SetProperty(ref _firstNight, value);
        }

        [JsonProperty("otherNight", NullValueHandling = NullValueHandling.Ignore)]
        public List<string>? OtherNight
        {
            get => _otherNight;
            set => SetProperty(ref _otherNight, value);
        }
    }

    /// <summary>
    /// 狀態說明 (醉酒/中毒) - 只定義一次
    /// </summary>
    public class StatusInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("skill")]
        public string Skill { get; set; } = string.Empty;
    }
}