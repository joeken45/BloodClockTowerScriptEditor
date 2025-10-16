using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 相剋規則資料庫上下文
    /// </summary>
    public class JinxRuleContext : DbContext
    {
        /// <summary>
        /// 相剋規則表
        /// </summary>
        public DbSet<JinxRule> JinxRules { get; set; }

        /// <summary>
        /// 設定資料庫連線
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // 與 RoleTemplateContext 一樣,放在 Data 資料夾
                string appDataPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Data"
                );

                // 確保目錄存在
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                string dbPath = Path.Combine(appDataPath, "JinxRules.db");
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        /// <summary>
        /// 設定模型
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 設定 JinxRule 的索引
            modelBuilder.Entity<JinxRule>()
                .HasIndex(j => j.Id)
                .IsUnique();

            // 為角色名稱建立索引,加速查詢
            modelBuilder.Entity<JinxRule>()
                .HasIndex(j => new { j.Character1, j.Character2 });
        }

        /// <summary>
        /// 確保資料庫已建立
        /// </summary>
        public static void EnsureDatabaseCreated()
        {
            using var context = new JinxRuleContext();
            context.Database.EnsureCreated();
        }
    }
}