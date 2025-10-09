using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 劇本元數據
    /// </summary>
    public partial class ScriptMeta : ObservableObject
    {
        [ObservableProperty]
        [JsonProperty("id")]
        private string id = "_meta";

        [ObservableProperty]
        [JsonProperty("name")]
        private string name = "未命名劇本";

        [ObservableProperty]
        [JsonProperty("author")]
        private string author = string.Empty;

        [ObservableProperty]
        [JsonProperty("description")]
        private string description = string.Empty;

        [ObservableProperty]
        [JsonProperty("logo")]
        private string logo = string.Empty;

        [ObservableProperty]
        [JsonProperty("townsfolkName")]
        private string townsfolkName = "鎮民";

        [ObservableProperty]
        [JsonProperty("outsidersName")]
        private string outsidersName = "外來者";

        [ObservableProperty]
        [JsonProperty("minionsName")]
        private string minionsName = "爪牙";

        [ObservableProperty]
        [JsonProperty("demonsName")]
        private string demonsName = "惡魔";

        [ObservableProperty]
        [JsonProperty("townsfolk")]
        private string townsfolk = "鎮民";

        [ObservableProperty]
        [JsonProperty("outsider")]
        private string outsider = "外來者";

        [ObservableProperty]
        [JsonProperty("minion")]
        private string minion = "爪牙";

        [ObservableProperty]
        [JsonProperty("demon")]
        private string demon = "惡魔";

        [ObservableProperty]
        [JsonProperty("traveler")]
        private string traveler = "旅行者";

        [ObservableProperty]
        [JsonProperty("a jinxed")]
        private string aJinxed = "相剋規則";

        [ObservableProperty]
        [JsonProperty("a jinxedName")]
        private string? aJinxedName;

        [ObservableProperty]
        [JsonProperty("status")]
        private List<StatusInfo> status = new();
    }

    /// <summary>
    /// 狀態說明 (醉酒/中毒)
    /// </summary>
    public class StatusInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("skill")]
        public string Skill { get; set; } = string.Empty;
    }
}