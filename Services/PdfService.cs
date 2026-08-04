using BloodClockTowerScriptEditor.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace BloodClockTowerScriptEditor.Services
{
    public class PdfService
    {
        static PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // 顏色定義
        private static readonly string ColorTownsfolk = "#3d85c8";
        private static readonly string ColorOutsider = "#6fa8dc";
        private static readonly string ColorMinion = "#cc0000";
        private static readonly string ColorDemon = "#990000";
        private static readonly string ColorFabled = "#bf9000";
        private static readonly string ColorLoric = "#38761d";
        private static readonly string ColorHeader = "#2c2c2c";

        // 傳奇區塊排除的系統角色
        private static readonly string[] FabledExcludeIds =
            { "minioninfo", "demoninfo", "dawn", "dusk" };

        public void ExportScript(Script script, string filePath)
        {
            // 夜順不過濾系統角色
            var firstNightRoles = script.Roles
                .Where(r => r.FirstNight > 0)
                .OrderBy(r => r.FirstNight)
                .ToList();

            var otherNightRoles = script.Roles
                .Where(r => r.OtherNight > 0)
                .OrderBy(r => r.OtherNight)
                .ToList();

            var townsfolk = script.Roles.Where(r => r.Team == TeamType.Townsfolk).ToList();
            var outsiders = script.Roles.Where(r => r.Team == TeamType.Outsider).ToList();
            var minions = script.Roles.Where(r => r.Team == TeamType.Minion).ToList();
            var demons = script.Roles.Where(r => r.Team == TeamType.Demon).ToList();
            var fabled = script.Roles
                .Where(r => r.Team == TeamType.Fabled && !FabledExcludeIds.Contains(r.Id))
                .ToList();
            var loric = script.Roles.Where(r => r.Team == TeamType.Loric).ToList();

            var bootleggerRules = script.Meta.Bootlegger ?? new List<string>();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(12);
                    page.DefaultTextStyle(x => x.FontFamily("Microsoft JhengHei").FontSize(7));

                    page.Content().Column(col =>
                    {
                        // ── 標題區 ──
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().AlignMiddle().Text(script.Meta.Name)
                                .FontSize(20).Bold().FontColor(ColorHeader);

                            if (bootleggerRules.Count > 0)
                            {
                                row.ConstantItem(180).Border(0.5f).BorderColor("#cccccc")
                                    .Padding(4).Column(bc =>
                                    {
                                        bc.Item().Text("私貨商人").Bold().FontSize(7).FontColor(ColorLoric);
                                        foreach (var rule in bootleggerRules)
                                        {
                                            bc.Item().Text($"• {rule}").FontSize(6).FontColor("#555555");
                                        }
                                    });
                            }
                        });

                        if (!string.IsNullOrWhiteSpace(script.Meta.Author))
                        {
                            col.Item().Text($"作者：{script.Meta.Author}")
                                .FontSize(8).FontColor("#666666");
                        }

                        col.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor("#cccccc");

                        // ── 主體三欄：左夜順 | 中角色 | 右夜順 ──
                        col.Item().Row(mainRow =>
                        {
                            // 左：首個夜晚（只顯示圖示）
                            mainRow.ConstantItem(24).Column(nightCol =>
                            {
                                nightCol.Item().Height(90f).Svg(_ => "<svg/>");
                                nightCol.Item().Text("首個\n夜晩").Bold().FontSize(7)
                                    .FontColor(ColorHeader);
                                nightCol.Item().PaddingTop(2).PaddingBottom(3).LineHorizontal(0.5f).LineColor("#aaaaaa");

                                foreach (var role in firstNightRoles)
                                {
                                    nightCol.Item().PaddingBottom(2).Row(r =>
                                    {
                                        RenderRoleIcon(r, role.ImageUrl, 18);
                                    });
                                }
                            });

                            mainRow.ConstantItem(4);

                            // 中：角色列表
                            mainRow.RelativeItem().Column(rolesCol =>
                            {
                                RenderTeamSection(rolesCol, "善良陣營・鎮民", townsfolk, ColorTownsfolk, script);
                                RenderTeamSection(rolesCol, "善良陣營・外來者", outsiders, ColorOutsider, script);
                                RenderTeamSection(rolesCol, "邪惡陣營・爪牙", minions, ColorMinion, script);
                                RenderTeamSection(rolesCol, "邪惡陣營・惡魔", demons, ColorDemon, script);

                                RenderBottomSection(rolesCol, fabled, loric, script.Meta.Status);
                            });

                            mainRow.ConstantItem(4);

                            // 右：其他夜晚（只顯示圖示）
                            mainRow.ConstantItem(24).Column(nightCol =>
                            {
                                nightCol.Item().Height(90f).Svg(_ => "<svg/>");
                                nightCol.Item().Text("其他\n夜晩").Bold().FontSize(7)
                                    .FontColor(ColorHeader);
                                nightCol.Item().PaddingTop(2).PaddingBottom(3).LineHorizontal(0.5f).LineColor("#aaaaaa");

                                foreach (var role in otherNightRoles)
                                {
                                    nightCol.Item().PaddingBottom(2).Row(r =>
                                    {
                                        RenderRoleIcon(r, role.ImageUrl, 18);
                                    });
                                }
                            });
                        });
                    });
                });
            }).GeneratePdf(filePath);
        }

        // ── 角色圖示（新 API）──
        private static void RenderRoleIcon(RowDescriptor r, string? imageUrl, float size = 22)
        {
            var bytes = LoadImageBytes(imageUrl);
            r.ConstantItem(size).Height(size).Image(bytes).FitArea();
        }

        private void RenderTeamSection(ColumnDescriptor col, string header,
            List<Role> roles, string color, Script script)
        {
            if (roles.Count == 0) return;

            col.Item().PaddingTop(4).Text(header).Bold().FontSize(12f).FontColor(color);
            col.Item().PaddingBottom(2).LineHorizontal(0.5f).LineColor(color);

            // 左欄先排完再排右欄
            int leftCount = (roles.Count + 1) / 2;
            var leftRoles = roles.Take(leftCount).ToList();
            var rightRoles = roles.Skip(leftCount).ToList();

            col.Item().Row(twoCol =>
            {
                // 左欄
                twoCol.RelativeItem().Column(leftCol =>
                {
                    foreach (var role in leftRoles)
                        RenderRoleCell(leftCol, role, color, script);
                });

                twoCol.ConstantItem(4);

                // 右欄
                twoCol.RelativeItem().Column(rightCol =>
                {
                    foreach (var role in rightRoles)
                        RenderRoleCell(rightCol, role, color, script);
                });
            });
        }

        private void RenderRoleCell(ColumnDescriptor col, Role role, string color, Script script)
        {
            col.Item().PaddingBottom(5).Row(r =>
            {
                RenderRoleIcon(r, role.ImageUrl, 33);
                r.RelativeItem().PaddingLeft(3).Column(rc =>
                {
                    rc.Item().Text(role.Name ?? "").Bold().FontSize(10f).FontColor(color);
                    rc.Item().Text(role.Ability ?? "").FontSize(8f).FontColor("#333333");

                    var jinxedRules = script.Roles
                        .Where(r2 => r2.Team == TeamType.Jinxed &&
                                     r2.Name.StartsWith(role.Name + "&") &&
                                     !string.IsNullOrWhiteSpace(r2.Ability))
                        .ToList();

                    foreach (var jinxedRule in jinxedRules)
                    {
                        var targetName = jinxedRule.Name.Substring(role.Name.Length + 1);
                        var targetRole = script.Roles.FirstOrDefault(r2 =>
                            r2.Name == targetName && r2.Team != TeamType.Jinxed);
                        rc.Item().PaddingTop(2f)
                            .Background("#E0E0E0")
                            .PaddingHorizontal(3f).PaddingVertical(2f)
                            .Row(jr =>
                            {
                                RenderRoleIcon(jr, targetRole?.ImageUrl, 14);
                                jr.RelativeItem().PaddingLeft(2f)
                                    .Text($"{targetName}：{jinxedRule.Ability}")
                                    .FontSize(6.5f).FontColor("#555555").Italic();
                            });
                    }
                });
            });
        }

        private void RenderBottomSection(ColumnDescriptor col,
            List<Role> fabled, List<Role> loric, List<StatusInfo> statusList)
        {
            col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor("#cccccc");

            bool hasFabled = fabled.Count > 0;
            bool hasLoric = loric.Count > 0;
            bool hasStatus = statusList?.Count > 0 == true;

            if (!hasFabled && !hasLoric)
            {
                if (hasStatus)
                    RenderStatusList(col, statusList!);
                return;
            }

            col.Item().Row(bottomRow =>
            {
                if (hasFabled)
                {
                    bottomRow.RelativeItem().Column(c =>
                    {
                        c.Item().Text("傳奇角色").Bold().FontSize(7).FontColor(ColorFabled);
                        foreach (var role in fabled)
                        {
                            c.Item().PaddingBottom(2).Row(r =>
                            {
                                RenderRoleIcon(r, role.ImageUrl, 18);
                                r.RelativeItem().PaddingLeft(3).Column(rc =>
                                {
                                    rc.Item().Text(role.Name ?? "").Bold().FontSize(7).FontColor(ColorFabled);
                                    rc.Item().Text(role.Ability ?? "").FontSize(6).FontColor("#333333");
                                });
                            });
                        }
                    });
                }

                if (hasLoric)
                {
                    bottomRow.RelativeItem().Column(c =>
                    {
                        c.Item().Text("奇遇角色").Bold().FontSize(7).FontColor(ColorLoric);
                        foreach (var role in loric)
                        {
                            c.Item().PaddingBottom(2).Row(r =>
                            {
                                RenderRoleIcon(r, role.ImageUrl, 18);
                                r.RelativeItem().PaddingLeft(3).Column(rc =>
                                {
                                    rc.Item().Text(role.Name ?? "").Bold().FontSize(7).FontColor(ColorLoric);
                                    rc.Item().Text(role.Ability ?? "").FontSize(6).FontColor("#333333");
                                });
                            });
                        }
                    });
                }

                if (hasStatus)
                {
                    bottomRow.RelativeItem().Column(c => RenderStatusList(c, statusList!));
                }
            });
        }

        private static void RenderStatusList(ColumnDescriptor col, List<StatusInfo> statusList)
        {
            col.Item().Text("狀態說明").Bold().FontSize(7).FontColor(ColorHeader);
            foreach (var s in statusList)
            {
                col.Item().PaddingBottom(2).Column(c =>
                {
                    c.Item().Text(s.Name ?? "").Bold().FontSize(6.5f).FontColor("#333333");
                    if (!string.IsNullOrWhiteSpace(s.Skill))
                        c.Item().Text(s.Skill).FontSize(6).FontColor("#555555");
                });
            }
        }

        // ── 圖片載入 ──
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
        private static readonly byte[] _fallbackIcon = CreateFallbackIcon();

        private static byte[] LoadImageBytes(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return _fallbackIcon;

            try
            {
                if (url.StartsWith("http://") || url.StartsWith("https://"))
                {
                    var bytes = _http.GetByteArrayAsync(url).Result;
                    if (bytes.Length > 0) return bytes;
                }
                else if (File.Exists(url))
                {
                    return File.ReadAllBytes(url);
                }
            }
            catch { }

            return _fallbackIcon;
        }

        private static byte[] CreateFallbackIcon()
        {
            return Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAHklEQVQ4jWNg" +
                "YGD4z8BAgGMYBaNgFJAOAAD//wMAAxABAwAAAABJRU5ErkJggg==");
        }

        private static bool IsRequiredPhase(string id) =>
            id is "minioninfo" or "demoninfo" or "dawn" or "dusk";
    }
}