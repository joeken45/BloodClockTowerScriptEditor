using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 相剋規則模型 - 儲存在資料庫中
    /// </summary>
    [Table("JinxRules")]
    public class JinxRule
    {
        /// <summary>
        /// 主鍵 (自動遞增)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JinxRuleId { get; set; }

        /// <summary>
        /// 規則 ID (來自 JSON)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 規則名稱 (例如: "方古&紅唇女郎")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 陣營類型 (固定為 "a jinxed")
        /// </summary>
        [MaxLength(50)]
        public string Team { get; set; } = "a jinxed";

        /// <summary>
        /// 相剋規則說明
        /// </summary>
        [Required]
        public string Ability { get; set; } = string.Empty;

        /// <summary>
        /// 圖片 URL
        /// </summary>
        [MaxLength(500)]
        public string? Image { get; set; }

        /// <summary>
        /// 角色 1 名稱 (從 Name 解析)
        /// </summary>
        [MaxLength(100)]
        public string Character1 { get; set; } = string.Empty;

        /// <summary>
        /// 角色 2 名稱 (從 Name 解析)
        /// </summary>
        [MaxLength(100)]
        public string Character2 { get; set; } = string.Empty;

        /// <summary>
        /// 建立日期
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新日期
        /// </summary>
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 從 Name 欄位解析角色名稱 (用 & 分割)
        /// </summary>
        public void ParseCharacterNames()
        {
            if (string.IsNullOrEmpty(Name))
                return;

            var parts = Name.Split('&');
            if (parts.Length == 2)
            {
                Character1 = parts[0].Trim();
                Character2 = parts[1].Trim();
            }
        }

        /// <summary>
        /// 轉換為 Role 物件 (加入劇本時使用)
        /// </summary>
        public Role ToRole()
        {
            return new Role
            {
                Id = this.Id,
                Name = this.Name,
                Team = TeamType.Jinxed,
                Ability = this.Ability,
                Image = string.IsNullOrEmpty(this.Image)
                    ? new List<string>()
                    : new List<string> { this.Image },
                // 相剋規則不需要這些欄位，設為預設值
                Setup = false,
                FirstNight = 0,
                OtherNight = 0,
                Reminders = new System.Collections.ObjectModel.ObservableCollection<ReminderItem>(),
                RemindersGlobal = new System.Collections.ObjectModel.ObservableCollection<ReminderItem>()
            };
        }
    }
}