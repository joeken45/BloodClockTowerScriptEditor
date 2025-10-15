using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 角色範本資料庫模型
    /// </summary>
    [Table("RoleTemplates")]
    public class RoleTemplate
    {
        /// <summary>
        /// 角色 ID（主鍵）
        /// </summary>
        [Key]
        [MaxLength(100)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 中文名稱
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 英文名稱
        /// </summary>
        [MaxLength(100)]
        public string? NameEng { get; set; }

        /// <summary>
        /// 角色類型（townsfolk, outsider, minion, demon, traveler, fabled）
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Team { get; set; } = "townsfolk";

        /// <summary>
        /// 能力描述
        /// </summary>
        public string? Ability { get; set; }

        /// <summary>
        /// 圖片 URL
        /// </summary>
        [MaxLength(500)]
        public string? Image { get; set; }

        /// <summary>
        /// 所屬劇本版本
        /// </summary>
        [MaxLength(50)]
        public string? Edition { get; set; }

        /// <summary>
        /// 風味文字/備註
        /// </summary>
        public string? Flavor { get; set; }

        /// <summary>
        /// 是否影響設置
        /// </summary>
        public bool Setup { get; set; }

        /// <summary>
        /// 首個夜晚行動順序（0 表示不行動）
        /// </summary>
        public int FirstNight { get; set; }

        /// <summary>
        /// 其他夜晚行動順序（0 表示不行動）
        /// </summary>
        public int OtherNight { get; set; }

        /// <summary>
        /// 首個夜晚說書人提示
        /// </summary>
        public string? FirstNightReminder { get; set; }

        /// <summary>
        /// 其他夜晚說書人提示
        /// </summary>
        public string? OtherNightReminder { get; set; }

        /// <summary>
        /// 分類標籤（官方/自訂/社群等）
        /// </summary>
        [MaxLength(50)]
        public string? Category { get; set; }

        /// <summary>
        /// 是否為官方角色
        /// </summary>
        public bool IsOfficial { get; set; } = true;

        /// <summary>
        /// UI 用：是否被選中（不儲存到資料庫）
        /// </summary>
        [NotMapped]
        public bool IsSelected { get; set; } = false;

        /// <summary>
        /// UI 用：類型的中文顯示名稱（不儲存到資料庫）
        /// </summary>
        [NotMapped]
        public string TeamDisplayName
        {
            get
            {
                return Team?.ToLower() switch
                {
                    "townsfolk" => "鎮民",
                    "outsider" => "外來者",
                    "minion" => "爪牙",
                    "demon" => "惡魔",
                    "traveler" => "旅行者",
                    "fabled" => "傳奇",
                    _ => "未知"
                };
            }
        }

        /// <summary>
        /// 建立日期
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新日期
        /// </summary>
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 提示標記（導航屬性）
        /// </summary>
        public List<RoleReminder> Reminders { get; set; } = new();

        /// <summary>
        /// 轉換為 Role 模型
        /// </summary>
        public Role ToRole()
        {
            var role = new Role
            {
                Id = this.Id,
                Name = this.Name,
                NameEng = this.NameEng,
                Team = Enum.Parse<TeamType>(this.Team, true),
                Ability = this.Ability ?? string.Empty,
                Image = this.Image,
                Edition = this.Edition,
                Flavor = this.Flavor,
                Setup = this.Setup,
                FirstNight = this.FirstNight,
                OtherNight = this.OtherNight,
                FirstNightReminder = this.FirstNightReminder,
                OtherNightReminder = this.OtherNightReminder
            };

            // 轉換提示標記
            foreach (var reminder in this.Reminders)
            {
                if (reminder.IsGlobal)
                {
                    role.RemindersGlobal.Add(new ReminderItem(reminder.ReminderText));
                }
                else
                {
                    role.Reminders.Add(new ReminderItem(reminder.ReminderText));
                }
            }

            return role;
        }
    }

    /// <summary>
    /// 提示標記資料庫模型
    /// </summary>
    [Table("RoleReminders")]
    public class RoleReminder
    {
        /// <summary>
        /// 提示標記 ID（主鍵）
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 關聯的角色 ID（外鍵）
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string RoleId { get; set; } = string.Empty;

        /// <summary>
        /// 提示標記文字
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string ReminderText { get; set; } = string.Empty;

        /// <summary>
        /// 是否為全局標記
        /// </summary>
        public bool IsGlobal { get; set; }

        /// <summary>
        /// 關聯的角色（導航屬性）
        /// </summary>
        [ForeignKey(nameof(RoleId))]
        public RoleTemplate? Role { get; set; }
    }
}