using System.Runtime.Serialization;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 角色陣營類型
    /// </summary>
    public enum TeamType
    {
        [EnumMember(Value = "townsfolk")]
        Townsfolk,

        [EnumMember(Value = "outsider")]
        Outsider,

        [EnumMember(Value = "minion")]
        Minion,

        [EnumMember(Value = "demon")]
        Demon,

        [EnumMember(Value = "traveler")]
        Traveler,

        [EnumMember(Value = "fabled")]
        Fabled,

        [EnumMember(Value = "a jinxed")]
        Jinxed
    }
}