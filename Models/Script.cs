using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace BloodClockTowerScriptEditor.Models
{
    /// <summary>
    /// 劇本根模型 - 移除 partial，完全手動實作
    /// </summary>
    public class Script : ObservableObject
    {
        // ==================== 私有欄位 ====================
        private ScriptMeta _meta;
        private ObservableCollection<Role> _roles;

        // ==================== 建構函式 ====================
        public Script()
        {
            _meta = new ScriptMeta();
            _roles = [];

            // 監聽集合變化
            _roles.CollectionChanged += Roles_CollectionChanged;
        }

        // ==================== 公開屬性 (不序列化) ====================

        [JsonIgnore]
        public ScriptMeta Meta
        {
            get => _meta;
            set => SetProperty(ref _meta, value);
        }

        [JsonIgnore]
        public ObservableCollection<Role> Roles
        {
            get => _roles;
            set
            {
                if (_roles != null)
                {
                    _roles.CollectionChanged -= Roles_CollectionChanged;
                }

                if (SetProperty(ref _roles, value))
                {
                    if (_roles != null)
                    {
                        _roles.CollectionChanged += Roles_CollectionChanged;
                    }
                    NotifyAllPropertiesChanged();
                }
            }
        }

        // ==================== 按陣營分類 (唯讀) ====================

        [JsonIgnore]
        public IEnumerable<Role> Townsfolk => Roles.Where(r => r.Team == TeamType.Townsfolk);

        [JsonIgnore]
        public IEnumerable<Role> Outsiders => Roles.Where(r => r.Team == TeamType.Outsider);

        [JsonIgnore]
        public IEnumerable<Role> Minions => Roles.Where(r => r.Team == TeamType.Minion);

        [JsonIgnore]
        public IEnumerable<Role> Demons => Roles.Where(r => r.Team == TeamType.Demon);

        [JsonIgnore]
        public IEnumerable<Role> Travelers => Roles.Where(r => r.Team == TeamType.Traveler);

        [JsonIgnore]
        public IEnumerable<Role> Fabled => Roles.Where(r => r.Team == TeamType.Fabled);

        [JsonIgnore]
        public IEnumerable<Role> Loric => Roles.Where(r => r.Team == TeamType.Loric);

        [JsonIgnore]
        public IEnumerable<Role> Jinxes => Roles.Where(r => r.Team == TeamType.Jinxed);

        // ==================== 統計資訊 ====================

        [JsonIgnore]
        public int TotalRoleCount => Roles.Count;

        [JsonIgnore]
        public int TownsfolkCount => Townsfolk.Count();

        [JsonIgnore]
        public int OutsiderCount => Outsiders.Count();

        [JsonIgnore]
        public int MinionCount => Minions.Count();

        [JsonIgnore]
        public int DemonCount => Demons.Count();

        [JsonIgnore]
        public int TravelerCount => Travelers.Count();

        // ==================== 事件處理 ====================

        /// <summary>
        /// 當角色集合變更時，通知所有分類屬性更新
        /// </summary>
        private void Roles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyAllPropertiesChanged();
        }

        /// <summary>
        /// 通知所有相關屬性變更
        /// </summary>
        private void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(Townsfolk));
            OnPropertyChanged(nameof(Outsiders));
            OnPropertyChanged(nameof(Minions));
            OnPropertyChanged(nameof(Demons));
            OnPropertyChanged(nameof(Travelers));
            OnPropertyChanged(nameof(Fabled));
            OnPropertyChanged(nameof(Loric)); 
            OnPropertyChanged(nameof(Jinxes));
            OnPropertyChanged(nameof(TotalRoleCount));
            OnPropertyChanged(nameof(TownsfolkCount));
            OnPropertyChanged(nameof(OutsiderCount));
            OnPropertyChanged(nameof(MinionCount));
            OnPropertyChanged(nameof(DemonCount));
            OnPropertyChanged(nameof(TravelerCount));
        }
    }
}