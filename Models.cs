using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MEABasicSaveEditor
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SaveHeaderInfo : ViewModelBase
    {
        private string _characterName = "";
        public string CharacterName
        {
            get => _characterName;
            set { _characterName = value; OnPropertyChanged(); }
        }

        private bool _isFemale = false;
        public bool IsFemale
        {
            get => _isFemale;
            set { _isFemale = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenderIndex)); }
        }

        public int GenderIndex
        {
            get => _isFemale ? 1 : 0;
            set { _isFemale = (value == 1); OnPropertyChanged(); }
        }

        private int _profileId = 6;
        public int ProfileId
        {
            get => _profileId;
            set { _profileId = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProfileName)); }
        }

        public string ProfileName
        {
            get
            {
                string[] names = { "Soldier (Combat)", "Engineer (Tech)", "Adept (Biotics)", "Sentinel (Tech/Biotic)", "Vanguard (Combat/Biotic)", "Infiltrator (Combat/Tech)", "Explorer (All-Rounder)" };
                if (ProfileId >= 0 && ProfileId < names.Length) return names[ProfileId];
                return "Explorer (All-Rounder)";
            }
        }

        private string _identityCode = "";
        public string IdentityCode
        {
            get => _identityCode;
            set { _identityCode = value; OnPropertyChanged(); }
        }

        private double _playTimeSeconds = 0;
        public double PlayTimeSeconds
        {
            get => _playTimeSeconds;
            set { _playTimeSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayTimeFormatted)); }
        }

        public string PlayTimeFormatted
        {
            get
            {
                var ts = TimeSpan.FromSeconds(_playTimeSeconds);
                return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
            }
        }

        private string _location = "";
        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(); }
        }

        private int _difficultyIndex = 2;
        public int DifficultyIndex
        {
            get => _difficultyIndex;
            set { _difficultyIndex = value; OnPropertyChanged(); }
        }

        private string _careerId = "";
        public string CareerId
        {
            get => _careerId;
            set { _careerId = value; OnPropertyChanged(); }
        }

        private int _level = 1;
        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); }
        }

        private int _currentXp = 0;
        public int CurrentXp
        {
            get => _currentXp;
            set { _currentXp = value; OnPropertyChanged(); }
        }

        private int _skillPoints = 0;
        public int SkillPoints
        {
            get => _skillPoints;
            set { _skillPoints = value; OnPropertyChanged(); }
        }

        private int _itemLimit = 50;
        public int ItemLimit
        {
            get => _itemLimit;
            set { _itemLimit = value; OnPropertyChanged(); }
        }

        private int _nexusLevel = 1;
        public int NexusLevel
        {
            get => _nexusLevel;
            set { _nexusLevel = value; OnPropertyChanged(); }
        }

        private int _avpPoints = 0;
        public int AvpPoints
        {
            get => _avpPoints;
            set { _avpPoints = Math.Max(0, value); OnPropertyChanged(); }
        }

        private int _credits = 0;
        public int Credits
        {
            get => _credits;
            set { _credits = value; OnPropertyChanged(); }
        }

        private int _milkyWayRD = 0;
        public int MilkyWayRD
        {
            get => _milkyWayRD;
            set { _milkyWayRD = value; OnPropertyChanged(); }
        }

        private int _heleusRD = 0;
        public int HeleusRD
        {
            get => _heleusRD;
            set { _heleusRD = value; OnPropertyChanged(); }
        }

        private int _remnantRD = 0;
        public int RemnantRD
        {
            get => _remnantRD;
            set { _remnantRD = value; OnPropertyChanged(); }
        }
    }

    public class ProfileRankModel : ViewModelBase
    {
        public int Index { get; set; }
        public string Name { get; set; }
        private int _rank = 1;
        public int Rank
        {
            get => _rank;
            set { _rank = Math.Max(1, Math.Min(6, value)); OnPropertyChanged(); OnPropertyChanged(nameof(RankIndex)); }
        }
        public int RankIndex
        {
            get => Rank - 1;
            set { Rank = value + 1; OnPropertyChanged(); }
        }
    }

    public class ResourceItemModel : ViewModelBase
    {
        public string FriendlyName { get; set; }
        public string Category { get; set; } // "Minerals", "Tech Materials", "Bio Materials", "Salvage", "Augmentations", "Special Items"
        public string PartitionName { get; set; }
        public int Rarity { get; set; }
        public string Description { get; set; } = "";

        public string RarityName
        {
            get
            {
                switch (Rarity)
                {
                    case 3: return "Ultra Rare";
                    case 2: return "Rare";
                    case 1: return "Uncommon";
                    default: return "Common";
                }
            }
        }

        public string RarityColor
        {
            get
            {
                switch (Rarity)
                {
                    case 3: return "#D8B4FE";
                    case 2: return "#FCD34D";
                    case 1: return "#93C5FD";
                    default: return "#94A3B8";
                }
            }
        }

        public string RarityBg
        {
            get
            {
                switch (Rarity)
                {
                    case 3: return "#2E1065";
                    case 2: return "#451A03";
                    case 1: return "#1E3A8A";
                    default: return "#1E293B";
                }
            }
        }

        public string RarityBorder
        {
            get
            {
                switch (Rarity)
                {
                    case 3: return "#9333EA";
                    case 2: return "#D97706";
                    case 1: return "#3B82F6";
                    default: return "#475569";
                }
            }
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = Math.Max(0, value); OnPropertyChanged(); }
        }

        public uint? ExistingRawId { get; set; }
    }

    public class BackupItemModel : ViewModelBase
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string OriginalSaveName { get; set; }
        public DateTime Timestamp { get; set; }
        public string FormattedDate => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        public long FileSizeBytes { get; set; }
        public string FormattedSize => $"{FileSizeBytes / 1024.0:F1} KB";
    }

    public class LiveSaveItemModel
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public DateTime LastModified { get; set; }
        public string DisplayName => $"{FileName}  [{LastModified:yyyy-MM-dd HH:mm:ss}]";

        public override string ToString() => DisplayName;
    }

    public class ArmoryItemModel : ViewModelBase
    {
        public uint Id { get; set; }
        public string PartitionName { get; set; }
        public string DisplayName { get; set; }
        public string Category { get; set; } // "Weapons", "Weapon Mods", "Armor", "Fusion Mods", "Consumables"
        public int Tier { get; set; }
        public int Rarity { get; set; }
        public bool IsNew { get; set; }

        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = Math.Max(1, value); OnPropertyChanged(); }
        }

        public MEA.SaveFormats.Items.ItemData ItemData { get; set; }
        public bool IsNewlyAdded { get; set; }
        public static Action<ArmoryItemModel, int> OnTierChangeRequested;

        public bool HasTier => (Category == "Weapons" || Category == "Armor" || Category == "Weapon Mods");
        public bool NoTier => !HasTier;
        public bool IsConsumable => Category == "Consumables";
        public bool IsNotConsumable => !IsConsumable;

        public string TechOrigin
        {
            get
            {
                string p = (PartitionName ?? "").ToLowerInvariant();
                if (p.Contains("remnant") || p.Contains("sweeper") || p.Contains("inferno") || p.Contains("shadow") || p.Contains("equalizer") || p.Contains("cryogauntlet") || p.Contains("heritage"))
                    return "Remnant";
                if (p.Contains("helar") || p.Contains("angara") || p.Contains("firaan") || p.Contains("rozerad") || p.Contains("ushior") || p.Contains("isharay") || p.Contains("dhan") || p.Contains("hedy") || p.Contains("kett") || p.Contains("zalkin") || p.Contains("soned") || p.Contains("hroshar"))
                    return "Heleus";
                return "Milky Way";
            }
        }

        public string TechOriginColor
        {
            get
            {
                switch (TechOrigin)
                {
                    case "Remnant": return "#C084FC";
                    case "Heleus": return "#38BDF8";
                    default: return "#FBBF24";
                }
            }
        }

        public int TierIndex
        {
            get
            {
                int rank = TierRank;
                return (rank >= 1 && rank <= 10) ? (rank - 1) : 0;
            }
            set
            {
                int targetRank = value + 1;
                if (targetRank >= 1 && targetRank <= 10 && targetRank != TierRank)
                {
                    OnTierChangeRequested?.Invoke(this, targetRank);
                }
            }
        }

        public int RarityIndex
        {
            get
            {
                switch (Rarity)
                {
                    case 3: return 3;
                    case 2: return 2;
                    case 1: return 1;
                    default: return 0;
                }
            }
            set
            {
                int r = Math.Max(0, Math.Min(3, value));
                if (Rarity != r)
                {
                    Rarity = r;
                    if (ItemData != null)
                    {
                        ItemData.Rarity = r;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RarityName));
                    OnPropertyChanged(nameof(RarityColor));
                    OnPropertyChanged(nameof(RarityBg));
                    OnPropertyChanged(nameof(RarityBorder));
                }
            }
        }

        public string FriendlySubtitle
        {
            get
            {
                string p = (PartitionName ?? "").ToLowerInvariant();
                string tech = "Milkyway Technology";
                if (p.Contains("angara") || p.Contains("firaan") || p.Contains("rozerad") || p.Contains("ushior") || p.Contains("isharay") || p.Contains("dhan") || p.Contains("hedy"))
                    tech = "Angaran Technology";
                else if (p.Contains("kett") || p.Contains("zalkin") || p.Contains("soned") || p.Contains("hroshar"))
                    tech = "Kett Technology";
                else if (p.Contains("remnant") || p.Contains("sweeper") || p.Contains("inferno") || p.Contains("shadow") || p.Contains("equalizer") || p.Contains("cryogauntlet"))
                    tech = "Remnant Technology";
                else if (p.Contains("pathfinder") || p.Contains("elite") || p.Contains("deluxe"))
                    tech = "Pathfinder Initiative Elite";
                else if (p.Contains("initiative"))
                    tech = "Andromeda Initiative";
                else if (p.Contains("helar"))
                    tech = "Heleus Technology";
                else if (p.Contains("mws") || p.Contains("launch") || p.Contains("n7") || p.Contains("avenger") || p.Contains("predator") || p.Contains("carnifex") || p.Contains("widow") || p.Contains("mattock"))
                    tech = "Milkyway Technology";

                if (Category == "Weapons")
                {
                    return $"{tech} • {SubCategory}";
                }
                if (Category == "Armor")
                {
                    return $"{tech} • {SubCategory}";
                }
                if (Category == "Weapon Mods")
                {
                    return $"Weapon Augmentation • {SubCategory}";
                }
                if (Category == "Fusion Mods")
                {
                    return "Remnant Chest Fusion Augmentation";
                }
                if (Category == "Consumables")
                {
                    return "Combat Consumable Supply";
                }
                return tech;
            }
        }

        public string TierDisplay
        {
            get
            {
                if (Category == "Consumables" || Category == "Fusion Mods")
                    return "--";

                int rank = 0;
                if (Tier >= 0)
                {
                    rank = Tier + 1;
                }
                else if (!string.IsNullOrEmpty(PartitionName))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(PartitionName, @"_0?(\d+)[a-z]?$");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int parsed))
                    {
                        rank = parsed;
                    }
                }

                if (rank <= 0) return "--";

                string[] roman = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
                if (rank >= 1 && rank < roman.Length)
                {
                    return $"Tier {roman[rank]}";
                }
                return $"Tier {rank}";
            }
        }

        public string CategoryColor
        {
            get
            {
                switch (Category)
                {
                    case "Weapons": return "#FF9100";     // Amber Orange
                    case "Weapon Mods": return "#00E676"; // Emerald Green
                    case "Armor": return "#00D2FF";       // Cyan Blue
                    case "Fusion Mods": return "#C084FC"; // Exotic Violet/Purple
                    case "Consumables": return "#FBBF24"; // Golden Yellow
                    default: return "#9AB2D1";
                }
            }
        }

        public string CategoryTag
        {
            get
            {
                switch (Category)
                {
                    case "Weapons": return "WEAPON";
                    case "Weapon Mods": return "MOD";
                    case "Armor": return "ARMOR";
                    case "Fusion Mods": return "FUSION";
                    case "Consumables": return "SUPPLY";
                    default: return "ITEM";
                }
            }
        }

        public int TierRank
        {
            get
            {
                if (Category == "Consumables" || Category == "Fusion Mods")
                    return 0;

                int rank = 0;
                if (Tier >= 0)
                {
                    rank = Tier + 1;
                }
                else if (!string.IsNullOrEmpty(PartitionName))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(PartitionName, @"_0?(\d+)[a-z]?$");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int parsed))
                    {
                        rank = parsed;
                    }
                }
                return rank;
            }
        }

        public bool CanUpgradeTier => (Category == "Weapons" || Category == "Armor") && TierRank > 0 && TierRank < 10;

        public string RarityName
        {
            get
            {
                switch (Rarity)
                {
                    case 3: return "Ultra Rare";
                    case 2: return "Rare";
                    case 1: return "Uncommon";
                    default: return "Common";
                }
            }
        }

        public string RarityColor
        {
            get
            {
                switch (Rarity)
                {
                    case 3: return "#D8B4FE";
                    case 2: return "#FCD34D";
                    case 1: return "#93C5FD";
                    default: return "#94A3B8";
                }
            }
        }

        public string RarityBg
        {
            get
            {
                switch (Rarity)
                {
                    case 3: return "#2E1065";
                    case 2: return "#451A03";
                    case 1: return "#1E3A8A";
                    default: return "#1E293B";
                }
            }
        }

        public string RarityBorder
        {
            get
            {
                switch (Rarity)
                {
                    case 3: return "#9333EA";
                    case 2: return "#D97706";
                    case 1: return "#3B82F6";
                    default: return "#475569";
                }
            }
        }

        public string SubCategory
        {
            get
            {
                string p = (PartitionName ?? "").ToLowerInvariant();
                if (Category == "Weapons")
                {
                    if (p.Contains("assaultrifle")) return "Assault Rifle";
                    if (p.Contains("pistol") || p.Contains("smg")) return "Pistol / SMG";
                    if (p.Contains("shotgun")) return "Shotgun";
                    if (p.Contains("sniperrifle") || p.Contains("sniper")) return "Sniper Rifle";
                    if (p.Contains("melee")) return "Melee";
                    return "Other Weapon";
                }
                if (Category == "Armor")
                {
                    if (p.Contains("/head/") || p.Contains("helmet")) return "Helmet";
                    if (p.Contains("/chest/")) return "Chest";
                    if (p.Contains("/arms/")) return "Arms";
                    if (p.Contains("/legs/")) return "Legs";
                    return "Full Armor";
                }
                if (Category == "Weapon Mods")
                {
                    if (p.Contains("assaultrifle")) return "AR Mod";
                    if (p.Contains("pistol") || p.Contains("smg")) return "Pistol Mod";
                    if (p.Contains("shotgun")) return "Shotgun Mod";
                    if (p.Contains("sniper")) return "Sniper Mod";
                    return "Mod";
                }
                return Category;
            }
        }
    }

    public class CatalogItemModel
    {
        public string Category { get; set; } // Weapons, Weapon Mods, Armor, Fusion Mods, Consumables
        public string Name { get; set; }
        public string PartitionName { get; set; }
        public int Tier { get; set; }
        public MEA.GameInfo.ItemType ItemType { get; set; }

        public int TierRank
        {
            get
            {
                if (Category == "Consumables" || Category == "Fusion Mods")
                    return 0;

                int rank = (Tier >= 0) ? Tier + 1 : 0;
                if (rank <= 0 && !string.IsNullOrEmpty(PartitionName))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(PartitionName, @"_0?(\d+)[a-z]?$");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int parsed))
                    {
                        rank = parsed;
                    }
                }
                return rank;
            }
        }

        public string TierDisplay
        {
            get
            {
                int rank = TierRank;
                if (rank <= 0) return (Category == "Consumables" || Category == "Fusion Mods") ? "N/A" : "Tier I";

                string[] roman = { "", "Tier I", "Tier II", "Tier III", "Tier IV", "Tier V", "Tier VI", "Tier VII", "Tier VIII", "Tier IX", "Tier X" };
                if (rank >= 1 && rank < roman.Length)
                {
                    return roman[rank];
                }
                return $"Tier {rank}";
            }
        }

        public string TechOrigin
        {
            get
            {
                string p = (PartitionName ?? "").ToLowerInvariant();
                if (p.Contains("remnant") || p.Contains("sweeper") || p.Contains("inferno") || p.Contains("shadow") || p.Contains("equalizer") || p.Contains("cryogauntlet") || p.Contains("heritage"))
                    return "Remnant";
                if (p.Contains("helar") || p.Contains("angara") || p.Contains("firaan") || p.Contains("rozerad") || p.Contains("ushior") || p.Contains("isharay") || p.Contains("dhan") || p.Contains("hedy") || p.Contains("kett") || p.Contains("zalkin") || p.Contains("soned") || p.Contains("hroshar"))
                    return "Heleus";
                return "Milky Way";
            }
        }

        public string DisplayText
        {
            get
            {
                if (Category == "Consumables" || Category == "Fusion Mods")
                    return Name;

                return $"{Name} ({TierDisplay})";
            }
        }

        public override string ToString() => DisplayText;
    }
}
