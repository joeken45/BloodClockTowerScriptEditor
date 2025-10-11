using BloodClockTowerScriptEditor.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace BloodClockTowerScriptEditor.Services
{
    /// <summary>
    /// 角色範本資料庫上下文
    /// </summary>
    public class RoleTemplateContext : DbContext
    {
        /// <summary>
        /// 角色範本資料表
        /// </summary>
        public DbSet<RoleTemplate> RoleTemplates { get; set; } = null!;

        /// <summary>
        /// 提示標記資料表
        /// </summary>
        public DbSet<RoleReminder> RoleReminders { get; set; } = null!;

        /// <summary>
        /// 資料庫檔案路徑
        /// </summary>
        public static string DatabasePath
        {
            get
            {
                string appDataPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Data"
                );

                // 確保目錄存在
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                return Path.Combine(appDataPath, "RoleTemplates.db");
            }
        }

        /// <summary>
        /// 設定資料庫連線
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
            }
        }

        /// <summary>
        /// 設定模型
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 設定 RoleTemplate
            modelBuilder.Entity<RoleTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Team);
                entity.HasIndex(e => e.Edition);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsOfficial);
            });

            // 設定 RoleReminder
            modelBuilder.Entity<RoleReminder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RoleId);

                // 設定外鍵關係
                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Reminders)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// 確保資料庫已建立
        /// </summary>
        public static void EnsureDatabaseCreated()
        {
            using var context = new RoleTemplateContext();
            context.Database.EnsureCreated();
        }
    }
}