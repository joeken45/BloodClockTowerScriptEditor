using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 劇本根模型
    /// </summary>
    public partial class Script : ObservableObject
    {
        [ObservableProperty]
        private ScriptMeta meta = new();

        [ObservableProperty]
        private ObservableCollection<Role> roles = new();

        // 按陣營分類 (唯讀屬性)
        public IEnumerable<Role> Townsfolk => Roles.Where(r => r.Team == TeamType.Townsfolk);
        public IEnumerable<Role> Outsiders => Roles.Where(r => r.Team == TeamType.Outsider);
        public IEnumerable<Role> Minions => Roles.Where(r => r.Team == TeamType.Minion);
        public IEnumerable<Role> Demons => Roles.Where(r => r.Team == TeamType.Demon);
        public IEnumerable<Role> Travelers => Roles.Where(r => r.Team == TeamType.Traveler);
        public IEnumerable<Role> Fabled => Roles.Where(r => r.Team == TeamType.Fabled);
        public IEnumerable<Role> Jinxes => Roles.Where(r => r.Team == TeamType.Jinxed);

        // 統計資訊
        public int TotalRoleCount => Roles.Count;
        public int TownsfolkCount => Townsfolk.Count();
        public int OutsiderCount => Outsiders.Count();
        public int MinionCount => Minions.Count();
        public int DemonCount => Demons.Count();
        public int TravelerCount => Travelers.Count();

        /// <summary>
        /// 當角色集合變更時,通知所有分類屬性更新
        /// </summary>
        partial void OnRolesChanged(ObservableCollection<Role> value)
        {
            if (value != null)
            {
                value.CollectionChanged += (s, e) =>
                {
                    OnPropertyChanged(nameof(Townsfolk));
                    OnPropertyChanged(nameof(Outsiders));
                    OnPropertyChanged(nameof(Minions));
                    OnPropertyChanged(nameof(Demons));
                    OnPropertyChanged(nameof(Travelers));
                    OnPropertyChanged(nameof(Fabled));
                    OnPropertyChanged(nameof(Jinxes));
                    OnPropertyChanged(nameof(TotalRoleCount));
                    OnPropertyChanged(nameof(TownsfolkCount));
                    OnPropertyChanged(nameof(OutsiderCount));
                    OnPropertyChanged(nameof(MinionCount));
                    OnPropertyChanged(nameof(DemonCount));
                    OnPropertyChanged(nameof(TravelerCount));
                };
            }
        }
    }
}